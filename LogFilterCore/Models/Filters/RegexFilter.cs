﻿using System;
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

        public override bool? Apply(List<LogEntry> filteredEntries, LogEntry currentEntry)
        {
            if (this.Value.IsMatch(currentEntry.OriginalLine))
            {
                this.Count++;
                return this.ShouldContinue;
            }

            return null;
        }
    }
}
