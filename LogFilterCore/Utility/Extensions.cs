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

        private static void MarkAndAddEntry(ICollection<LogEntry> list, LogEntry currentEntry)
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
        }

        /// <summary>
        /// Perform a deep Copy of the object.
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="source">The object instance to copy.</param>
        /// <returns>The copied object.</returns>
        public static T Clone<T>(this T source)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", nameof(source));
            }

            // Don't serialize a null object, simply return the default for that object
            if (ReferenceEquals(source, null))
            {
                return default(T);
            }

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}