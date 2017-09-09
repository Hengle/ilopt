// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using IlOptimizer.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace IlOptimizer.Optimizations
{
    // This has been tested on the v4.7 build of mscorlib.
    // We end up processing 1 Module, 3266 Types, 33 Events, 4939 Properties, and 34653 Methods
    // Of those methods, 30067 have a method body, but only 9003 have the `InitLocals` flag.
    // Ignoring methods containing `localloc`, we have 8928 methods remaining which can be processed
    // We then also currently ignore methods where all variables are not assigned before the first branch
    // This leaves us with 4091 methods that we can actually optimize right now (roughly 45%).

    /// <summary>Provides methods for stripping the 'init' flag from the '.locals' directive for a method.</summary>
    public static class StripLocalsInit
    {
        public static bool Optimize(MethodDefinition method, string parameter)
        {
            if (method.HasBody)
            {
                var methodBody = method.Body;

                if (methodBody.InitLocals)
                {
                    if (parameter.Equals("all", StringComparison.OrdinalIgnoreCase))
                    {
                        // The user wants us to strip the flag from all methods
                        methodBody.InitLocals = false;
                        return true;
                    }

                    var instructionGraph = new InstructionGraph(methodBody);
                    var root = instructionGraph.Root;

                    var variables = methodBody.Variables;
                    var variableAccessData = null as VariableAccessData;

                    // We first do a depth first traversal and determine, for each variable
                    // whether or not it was accessed (not-null) for a node and whether the
                    // access was a load (false) or a store (true).

                    var containsLocalloc = false;

                    root.TraverseDepthFirst((node) => {
                        var instructions = node.Instructions;

                        for (var index = 0; index < instructions.Length; index++)
                        {
                            var instruction = instructions[index];
                            var instructionFamily = instruction.GetInstructionFamily();

                            var variable = null as VariableReference;
                            var assigned = false;

                            if (instructionFamily == InstructionFamily.Localloc)
                            {
                                containsLocalloc = true;
                                continue;
                            }
                            else if (instructionFamily == InstructionFamily.Stloc)
                            {
                                variable = instruction.GetVariableForStloc(methodBody);
                                assigned = true;
                            }
                            else if (instructionFamily == InstructionFamily.Ldloc)
                            {
                                variable = instruction.GetVariableForLdloc(methodBody);
                            }
                            else if (instructionFamily == InstructionFamily.Ldloca)
                            {
                                variable = instruction.GetVariableForLdloca();
                            }
                            else
                            {
                                continue;
                            }

                            if (node.TryGetProperty(variable, out variableAccessData) == false)
                            {
                                variableAccessData = new VariableAccessData();
                                node.AddProperty(variable, variableAccessData);
                            }

                            var accessInstructions = variableAccessData.Instructions;

                            if ((accessInstructions.Count == 0) && (instructionFamily == InstructionFamily.Ldloca))
                            {
                                index++;

                                if (index < instructions.Length)
                                {
                                    var nextInstruction = instructions[index];

                                    if (nextInstruction.GetInstructionFamily() != InstructionFamily.Initobj)
                                    {
                                        index--;
                                    }
                                    else
                                    {
                                        instruction = nextInstruction;
                                        assigned = true;
                                    }
                                }
                            }

                            if (assigned)
                            {
                                variableAccessData.AssignedAfter = true;

                                if (accessInstructions.Count == 0)
                                {
                                    variableAccessData.AssignedFirst = true;
                                }
                            }

                            variableAccessData.Instructions.Add(instruction);
                        }
                    });

                    if (containsLocalloc)
                    {
                        return false;
                    }

                    var unassignedVariables = new Stack<int>();
                    
                    for (var index = 0; index < variables.Count; index++)
                    {
                        var variable = variables[index];

                        if ((root.TryGetProperty(variable, out variableAccessData) == false) || (variableAccessData.AssignedFirst == false))
                        {
                            unassignedVariables.Push(index);
                            continue;
                        }
                    }
                    
                    if (unassignedVariables.Count == 0)
                    {
                        // The simplest route, all variables were assigned in the root node.
                        methodBody.InitLocals = false;
                        return true;
                    }

                    // TODO: Handle the case where variables were assigned outside the root node.
                }
            }

            return false;
        }

        private class VariableAccessData
        {
            #region Constructors
            public VariableAccessData()
            {
                Instructions = new List<Instruction>();
            }
            #endregion

            #region Properties
            public bool AssignedAfter { get; set; }

            public bool AssignedBefore { get; set; }

            public bool AssignedFirst { get; set; }

            public List<Instruction> Instructions { get; }
            #endregion
        }
    }
}
