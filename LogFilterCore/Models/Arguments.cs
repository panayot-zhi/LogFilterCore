using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogFilterCore.Utility;

namespace LogFilterCore.Models
{
    public class Arguments
    {
        public ApplicationMode Mode { get; set; }

        public IList<string> ConfigurationFilePaths { get; set; }
    }
}
