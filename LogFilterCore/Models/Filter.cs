using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LogFilterCore.Utility;

namespace LogFilterCore.Models
{
    [Serializable]
    public class Filter
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
        public FilterType Type { get; set; } = FilterType.Include; // include entries by default

        /// <summary>
        /// Incremented whenever this occurance's 
        /// filter is matched upon a log entry.
        /// </summary>        
        public ulong Count { get; set; } = 0;

        /// <summary>
        /// Specifies number of log entries surrounding the matched entry by 
        /// this filter that should be included in the result set.
        /// </summary>
        public int Context { get; set; } = 0; // no context by default

        /// <summary>
        /// Specifies if log entries satisfying this filter 
        /// should be separated and written into a separate file.
        /// </summary>        
        //public bool WriteToFile { get; set; } = false; // writing to file is opt-in

        /// <summary>
        /// Specifies if the parsing should continue after this entry is matched. 
        /// Useful when filtering known log entries into another file.
        /// </summary>        
        //public bool ShouldContinue { get; set; }  // TODO: Resolve remove


        /// <summary>
        /// For which property of the log entry should this filter be applied on.
        /// If this property is null the value of the filter will be applied on the original line.
        /// </summary>        
        public string Property { get; set; }

        /// <summary>
        /// The filter value to be matched against the value of the log entry property.
        /// </summary>        
        public Regex Value { get; set; }       
    }
}
