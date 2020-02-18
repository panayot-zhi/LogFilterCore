using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogFilterCore.Utility
{
    public enum FilterType
    {
        /// <summary>
        /// If the filter matched a line, include the entry in the main result set.
        /// </summary>
        Include,

        /// <summary>
        /// If the filter matched a line, exclude the entry from the main result set.
        /// </summary>
        Exclude,

        /// <summary>
        /// If the filter matched a line, write the entry to a different result set.
        /// </summary>
        WriteToFile
    }
}
