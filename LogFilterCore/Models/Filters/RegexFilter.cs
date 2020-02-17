using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogFilterCore.Models.Filters
{
    public class RegexFilter : FilterBase
    {
        /// <summary>
        /// The regular expression to be applied to the original line entry.
        /// </summary>        
        public Regex Value { get; set; }

        public override bool? Filter(LogEntry[] filteredEntries, LogEntry currentEntry)
        {            
            return this.Value.IsMatch(currentEntry.OriginalLine);
        }
    }
}
