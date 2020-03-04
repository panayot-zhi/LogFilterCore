using LogFilterCore.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;

namespace LogFilterCore.Models
{
    [Serializable]
    public class Summary
    {
        protected string DateTimeFormat { get; }

        public Summary(string dateTimeFormat)
        {
            this.DateTimeFormat = dateTimeFormat;
        }

        /// <summary>
        /// Total number of lines read in this run.
        /// </summary>
        public ulong LinesRead { get; set; }

        /// <summary>
        /// Total number of log entries read in this run.
        /// </summary>
        public ulong LogsRead { get; set; }

        /// <summary>
        /// Entries that do not conform to this parser expression definitions. Either refine the definition or switch parser if this number exceeds threshold.
        /// </summary>
        public int NonStandardEntries { get; set; }

        /// <summary>
        /// Number of entries constructed in this run. Other entries should be non-standard according to parser.
        /// </summary>
        public ulong EntriesConstructed { get; set; }

        /// <summary>
        /// Entries filtered by current filters (entries only written to files are excluded).
        /// </summary>
        public ulong FilteredEntries { get; set; }

        /// <summary>
        /// Total of lines written in all files in this run.
        /// </summary>
        public ulong LinesWritten { get; set; }

        /// <summary>
        /// Total of files written in this run.
        /// </summary>
        public int FilesWritten { get; set; }

        /// <summary>
        /// Total of files read in this run.
        /// </summary>
        public int FilesRead { get; set; }

        /// <summary>
        /// Marks the creation of the summary.
        /// </summary>
        public DateTime BeginProcessTimestamp { get; set; }

        /// <summary>
        /// Marks the end of the summary. After this property is updated the summary should be written to a file.
        /// </summary>
        public DateTime EndProcessTimestamp { get; set; }

        /// <summary>
        /// At the end of the summary calculates the total running time of this run.
        /// </summary>
        public TimeSpan Elapsed { get; set; }

        /// <summary>
        /// The input log file to be filtered.
        /// Assigning this will set the output folder in the directory where the file resides.
        /// </summary>
        public string InputFile { get; set; }

        /// <summary>
        /// The output folder where the directory structure with the result sets will be created.
        /// </summary>
        public string OutputFolder { get; set; }

        /// <summary>
        /// The input log folder from which to take all the log files.
        /// Assigning this will keep the directory structure when outputting, substituing input files with folders.
        /// </summary>
        public string InputFolder { get; set; }

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
        /// A copy of the filters from configuration. This collection gets updated with the relevant count values.
        /// </summary>
        public Filter[] Filters { get; set; }

        /// <summary>
        /// Name of the parser that will be instantiated to process the input log folder.
        /// </summary>
        public string ParserName { get; set; }



        public void CopyConfiguration(Configuration cfg, string inputFile, string outputFolder)
        {
            // TODO: Use reflection here pls
                        
            this.InputFile = inputFile;
            this.InputFolder = cfg.InputFolder;
            this.OutputFolder = outputFolder;

            this.VerboseMode = cfg.VerboseMode;
            this.TakeLastFiles = cfg.TakeLastFiles;
            this.OverwriteFiles = cfg.OverwriteFiles;
            this.CopyOriginal = cfg.CopyOriginal;
            this.BeginDateTime = cfg.BeginDateTime;
            this.EndDateTime = cfg.EndDateTime;
            this.SplitByThreads = cfg.SplitByThreads;
            this.SplitByIdentities = cfg.SplitByIdentities;
            this.SplitByLogLevels = cfg.SplitByLogLevels;
            this.Reparse = cfg.Reparse;

            this.ParserName = cfg.ParserName;
        }

        public virtual string ToJson()
        {
            var settings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new JsonConverter[]
                {
                    new StringEnumConverter(),
                    new ParserDateTimeConverter(this.DateTimeFormat),
                },
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };

            return JsonConvert.SerializeObject(this, settings);
        }
    }
}