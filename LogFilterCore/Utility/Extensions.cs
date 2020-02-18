using LogFilterCore.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

namespace LogFilterCore.Utility
{
    public static class Extensions
    {
        public static bool HasPlaceholder(this string s)
        {
            return Regex.IsMatch(s, "{\\d+}");
        }

        /*private static void MarkAndAddEntry(ICollection<LogEntry> list, LogEntry currentEntry)
        {
            currentEntry.InResultSet = true;
            list.Add(currentEntry);
        }

        public static void AddLogEntry(this List<LogEntry> me, LogEntry currentEntry, LogEntry[] sourceList, int context, int? index = null)
        {
            if (context == 0)
            {
                MarkAndAddEntry(me, currentEntry);
                return;
            }

            if (!index.HasValue)
            {
                index = Array.IndexOf(sourceList, currentEntry);
            }

            var current = index.Value - context;
            while (current <= index.Value + context)
            {
                if (current >= 0 && current < sourceList.Length && !sourceList[current].InResultSet)
                {
                    MarkAndAddEntry(me, sourceList[current]);
                }

                current++;
            }
        }*/
    }
}