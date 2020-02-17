using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LogFilterCore.Utility;
using Newtonsoft.Json;

namespace LogFilterCore.Models
{
    public class Configuration
    {
        /// <summary>
        /// The input log file to be filtered.
        /// Assigning this will set the output folder in the directory where the file resides.
        /// </summary>
        public string InputFile { get; set; }
        
        /// <summary>
        /// The input log folder from which to take all the log files.
        /// Assigning this will keep the directory structure when outputting substituing input files with folders.
        /// </summary>
        public string InputFolder { get; set; }

        /// <summary>
        /// The output folder where the directory structure with the result sets will be created.
        /// </summary>
        public string OutputFolder { get; set; }

        /// <summary>
        /// Instructs the parser to take the last n number of file sorted by LastWriteTime.
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
        /// Prefix to mark the original file copy in the output folder.
        /// </summary>
        public string OriginalFilePrefix { get; set; }

        /// <summary>
        /// Flag indicating if the input folder is the product of a previous parser run.
        /// </summary>
        public bool Reparse { get; set; }

        /// <summary>
        /// Prefix to use previously outputed files from the parser as input.
        /// </summary>
        public string ReparseFilePrefix { get; set; }

        /// <summary>
        /// prefix to mark the filtered 
        /// </summary>
        public string FilteredFilePrefix { get; set; }


        /// <summary>
        /// Name of the parser that will be instantiated to process the input log folder.
        /// </summary>
        public string ParserName { get; set; }

        // TODO: Revise this section

        /// <summary>
        /// Split log entries by thread. If it is null - no thread splitting is performed; if it is empty, 
        /// all filtered entries are separated by files for separate threads, 
        /// if it has a specific value - results in a file with logs from this specific thread only.        
        /// </summary>
        //public string SplitByThread { get; set; }

        /// <summary>
        /// Split log entries by user. If it is null - no user splitting is performed; if it is empty, 
        /// all filtered entries are separated by files for separate users, 
        /// if it has a specific value - results in a file with logs from this specific user only.        
        /// </summary>
        //public string SplitByUser { get; set; }

        /// <summary>
        /// Split log entries by level(s). If it is null - no log level splitting is performed; if it is empty, 
        /// all filtered entries are separated by files for separate log levels, 
        /// if it has a specific value(s) - results in a file(s) with logs from this specific log level(s).        
        /// </summary>
        //public LogLevel[] SplitByLogLevels { get; set; }
    }
}
