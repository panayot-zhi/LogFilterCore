using System;
using System.Collections.Generic;
using System.Globalization;
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
        public static string SummaryFilePrefix { get; set; } = "summary";

        public static string CurrentFilePrefix { get; set; }

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

        public static bool WriteFile(string filePath, string content, bool overwrite = false)
        {
            if (!overwrite && File.Exists(filePath))
            {
                //throw new ParserException($"File '{Path.GetFileNameWithoutExtension(filePath)}' already exists (overwrite: false).");
                return false;
            }

            File.WriteAllText(filePath, content);
            return true;
        }

        public static void SetReadonly(string path)
        {
            var attr = File.GetAttributes(path);    // get all            
            attr = attr | FileAttributes.ReadOnly;  // add one
            File.SetAttributes(path, attr);         // overwrite
        }
        public static string[] GetLogsFromDirectory(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                return new string[0];
            }

            var files = new List<string>();
            foreach (var directory in Directory.GetDirectories(dirPath))
            {
                files.AddRange(GetLogsFromDirectory(directory).ToList());
            }

            files.AddRange(Directory.GetFiles(dirPath, "*.log"));
            return files.ToArray();
        }

        public static string[] GetPrefixedLogsFromDirectory(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                return new string[0];
            }

            var files = new List<string>();
            foreach (var directory in Directory.GetDirectories(dirPath))
            {
                files.AddRange(GetPrefixedLogsFromDirectory(directory).ToList());
            }

            files.AddRange(Directory.GetFiles(dirPath, $"{GetPrefixFormat(CurrentFilePrefix)}*.log"));
            return files.ToArray();
        }

        /// <summary>
        /// Try filtering of files, which have a filename matching the date and time format of the parser.
        /// Do this only if entries have the specific filename pattern and if begin & end filters have value and
        /// if the parsed date from the filename falls out of range from any one of those filters.
        /// </summary>
        /// <param name="files">Array of full file paths.</param>
        /// <param name="dateFormat">The datetime format of the parser.</param>
        /// <param name="beginFilter">Filter logs by filename from this date on.</param>
        /// <param name="endFilter">Filter logs by filename until this day.</param>
        /// <returns></returns>
        public static IEnumerable<string> FilterFilesByDateFilter(IEnumerable<string> files, string dateFormat, DateTime? beginFilter, DateTime? endFilter, bool originals = false)
        {
            foreach (var file in files)
            {
                var fileNameAsDate = TryGetDateFromFileName(file, dateFormat, originals);
                if (!fileNameAsDate.HasValue)
                {
                    // if the filename cannot be parsed as date,
                    // filtering is not posible,
                    // return the file for further processing...
                    yield return file;
                }

                // -> filename is parsed to date

                if (beginFilter.HasValue && beginFilter.Value.Date > fileNameAsDate)
                {
                    // if the beginFilter has value, and the
                    // value is greater than the date in the file name
                    // ex. 2014-12-31.log < 2015-01-01 00:00:00
                    continue;
                }

                if (endFilter.HasValue && endFilter.Value.Date < fileNameAsDate)
                {
                    // if the endFilter has value, and the
                    // value is less than the date in the file name
                    // ex. 2017-01-01.log > 2016-01-31 00:00:00
                    continue;
                }

                // in all other cases,
                // return the file for
                // further processing
                yield return file;
            }
        }

        public static IEnumerable<FileInfo> FilterFilesByDateFilter(IEnumerable<FileInfo> files, string dateFormat, DateTime? beginFilter, DateTime? endFilter, bool originals = false)
        {
            return FilterFilesByDateFilter(files.Select(x => x.FullName), dateFormat, beginFilter, endFilter, originals).Select(x => new FileInfo(x));
        }

        public static DateTime? TryGetDateFromFileName(string path, string format, bool originals = false)
        {
            try
            {
                var filename = Path.GetFileNameWithoutExtension(path);
                if (originals)
                {
                    // ReSharper disable once PossibleNullReferenceException: Intentional - indicates error
                    filename = filename.Replace(GetPrefixFormat(CurrentFilePrefix), string.Empty);
                }

                if (DateTime.TryParseExact(filename, format, CultureInfo.CurrentCulture.DateTimeFormat, DateTimeStyles.None, out var candidate))
                {
                    return candidate;
                }
            }
            catch (Exception)
            {
                // ignore
            }

            return null;
        }

        /// <summary>
        /// Seeks to find if a directory with the same name as the file has been created by a previous run of the parser.
        /// </summary>
        /// <param name="inputFile">File information for the current file.</param>
        /// <param name="inputFolder">Directory of origin.</param>
        /// <param name="outputFolder">Destination directory.</param>
        /// <param name="originals">Use files with [original] prefix from the filename</param>
        /// <returns>True if a directory is found, otherwise false.</returns>
        public static bool HasBeenProcessed(FileInfo inputFile, string inputFolder, string outputFolder, bool originals = false)
        {
            var resolvedDirectoryName = inputFile.FullName.Replace(inputFolder, outputFolder).Replace(inputFile.Extension, string.Empty);
            if (originals)
            {
                resolvedDirectoryName = ExtractDirectoryName(resolvedDirectoryName);
            }

            return Directory.Exists(resolvedDirectoryName);
        }

        public static string ExtractFileName(string filePath)
        {
            // after reading, reassemble path to make output pattern for the file the same as within the original
            return filePath.Substring(0, filePath.IndexOf($"\\{GetPrefixFormat(CurrentFilePrefix)}", StringComparison.Ordinal)) + ".log";
        }

        public static string ExtractDirectoryName(string filePath)
        {
            // after reading, reassemble path to make output pattern for the directory the same as within the original
            return filePath.Substring(0, filePath.IndexOf($"\\{GetPrefixFormat(CurrentFilePrefix)}", StringComparison.Ordinal));
        }
    }
}
