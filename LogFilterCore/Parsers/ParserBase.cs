using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LogFilterCore.Models;
using LogFilterCore.Utility;
using LogFilterCore.Utility.Tracing;

namespace LogFilterCore.Parsers
{
    public abstract class ParserBase
    {        
        public virtual string DateFormat { get; } = "yyyy-MM-dd";

        public virtual string TimeFormat { get; } = "HH:mm:ss,fff";

        public virtual string DateTimeFormat => $"{this.DateFormat} {this.TimeFormat}";

        public Regex Expression { get; protected set; }

        protected virtual Summary Summary { get; set; }

        private Configuration Configuration { get; set; }

        public const int NonStandardLinesThreshold = 100;
        public List<string> NonStandardLines = new List<string>(NonStandardLinesThreshold);

        protected ParserBase(Configuration cfg)
        {
            Configuration = cfg;
        }

        public virtual IEnumerable<LogEntry> ToLogEntry(IEnumerable<string> logLines)
        {
            NonStandardLines.Clear();
            foreach (var line in logLines)
            {
                var entry = MatchLogEntry(line);
                if (entry != null)
                {
                    yield return entry;
                }
            }
        }

        public virtual IEnumerable<string> ToString(IEnumerable<LogEntry> logEntries)
        {
            foreach (var logEntry in logEntries.OrderBy(x => x.Timestamp))
            {
                yield return FormatLogEntry(logEntry);
            }
        }

        protected virtual LogEntry MatchLogEntry(string line)
        {
            var result = Expression.Match(line);
            if (!result.Success)
            {
                MarkNonStandard(line, "not-matched");
                return null;
            }

            try
            {
                return ConstructLogEntry(result, line);
            }
            catch (Exception ex)
            {
                MarkNonStandard(line, ex.Message);
                return null;
            }
        }

        protected virtual void MarkNonStandard(string line, string error = null)
        {
            if (NonStandardLines.Count >= NonStandardLinesThreshold)
            {
                throw new ParserException($"Too many non-standard lines! (threshold {NonStandardLinesThreshold})");
            }

            if (error != null)
            {
                line += $" ({error})";
            }

            NonStandardLines.Add(line);
        }

        protected abstract LogEntry ConstructLogEntry(Match result, string line);

        protected abstract string FormatLogEntry(LogEntry logEntry);

        public virtual LogEntry[] Parse(LogEntry[] logEntries, Action<int> reportProgress)
        {
            //var cfg = Configuration;
            //var filters = cfg.Filters;
            var logEntriesCount = logEntries.Length;
            var filteredEntries = new List<LogEntry>();
            var logEntriesProcessed = 0;

            for (var index = 0; index < logEntriesCount; index++)
            {
                var currentEntry = logEntries[index];

                logEntriesProcessed++;
                if (logEntriesProcessed % 10 == 0)
                {
                    var progress = logEntriesProcessed * 100 / logEntriesCount;
                    reportProgress?.Invoke(progress);
                }

                var filterResult = Filter(currentEntry);
                if (filterResult.HasValue)
                {
                    if (!filterResult.Value)
                    {
                        //filteredEntries.AddLogEntry(currentEntry);
                        filteredEntries.AddLogEntry(currentEntry, logEntries, this.Context, index);
                    }

                    continue;
                }

                //filteredEntries.AddLogEntry(currentEntry, logEntries, filters.Context, index);
            }

            reportProgress?.Invoke(100);
            return filteredEntries.ToArray();
        }

        protected virtual bool? Filter(LogEntry currentEntry)
        {
            if (currentEntry.InResultSet)
            {
                // the currentEntry was appended in
                // advance due to context filtering
                return false;
            }

            throw new NotImplementedException();

            // custom filters are processed with priority
            // but are not a condition to filter the LogEntry into the result set
            /*if (filters.CustomFilters != null)
            {
                var shouldContinue = ApplyCustomFilters(filters.CustomFilters, currentEntry);

                // continue parsing
                // with another logEntry
                if (shouldContinue)
                {
                    continue;
                }
            }*/

            // child filters are applied second
            // true - should skip entry
            // false - should add entry and proceed
            // null - filter irrelevant to entry
//            var filterResult = Filter(currentEntry);
//            if (filterResult.HasValue)
//            {
//                if (!filterResult.Value)
//                {
//                    //filteredEntries.AddLogEntry(currentEntry);
//                    filteredEntries.AddLogEntry(currentEntry, logEntries, filters.Context, index);
//                }
//
//                continue;
//            }

            // if there is a regex filter and it matches the original line - include line and continue
//            if (filters.RegexFilter != null && filters.RegexFilter.IsMatch(currentEntry.OriginalLine))
//            {
//                //filteredEntries.AddLogEntry(currentEntry);
//                filteredEntries.AddLogEntry(currentEntry, logEntries, filters.Context, index);
//                continue;
//            }

            // if there is any include filter that is contained within the message - include and continue
//            if (filters.MessageFilters.Include.Any(x => currentEntry.Message.Contains(x)))
//            {
//                //filteredEntries.AddLogEntry(currentEntry);
//                filteredEntries.AddLogEntry(currentEntry, logEntries, filters.Context, index);
//                continue;
//            }

            // if there are logLevel's specified, and if the current entry's log is in the filter - skip it
//            if (filters.LogLevelFilters.Any() && filters.LogLevelFilters.Contains(currentEntry.LogLevel))
//            {
//                continue;
//            }
//
//            // filter logs by begin date less than the one specified by the filter value
//            if (filters.BeginDateTimeFilter.HasValue && currentEntry.Timestamp < filters.BeginDateTimeFilter.Value)
//            {
//                continue;
//            }
//
//            // filter logs by end date greater than the specified; this is also an end condition
//            if (filters.EndDateTimeFilter.HasValue && currentEntry.Timestamp > filters.EndDateTimeFilter.Value)
//            {
//                return filteredEntries.ToArray();
//            }
//
//            // if thread is specified with a concrete value greater than 0, filter other threads
//            if (filters.SplitByThread > 0 && filters.SplitByThread != currentEntry.ThreadId)
//            {
//                continue;
//            }
//
//            // if user filter is not null (no filter by user) or empty (filter by all users), filter entries with other users
//            if (!string.IsNullOrEmpty(filters.SplitByUser) && filters.SplitByUser != currentEntry.User)
//            {
//                continue;
//            }
//
//            // if classNameFilters specified, and if the className of the entry is within the values - filter entry
//            if (filters.ClassNameFilters.Any() && filters.ClassNameFilters.Contains(currentEntry.OriginClassName))
//            {
//                continue;
//            }
//
//            // if methodNameFilters specified, and if the methodName of the entry is within the values - filter entry
//            if (filters.MethodNameFilters.Any() && filters.MethodNameFilters.Contains(currentEntry.OriginMethodName))
//            {
//                continue;
//            }
//
//            // if there is any exclude filter that is contained within the message - filter it
//            if (filters.MessageFilters.Exclude.Any(x => currentEntry.Message.Contains(x)))
//            {
//                continue;
//            }
        }

        /*public virtual Summary BeginSummary()
        {
            Summary = new Summary(this.DateTimeFormat);
            this.Summary.Filters = this.CurrentFilters;
            this.Summary.Filters.C();
            this.Summary.BeginProcessTimestamp = DateTime.Now;
            return this.Summary;
        }

        public virtual void EndSummary()
        {
            this.Summary.EndProcessTimestamp = DateTime.Now;
        }*/
    }
}
