using System.Text;

namespace DriverDrowsinessDetection;

internal static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help"))
        {
            Console.WriteLine("Usage: dotnet run -- <input.vbt> [output.cs]");
            Console.WriteLine("       dotnet run -- --stdin [output.cs]");
            return 1;
        }

        var inputPath = args[0];
        var outputPath = args.Length > 1 ? args[1] : null;

        string vbtSource;
        if (inputPath == "--stdin")
        {
            using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
            vbtSource = reader.ReadToEnd();
        }
        else
        {
            if (!File.Exists(inputPath))
            {
                Console.Error.WriteLine($"Input file not found: {inputPath}");
                return 1;
            }

            vbtSource = File.ReadAllText(inputPath, Encoding.UTF8);
        }

        var converter = new VbtToCSharpConverter();
        var result = converter.Convert(vbtSource);

        if (outputPath is null)
        {
            Console.WriteLine(result);
        }
        else
        {
            File.WriteAllText(outputPath, result, Encoding.UTF8);
            Console.WriteLine($"Converted output written to {outputPath}");
        }

        return 0;
    }
}
