using LogFilterCore.Utility;
using System;
using System.Threading;

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
                    if (string.IsNullOrEmpty(message))
                    {
                        Console.Write($"\rProgress: {percent}%");
                    }
                    else if (message.HasPlaceholder())
                    {
                        Console.Write($"\r{string.Format(message, percent)}");
                    }
                    else
                    {
                        Console.Write($"\r{message}: " + percent + "%");
                    }
                }
                else
                {
                    Console.WriteLine(message);
                }
            }

            var runner = new ConfigurationRunner(ReportProgress);

            foreach (var arg in args)
            {
                if (!FileHelper.IsFile(arg))
                {
                    Console.WriteLine("Does not exist: " + arg);
                    Thread.Sleep(500);
                    continue;
                }

                try
                {
                    runner.Run(arg);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    Console.WriteLine(ex);
                }
            }

            Console.WriteLine("Exiting...");
            Thread.Sleep(1000);
        }
    }
}