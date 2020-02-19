using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LogFilterCore.Models;
using LogFilterCore.Parsers;
using LogFilterCore.Utility;
using LogFilterCore.Utility.Tracing;

namespace LogFilterCore
{
    public class ConfigurationRunner
    {
        private Summary RunSummary { get; set; }

        private Configuration Current { get; set; }        

        private readonly Action<string, int?> _reportProgress;

        public ConfigurationRunner(Action<string, int?> reportProgress)
        {
            _reportProgress = reportProgress;
        }

        public void Run(string configurationFilePath)
        {            
            try
            {
                Current = FileProcessor.LoadConfiguration(configurationFilePath);
            }
            catch (Exception ex)
            {
                throw new ConfigurationException($"Could not resolve current configuration from file path: {configurationFilePath}", ex);
            }
            
            Run();
        }

        public void Run(Configuration cfg)
        {
            Current = cfg;
            Run();
        }

        protected void Run()
        {
            var cfg = Current;
            if (!string.IsNullOrWhiteSpace(cfg.InputFolder))
            {
                var outputPath = $"{cfg.InputFolder}\\parsed\\";
                cfg.OutputFolder = outputPath;
            }

            if (!string.IsNullOrWhiteSpace(cfg.InputFile))
            {
                var fileDirectory = FileProcessor.GetFileDirectory(cfg.InputFile);
                var outputPath = $"{fileDirectory}\\parsed\\";

                cfg.OutputFolder = outputPath;
            }

            cfg.Parser = InstantiateParser(cfg.ParserName);
            BeginRunSummary(cfg.Parser.DateTimeFormat);

            //FileProcessor.CurrentFilePrefix = cfg.ReparseFilePrefix;

            var inputFiles = GatherInputFiles();

            if (!inputFiles.Any())
            {
                ReportProgress("No log files found in input folder or none passed pre-filtering.", 100);
                return;
            }

            if (cfg.Filters == null)
            {
                cfg.Filters = new List<Filter>();
            }

            var parser = cfg.Parser;
            var filters = cfg.Filters;
            var runSummary = RunSummary;
            var currentSummary = parser.BeginSummary();

            void ProgressCallback(int percent)
            {
                ReportProgress("Progress: {0}", percent);
            }

            // clone filters for the run summary
            runSummary.Filters = cfg.Filters.Clone().ToArray();

            // NOTE: files are ordered here by LastWriteTime
            // reverse it to preserve the order in the output files            
            foreach (var fileInfo in inputFiles.Reverse())
            {
                var filePath = fileInfo.FullName;
                ReportProgress($"Reading file '{filePath}'...");

                var logLines = FileProcessor.ReadLogLines(filePath, ProgressCallback, out var linesRead, parser.Expression);

                if (!string.IsNullOrEmpty(cfg.Reparse))
                {
                    filePath = FileProcessor.ExtractFileName(filePath, cfg.Reparse);
                }

                runSummary.FilesRead++;
                currentSummary.FilesRead++;
                runSummary.LinesRead += (ulong)linesRead;
                currentSummary.LinesRead = (ulong)linesRead;
                runSummary.LogsRead += (ulong)logLines.Length;
                currentSummary.LogsRead = (ulong)logLines.Length;

                ReportProgress($"Lines: {linesRead}, Logs: {logLines.Length}, Constructing entries...");

                var logEntries = parser.ToLogEntry(logLines).ToArray();
                runSummary.NonStandardEntries += parser.NonStandardLines.Count;
                currentSummary.NonStandardEntries = parser.NonStandardLines.Count;

                if (parser.NonStandardLines.Any())
                {
                    // write failed log entries and continue
                    var outputPath = FileProcessor.GetOutputFilePath(filePath, cfg.InputFolder, cfg.OutputFolder, "FAILED-" + cfg.ParserName);
                    if (FileProcessor.WriteFile(outputPath, parser.NonStandardLines, cfg.OverwriteFiles))
                    {
                        runSummary.FilesWritten++;
                        currentSummary.FilesWritten++;
                    }
                }

                runSummary.EntriesConstructed += (ulong)logEntries.Length;
                currentSummary.EntriesConstructed = (ulong)logEntries.Length;
                ReportProgress($"Logs: {logLines.Length}, Constructed: {logEntries.Length}, Filtering file...");

                var filteredEntries = parser.FilterLogEntries(logEntries, ProgressCallback);

                runSummary.FilteredEntries += (ulong)filteredEntries.Length;
                currentSummary.FilteredEntries = (ulong)filteredEntries.Length;
                ReportProgress($"Entries: {logEntries.Length}, Filtered: {filteredEntries.Length}, Writing files...");
            }
        }

