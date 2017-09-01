// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Text.RegularExpressions;
using Mono.Cecil;

namespace IlOptimizer
{
    /// <summary>Provides methods for stripping the 'init' flag from the '.locals' directive for a method.</summary>
    public static class StripLocalsInit
    {
        /// <summary>Processes all methods matching a <see cref="Regex" /> in a <see cref="AssemblyDefinition" />.</summary>
        /// <param name="assembly">The assembly to process.</param>
        /// <param name="regex">The regular expression to use when matching methods.</param>
        public static void Process(AssemblyDefinition assembly, string regex)
        {
            Console.WriteLine($"    Stripping the 'init' flag from the '.locals' directive for methods in {assembly.Name.Name} matching {((regex == string.Empty) ? "*" : regex)}");

            int modulesProcessed = 0;
            int typesProcessed = 0;
            int methodsProcessed = 0;
            int propertiesProcessed = 0;
            int eventsProcessed = 0;
            int methodsUpdated = 0;

            foreach (var module in assembly.Modules)
            {
                Process(module, regex, ref typesProcessed, ref methodsProcessed, ref propertiesProcessed, ref eventsProcessed, ref methodsUpdated);
                modulesProcessed++;
            }

            Console.WriteLine($"        Modules Processed: {modulesProcessed}");
            Console.WriteLine($"        Types Processed: {typesProcessed}");
            Console.WriteLine($"        Methods Processed: {methodsProcessed}");
            Console.WriteLine($"        Properties Processed: {propertiesProcessed}");
            Console.WriteLine($"        Events Processed: {eventsProcessed}");
            Console.WriteLine($"    Methods Updated: {methodsUpdated}");
        }

        private static void Process(ModuleDefinition module, string regex, ref int typesProcessed, ref int methodsProcessed, ref int propertiesProcessed, ref int eventsProcessed, ref int methodsUpdated)
        {
            foreach (var type in module.Types)
            {
                Process(type, regex, ref typesProcessed, ref methodsProcessed, ref propertiesProcessed, ref eventsProcessed, ref methodsUpdated);
                typesProcessed++;
            }
        }

        private static void Process(TypeDefinition type, string regex, ref int typesProcessed, ref int methodsProcessed, ref int propertiesProcessed, ref int eventsProcessed, ref int methodsUpdated)
        {
            foreach (var nestedType in type.NestedTypes)
            {
                Process(nestedType, regex, ref typesProcessed, ref methodsProcessed, ref propertiesProcessed, ref eventsProcessed, ref methodsUpdated);
                typesProcessed++;
            }

            foreach (var method in type.Methods)
            {
                Process(method, regex, ref methodsProcessed, ref methodsUpdated);
                methodsProcessed++;
            }

            foreach (var property in type.Properties)
            {
                Process(property, regex, ref methodsProcessed, ref methodsUpdated);
                propertiesProcessed++;
            }

            foreach (var @event in type.Events)
            {
                Process(@event, regex, ref methodsProcessed, ref methodsUpdated);
                eventsProcessed++;
            }
        }

        private static void Process(MethodDefinition method, string regex, ref int methodsProcessed, ref int methodsUpdated)
        {
            if (method is null)
            {
                methodsProcessed--;
                return;
            }

            if (method.HasBody && method.Body.InitLocals && ((regex == string.Empty) || Regex.IsMatch(method.FullName, regex)))
            {
                method.Body.InitLocals = false;
                methodsUpdated++;
            }
        }

        private static void Process(PropertyDefinition property, string regex, ref int methodsProcessed, ref int methodsUpdated)
        {
            Process(property.GetMethod, regex, ref methodsProcessed, ref methodsUpdated);
            Process(property.SetMethod, regex, ref methodsProcessed, ref methodsUpdated);

            methodsProcessed += 2;

            foreach (var otherMethod in property.OtherMethods)
            {
                Process(otherMethod, regex, ref methodsProcessed, ref methodsUpdated);
                methodsProcessed++;
            }
        }

        private static void Process(EventDefinition @event, string regex, ref int methodsProcessed, ref int methodsUpdated)
        {
            Process(@event.AddMethod, regex, ref methodsProcessed, ref methodsUpdated);
            Process(@event.InvokeMethod, regex, ref methodsProcessed, ref methodsUpdated);
            Process(@event.RemoveMethod, regex, ref methodsProcessed, ref methodsUpdated);

            methodsProcessed += 3;

            foreach (var otherMethod in @event.OtherMethods)
            {
                Process(otherMethod, regex, ref methodsProcessed, ref methodsUpdated);
                methodsProcessed++;
            }
        }
    }
}
