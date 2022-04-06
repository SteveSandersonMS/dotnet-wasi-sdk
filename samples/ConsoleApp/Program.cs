using System.Text.Json;

namespace WasiConsoleApp
{
    public class Program
    {
        public static int Main()
        {
            var file = File.Open("ConsoleApp.deps.json", FileMode.Open);
            var elem = JsonSerializer.Deserialize<JsonElement>(file);
            WalkJson(elem, depth: 0);
            return 0;
        }

        private static void WalkJson(JsonElement root, int depth)
        {
            switch (root.ValueKind)
            {
                case JsonValueKind.Object:
                    Console.WriteLine();
                    foreach (var prop in root.EnumerateObject())
                    {
                        Console.Write(new String(' ', depth * 4));
                        Console.Write($"{prop.Name}: ");
                        WalkJson(prop.Value, depth + 1);
                    }
                    break;
                case JsonValueKind.Array:
                    Console.WriteLine();
                    var index = 0;
                    foreach (var arrayEntry in root.EnumerateArray())
                    {
                        Console.Write(new String(' ', depth * 4));
                        Console.Write($"[{index}]: ");
                        WalkJson(arrayEntry, depth + 1);
                        index++;
                    }
                    break;
                case JsonValueKind.Undefined:
                    Console.WriteLine("undefined");
                    break;
                case JsonValueKind.Null:
                    Console.WriteLine("null");
                    break;
                default:
                    Console.WriteLine(root.ToString());
                    break;
            }
        }
    }
}
