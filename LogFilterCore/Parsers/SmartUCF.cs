﻿using LogFilterCore.Models;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LogFilterCore.Parsers
{
    public class SmartUCF : ParserBase
    {
        public readonly string LogLevelPattern = @"(?<level>DEBUG|INFO|WARN|ERROR|FATAL)";

        public readonly string DateTimePattern = @"(?<dateTime>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2},\d{3})";
        public readonly string ThreadPattern = @"(?<thread>\d+)";
        public readonly string IdentityPattern = @"(?<identity>\w+)";
        public readonly string LoggerPattern = @"(?<logger>\w+)";
        public readonly string MethodPattern = @"(?<method>(\w+))";
        public readonly string Method2Pattern = @"<(?<method>\w+)>(\w|_|\d)+";
        public readonly string MessagePattern = @"(?<message>.*)";

        public SmartUCF(Configuration cfg) : base(cfg)
        {
            Expression = new Regex(
                $"{LogLevelPattern}\\s+{DateTimePattern}\\s" +
                $"{ThreadPattern}\\s({IdentityPattern}\\s|\\s)" +
                $"{LoggerPattern}\\s" +
                $"({MethodPattern}|{Method2Pattern})\\s" +
                $"{MessagePattern}",
                RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
                TimeSpan.FromSeconds(5));
        }

        protected override LogEntry ConstructLogEntry(Match result, string line)
        {
            return new LogEntry(line)
            {
                Level = result.Groups["level"].Value,
                Date = DateTime.ParseExact(result.Groups["dateTime"].Value, DateTimeFormat,
                    CultureInfo.CurrentCulture.DateTimeFormat, DateTimeStyles.None),
                Thread = result.Groups["thread"].Value,

                Identity = result.Groups["identity"].Value,

                Logger = result.Groups["logger"].Value,
                Method = result.Groups["method"].Value,
                Message = result.Groups["message"].Value
            };
        }

        protected override string FormatLogEntry(LogEntry logEntry)
        {
            return $"{logEntry.Level.PadRight(5)} " +
                   $"{logEntry.Date.Value.ToString(DateTimeFormat)} " +
                   $"{logEntry.Thread} {logEntry.Identity} {logEntry.Logger} {logEntry.Method} {logEntry.Message}";
        }
    }
}