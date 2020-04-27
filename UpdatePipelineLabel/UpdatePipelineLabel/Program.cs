using CommandLine;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ZenHub;
using ZenHub.Models;

namespace UpdatePipelineLabel
{
    class Program
    {
        static void Main(string[] args)

        {
            ParserResult<Options> parserResult = Parser.Default.ParseArguments<Options>(args);

            parserResult.WithParsed(options =>
            {
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await new LabelUpdateHelper(options).UpdateLabel();

                        Console.WriteLine("Update label complete");

                        Environment.Exit(0);
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                });

                task.Wait();

            })

                .WithNotParsed(errors =>

                {

                    Environment.Exit(1);

                    Console.ReadLine();

                });

        }
    }

}
