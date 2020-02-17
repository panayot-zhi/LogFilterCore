using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;

namespace LogFilterCore.Utility
{
    public class ParserDateTimeConverter : IsoDateTimeConverter
    {
        public ParserDateTimeConverter(string format)
        {
            DateTimeFormat = format;
        }
    }
}
