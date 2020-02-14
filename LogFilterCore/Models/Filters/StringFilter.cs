using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogFilterCore.Models.Filters
{
    public class StringFilter : FilterBase
    {
        /// <summary>
        /// The string to be searched for in the original line entry.
        /// </summary>        
        public string Value { get; set; }
    }
}
