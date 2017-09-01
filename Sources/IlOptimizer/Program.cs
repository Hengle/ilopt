// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace IlOptimizer
{
    /// <summary>Provides a set of methods for executing a program.</summary>
    public static class Program
    {
        private static readonly string[] Options = {
            "StripLocalsInit"
        };

        /// <summary>The entry point of the program.</summary>
        /// <param name="args">The arguments that should be used when running the program.</param>
        /// <returns>The exit code of the program.</returns>
        public static int Main(string[] args)
        {
            if ((args.Length == 0) || args.Any((arg) => Matches(arg, "?", "h", "help") == string.Empty))
            {
                PrintHelp(args);
            }
            else
            {
                var assemblyPaths = new List<string>();
                var options = new List<(string name, string parameter)>();

                foreach (var arg in args)
                {
                    string parameter = null;

                    foreach (var option in Options)
                    {
                        parameter = Matches(arg, option);

                        if (parameter != null)
                        {
                            options.Add((option, parameter));
                            break;
                        }
                    }

                    if (parameter is null)
                    {
                        var path = Path.GetFullPath(arg);

                        if (!File.Exists(arg))
                        {
                            WriteLine(ConsoleColor.Yellow, $"    Warning: A specified assembly could not be located.");
                            WriteLine(ConsoleColor.Yellow, $"             Argument: '{arg}'");
                            WriteLine(ConsoleColor.Yellow, $"             Resolved Path: '{path}'");
                            WriteLine(ConsoleColor.Yellow, $"    Continuing will ignore this argument; Otherwise, the process will exit.");

                            if (!CheckForContinue())
                            {
                                Exit();
                            }
                        }
                        else
                        {
                            assemblyPaths.Add(path);
                        }
                    }
                }

                if (options.Count == 0)
                {
                    WriteLine(ConsoleColor.Red, $"    Error: No options were specified.");
                    Exit();
                }
                else if (assemblyPaths.Count == 0)
                {
                    WriteLine(ConsoleColor.Red, $"    Error: No assemblies were specified.");
                    Exit();
                }

                Run(options, assemblyPaths);
            }

            return 0;
        }

        private static bool CheckForContinue()
        {
            Write(ConsoleColor.Yellow, $"    Would you like to continue [Y/n]: ");

            while (true)
            {
                var input = Console.ReadLine().ToLower();

                if (string.IsNullOrWhiteSpace(input) || (input == "yes") || (input == "y"))
                {
                    return true;
                }
                else if ((input == "no") || (input == "n"))
                {
                    return false;
                }
                else
                {
                    Write(ConsoleColor.Yellow, $"        Unrecognized input: '{input}'. Please use 'yes', 'y', 'no', or 'n': ");
                }
            }
        }

        private static void Exit()
        {
            Console.WriteLine("Exiting...");
            Environment.Exit(int.MinValue);
        }

        private static string Matches(string arg, params string[] keywords)
        {
            foreach (var keyword in keywords)
            {
                if ((arg[0] != '-') && (arg[0] != '/'))
                {
                    return null;
                }

                if ((((arg.Length - 1) == keyword.Length) || (arg.Length > (keyword.Length + 2))) && (string.Compare(arg, 1, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase) == 0))
                {
                    if ((arg.Length - 1) == keyword.Length)
                    {
                        return string.Empty;
                    }

                    if ((arg[keyword.Length + 1] == ':') || (arg[keyword.Length + 1] == '='))
                    {
                        return arg.Substring(keyword.Length + 2);
                    }
                }
            }

            return null;
        }

        private static void PrintHelp(string[] args)
        {
            var thisAssembly = Assembly.GetExecutingAssembly();

            Console.WriteLine($"IlOptimizer v{thisAssembly.GetName().Version} - A tool for performing post-compilation optimization on managed assemblies.");
            Console.WriteLine($"    {Path.GetFileName(thisAssembly.Location)} [help-command] <options> <assemblies>");
            Console.WriteLine();
            Console.WriteLine($"    All options and commands must be prefixed with '-' or '/'.");
            Console.WriteLine($"    Any options that take parameters can use '=' or ':'");
            Console.WriteLine($"    Any unrecognized arguments are treated as <assemblies>.");
            Console.WriteLine();
            Console.WriteLine($"    Help Command: 'help', 'h', or '?'");
            Console.WriteLine($"        Prints this help message.");
            Console.WriteLine();
            Console.WriteLine($"    Options");
            Console.WriteLine($"        StripLocalsInit: 'striplocalsinit' or 'striplocalsinit=<RegEx>'");
            Console.WriteLine($"            Strips the 'init' flag from the '.locals' directive for the matching methods.");
            Console.WriteLine($"            This matches against all methods if a regular expression is not given.");
            Console.WriteLine($"            When a regular expression is given, it is used to match against a method's full name.");
            Console.WriteLine();
            Console.WriteLine($"    Asssemblies");
            Console.WriteLine($"        One or more assemblies on which the selected optimizations should be performed.");
            Console.WriteLine($"        The post-processed assemblies will be output to a folder called 'optimized' in the working directory.");
        }

        private static void Run(IEnumerable<(string name, string parameter)> options, IEnumerable<string> assemblyPaths)
        {
            var outputPath = Path.GetFullPath("optimized");
            Directory.CreateDirectory(outputPath);

            foreach (var assemblyPath in assemblyPaths)
            {
                var outputAssemblyPath = Path.Combine(outputPath, Path.GetFileName(assemblyPath));

                if (Path.GetDirectoryName(assemblyPath) == outputPath)
                {
                    WriteLine(ConsoleColor.Yellow, $"    Warning: An assembly was given which exists in the output folder for the tool.");
                    WriteLine(ConsoleColor.Yellow, $"             Assembly: '{assemblyPath}'");
                    WriteLine(ConsoleColor.Yellow, $"    Continuing will skip processing for this assembly; Otherwise, the process will exit.");

                    if (CheckForContinue())
                    {
                        continue;
                    }

                    Exit();
                }

                if (File.Exists(outputAssemblyPath))
                {
                    WriteLine(ConsoleColor.Yellow, $"    Warning: A post-processed assembly with the same name already exists in the output folder.");
                    WriteLine(ConsoleColor.Yellow, $"             Assembly: '{assemblyPath}'");
                    WriteLine(ConsoleColor.Yellow, $"             Output Assembly: '{outputAssemblyPath}'");
                    WriteLine(ConsoleColor.Yellow, $"    Continuing will cause the existing post-processed assembly to be overwritten; Otherwise, processing for this assembly will be skipped.");

                    if (!CheckForContinue())
                    {
                        continue;
                    }
                }

                var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);

                foreach (var option in options)
                {
                    if (option.name.Equals("StripLocalsInit", StringComparison.OrdinalIgnoreCase))
                    {
                        StripLocalsInit.Process(assembly, option.parameter);
                    }
                    else
                    {
                        WriteLine(ConsoleColor.Red, $"    Error: An unrecognized option was encountered. No changes will be saved.");
                        WriteLine(ConsoleColor.Red, $"        Option: '{option}'");
                        Exit();
                    }
                }

                assembly.Write(outputAssemblyPath);
            }
        }

        private static void Write(ConsoleColor color, string message)
        {
            var foregroundColor = Console.ForegroundColor;
            {
                Console.ForegroundColor = color;
                Console.Write(message);
            }
            Console.ForegroundColor = foregroundColor;
        }

        private static void WriteLine(ConsoleColor color, string message)
        {
            var foregroundColor = Console.ForegroundColor;
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
            }
            Console.ForegroundColor = foregroundColor;
        }
    }
}
