﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogFilterCore.Models.Filters
{
    public class StringFilter : FilterBase
    {
        /// <summary>
        /// The string to be searched for in the original line entry.
        /// </summary>        
        public string Value { get; set; }

        public StringComparison Comparison { get; set; } = StringComparison.InvariantCulture;

        public override bool? Apply(List<LogEntry> filteredEntries, LogEntry currentEntry)
        {
            if (currentEntry.OriginalLine.IndexOf(this.Value, Comparison) >= 0)
            {
                this.Count++;
                return this.ShouldContinue;                
            }

            return null;
        }
    }
}
