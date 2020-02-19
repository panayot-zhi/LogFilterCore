﻿using LogFilterCore.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace LogFilterCore.Models
{
    [Serializable]
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

        public Filter[] Filters { get; set; }

        public virtual string ToJson()
        {
            var settings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new JsonConverter[] { new ParserDateTimeConverter(this.DateTimeFormat) },
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };

            return JsonConvert.SerializeObject(this, settings);
        }
    }
}