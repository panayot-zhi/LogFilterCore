using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogFilterCore.Models.Filters
{
    public class DateFilter : FilterBase
    {
        /// <summary>
        /// The date to be compared to the log entry's date.
        /// </summary>        
        public DateTime Value { get; set; }
    }
}
