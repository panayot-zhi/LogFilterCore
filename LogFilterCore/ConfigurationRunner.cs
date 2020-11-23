using LogFilterCore.Models;
using LogFilterCore.Parsers;
using LogFilterCore.Utility;
using LogFilterCore.Utility.Tracing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogFilterCore
{
    public class ConfigurationRunner
    {

        // TODO: Add an overload to the run method, that accepts only string[] (or IEnumerable),
        // read the file beforehand and pass the same reference to this method (do not re-read the file)

        private Summary RunSummary { get; set; }

        private Configuration Current { get; }

        public ConfigurationRunner(string configurationFilePath)
        {
            try
            {
                Current = FileProcessor.LoadConfiguration(configurationFilePath);
            }
            catch (Exception ex)
            {
                throw new ConfigurationException($"Could not resolve current configuration from file path: {configurationFilePath}", ex);
            }

            Check();
        }

        private void Check()
        {
            var cfg = Current;

            if (string.IsNullOrWhiteSpace(cfg.InputFolder) && string.IsNullOrEmpty(cfg.InputFile))
            {
                throw new ConfigurationException("No input, please specify either input file or folder.");
            }

            if (string.IsNullOrEmpty(cfg.OutputFolder))
            {
                if (!string.IsNullOrWhiteSpace(cfg.InputFolder))
                {
                    var outputPath = $"{cfg.InputFolder}\\parsed\\";
                    cfg.OutputFolder = outputPath;
                }
                else if (!string.IsNullOrWhiteSpace(cfg.InputFile))
                {
                    var fileDirectory = FileProcessor.GetFileDirectory(cfg.InputFile);
                    var outputPath = $"{fileDirectory}\\parsed\\";

                    cfg.OutputFolder = outputPath;
                }
            }

            if (cfg.Filters == null || !cfg.Filters.Any())
            {
                throw new ConfigurationException("No filters provided, please provide at least one filter.");
            }

            Current.Parser = InstantiateParser(cfg.ParserName);
        }

        public virtual void Run(string inputFile = null)
        {
            var cfg = Current;
            if (inputFile != null)
            {
                cfg.InputFile = inputFile;
            }

            var parser = cfg.Parser;
            BeginRunSummary(cfg.Parser.DateTimeFormat);
            var runSummary = RunSummary;

            // pre-filtering is done here
            var inputFiles = GatherInputFiles();

            if (!inputFiles.Any())
            {
                InvokeReportProgress("No log files found in input folder or none passed pre-filtering.", 100);
                return;
            }

            // NOTE: files are ordered here by LastWriteTime
            // reverse it to preserve the order in the output files
            foreach (var fileInfo in inputFiles.Reverse())
            {
                try
                {
                    Run(fileInfo);
                }
                catch (ParserException)
                {
                    if (parser.NonStandardLines.Any())
                    {
                        // write non-standard entries to runSummary
                        runSummary.NonStandardEntries += parser.NonStandardLines.Count;

                        var filePath = fileInfo.FullName;
                        var outputPath = FileProcessor.GetOutputFilePath(filePath, cfg.InputFolder, cfg.OutputFolder, $"[{cfg.ParserName}-FAILED]-");
                        if (FileProcessor.WriteFile(outputPath, parser.NonStandardLines, cfg.OverwriteFiles))
                        {
                            runSummary.FilesWritten++;
                        }
                    }

                    throw;
                }
            }

            if (inputFiles.Length > 1)
            {
                // do not write run summary if
                // only one file has been processed
                EndRunSummary();
            }
        }

        protected virtual void Run(FileInfo logFileInput)
        {
            var cfg = Current;
            var parser = cfg.Parser;
            var filters = cfg.Filters;
            var runSummary = RunSummary;

            var filePath = logFileInput.FullName;
            var currentSummary = parser.BeginSummary();

            InvokeReportProgress($"Reading file '{filePath}'...");

            void ProgressCallback(int percent)
            {
                InvokeReportProgress("Processing...", percent);
            }

            var logLines = FileProcessor.ReadLogLines(filePath, ProgressCallback, out var linesRead, parser.Expression);
            InvokeReportProgress("Done!        ", 100);
            InvokeReportProgress(Environment.NewLine);

            if (!string.IsNullOrEmpty(cfg.FilePrefix))
            {
                // if we're reparsing we need to replace the original file name (that's with a prefix)
                // with a one without a prefix, and prefix it accordingly during this parser run
                filePath = FileProcessor.ExtractFileName(filePath, cfg.FilePrefix);
            }

            var currentOutputPath = FileProcessor.GetOutputFilePath(filePath, cfg.InputFolder, cfg.OutputFolder, cfg.FilePrefix);
            var currentDirectoryOutputPath = FileProcessor.GetFileDirectory(currentOutputPath);
            currentSummary.CopyConfiguration(cfg, filePath, currentDirectoryOutputPath);

            runSummary.FilesRead++;
            currentSummary.FilesRead++;
            runSummary.LinesRead += (ulong)linesRead;
            currentSummary.LinesRead = (ulong)linesRead;
            runSummary.LogsRead += (ulong)logLines.Length;
            currentSummary.LogsRead = (ulong)logLines.Length;

            InvokeReportProgress($"Lines: {linesRead}, Logs: {logLines.Length}, Constructing entries...");

            var logEntries = parser.ToLogEntry(logLines).ToArray();
            runSummary.NonStandardEntries += parser.NonStandardLines.Count;
            currentSummary.NonStandardEntries = parser.NonStandardLines.Count;

            if (parser.NonStandardLines.Any())
            {
                // write failed log entries and continue with the parsing run (if it didn't throw an exception, threshold has not been exceeded)
                var outputPath = FileProcessor.GetOutputFilePath(filePath, cfg.InputFolder, cfg.OutputFolder, $"[{cfg.ParserName}-FAILED]-");
                if (FileProcessor.WriteFile(outputPath, parser.NonStandardLines, cfg.OverwriteFiles))
                {
                    runSummary.FilesWritten++;
                    currentSummary.FilesWritten++;
                }
            }

            runSummary.EntriesConstructed += (ulong)logEntries.Length;
            currentSummary.EntriesConstructed = (ulong)logEntries.Length;

            InvokeReportProgress($"Logs: {logLines.Length}, Constructed: {logEntries.Length}, Filtering file...");

            var filteredEntries = parser.FilterLogEntries(logEntries, ProgressCallback);
            InvokeReportProgress("Done!        ", 100);
            InvokeReportProgress(Environment.NewLine);

            runSummary.FilteredEntries += (ulong)filteredEntries.Length;
            currentSummary.FilteredEntries = (ulong)filteredEntries.Length;

            InvokeReportProgress($"Entries: {logEntries.Length}, Filtered: {filteredEntries.Length}, Writing files...");

            if (filteredEntries.Any())
            {
                Split(filePath, filteredEntries, currentSummary);

                // write filtered file
                var filteredLines = parser.ToLines(filteredEntries).ToArray();

                var filteredEntriesOutputPath = FileProcessor.GetOutputFilePath(filePath, cfg.InputFolder, cfg.OutputFolder, "filtered");
                if (FileProcessor.WriteFile(filteredEntriesOutputPath, filteredLines, cfg.OverwriteFiles))
                {
                    runSummary.FilesWritten++;
                    currentSummary.FilesWritten++;

                    runSummary.LinesWritten += (ulong)filteredLines.Length;
                    currentSummary.LinesWritten = (ulong)filteredLines.Length;
                }

                InvokeReportProgress($"FILTERED: {filteredEntries.Length}");
            }
            else
            {
                // TODO: At level WARN!
                InvokeReportProgress("No filtered entries resulted after run.");
            }

            // write entries accumulated in the filters
            var dedicatedFilterLogs = filters.Where(f => f.Type == FilterType.WriteToFile || f.Type == FilterType.IncludeAndWriteToFile);
            foreach (var dedicatedFilter in dedicatedFilterLogs)
            {
                // do not write empty files
                if (!dedicatedFilter.Entries.Any())
                {
                    continue;
                }

                // write one file per each custom filter that requires output
                var dedicatedFilterOutputPath = FileProcessor.GetOutputFilePath(filePath, cfg.InputFolder, cfg.OutputFolder, dedicatedFilter.Name);
                //dedicatedFilter.FileName = dedicatedFilterOutputPath; // TODO: Figure out what's the idea here?
                if (FileProcessor.WriteFile(dedicatedFilterOutputPath, parser.ToLines(dedicatedFilter.Entries), cfg.OverwriteFiles))
                {
                    runSummary.FilesWritten++;
                    currentSummary.FilesWritten++;
                }
            }

            if (cfg.CopyOriginal)
            {
                InvokeReportProgress("Writing original file...");

                // write the original file to output folder
                var originalOutputFilePath = FileProcessor.GetOutputFilePath(filePath, cfg.InputFolder, cfg.OutputFolder, "original");
                if (FileProcessor.WriteOriginalFile(sourcePath: filePath, destinationPath: originalOutputFilePath, lines: logLines, overwrite: cfg.OverwriteFiles))
                {
                    runSummary.FilesWritten++;
                    currentSummary.FilesWritten++;
                }
            }

            // end current summary
            parser.EndSummary();

            // write current summary
            var currentSummaryOutputPath = FileProcessor.GetSummaryFilePath(filePath, cfg.InputFolder, cfg.OutputFolder);
            if (FileProcessor.WriteFile(currentSummaryOutputPath, currentSummary.ToJson(), cfg.OverwriteFiles))
            {
                FileProcessor.SetReadonly(currentSummaryOutputPath);
                runSummary.FilesWritten++;
            }

            AggregateRunSummaryCounters(currentSummary);

            // NOTE: A double empty console line intended
            InvokeReportProgress("Done!" + Environment.NewLine);
        }

        protected void Split(string filePath, LogEntry[] filteredEntries, Summary currentSummary)
        {
            var cfg = Current;
            var parser = cfg.Parser;
            var runSummary = RunSummary;

            if (cfg.SplitByThreads != null)
            {
                // whether or not we should write file for each thread
                var splitByAllThreads = cfg.SplitByThreads.Length == 0;

                var groupsByKey = filteredEntries.GroupBy(entry => entry.Thread);
                foreach (var groupedEntries in groupsByKey)
                {
                    // if we're not splitting by all threads, other threads should be skipped
                    if (!splitByAllThreads && !cfg.SplitByThreads.Contains(groupedEntries.Key))
                    {
                        continue;
                    }

                    var currentThreadOutputPath = FileProcessor.GetOutputFilePath(filePath, cfg.InputFolder, cfg.OutputFolder, $"[THREAD#{groupedEntries.Key}]-");
                    if (FileProcessor.WriteFile(currentThreadOutputPath, parser.ToLines(groupedEntries), cfg.OverwriteFiles))
                    {
                        runSummary.FilesWritten++;
                        currentSummary.FilesWritten++;
                    }

                    InvokeReportProgress($"\rTHREAD#{groupedEntries.Key}: {groupedEntries.Count()}", -1);
                }

                InvokeReportProgress(string.Empty);
            }

            if (cfg.SplitByIdentities != null)
            {
                // whether or not we should write file for each identity
                var splitByAllIdentities = cfg.SplitByIdentities.Length == 0;

                var groupsByKey = filteredEntries.GroupBy(entry => entry.Identity);
                foreach (var groupedEntries in groupsByKey)
                {
                    // if we're not splitting by all users, other users should be skipped
                    if (!splitByAllIdentities && !cfg.SplitByIdentities.Contains(groupedEntries.Key))
                    {
                        continue;
                    }

                    var currentIdentityOutputPath = FileProcessor.GetOutputFilePath(filePath, cfg.InputFolder, cfg.OutputFolder, $"[IDENTITY#{groupedEntries.Key}]-");
                    if (FileProcessor.WriteFile(currentIdentityOutputPath, parser.ToLines(groupedEntries), cfg.OverwriteFiles))
                    {
                        runSummary.FilesWritten++;
                        currentSummary.FilesWritten++;
                    }

                    InvokeReportProgress($"\rIDENTITY#{groupedEntries.Key}: {groupedEntries.Count()}", -1);
                }

                InvokeReportProgress(string.Empty);
            }

            if (cfg.SplitByLogLevels != null)
            {
                // whether or not we should write file for each log level
                var splitByAllLogLevels = cfg.SplitByLogLevels.Length == 0;

                var groupsByKey = filteredEntries.GroupBy(entry => entry.Level);
                foreach (var groupedEntries in groupsByKey)
                {
                    // if we're not splitting by all log levels, other levels should be skipped
                    if (!splitByAllLogLevels && !cfg.SplitByLogLevels.Contains(groupedEntries.Key))
                    {
                        continue;
                    }

                    var currentLogLevelOutputPath = FileProcessor.GetOutputFilePath(filePath, cfg.InputFolder, cfg.OutputFolder, $"[LEVEL#{groupedEntries.Key}]-");
                    if (FileProcessor.WriteFile(currentLogLevelOutputPath, parser.ToLines(groupedEntries), cfg.OverwriteFiles))
                    {
                        runSummary.FilesWritten++;
                        currentSummary.FilesWritten++;
                    }

                    InvokeReportProgress($"\rLEVEL#{groupedEntries.Key}: {groupedEntries.Count()}", -1);
                }

                InvokeReportProgress(string.Empty);
            }
        }

        protected FileInfo[] GatherInputFiles()
        {
            var cfg = Current;
            var parser = cfg.Parser;

            InvokeReportProgress("Gathering input files...");

            if (!string.IsNullOrEmpty(cfg.InputFile))
            {
                var inputFileInfo = new FileInfo(cfg.InputFile);
                if (!string.IsNullOrEmpty(cfg.InputFolder))
                {
                    // TODO: at level WARN!
                    InvokeReportProgress("Both input file and input folder are set, disregarding the latter.");
                }
                else
                {
                    cfg.InputFolder = inputFileInfo.Directory.FullName;
                }

                if (!cfg.OverwriteFiles && FileProcessor.HasBeenProcessed(inputFileInfo, cfg.InputFolder, cfg.OutputFolder))
                {
                    return Array.Empty<FileInfo>();
                }

                return new[] { inputFileInfo };
            }

            IEnumerable<FileInfo> inputFiles;

            if (!string.IsNullOrEmpty(cfg.FilePrefix))
            {
                InvokeReportProgress($"Gathering previously parsed files with the prefix '{cfg.FilePrefix}'.");

                // if we are reparsing, gather the files with the Reparse prefix only
                inputFiles = FileProcessor.GetPrefixedLogsFromDirectory(cfg.InputFolder, cfg.FilePrefix)
                    .Select(x => new FileInfo(x))
                    .OrderByDescending(x => x.LastWriteTime);
            }
            else
            {
                InvokeReportProgress("Gathering all log files from input directory.");

                // if we're not, gather ALL log files from the directory
                inputFiles = FileProcessor.GetLogsFromDirectory(cfg.InputFolder)
                    .Select(x => new FileInfo(x))
                    .OrderByDescending(x => x.LastWriteTime);
            }

            if (cfg.BeginDateTime.HasValue || cfg.EndDateTime.HasValue)
            {
                InvokeReportProgress($"Pre-filtering by file name based on the begin ({cfg.BeginDateTime}) and end ({cfg.EndDateTime}) configuration values.");
                
                // apply pre-filtering of files if there is a value in any of those two filters
                inputFiles = FileProcessor.FilterFilesByDateFilter(inputFiles, parser.DateFileNameFormat,
                    cfg.BeginDateTime, cfg.EndDateTime, cfg.FilePrefix);
            }

            if (cfg.TakeLastFiles.HasValue)
            {
                InvokeReportProgress($"Taking last {cfg.TakeLastFiles.Value} files.");

                // take only so many entries, if this value is specified
                inputFiles = inputFiles.Take(cfg.TakeLastFiles.Value);
            }

            InvokeReportProgress($"Overwriting of files is '{cfg.OverwriteFiles}'.");

            if (!cfg.OverwriteFiles)
            {
                inputFiles = inputFiles.Where(x => !FileProcessor.HasBeenProcessed(x, cfg.InputFolder, cfg.OutputFolder, cfg.FilePrefix));
            }

            var inputFilesArray = inputFiles.ToArray();
            InvokeReportProgress($"Pre-filtering resolved {inputFilesArray.Length} file(s) ready for processing.");
            return inputFilesArray;
        }

        protected ParserBase InstantiateParser(string parserName)
        {
            var type = Type.GetType(parserName, throwOnError: false);

            if (type == null)
            {
                type = Type.GetType($"LogFilterCore.Parsers.{parserName}", throwOnError: true);
            }

            object[] parameters = { Current };
            if (!(Activator.CreateInstance(type, parameters) is ParserBase parser))
            {
                throw new ConfigurationException($"Could not create a parser instance with the type of '{type}'.");
            }

            return parser;
        }

        private void BeginRunSummary(string datetimeFormat)
        {
            RunSummary = new Summary(datetimeFormat);
            RunSummary.BeginProcessTimestamp = DateTime.Now;
            RunSummary.CopyConfiguration(Current, Current.InputFile, Current.OutputFolder);

            // annul any counters and entries
            var filters = Current.Filters;
            filters.ForEach((x) =>
            {
                x.Count = 0;
                x.Entries =
                    x.Type == FilterType.WriteToFile ||
                    x.Type == FilterType.IncludeAndWriteToFile
                        ? new List<LogEntry>()
                        : null;
            });

            // make a copy of the filters
            var filtersCopy = filters.Clone();
            RunSummary.Filters = filtersCopy.ToArray();
        }

        private void AggregateRunSummaryCounters(Summary currentSummary)
        {
            foreach (var summaryFilter in currentSummary.Filters)
            {
                // TODO: Ensure that there won't be two filters with the same name
                RunSummary.Filters.Single(x => x.Name == summaryFilter.Name).Count += summaryFilter.Count;
            }
        }

        private void EndRunSummary()
        {
            var cfg = Current;
            var summary = RunSummary;

            summary.EndProcessTimestamp = DateTime.Now;
            summary.Elapsed = summary.EndProcessTimestamp - summary.BeginProcessTimestamp;

            // write run summary, set readonly
            var summaryOutputFilePath = FileProcessor.GetRunSummaryFilePath(cfg.OutputFolder);
            FileProcessor.WriteFile(summaryOutputFilePath, summary.ToJson(), cfg.OverwriteFiles);
            FileProcessor.SetReadonly(summaryOutputFilePath);
        }

        public Action<string, int?> ReportProgress;

        protected void InvokeReportProgress(string message, int? progress = null)
        {
            ReportProgress?.Invoke(message, progress);
        }
    }
}