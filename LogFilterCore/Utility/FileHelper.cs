using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogFilterCore.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LogFilterCore.Utility
{
    public static class FileHelper
    {
        public static string ReparseFilePrefix { get; set; } = "original";

        public static string SummaryFilePrefix { get; set; } = "summary";

        public static string GetPrefixFormat(string prefix)
        {
            return "[" + prefix + "]-";
        }

        public static bool IsFolder(string path)
        {
            return Directory.Exists(path);
        }

        public static bool IsFile(string path)
        {
            return File.Exists(path);
        }



        public static string GetCurrentSummaryFilePath(string filePath, string inputFolder, string outputFolder)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var fullFileName = Path.GetFileName(filePath);
            var prefixedFileName = GetPrefixFormat(SummaryFilePrefix) + fileName + ".json";

            /*if (fullFileName == null)
            {
                fullFileName = filePath.Substring(filePath.LastIndexOf("\\", StringComparison.InvariantCulture));
            }*/

            if (string.IsNullOrEmpty(inputFolder))
            {
                inputFolder = GetFileDirectory(filePath);
            }

            var baseOutputPath = filePath.Replace(inputFolder, outputFolder);
            var newOutputPath = baseOutputPath.Replace(fullFileName, Path.Combine(fileName, prefixedFileName));
            var newOutputDirectory = newOutputPath.Replace(prefixedFileName, string.Empty);

            Directory.CreateDirectory(newOutputDirectory);

            return newOutputPath;
        }

        public static string GetFileDirectory(string filePath)
        {
            return Path.GetDirectoryName(filePath);
        }

        public static string GetRunSummaryFilePath(string outputFolder)
        {
            return Path.Combine(outputFolder, $"[{DateTime.Now:yyyy-MM-dd}]-summary_{DateTime.Now:HHmmss}.json");
        }


        public static Configuration LoadConfiguration(string path, JsonSerializerSettings settings = null)
        {
            if (settings == null)
            {
                // default settings for .NET (pretty)
                settings = new JsonSerializerSettings()
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    Formatting = Formatting.Indented
                };
            }

            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Configuration>(json, settings);
        }
    }
}
