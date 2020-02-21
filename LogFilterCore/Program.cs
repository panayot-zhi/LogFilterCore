using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using LogFilterCore.Models;

namespace LogFilterCore
{
    internal class Program
    {
        private const string Hello = @"

╦  ┌─┐┌─┐╔═╗┬┬ ┌┬┐┌─┐┬─┐╔═╗┌─┐┬─┐┌─┐
║  │ ││ ┬╠╣ ││  │ ├┤ ├┬┘║  │ │├┬┘├┤
╩═╝└─┘└─┘╚  ┴┴─┘┴ └─┘┴└─╚═╝└─┘┴└─└─┘

";

        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No arguments!");
                Console.WriteLine("Exiting...");
                Thread.Sleep(1000);
                return;
            }

            foreach (var line in Hello.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                Console.WriteLine(line);
                Thread.Sleep(500);
            }

            void ReportProgress(string message, int? percent)
            {
                if (percent.HasValue)
                {
                    Console.Write($"\r{percent}% {message}");
                }
                else
                {
                    Console.WriteLine(message);
                }
            }

            foreach (var arg in args)
            {
                if (!FileProcessor.IsFile(arg))
                {
                    Console.WriteLine("Does not exist: " + arg);
                    Thread.Sleep(500);
                    continue;
                }

                try
                {
                    Console.WriteLine("Running configuration: " + arg);
                    var runner = new ConfigurationRunner(ReportProgress);
                    runner.Run(arg);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    Console.WriteLine(ex);
                    Console.ReadKey();
                }
            }

            Console.WriteLine("Exiting...");
            Thread.Sleep(1000);
        }
    }
}