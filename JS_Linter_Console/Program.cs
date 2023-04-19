using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JS_Linter_Console
{
    class Program
    {
        private const string LogFileName = "linter_app_log.txt";

        static async Task Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;

                string? choice = GetValidChoice();

                Console.Write($"Please enter the path to the {(choice == "1" ? "single website" : "folder containing multiple websites")}: ");
                string? path = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(path))
                {
                    await LintWebsitesAsync(path, choice == "1");
                }
                else
                {
                    Console.WriteLine("Invalid path provided. Exiting...");
                    Log("Invalid path provided.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred during execution:");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Exiting...");
                Log($"An error occurred during execution: {ex.Message}");
            }
        }

        private static string GetValidChoice()
        {
            string? choice;
            do
            {
                Console.Write("Choose an option:\n1. Lint a single website\n2. Lint multiple websites in a folder\nEnter 1 or 2: ");
                choice = Console.ReadLine();
            } while (choice != "1" && choice != "2");

            return choice;
        }

        private static async Task LintWebsitesAsync(string path, bool isSingleWebsite)
        {
            var directories = isSingleWebsite ? new string[] { path } : Directory.GetDirectories(path);
            Console.WriteLine($"Checking {directories.Length} {(isSingleWebsite ? "website" : "directories")}...");

            int totalErrors = 0;
            int totalWarnings = 0;

            string logsDirectory = "linter_error_logs";
            Directory.CreateDirectory(logsDirectory);

            foreach (var (directory, progress) in directories.Select((dir, idx) => (dir, idx + 1)))
            {
                var wwwDirectory = Path.Combine(directory, "www");
                if (!Directory.Exists(wwwDirectory)) continue;

                Console.WriteLine($"[{progress}/{directories.Length}] Processing: {directory}");
                var websiteName = Path.GetFileName(directory);

                Console.WriteLine("Connecting to website...");
                var linterResults = await LinterAsync(wwwDirectory);

                if (!linterResults.HasErrorsOrWarnings) continue;

                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                string outputFile = Path.Combine(logsDirectory, $"{websiteName}_{timestamp}.txt");
                await File.WriteAllTextAsync(outputFile, linterResults.Output);
                Console.WriteLine($"Linter issues found for {websiteName} and written to {outputFile}");

                totalErrors += linterResults.TotalErrors;
                totalWarnings += linterResults.TotalWarnings;
            }

            Console.WriteLine("\nProcess completed.");
            Console.WriteLine($"Total errors: {totalErrors}");
            Console.WriteLine($"Total warnings: {totalWarnings}");

            string websitesLinted = string.Join(", ", directories.Select(directory => Path.GetFileName(directory)));
            Log($"{(isSingleWebsite ? "Single" : "Multiple")} website linting completed for {websitesLinted}. Total errors: {totalErrors}, Total warnings: {totalWarnings}");
        }

        private static async Task<LinterResult> LinterAsync(string wwwDirectory)
        {
            var lintErrors = "";
            int totalErrors = 0;
            int totalWarnings = 0;
            var jsFiles = GetJavaScriptFiles(wwwDirectory);
            int totalFiles = jsFiles.Count;

            foreach (var (jsFile, progress) in jsFiles.Select((file, idx) => (file, idx + 1)))
            {
                Console.WriteLine($"Processing file {progress}/{totalFiles}: {jsFile}");

                var rawErrors = await LintJavaScriptFileAsync(jsFile);
                Console.WriteLine();
                var errors = ParseLinterOutput(rawErrors);
                if (!errors.HasErrorsOrWarnings) continue;

                totalErrors += errors.TotalErrors;
                totalWarnings += errors.TotalWarnings;

                lintErrors += $"File: {jsFile} - Errors: {errors.TotalErrors}, Warnings: {errors.TotalWarnings}\n";
                lintErrors += errors.Output;
                lintErrors += "\n\n";
            }

            return new LinterResult
            {
                Output = lintErrors,
                TotalErrors = totalErrors,
                TotalWarnings = totalWarnings,
                HasErrorsOrWarnings = totalErrors > 0 || totalWarnings > 0
            };
        }

        private static List<string> GetJavaScriptFiles(string rootPath)
        {
            var allJavaScriptFiles = Directory.GetFiles(rootPath, "*.js", SearchOption.AllDirectories).ToList();
            var cmsIncludesPath = Path.Combine(rootPath, "cms", "includes");

            if (Directory.Exists(cmsIncludesPath))
            {
                var cmsIncludesFiles = Directory.GetFiles(cmsIncludesPath, "*.js", SearchOption.TopDirectoryOnly)
                    .Where(file => Path.GetFileName(file).StartsWith("inline_", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                allJavaScriptFiles.RemoveAll(file => file.StartsWith(cmsIncludesPath, StringComparison.OrdinalIgnoreCase));
                allJavaScriptFiles.AddRange(cmsIncludesFiles);
            }

            return allJavaScriptFiles;
        }

        private static async Task<string> LintJavaScriptFileAsync(string filePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."));
            string configFilePath = Path.Combine(projectRoot, ".eslintrc.json");
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C eslint \"{filePath}\" --fix --no-color --rule no-undef:error --config \"{configFilePath}\"",
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

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogFileName, $"{DateTime.UtcNow}: {message}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while logging: {ex.Message}");
            }
        }

        private static LinterResult ParseLinterOutput(string rawOutput)
        {
            int errors = 0;
            int warnings = 0;
            var lines = rawOutput.Split('\n');
            var output = "";

            foreach (var line in lines)
            {
                if (line.Contains("error") && !line.StartsWith("✖"))
                {
                    errors++;
                    output += line + "\n";
                }
                else if (line.Contains("warning") && !line.StartsWith("✖"))
                {
                    warnings++;
                    output += line + "\n";
                }
                else if (line.StartsWith("✖"))
                {
                    var parts = line.Split('(', ')');
                    if (parts.Length > 1)
                    {
                        var counts = parts[1].Split(',');
                        if (counts.Length == 2)
                        {
                            if (int.TryParse(counts[0].Split(' ')[0], out int parsedErrors))
                            {
                                errors = parsedErrors;
                            }
                            if (int.TryParse(counts[1].Split(' ')[1], out int parsedWarnings))
                            {
                                warnings = parsedWarnings;
                            }
                        }
                    }
                }
            }
            return new LinterResult
            {
                Output = output,
                TotalErrors = errors,
                TotalWarnings = warnings,
                HasErrorsOrWarnings = errors > 0 || warnings > 0
            };
        }
    }

    class LinterResult
    {
        public string Output { get; set; } = string.Empty;
        public int TotalErrors { get; set; }
        public int TotalWarnings { get; set; }
        public bool HasErrorsOrWarnings { get; set; }
    }
}