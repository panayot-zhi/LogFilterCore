using LogFilterCore.Parsers;
using LogFilterCore.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LogFilterCore.Models
{
    [Serializable]
    public class Configuration
    {
        /// <summary>
        /// The input log file to be filtered.
        /// Assigning this will set the output folder in the directory where the file resides.
        /// </summary>
        public string InputFile { get; set; }

        /// <summary>
        /// The input log folder from which to take all the log files.
        /// Assigning this will keep the directory structure when outputting, substituing input files with folders.
        /// </summary>
        public string InputFolder { get; set; }

        /// <summary>
        /// The output folder where the directory structure with the result sets will be created.
        /// </summary>
        public string OutputFolder { get; set; }

        /// <summary>
        /// Specifies the level of output the program provides during processing.
        /// </summary>
        public VerboseMode VerboseMode { get; set; }

        /// <summary>
        /// Instructs the parser to take the last n number of files sorted by LastWriteTime.
        /// </summary>
        public ushort? TakeLastFiles { get; set; }

        /// <summary>
        /// Flag indicating whether or not files resulting in the same name in the output directory should be overwritten.
        /// </summary>
        public bool OverwriteFiles { get; set; }

        /// <summary>
        /// Flag indicating whether or not the original filtered file should be copied to output folder with a special prefix.
        /// </summary>
        public bool CopyOriginal { get; set; }

        /// <summary>
        /// Take as input previously parsed files. If it is null -
        /// no reparsing is done, all log files from the input directory are taken;
        /// if it has value - only files with that specific prefix are taken.
        /// </summary>
        public string Reparse { get; set; }

        /// <summary>
        /// Begin date for files pre-filtering.
        /// </summary>
        public DateTime? BeginDateTime { get; set; }

        /// <summary>
        /// End date for files pre-filtering.
        /// </summary>
        public DateTime? EndDateTime { get; set; }

        /// <summary>
        /// Split log entries by thread(s). If it is null - no thread splitting is performed; if it is empty,
        /// all filtered entries are separated by their respective threads;
        /// if it has a specific value(s) - results in a file with logs from this specific thread(s) only.
        /// </summary>
        public string[] SplitByThreads { get; set; }

        /// <summary>
        /// Split log entries by user(s). If it is null - no user splitting is performed; if it is empty,
        /// all filtered entries are separated by their respective users;
        /// if it has a specific value(s) - results in a file with logs from these user(s) only.
        /// </summary>
        public string[] SplitByIdentities { get; set; }

        /// <summary>
        /// Split log entries by level(s). If it is null - no log level splitting is performed; if it is empty,
        /// all filtered entries are separated by their respective log levels;
        /// if it has a specific value(s) - results in a file(s) with logs from this specific log level(s).
        /// </summary>
        public string[] SplitByLogLevels { get; set; }

        /// <summary>
        /// List of filters to be applied on each of the log entries.
        /// </summary>
        public List<Filter> Filters { get; set; }

        /// <summary>
        /// Name of the parser that will be instantiated to process the input log folder.
        /// </summary>
        public string ParserName { get; set; }

        /// <summary>
        /// Parser instance to apply the filters for each of the log entries.
        /// </summary>
        [JsonIgnore]
        public ParserBase Parser { get; set; }
    }
}