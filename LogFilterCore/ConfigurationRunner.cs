﻿using System;
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

        protected virtual void Run()
        {
            var cfg = Current;

            // TODO: perform configuration checks!

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
                else
                {
                    throw new ConfigurationException($"No input, please specify either input file or folder.");
                }
            }

            if (cfg.Filters == null || !cfg.Filters.Any())
            {
                throw new ConfigurationException($"No filters provided, please provide at least one filter.");
            }

            cfg.Parser = InstantiateParser(cfg.ParserName);

            var parser = cfg.Parser;                        
            BeginRunSummary(cfg.Parser.DateTimeFormat);
            var runSummary = RunSummary;

            // pre-filtering is done here
            var inputFiles = GatherInputFiles();

            if (!inputFiles.Any())
            {
                ReportProgress("No log files found in input folder or none passed pre-filtering.", 100);
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
                catch (FileProcessorException fpex)
                {
                    ReportProgress(fpex.Message);
                }
                catch (ParserException pex)
                {
                    ReportProgress(pex.Message);

                    if (parser.NonStandardLines.Any())
                    {
                        // write non-standard entries to tunSummary
                        runSummary.NonStandardEntries += parser.NonStandardLines.Count;

                        var filePath = fileInfo.FullName;
                        var outputPath = FileProcessor.GetOutputFilePath(filePath, cfg.InputFolder, cfg.OutputFolder, $"[{cfg.ParserName}-FAILED]-");
                        if (FileProcessor.WriteFile(outputPath, parser.NonStandardLines, cfg.OverwriteFiles))
                        {
                            runSummary.FilesWritten++;
                        }                                                                
                    }
                }
                catch (Exception ex)
                {
                    ReportProgress(ex.Message);
                }
            }

            EndRunSummary();
        }

        protected virtual void Run(FileInfo fileInfo)
        {
            var cfg = Current;            
            var parser = cfg.Parser;
            var filters = cfg.Filters;
            var runSummary = RunSummary;            

            var filePath = fileInfo.FullName;
            var currentSummary = parser.BeginSummary();
            ReportProgress($"Reading file '{filePath}'...");

            void ProgressCallback(int percent)
            {
                ReportProgress("Progress: {0}", percent);
            }

            var logLines = FileProcessor.ReadLogLines(filePath, ProgressCallback, out var linesRead, parser.Expression);

            if (!string.IsNullOrEmpty(cfg.Reparse))
            {
                // if we're reparsing we need to replace the original file name (thhat's with a prefix)
                // with a one without a prefix, and prefix it accordingly during this parser run
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

            ReportProgress($"Logs: {logLines.Length}, Constructed: {logEntries.Length}, Filtering file...");

            var filteredEntries = parser.FilterLogEntries(logEntries, ProgressCallback);

            runSummary.FilteredEntries += (ulong)filteredEntries.Length;
            currentSummary.FilteredEntries = (ulong)filteredEntries.Length;

            ReportProgress($"Entries: {logEntries.Length}, Filtered: {filteredEntries.Length}, Writing files...");

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

                ReportProgress($"ALL: {filteredEntries.Length}");
            }
            else
            {
                // TODO: At level WARN!
                ReportProgress("No filtered entries resulted after run.");
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
                ReportProgress("Writing original file...");

                // write the original file to output folder
                var originalOutputFilePath = FileProcessor.GetOutputFilePath(filePath, cfg.InputFolder, cfg.OutputFolder, "original");
                if (FileProcessor.WriteFile(originalOutputFilePath, logLines, cfg.OverwriteFiles))
                {
                    runSummary.FilesWritten++;
                    currentSummary.FilesWritten++;
                }
            }

            // end current summary
            parser.EndSummary();

            // write summary, current summary
            var currentSummaryOutputPath = FileProcessor.GetCurrentSummaryFilePath(filePath, cfg.InputFolder, cfg.OutputFolder);
            if (FileProcessor.WriteFile(currentSummaryOutputPath, currentSummary.ToJson(), cfg.OverwriteFiles))
            {
                FileProcessor.SetReadonly(currentSummaryOutputPath);
                runSummary.FilesWritten++;
            }

            AggregateRunSummaryCounters(currentSummary.Filters);

            ReportProgress("Done.");
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

                    ReportProgress($"THREAD#{groupedEntries.Key}: {groupedEntries.Count()}");
                }
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

                    ReportProgress($"IDENTITY#{groupedEntries.Key}: {groupedEntries.Count()}");
                }
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

                    ReportProgress($"LEVEL#{groupedEntries.Key}: {groupedEntries.Count()}");
                }
            }
        }

        protected FileInfo[] GatherInputFiles()
        {
            var cfg = Current;
            var parser = cfg.Parser;            

            ReportProgress("Gathering input files...");

            if (!string.IsNullOrEmpty(cfg.InputFile))
            {
                if (!string.IsNullOrEmpty(cfg.InputFolder))
                {
                    // TODO: at level WARN!
                    ReportProgress("Both input file and input folder are set, disregarding the latter.");
                }

                return new[] { new FileInfo(cfg.InputFile) };
            }

            IEnumerable<FileInfo> inputFiles;

            if (!string.IsNullOrEmpty(cfg.Reparse))
            {                
                ReportProgress($"Gathering previously parsed files with the prefix '{cfg.Reparse}'.");

                // if we are reparsing, gather the files with the Reparse prefix only                
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

            ReportProgress($"Overwriting of files is '{cfg.OverwriteFiles}'.");

            if (!cfg.OverwriteFiles)
            {                
                inputFiles = inputFiles.Where(x => !FileProcessor.HasBeenProcessed(x, cfg.InputFolder, cfg.OutputFolder, cfg.Reparse));                
            }

            var inputFilesArray = inputFiles.ToArray();
            ReportProgress($"Pre-filtering resolved {inputFilesArray.Length} file(s) ready for proccessing.");
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

            // make a copy of the filters and anul current counters
            var filtersCopy = Current.Filters.Clone();
            filtersCopy.ForEach((x) => { x.Count = 0; });
            RunSummary.Filters = filtersCopy.ToArray();            
        }

        private void AggregateRunSummaryCounters(IEnumerable<Filter> filters)
        {
            foreach (var filter in filters)
            {
                // TODO: This relies on filter naming to match the one in the summary, which is not guaranteed
                RunSummary.Filters.Single(x => x.Name == filter.Name).Count += filter.Count;
            }
        }

        private void EndRunSummary()
        {
            var cfg = Current;
            var summary = RunSummary;                        

            summary.EndProcessTimestamp = DateTime.Now;

            // write run summary, set readonly
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
