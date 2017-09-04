// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;

namespace IlOptimizer.CodeAnalysis
{
    public class InstructionNode
    {
        public static InstructionNode CreateGraph(MethodBody methodBody)
        {
            var instructions = methodBody.Instructions;
            var instructionToNodeMap = new Dictionary<Instruction, InstructionNode>();
            var rootNode = new InstructionNode(instructions[0]);

            instructionToNodeMap.Add(instructions[0], rootNode);
            AddNodesForCodePath(rootNode);
            return rootNode;

            void AddNodesForCodePath(InstructionNode instructionNode)
            {
                while (instructionNode != null)
                {
                    var instruction = instructionNode.Instruction;

                    switch (instruction.OpCode.FlowControl)
                    {
                        case FlowControl.Branch:
                        {
                            // Unconditional branches don't need to recurse since they only end up
                            // creating a single child node.

                            var branchInstruction = (Instruction)(instruction.Operand);
                            instructionToNodeMap.TryGetValue(branchInstruction, out var branchNode);

                            if (branchNode is null)
                            {
                                branchNode = new InstructionNode(branchInstruction);
                                instructionToNodeMap.Add(branchInstruction, branchNode);
                                instructionNode.ChildNodes.Add(branchNode);
                                instructionNode = branchNode;
                            }
                            else
                            {
                                instructionNode.ChildNodes.Add(branchNode);
                            }
                        }
                        break;

                        case FlowControl.Cond_Branch:
                        {
                            if (instruction.OpCode.Code == Code.Switch)
                            {
                                var switchInstructions = (Instruction[])(instruction.Operand);

                                foreach (var switchInstruction in switchInstructions)
                                {
                                    instructionToNodeMap.TryGetValue(switchInstruction, out var switchNode);

                                    if (switchNode is null)
                                    {
                                        switchNode = new InstructionNode(switchInstruction);
                                        instructionToNodeMap.Add(switchInstruction, switchNode);
                                        AddNodesForCodePath(switchNode);
                                    }

                                    instructionNode.ChildNodes.Add(switchNode);
                                }
                            }
                            else
                            {
                                var branchInstruction = (Instruction)(instruction.Operand);
                                instructionToNodeMap.TryGetValue(branchInstruction, out var branchNode);

                                if (branchNode is null)
                                {
                                    branchNode = new InstructionNode(branchInstruction);
                                    instructionToNodeMap.Add(branchInstruction, branchNode);
                                    AddNodesForCodePath(branchNode);
                                }

                                instructionNode.ChildNodes.Add(branchNode);
                            }
                        }
                        goto case FlowControl.Next;

                        case FlowControl.Meta:
                        {
                            var metaCode = instruction.OpCode.Code;

                            if (metaCode != Code.Volatile)
                            {
                                throw new NotImplementedException();
                            }
                        }
                        goto case FlowControl.Next;

                        case FlowControl.Break:
                        case FlowControl.Call:
                        case FlowControl.Next:
                        {
                            var nextInstruction = instruction.Next;

                            if (nextInstruction is null)
                            {
                                return;
                            }

                            instructionToNodeMap.TryGetValue(nextInstruction, out var nextNode);

                            if (nextNode is null)
                            {
                                nextNode = new InstructionNode(nextInstruction);

                                instructionToNodeMap.Add(nextInstruction, nextNode);
                                instructionNode.ChildNodes.Add(nextNode);

                                instructionNode = nextNode;
                            }
                            else
                            {
                                instructionNode.ChildNodes.Add(nextNode);
                            }
                            
                        }
                        break;

                        case FlowControl.Phi:
                            throw new NotImplementedException();

                        case FlowControl.Return:
                        case FlowControl.Throw:
                        {
                            instructionNode = null;
                        }
                        break;
                    }
                }
            }
        }

        public InstructionNode(Instruction instruction)
        {
            ChildNodes = new HashSet<InstructionNode>();
            Instruction = instruction;
        }

        public HashSet<InstructionNode> ChildNodes { get; }

        public Instruction Instruction { get; }

        public bool Contains(InstructionNode instructionNode)
        {
            var children = ChildNodes;

            // Special-case the common scenario of only one child
            // since that means the next sequence can't branch and
            // we don't actually have to start recursing yet. This
            // ensures we don't stack overflow for long methods
            // with long sequences of non-branching code.

            while (ChildNodes.Count == 1)
            {
                var childNode = ChildNodes.Single();

                if (childNode == instructionNode)
                {
                    return true;
                }

                children = childNode.ChildNodes;
            }

            foreach (var childNode in children)
            {
                if ((childNode == instructionNode) || childNode.Contains(instructionNode))
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            return Instruction.ToString();
        }
    }
}
