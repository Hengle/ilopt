// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Generic;
using System.Linq;
using IlOptimizer.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace IlOptimizer
{
    /// <summary>Provides methods for stripping the 'init' flag from the '.locals' directive for a method.</summary>
    public static class StripLocalsInit
    {
        public static bool Optimize(MethodDefinition method)
        {
            if (method.HasBody)
            {
                var methodBody = method.Body;

                if (methodBody.InitLocals)
                {

                    var instructionGraph = new InstructionGraph(methodBody);
                    var root = instructionGraph.Root;

                    var variableDataMap = new Dictionary<InstructionNode, bool?[]>();
                    var variables = methodBody.Variables;

                    var foundLocalloc = false;

                    // We first do a depth first traversal and determine, for each variable
                    // whether or not it was accessed (not-null) for a node and whether the
                    // access was a load (false) or a store (true).

                    root.TraverseDepthFirst((node) => {
                        var instructions = node.Instructions;

                        var variableData = new bool?[variables.Count];
                        variableDataMap[node] = variableData;

                        for (var index = 0; index < instructions.Length; index++)
                        {
                            var instruction = instructions[index];

                            if (instruction.OpCode.Code == Code.Localloc)
                            {
                                foundLocalloc = true;
                                return;
                            }
                            else if (instruction.IsStoreLocalInstruction())
                            {
                                var variableIndex = instruction.GetStoreLocalIndex();

                                if (variableData[variableIndex].HasValue)
                                {
                                    continue;
                                }

                                variableData[variableIndex] = true;
                            }
                            else if (instruction.IsLoadLocalInstruction())
                            {
                                var variableIndex = instruction.GetLoadLocalIndex();

                                if (variableData[variableIndex].HasValue)
                                {
                                    continue;
                                }

                                index++;

                                variableData[variableIndex] = (index < instructions.Length)
                                                           && (instructions[index].OpCode.Code == Code.Initobj);
                            }
                        }
                    });

                    if (foundLocalloc)
                    {
                        // TODO: Actually handle localloc
                        return false;
                    }

                    var unassignedVariables = new Stack<int>();
                    var rootVariableData = variableDataMap[root];

                    for (var index = 0; index < rootVariableData.Length; index++)
                    {
                        if (rootVariableData[index].GetValueOrDefault() == false)
                        {
                            unassignedVariables.Push(index);
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
    }
}
