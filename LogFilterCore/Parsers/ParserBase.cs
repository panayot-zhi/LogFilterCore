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

        public virtual string DateTimeFormat => $"{this.DateFormat} {this.TimeFormat}";

        public virtual string FileFormat { get; } = "yyyy-MM-dd";

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

                foreach (var filter in filters)
                {
                    /*var match = filter.Apply(currentEntry);
                    if (!match.HasValue)
                    {
                        // filter application indecisive, continue with another filter
                        continue;
                    }
*/
                    // TODO: Process filter application
                }

            }

            reportProgress?.Invoke(100);
            return filteredEntries.ToArray();
        }

        public virtual Summary BeginSummary()
        {
            Summary = new Summary(this.DateTimeFormat);
            this.Configuration.Filters.ForEach(filter =>
            {
                filter.Count = 0;
            });

            this.Summary.Filters = this.Configuration.Filters.Clone().ToArray();
            this.Summary.BeginProcessTimestamp = DateTime.Now;
            return this.Summary;
        }

        public virtual void EndSummary()
        {
            this.Summary.EndProcessTimestamp = DateTime.Now;
        }
    }
}
