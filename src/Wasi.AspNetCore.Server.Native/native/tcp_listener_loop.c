#include <stdio.h>
#include <string.h>
#include <unistd.h>
#include <malloc.h>
#include <stdlib.h>
#include <errno.h>
#include <assert.h>
#include <sys/socket.h>
#include <sys/stat.h>
#include <mono-wasi/driver.h>
#include "dotnet_method.h"

/* 
WARNING: Both 'read' and 'write' appear to be buggy in Wasmtime when operating on preopened sockets on Windows.
If the client has disconnected ungracefully, then:
  - On Linux, read returns 0 (not sure about write - didn't check)
  - On Windows, read/write doesn't return at all and instead fatally kills the whole wasm runtime, logging a message similar to:
      Error: failed to run main module `yourmodule.wasm`
      Caused by ... An existing connection was forcibly closed by the remote host. (os error 10054)
To repro, just create an endpoint with Thread.Sleep(5000), use curl to call it, and while it's sleeping use ctrl+c to kill the client.
Or to repro without .NET, use C to make a simple "read"/"write" loop with usleep() calls in the loop, and ctrl+c kill a curl request.
My guess is that Wasmtime needs to know about WSAECONNRESET/WSAENETRESET/WSAECONNABORTED and that it should surface these as error
codes to the wasm code instead of aborting. For prototype purposes, I can ignore this since clients normally do close gracefully.
But it would be a vulnerability if done for real.
*/

DEFINE_DOTNET_METHOD(notify_opened_connection, "Wasi.AspNetCore.Server.Native.dll", "Wasi.AspNetCore.Server.Native", "Interop", "NotifyOpenedConnection");
DEFINE_DOTNET_METHOD(notify_closed_connection, "Wasi.AspNetCore.Server.Native.dll", "Wasi.AspNetCore.Server.Native", "Interop", "NotifyClosedConnection");
DEFINE_DOTNET_METHOD(notify_data_received, "Wasi.AspNetCore.Server.Native.dll", "Wasi.AspNetCore.Server.Native", "Interop", "NotifyDataReceived");

typedef struct ConnectionFd
{
    int fd;
    struct ConnectionFd* next;
} ConnectionFd;

// Hold a linked list of preopened connection file descriptors
ConnectionFd* first_preopen;
// Hold a linked list of active connections for the busy-polling
ConnectionFd* first_connection;

void init_preopen_fds() {
    // It's a bit odd, but WASI preopened listeners have file handles sequentially starting from 3. If the host preopened more than
    // one, you could sock_accept with fd=3, then fd=4, etc., until you run out of preopens.
    int base_fd = getenv("DEBUGGER_FD") ? 4 : 3;

    for(int i = base_fd; i < 1024; i++) {
        struct stat fdStat;
        if(fstat(i, &fdStat) != -1 
        && S_ISSOCK(fdStat.st_mode) != 0) {
            ConnectionFd* next_preopen = (ConnectionFd *)malloc(sizeof(ConnectionFd));
            next_preopen->fd = i;
            next_preopen->next = first_preopen;
            first_preopen = next_preopen;
        }
    }
}

void accept_any_new_connection(int preopen_fd, int interop_gchandle) {

    // libc's accept4 is mapped to WASI's sock_accept with some additional parameter/return mapping at https://github.com/WebAssembly/wasi-libc/blob/63e4489d01ad0262d995c6d9a5f1a1bab719c917/libc-bottom-half/sources/accept.c#L10
    struct sockaddr addr_out_ignored;
    socklen_t addr_len_out_ignored;
    int new_connection_fd = accept4(preopen_fd, &addr_out_ignored, &addr_len_out_ignored, SOCK_NONBLOCK);
    if (new_connection_fd > 0) {
        ConnectionFd *new_connection = (ConnectionFd *)malloc(sizeof(ConnectionFd));
        new_connection->fd = new_connection_fd;
        new_connection->next = first_connection;
        first_connection = new_connection;

        notify_opened_connection(mono_gchandle_get_target(interop_gchandle), (void*[]){ &new_connection_fd });
    } else if (errno != __WASI_ERRNO_AGAIN) { // __WASI_ERRNO_AGAIN means "would block so try again later"
        printf("Fatal: TCP accept failed with errno %i. This may mean the host isn't listening for connections. Be sure to pass the --tcplisten parameter.\n", errno);
        exit(1);
    }
}

