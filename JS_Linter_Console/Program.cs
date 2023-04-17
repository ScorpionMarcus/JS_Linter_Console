using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JS_Linter_Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var websitesPath = @"D:\inetpub";
            await LintAllWebsitesAsync(websitesPath);
        }

        private static async Task LintAllWebsitesAsync(string websitesPath)
        {
            var directories = Directory.GetDirectories(websitesPath);
            foreach (var directory in directories)
            {
                var wwwDirectory = Path.Combine(directory, "www");
                if (Directory.Exists(wwwDirectory))
                {
                    var websiteName = Path.GetFileName(directory);
                    var linterErrors = await LintWebsiteAsync(wwwDirectory);

                    if (!string.IsNullOrEmpty(linterErrors))
                    {
                        string outputFile = $"linter_errors_{websiteName}.txt";
                        await File.WriteAllTextAsync(outputFile, linterErrors);
                        Console.WriteLine($"Linter errors found for {websiteName} and written to {outputFile}");
                    }
                    else
                    {
                        Console.WriteLine($"No Linter errors found for {websiteName}.");
                    }
                }
            }
        }

        private static async Task<string> LintWebsiteAsync(string wwwDirectory)
        {
            var lintErrors = "";
            var jsFiles = Directory.GetFiles(wwwDirectory, "*.js", SearchOption.AllDirectories);

            foreach (var jsFile in jsFiles)
            {
                var errors = await LintJavaScriptFileAsync(jsFile);
                if (!string.IsNullOrEmpty(errors))
                {
                    lintErrors += $"File: {jsFile}\n";
                    lintErrors += errors;
                    lintErrors += "\n\n";
                }
            }

            return lintErrors;
        }

        private static async Task<string> LintJavaScriptFileAsync(string filePath)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = $"eslint \"{filePath}\" --no-color --rule no-undef:error",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string errorOutput = await process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            if (!string.IsNullOrEmpty(errorOutput))
            {
                Console.WriteLine($"Error while processing file: {filePath}");
                Console.WriteLine(errorOutput);
            }

            return output;
        }
    }
}
