﻿namespace LogFilterCore.Utility
{
    public enum FilterType
    {
        /// <summary>
        /// If the filter matched a line, exclude the entry from the main result set and continue with another entry.
        /// </summary>
        Exclude,

        /// <summary>
        /// If the filter matched a line, write the entry to a different result set and continue with another entry.
        /// </summary>
        WriteToFile,

        /// <summary>
        /// If the filter matched a line, write the entry to a both the specific filter result set and the output filtered result set.
        /// </summary>
        IncludeAndWriteToFile,

        /// <summary>
        /// If the filter matched a line, include the entry in the main result set and continue with another entry.
        /// </summary>
        Include,
    }
}