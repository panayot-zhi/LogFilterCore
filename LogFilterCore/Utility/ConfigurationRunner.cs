using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogFilterCore.Models;
using Newtonsoft.Json;

namespace LogFilterCore.Utility
{
    public class ConfigurationRunner
    {        
        private Summary RunSummary { get; set; }

        private Summary CurrentSummary { get; set; }

        private readonly Action<string, int?> _reportProgress;

        public ConfigurationRunner(Action<string, int?> reportProgress)
        {
            _reportProgress = reportProgress;
        }

        public void Run(string configuration)
        {
            var cfg = FileHelper.LoadConfiguration(configuration);

            if (!string.IsNullOrWhiteSpace(cfg.InputFolder))
            {
                var outputPath = $"{cfg.InputFolder}\\parsed\\";
                cfg.OutputFolder = outputPath;
            }

            if (!string.IsNullOrWhiteSpace(cfg.InputFile))
            {
                var fileDirectory = FileHelper.GetFileDirectory(cfg.InputFile);
                var outputPath = $"{fileDirectory}\\parsed\\";

                cfg.OutputFolder = outputPath;
            }
            
            Run(cfg);
        }

        public void Run(Configuration cfg)
        {
            // TODO...
        }        

        protected void ReportProgress(string message, int? progress)
        {
            _reportProgress?.Invoke(message, progress);
        }               
    }
}
