using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LogFilterCore.Utility;
using Newtonsoft.Json;

namespace LogFilterCore.Models.Filters
{
    public abstract class FilterBase
    {
        /// <summary>
        /// Specifies the human-friendly name of this filter. 
        /// It may be included in the output file name if WriteToFile flag is true.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Short explanatory description about what this filter does.
        /// It can be used only for counting or for file separation etc.        
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Specifies whether or not the filtered line should be
        /// included or excluded in the result set after the filter is applied.        
        /// </summary>
        public FilterType Type { get; set; }

        /// <summary>
        /// Incremented whenever this occurance's 
        /// filter is matched upon a log entry.
        /// </summary>        
        public ulong Count { get; set; }

        /// <summary>
        /// Specifies if the parsing should continue after this entry is matched. 
        /// Useful when filtering known log entries into another file.
        /// </summary>        
        public bool ShouldContinue { get; set; }

        /// <summary>
        /// Specifies if log entries satisfying this filter 
        /// should be separated and written into a separate file.
        /// </summary>        
        public bool WriteToFile { get; set; }

        /// <summary>
        /// The output file name of the filtered entries 
        /// if this filter should be written into another file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Specifies number of log entries surrounding the matched entry by 
        /// this filter that should be included in the result set.
        /// </summary>
        public int Context { get; set; }

        /// <summary>
        /// Accumulative variable holding all log entries that satisfy the filter if WriteToFile is true and should be written into another file.
        /// </summary>        
        [JsonIgnore]
        public List<LogEntry> Entries { get; set; } = new List<LogEntry>();

        public abstract bool? Apply(List<LogEntry> filteredEntries, LogEntry currentEntry);
    }
}
