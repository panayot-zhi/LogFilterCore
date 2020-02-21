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