        protected FileInfo[] GatherInputFiles()
        {
            var cfg = Current;
            var parser = cfg.Parser;            

            ReportProgress("Gathering input files...");

            if (!string.IsNullOrEmpty(cfg.InputFile))
            {
                return new[] { new FileInfo(cfg.InputFile) };
            }

            IEnumerable<FileInfo> inputFiles;

            if (!string.IsNullOrEmpty(cfg.Reparse))
            {                
                ReportProgress($"Gathering previously parsed files with the prefix '{cfg.Reparse}'.");

                // if we are reparsing, gather the files with the ReparseFilePrefix only                
                inputFiles = FileProcessor.GetPrefixedLogsFromDirectory(cfg.InputFolder, cfg.Reparse)
                    .Select(x => new FileInfo(x))
                    .OrderByDescending(x => x.LastWriteTime);
            }
            else
            {
                ReportProgress("Gathering all logs files from input directory.");

                // if we're not, gather ALL log files from the directory
                inputFiles = FileProcessor.GetLogsFromDirectory(cfg.InputFolder)
                    .Select(x => new FileInfo(x))
                    .OrderByDescending(x => x.LastWriteTime);
            }

            if (cfg.BeginDateTime.HasValue || cfg.EndDateTime.HasValue)
            {
                ReportProgress($"Prefiltering by file name based on the begin ({cfg.BeginDateTime}) and end ({cfg.EndDateTime}) configuration values.");

                // apply prefiltering of files if there is a value in any of those two filters
                inputFiles = FileProcessor.FilterFilesByDateFilter(inputFiles, parser.FileFormat,
                    cfg.BeginDateTime, cfg.EndDateTime, cfg.Reparse);
            }

            if (cfg.TakeLastFiles.HasValue)
            {
                ReportProgress($"Taking last {cfg.TakeLastFiles.Value} files.");

                // take only so many entries, if this value is specified
                inputFiles = inputFiles.Take(cfg.TakeLastFiles.Value);
            }

            if (!cfg.OverwriteFiles)
            {
                int skippingFiles = 0;
                inputFiles = inputFiles.Where(x =>
                {
                    if (!FileProcessor.HasBeenProcessed(x, cfg.InputFolder, cfg.OutputFolder, cfg.Reparse))
                    {
                        return true;
                    }

                    skippingFiles++;
                    return false;
                });

                ReportProgress($"No overwriting of files, skipping {skippingFiles} files.");
            }

            return inputFiles.ToArray();
        }

        protected ParserBase InstantiateParser(string parserName)
        {
            var type = Type.GetType(parserName, throwOnError: false);

            if (type == null)
            {
                type = Type.GetType($"LogFilterCore.Parsers.{parserName}", throwOnError: true);                
            }

            if (!(Activator.CreateInstance(type) is ParserBase parser))
            {
                throw new ParserException($"Could not create a parser instance with the type of '{type}'.");
            }

            return parser;
        }

        private void BeginRunSummary(string datetimeFormat)
        {
            RunSummary = new Summary(datetimeFormat);
            RunSummary.BeginProcessTimestamp = DateTime.Now;
        }

        private void EndRunSummary()
        {
            var cfg = Current;
            var summary = RunSummary;                        

            summary.EndProcessTimestamp = DateTime.Now;

            // write summary, run summary, set readonly
            var summaryOutputFilePath = FileProcessor.GetRunSummaryFilePath(cfg.OutputFolder);
            FileProcessor.WriteFile(summaryOutputFilePath, summary.ToJson(), cfg.OverwriteFiles);
            FileProcessor.SetReadonly(summaryOutputFilePath);
        }

        protected void ReportProgress(string message, int? progress = null)
        {
            _reportProgress?.Invoke(message, progress);
        }               
    }
}
