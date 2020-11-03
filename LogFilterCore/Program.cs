using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using CommandLine;
using Newtonsoft.Json;

namespace LogFilterCore
{
    internal class Program
    {
        private const string Hello = @"

╦  ┌─┐┌─┐╔═╗┬┬ ┌┬┐┌─┐┬─┐╔═╗┌─┐┬─┐┌─┐
║  │ ││ ┬╠╣ ││  │ ├┤ ├┬┘║  │ │├┬┘├┤
╩═╝└─┘└─┘╚  ┴┴─┘┴ └─┘┴└─╚═╝└─┘┴└─└─┘

";

        internal class Options
        {
            [Option('c', "config", Required = true, HelpText = "A list of configurations to run.")]
            public IEnumerable<string> ConfigurationPaths { get; set; }

            [Option('f', "file", Required = false, HelpText = "A single file to run configurations on.")]
            public string InputFilePath { get; set; }
        }

        private static void Main(string[] args)
        {
            foreach (var line in Hello.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                Console.WriteLine(line);
                Thread.Sleep(500);
            }

            var expression = new Regex(".*",
                RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
                TimeSpan.FromSeconds(5));

            var jsonExpression = JsonConvert.SerializeObject(expression);
            var newExpression = JsonConvert.DeserializeObject<Regex>(jsonExpression);

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);

            Console.WriteLine("Exiting...");
            Console.WriteLine("");
            Thread.Sleep(1000);
        }

        private static void ReportProgressDelegate(string message, int? percent)
        {
            if (percent.HasValue)
            {
                Console.Write(percent.Value == 100
                    ? $"\r{percent}% {message}{Environment.NewLine}"
                    : $"\r{percent}% {message}");
            }
            else
            {
                Console.WriteLine(message);
            }
        }


        private static void HandleParseError(IEnumerable<Error> err)
        {
            Console.WriteLine("Failed parsing arguments: {0}", string.Join(",", err));
        }

        private static void RunOptions(Options opt)
        {
            foreach (var path in opt.ConfigurationPaths)
            {
                if (!FileProcessor.IsFile(path))
                {
                    Console.WriteLine("Does not exist: " + path);
                    Thread.Sleep(500);
                    continue;
                }

                try
                {
                    Console.WriteLine("Running configuration: " + path);
                    var runner = new ConfigurationRunner(path)
                    {
                        ReportProgress = ReportProgressDelegate
                    };

                    runner.Run(opt.InputFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    Console.WriteLine(ex);
                    Console.ReadKey();
                }
            }
        }
    }
}