using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LogFilterCore.Models;
using LogFilterCore.Utility;
using LogFilterCore.Utility.Tracing;

namespace LogFilterCore.Parsers
{
    public abstract class ParserBase
    {                

        public virtual string DateFormat { get; } = "yyyy-MM-dd";

        public virtual string TimeFormat { get; } = "HH:mm:ss,fff";

        public virtual string DateTimeFormat => $"{DateFormat} {TimeFormat}";

        public virtual string FileFormat { get; } = "yyyy-MM-dd";

        public Regex Expression { get; protected set; }

        protected virtual Summary Summary { get; set; }

        private Configuration Configuration { get; set; }

        public static int NonStandardLinesThreshold = 100;

        public List<string> NonStandardLines { get; } = new List<string>(NonStandardLinesThreshold);
        
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

        public virtual IEnumerable<string> ToLines(IEnumerable<LogEntry> logEntries)
        {
            foreach (var logEntry in logEntries.OrderBy(x => x.Timestamp))
            {
                var formattedLogEntry = FormatLogEntry(logEntry);
                var stringsPerLine = formattedLogEntry.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                foreach (var s in stringsPerLine)
                {
                    yield return s;
                }
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

        public virtual LogEntry[] FilterLogEntries(LogEntry[] logEntries, Action<int> reportProgress)
        {
            var cfg = Configuration;
            var filters = cfg.Filters;
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

                if (currentEntry.InResultSet)
                {
                    // the currentEntry was appended in
                    // advance due to context filtering
                    continue;
                }

                // NOTE: configuration filters preceed other filtering

                // filter logs by begin date less than the one specified by the filter value
                if (cfg.BeginDateTime.HasValue && (currentEntry.Date < cfg.BeginDateTime.Value || currentEntry.UtcDate < cfg.BeginDateTime.Value))
                {
                    continue;
                }

                // filter logs by end date greater than the specified; this is also an end condition
                if (cfg.EndDateTime.HasValue && (currentEntry.Date > cfg.EndDateTime.Value || currentEntry.UtcDate > cfg.EndDateTime.Value))
                {
                    return filteredEntries.ToArray();
                }

                // if threads are specified with concrete values, filter entries from other threads
                if (cfg.SplitByThreads != null && cfg.SplitByThreads.Length > 0)
                {
                    if (!cfg.SplitByThreads.Contains(currentEntry.Thread))
                    {
                        continue;
                    }                    
                }

                // if users are specified with concrete values, filter entries with other users
                if (cfg.SplitByUsers != null && cfg.SplitByUsers.Length > 0)
                {
                    if (!cfg.SplitByUsers.Contains(currentEntry.Identity) || !cfg.SplitByUsers.Contains(currentEntry.Username))
                    {
                        continue;
                    }                    
                }

                // NOTE: begin filter processing

                foreach (var filter in filters)
                {
                    var targetValue = ResolveFilterPropertyValue(currentEntry, filter);
                    if (!filter.Value.IsMatch(targetValue))
                    {
                        // filter did not match,
                        // proceed to next filter
                        continue;
                    }

                    filter.Count++;
                    if (filter.Type == FilterType.Exclude)
                    {
                        // this entry should not be 
                        // added to the result entry
                        break;
                    }

                    if (filter.Type == FilterType.Include)
                    {
                        // add to the filtered entries result set and continue
                        AddLogEntry(filteredEntries, currentEntry, logEntries, index);
                        break;
                    }

                    if (filter.Type == FilterType.WriteToFile)
                    {
                        // add to the specific filter entries result set and continue
                        AddLogEntry(filter.Entries, currentEntry, logEntries, index);
                        break;
                    }

                    if (filter.Type == FilterType.IncludeAndWriteToFile)
                    {
                        // add to both the specific filter and the filtered result set
                        AddLogEntry(filteredEntries, currentEntry, logEntries, index);
                        AddLogEntry(filter.Entries, currentEntry, logEntries, index);
                        break;
                    }
                }                
            }

            reportProgress?.Invoke(100);
            return filteredEntries.ToArray();
        }

        protected virtual string ResolveFilterPropertyValue(LogEntry currentEntry, Filter filter)
        {
            var targetProperty = typeof(LogEntry).GetProperty(filter.Property);
            if (targetProperty == null)
            {
                throw new ParserException($"Invalid filter property name, no such property in LogEntry '{filter.Property}'.");
            }

            var targetPropertyValue = targetProperty.GetValue(currentEntry);

            string stringTargetPropertyValue;

            while (true)
            {
                // most of the filters are for string properties
                stringTargetPropertyValue = targetPropertyValue as string;
                if (stringTargetPropertyValue != null)
                {
                    break;
                }

                if (targetPropertyValue is DateTime dateTimeTargetPropertyValue)
                {
                    // format dateTimeValues to string dates with the parser dat
                    // TODO: Why use DateTime? in the LogEntry class at all then?
                    stringTargetPropertyValue = dateTimeTargetPropertyValue.ToString(DateTimeFormat);
                    break;
                }

                throw new ParserException($"The value of the property ({filter.Property}: {targetPropertyValue}) cannot be cast to a known parser type.");
            }

            return stringTargetPropertyValue;
        }

        private static void MarkAndAddEntry(ICollection<LogEntry> list, LogEntry currentEntry)
        {
            currentEntry.InResultSet = true;
            list.Add(currentEntry);
        }

        public static void AddLogEntry(List<LogEntry> resultEntries, LogEntry currentEntry, LogEntry[] logEntries, int context, int? index = null)
        {
            if (context == 0)
            {
                MarkAndAddEntry(resultEntries, currentEntry);
                return;
            }

            if (!index.HasValue)
            {
                index = Array.IndexOf(logEntries, currentEntry);
            }

            var current = index.Value - context;
            while (current <= index.Value + context)
            {
                if (current >= 0 && current < logEntries.Length && !logEntries[current].InResultSet)
                {
                    MarkAndAddEntry(resultEntries, logEntries[current]);
                }

                current++;
            }
        }

        public virtual Summary BeginSummary()
        {
            Summary = new Summary(DateTimeFormat);

            // make a copy of the filters and anul current counters
            var filtersCopy = Configuration.Filters.Clone();
            filtersCopy.ForEach(x => { x.Count = 0; });
            Summary.Filters = filtersCopy.ToArray();
            Summary.BeginProcessTimestamp = DateTime.Now;
            return Summary;
        }

        public virtual void EndSummary()
        {
            Summary.EndProcessTimestamp = DateTime.Now;
        }
    }
}
