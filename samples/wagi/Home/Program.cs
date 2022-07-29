using System.Runtime.InteropServices;

Console.WriteLine($"Content-Type: text/html");
Console.WriteLine();
Console.WriteLine($"<head><title>Hello from C#</title></head>");
Console.WriteLine($"<body>");
Console.WriteLine($"<h1>Hello from C#</h1>");
Console.WriteLine($"<p>Current time (UTC): {DateTime.UtcNow.ToLongTimeString()}</p>");
Console.WriteLine($"<p>Current architecture: {RuntimeInformation.OSArchitecture}</p>");
Console.WriteLine($"<p>Today's French semiotician, brought to you by C#, is Jacques Derrida</p>");
Console.WriteLine($"<p><img src=\"csharp-static/derrida.png\"></p>");
Console.WriteLine($"<p>Today's pet cat, brought to you by F#, is Slats</p>");
Console.WriteLine($"<p><img src=\"fsharp-static/smol-slats.jpg\"></p>");
Console.WriteLine($"</body>");
