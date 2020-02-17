using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogFilterCore.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LogFilterCore.Models
{
    public class Summary
    {        
        protected string DateTimeFormat { get; }

        public Summary(string dateTimeFormat)
        {
            this.DateTimeFormat = dateTimeFormat;
        }

        public ulong LinesRead { get; set; }

        public ulong LogsRead { get; set; }

        public int NonStandardEntries { get; set; }

        public ulong EntriesConstructed { get; set; }

        public ulong FilteredEntries { get; set; }

        public ulong LinesWritten { get; set; }

        public int FilesWritten { get; set; }

        public int FilesRead { get; set; }

        public DateTime BeginProcessTimestamp { get; set; }

        public DateTime EndProcessTimestamp { get; set; }        

        public virtual string ToJson()
        {
            var settings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new JsonConverter[] { new ParserDateTimeConverter(this.DateTimeFormat) },
                Formatting = Formatting.Indented
            };

            return JsonConvert.SerializeObject(this, settings);
        }
    }
}
