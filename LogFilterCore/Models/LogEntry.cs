using System;
using LogFilterCore.Utility;

namespace LogFilterCore.Models
{
    /// <summary>
    /// http://spoiledtechie.com/post/2015/11/19/Conversion-Patterns-for-Log4Net.aspx
    /// </summary>
    public class LogEntry
    {
        public string OriginalLine { get; }

        public bool? InResultSet { get; set; } = false;

        public LogEntry(string line)
        {
            OriginalLine = line;
        }

        /// <summary>
        /// Used to output the friendly name of the AppDomain where the logging event was generated (appdomain, a).
        /// </summary>
        public string AppDomain { get; set; }

        /// <summary>
        /// Used to output the date of the logging event in the local time zone (date, d).
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        /// Used to output the exception passed in with the log message (main: exception).
        /// </summary>
        public string Exception { get; set; }

        /// <summary>
        /// Used to output the file name where the logging request was issued (file, F).
        /// </summary>
        public string File { get; set; }

        /// <summary>        
        /// Used to output the user name for the currently active user (Principal.Identity.Name) (identity, u).
        /// </summary>
        public string Identity { get; set; }

        /// <summary>        
        /// Used to output location information of the caller which generated the logging event (location, l).
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Used to output the level of the logging event (level, p).        
        /// </summary>
        public LogLevel? Level { get; set; }

        /// <summary>
        /// Used to output the line number from where the logging request was issued (line, L).
        /// </summary>
        public int? Line { get; set; }

        /// <summary>
        /// Used to output the logger of the logging event. The logger conversion specifier can be optionally 
        /// followed by precision specifier, that is a decimal constant in brackets (logger, c).
        /// </summary>
        public string Logger { get; set; }

        /// <summary>        
        /// Used to output the application supplied message associated with the logging event (message, m).
        /// </summary>
        public string Message { get; set; }

        /// <summary>        
        /// Used to output the method name where the logging request was issued (method, M).
        /// </summary>
        public string Method { get; set; }

        /// <summary>        
        /// Outputs the platform dependent line separator character or characters (newline, n).
        /// </summary>
        public string NewLine = Environment.NewLine;

        /// <summary>        
        /// Used to output the NDC (nested diagnostic context) associated with the thread that generated the logging event (ndc, x).
        /// </summary>
        public string Ndc { get; set; }

        /// <summary>
        /// Used to output the an event specific property (property, properties, P, mdc, X).
        /// </summary>
        public string Property { get; set; }

        /// <summary>        
        /// Used to output the stack trace of the logging event The stack trace level specifier may be enclosed between braces (stacktrace).        
        /// </summary>
        public string Stacktrace { get; set; }

        /// <summary>        
        /// Used to output the stack trace of the logging event The stack trace level specifier may be enclosed between braces (stacktracedetail).
        /// </summary>
        public string StacktraceDetail { get; set; }

        /// <summary>
        /// Used to output the number of milliseconds elapsed since the start of the application until the creation of the logging event (timestamp, r).
        /// </summary>
        public ulong? Timestamp { get; set; }

        /// <summary>        
        /// Used to output the name of the thread that generated the logging event. Uses the thread number if no name is available (thread, t).
        /// </summary>
        public string Thread { get; set; }

        /// <summary>        
        /// Used to output the fully qualified type name of the caller issuing the logging request. 
        /// This conversion specifier can be optionally followed by precision specifier, that is a decimal constant in brackets (type, class, C).
        /// </summary>
        public string Type { get; set; }

        /// <summary>        
        /// Used to output the WindowsIdentity for the currently active user (username, w).
        /// </summary>
        public string Username { get; set; }

        /// <summary>        
        /// Used to output the date of the logging event in universal time. The date conversion 
        /// specifier may be followed by a date format specifier enclosed between braces (utcdate, ).
        /// </summary>
        public DateTime? UtcDate { get; set; }
    }
}