void poll_connections(int interop_gchandle, void* read_buffer, int read_buffer_len) {
    ConnectionFd* prev_connection = NULL;
    ConnectionFd* connection = first_connection;
    while (connection) {
        ConnectionFd* next_connection = connection->next;

        int bytes_read = read(connection->fd, read_buffer, read_buffer_len);
        int has_received_data = bytes_read > 0;

        if (has_received_data || (bytes_read < 0 && errno == EWOULDBLOCK)) {
            if (has_received_data) {
                notify_data_received(mono_gchandle_get_target(interop_gchandle), (void* []) { &connection->fd, read_buffer, &bytes_read });
            }

            prev_connection = connection;
        }
        else {
            // In this branch, we're definitely closing the connection. First, figure out whether this is an error or not.
            if (bytes_read == 0) {
                // Client initiating graceful close
                //printf("Connection %i closed by client\n", connection->fd);
            } else {
                // Unexpected error
                printf("Connection %i failed with error %i; closing connection.\n", connection->fd, errno);
            }

            if (prev_connection) {
                prev_connection->next = next_connection;
            } else {
                first_connection = next_connection;
            }

            close(connection->fd);
            notify_closed_connection(mono_gchandle_get_target(interop_gchandle), (void* []) { &connection->fd });
            free(connection);
        }

        connection = next_connection;
    }
}

void dispose_preopen_fds() {
    ConnectionFd* preopen;
    while ((preopen = first_preopen)) {
        first_preopen = preopen->next;
        free(preopen);
    }
}

void close_all_connections() {
    ConnectionFd* connection;
    while ((connection = first_connection)) {
        first_connection = connection->next;
        close(connection->fd);
        free(connection);
    }
}

void run_polling_listener(int interop_gchandle, int* cancellation_flag) {
    int read_buffer_len = 1024*1024;
    void* read_buffer = malloc(read_buffer_len);
    init_preopen_fds();
    // TODO: Stop doing busy-polling. This is the only cross-platform supported option at the moment, but
    // on Linux there's a notification mechanism (https://github.com/bytecodealliance/wasmtime/issues/3730).
    // Once Wasmtime (etc) implement cross-platform support for notification, this code should use it.
    while (*cancellation_flag == 0) {
        usleep(10000);
        ConnectionFd* curr = first_preopen;
        while(curr != NULL) {
            accept_any_new_connection(curr->fd, interop_gchandle);
            curr = curr->next;
        }
        poll_connections(interop_gchandle, read_buffer, read_buffer_len);
    }

    close_all_connections();
    dispose_preopen_fds();
    free(read_buffer);
}

void run_tcp_listener_loop(MonoObject* interop) {
    int interop_gchandle = mono_gchandle_new(interop, /* pinned */ 1);

    // TODO: Find some way to let .NET set this cancellation flag
    int cancel = 0;
    run_polling_listener(interop_gchandle, &cancel);

    mono_gchandle_free(interop_gchandle);
}

void send_response_data(int fd, char* buf, int buf_len) {
    while (1) {
        int res = write(fd, buf, buf_len);

        if (res == -1) {
            if (errno == EAGAIN || errno == EWOULDBLOCK) {
                // Clearly this is not smart, as a single bad client could block us indefinitely.
                // We should instead just return back to .NET code telling it the write
                // was incomplete, then it should do an async yield before trying to resend
                // the rest
                continue;
            } else {
                // TODO: Proper error reporting back into .NET
                printf ("Error sending response data. errno: %i\n", errno);
                break;
            }
        } else if (res < buf_len) {
            // It's a partial write, so keep going
            buf += res;
            buf_len -= res;
        } else {
            break;
        }
    }
}

void tcp_listener_attach_internal_calls() {
    mono_add_internal_call("Wasi.AspNetCore.Server.Native.Interop::RunTcpListenerLoop", run_tcp_listener_loop);
    mono_add_internal_call("Wasi.AspNetCore.Server.Native.Interop::SendResponseData", send_response_data);
}
