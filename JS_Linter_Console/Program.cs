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
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.Write("Please enter the path to the websites folder: ");
            string? websitesPath = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(websitesPath))
            {
                await LintAllWebsitesAsync(websitesPath);
            }
            else
            {
                Console.WriteLine("Invalid path provided. Exiting...");
            }
        }

        private static async Task LintAllWebsitesAsync(string websitesPath)
        {
            var directories = Directory.GetDirectories(websitesPath);
            Console.WriteLine($"Checking {directories.Length} directories...");

            int totalErrors = 0;
            int totalWarnings = 0;
            int progress = 0;

            string logsDirectory = "linter_error_logs";
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }

            foreach (var directory in directories)
            {
                var wwwDirectory = Path.Combine(directory, "www");
                if (Directory.Exists(wwwDirectory))
                {
                    progress++;
                    Console.WriteLine($"[{progress}/{directories.Length}] Processing: {directory}");

                    var websiteName = Path.GetFileName(directory);
                    var linterResults = await LintWebsiteAsync(wwwDirectory);

                    if (linterResults.HasErrorsOrWarnings)
                    {
                        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                        string outputFile = Path.Combine(logsDirectory, $"linter_errors_{websiteName}_{timestamp}.txt");
                        await File.WriteAllTextAsync(outputFile, linterResults.Output);
                        Console.WriteLine($"Linter issues found for {websiteName} and written to {outputFile}");

                        totalErrors += linterResults.TotalErrors;
                        totalWarnings += linterResults.TotalWarnings;
                    }
                    else
                    {
                        Console.WriteLine($"No Linter issues found for {websiteName}.");
                    }
                }
            }

            Console.WriteLine("\nProcess completed.");
            Console.WriteLine($"Total errors: {totalErrors}");
            Console.WriteLine($"Total warnings: {totalWarnings}");
        }

        private static async Task<LinterResult> LintWebsiteAsync(string wwwDirectory)
        {
            var lintErrors = "";
            int totalErrors = 0;
            int totalWarnings = 0;
            var jsFiles = GetJavaScriptFiles(wwwDirectory);

            int totalFiles = jsFiles.Count;
            int progress = 0;

            foreach (var jsFile in jsFiles)
            {
                progress++;
                Console.WriteLine($"Processing file {progress}/{totalFiles}: {jsFile}");

                var rawErrors = await LintJavaScriptFileAsync(jsFile);
                var errors = ParseLinterOutput(rawErrors);
                if (errors.HasErrorsOrWarnings)
                {
                    totalErrors += errors.TotalErrors;
                    totalWarnings += errors.TotalWarnings;

                    lintErrors += $"File: {jsFile} - Errors: {errors.TotalErrors}, Warnings: {errors.TotalWarnings}\n";
                    lintErrors += errors.Output;
                    lintErrors += "\n\n";
                }
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
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C eslint \"{filePath}\" --no-color --rule no-undef:error",
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
