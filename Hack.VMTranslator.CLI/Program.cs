﻿using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Hack.VMTranslator.Lib.Extensions;
using Hack.VMTranslator.Lib.Output;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hack.VMTranslator.CLI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var command = GetCommand();
                await command.InvokeAsync(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("Program run into an exception");
                Console.WriteLine(e.Message);
            }
        }
        
        private static IHostBuilder GetHostBuilder(Options options)
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                {
                    services.AddCoreServices(options.IncludeBootstrap);
                    services.AddInputSupport();
                    services.AddOutputSupport();
                    services.AddHostedService<HostedService>();
                    services.Configure<HostedServiceOptions>(o =>
                    {
                        o.InputPath = options.InputPath;
                        o.OutputPath = options.OutputFile.FullName;
                    });
                });
        }

        private static RootCommand GetCommand()
        {
            // Create a root command with some options
            var rootCommand = new RootCommand
            {
                new Argument<FileSystemInfo>(
                    "input",
                    "A path to a single VM file or a directory containing 1 or many VM files to translate into Assembler code"),
                new Option<FileInfo>(
                    new[] {"--output", "-o"},
                    () => null,
                    "A result file to create"),
                new Option<bool>(
                    new[] {"--include-bootstrap", "-b"},
                    () => true,
                    "Decides whether bootstrap (initialization) code should be included in the Assembler code"),
            };

            rootCommand.Description = "Nand2tetris VM Translator CLI";

            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.Handler = CommandHandler.Create((Func<FileSystemInfo, FileInfo, bool, Task>)(async (input, outputFile, includeBootstrap) =>
            {
                outputFile ??= OutputUtilities.GetOutputFileInfo(input); 
                var options = new Options(input, outputFile, includeBootstrap);
                var builder = GetHostBuilder(options);
                await builder.RunConsoleAsync(o => o.SuppressStatusMessages = true);
            }));

            return rootCommand;
        }
    }
}