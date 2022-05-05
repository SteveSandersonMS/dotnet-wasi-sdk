namespace WasiConsoleApp
{
    public class Program
    {
        public static int Main()
        {
            int a = 10;
            a += 10;
            Console.WriteLine($"a value = {a}");
            Console.WriteLine($"a value = {a += 10}");
            Console.WriteLine($"a value = {a += 10}");
            Console.WriteLine($"a value = {a += 10}");
            Console.WriteLine($"Hello from .NET at {DateTime.Now.ToLongTimeString()}");
            return 0;
        }
    }
}
