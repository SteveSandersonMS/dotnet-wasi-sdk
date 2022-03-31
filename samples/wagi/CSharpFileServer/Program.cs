var path = Environment.GetEnvironmentVariable("PATH_INFO");
if (path == null) {
    Console.WriteLine("Status: 500");
    return;
}

try
{
    var buffer = File.ReadAllBytes(path);
    if (buffer == null) {
        Console.WriteLine("Status: 404");
        return;
    }

    Console.WriteLine($"Content-Type: {MimeType(path)}");
    Console.WriteLine($"Content-Length: {buffer.Length}");
    Console.WriteLine();

    var stream = Console.OpenStandardOutput();
    stream.Write(buffer, 0, buffer.Length);
    stream.Flush();
}
catch (Exception e)
{
    Console.WriteLine("Status: 500");
    Console.WriteLine();
    Console.WriteLine(e.ToString());
}

string MimeType(string path)
{
    try
    {
        return MimeTypes.MimeTypeMap.GetMimeType(path);
    }
    catch
    {
        return "application/octet-stream";
    }
}
