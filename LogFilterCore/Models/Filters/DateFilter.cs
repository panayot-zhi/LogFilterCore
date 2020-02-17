using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogFilterCore.Utility;

namespace LogFilterCore.Models.Filters
{
    public class DateFilter : FilterBase
    {
        /// <summary>
        /// The date to be compared (greater than or equal) to the log entry's date.
        /// </summary>        
        public DateTime Value { get; set; }

        public override bool? Apply(List<LogEntry> filteredEntries, LogEntry currentEntry)
        {
            if (!currentEntry.Date.HasValue)
            {
                // filter cannot decide whether or not the entry should pass filtering,
                // pass to a different filter along the pipeline
                return null;
            }

            if (currentEntry.Date >= this.Value)
            {
                if (this.Type == FilterType.Include)
                {
                    this.Count++;
                    return this.ShouldContinue;
                }                
            }

            return false;
        }
    }
}
