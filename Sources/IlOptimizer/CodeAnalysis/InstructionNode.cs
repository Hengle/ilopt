// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil.Cil;

namespace IlOptimizer.CodeAnalysis
{
    // This is a node which defines a basic block of instructions, for use in a control flow graph.

    public class InstructionNode
    {
        #region Constructors
        public InstructionNode(ImmutableArray<Instruction> instructions, ImmutableArray<InstructionNode> childNodes, bool isSelfReferencing)
        {
            if (isSelfReferencing && (childNodes.Contains(this) == false))
            {
                childNodes = childNodes.Add(this);
            }

            ChildNodes = childNodes;
            Instructions = instructions;
        }
        #endregion

        #region Properties
        public ImmutableArray<InstructionNode> ChildNodes { get; private set; }

        public ImmutableArray<Instruction> Instructions { get; private set; }

        public bool ContainsCyclicNodes
        {
            get
            {
                return IsCyclic || ChildNodes.Any((childNode) => childNode.ContainsCyclicNodes);
            }
        }

        public bool IsCyclic
        {
            get
            {
                return Contains(this, recurse: true);
            }
        }

        public bool IsLeafNode
        {
            get
            {
                return ChildNodes.IsEmpty;
            }
        }

        public bool IsSelfReferencing
        {
            get
            {
                return Contains(this, recurse: false);
            }
        }
        #endregion

        #region Properties
        public bool Contains(Instruction instruction, bool recurse = false)
        {
            return Instructions.Contains(instruction)
                || (recurse && ChildNodes.Any((childNode) => childNode.Contains(instruction, recurse)));
        }

        public bool Contains(InstructionNode instructionNode, bool recurse = false)
        {
            return ChildNodes.Any((childNode) => (childNode == instructionNode)
                                              || (recurse && (childNode != this) && childNode.Contains(instructionNode, recurse)));
        }
        #endregion

        #region Static Methods
        public static InstructionNode Create(Instruction instruction)
        {
            var instructionNodeMap = new Dictionary<Instruction, Builder>();
            var firstInstructionNodeMap = new Dictionary<Instruction, Builder>();

            var builder = Builder.Create(instruction, instructionNodeMap, firstInstructionNodeMap);
            return builder.ToInstructionNode();
        }
        #endregion

        #region System.Object Methods
        public override string ToString()
        {
            if (Instructions.Length == 1)
            {
                return $"({Instructions.Single()})";
            }
            else
            {
                return $"({Instructions.First()}, {Instructions.Last()})";
            }
        }
        #endregion

        #region Structs
        private sealed class Builder
        {
            #region Constructors
            public Builder()
            {
                ChildBuilders = ImmutableArray.CreateBuilder<Builder>();
                InstructionsBuilder = ImmutableArray.CreateBuilder<Instruction>();
            }
            #endregion

            #region Properties
            public ImmutableArray<Builder>.Builder ChildBuilders { get; }

            public ImmutableArray<Instruction>.Builder InstructionsBuilder { get; }
            #endregion

            #region Static Methods
            public static Builder Create(Instruction instruction, Dictionary<Instruction, Builder> instructionNodeMap, Dictionary<Instruction, Builder> firstInstructionNodeMap)
            {
                Debug.Assert(instruction != null);
                Debug.Assert(instructionNodeMap != null);
                Debug.Assert(firstInstructionNodeMap != null);

                var builder = GetExistingBuilder(instruction, instructionNodeMap, firstInstructionNodeMap);

                if (builder != null)
                {
                    return builder;
                }

                builder = new Builder();

                AddInstruction(builder.InstructionsBuilder, instruction);
                firstInstructionNodeMap.Add(instruction, builder);
                instructionNodeMap.Add(instruction, builder);

                do
                {
                    var opCode = instruction.OpCode;

                    switch (opCode.FlowControl)
                    {
                        case FlowControl.Branch:
                        {
                            // Unconditional branches specify the next instruction as their operand.
                            // We also don't need to process the instruction at the next logical offset
                            // since it will never be hit anyways.

                            var childBuilder = Create((Instruction)(instruction.Operand), instructionNodeMap, firstInstructionNodeMap);

                            var existingBuilder = instructionNodeMap[instruction];
                            AddChildBuilder(existingBuilder.ChildBuilders, childBuilder);

                            instruction = null;
                            break;
                        }

                        case FlowControl.Cond_Branch:
                        {
                            if (opCode.Code == Code.Switch)
                            {
                                // Switch statements are special in that they can have more than two
                                // child nodes. We need to process each of the normal nodes plus the
                                // instruction at the next logical offset.

                                var nextInstructions = (Instruction[])(instruction.Operand);

                                foreach (var nextInstruction in nextInstructions)
                                {
                                    var childBuilder = Create(nextInstruction, instructionNodeMap, firstInstructionNodeMap);

                                    var parentBuilder = instructionNodeMap[instruction];
                                    AddChildBuilder(parentBuilder.ChildBuilders, childBuilder);
                                }
                            }
                            else
                            {
                                // Conditional branches should always have two children. One child will
                                // be the target instruction if the branch is taken and the other will
                                // be the instruction at the next logical offset.

                                var childBuilder = Create((Instruction)(instruction.Operand), instructionNodeMap, firstInstructionNodeMap);

                                var parentBuilder = instructionNodeMap[instruction];
                                AddChildBuilder(parentBuilder.ChildBuilders, childBuilder);
                            }

                            // We need to add the next logical instruction as well, for when none of the
                            // branch conditions are met. However, we want to add this as a child node and
                            // then do no further processing.

                            var nextBuilder = Create(instruction.Next, instructionNodeMap, firstInstructionNodeMap);

                            var existingBuilder = instructionNodeMap[instruction];
                            AddChildBuilder(existingBuilder.ChildBuilders, nextBuilder);

                            instruction = null;
                            break;
                        }

                        case FlowControl.Meta:
                        {
                            // Meta nodes generally provide additional information to the instruction at
                            // the next logical offset. We just want to treat them as `Next` for most cases.

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
                            // Break, Call, and Next instructions just transfer control to the instruction
                            // at the next logical offset, resulting in a linear flow.

                            instruction = ProcessInstruction(instruction.Next, builder, instructionNodeMap, firstInstructionNodeMap);
                        }
                        break;

                        case FlowControl.Phi:
                            throw new NotImplementedException();

                        case FlowControl.Return:
                        case FlowControl.Throw:
                        {
                            // Return and Throw instructions terminate the sequence and require no further processing.
                            instruction = null;
                            break;
                        }
                    }
                }
                while (instruction != null);

                return builder;
            }

            private static void AddChildBuilder(ImmutableArray<Builder>.Builder childBuilders, Builder builder)
            {
                if (childBuilders.Contains(builder) == false)
                {
                    childBuilders.Add(builder);
                }
            }

            private static void AddInstruction(ImmutableArray<Instruction>.Builder instructionsBuilder, Instruction instruction)
            {
                if (instructionsBuilder.Contains(instruction) == false)
                {
                    instructionsBuilder.Add(instruction);
                }
            }

            private static void AddInstructionNode(ImmutableArray<InstructionNode>.Builder instructionNodeBuilder, InstructionNode instructionNode)
            {
                if (instructionNodeBuilder.Contains(instructionNode) == false)
                {
                    instructionNodeBuilder.Add(instructionNode);
                }
            }

            private static Builder GetExistingBuilder(Instruction instruction, Dictionary<Instruction, Builder> instructionNodeMap, Dictionary<Instruction, Builder> firstInstructionNodeMap)
            {
                if (firstInstructionNodeMap.TryGetValue(instruction, out var existingBuilder))
                {
                    // This is already the "entry-point" for a non-branching instruction sequence
                    // So we don't need to do anything and can just return the existing builder.

                    Debug.Assert(instructionNodeMap.ContainsKey(instruction));
                }
                else if (instructionNodeMap.TryGetValue(instruction, out existingBuilder))
                {
                    // We have already inserted this instruction into an existing builder but it
                    // is not an "entry-point". We need to create a new builder where this
                    // instruction is the "entry-point" and insert it as the only child of the
                    // previous builder the instruction was a member of.

                    var insertedBuilder = new Builder();

                    var existingInstructionsBuilder = existingBuilder.InstructionsBuilder;
                    var existingChildNodesBuilders = existingBuilder.ChildBuilders;

                    var instructionIndex = existingInstructionsBuilder.IndexOf(instruction);
                    var existingInstructionsBuilderCount = existingInstructionsBuilder.Count;

                    for (var index = instructionIndex; index < existingInstructionsBuilderCount; index++)
                    {
                        var existingInstruction = existingInstructionsBuilder[instructionIndex];
                        AddInstruction(insertedBuilder.InstructionsBuilder, existingInstruction);

                        existingInstructionsBuilder.RemoveAt(instructionIndex);
                        instructionNodeMap[existingInstruction] = insertedBuilder;
                    }

                    foreach (var childBuilder in existingBuilder.ChildBuilders)
                    {
                        AddChildBuilder(insertedBuilder.ChildBuilders, childBuilder);
                    }

                    existingChildNodesBuilders.Clear();
                    AddChildBuilder(existingChildNodesBuilders, insertedBuilder);

                    firstInstructionNodeMap.Add(instruction, insertedBuilder);
                    existingBuilder = insertedBuilder;
                }

                return existingBuilder;
            }

            private static Instruction ProcessInstruction(Instruction instruction, Builder builder, Dictionary<Instruction, Builder> instructionNodeMap, Dictionary<Instruction, Builder> firstInstructionNodeMap)
            {
                var existingBuilder = GetExistingBuilder(instruction, instructionNodeMap, firstInstructionNodeMap);

                if (existingBuilder != null)
                {
                    // The instruction has already been processed and is part of an existing builder
                    // So we just need to add it as a child of the current build and can return null
                    // to indicate no further processing should be done.

                    AddChildBuilder(builder.ChildBuilders, existingBuilder);
                    return null;
                }

                // Otherwise, we need to add the instruction to the current builder, and to the
                // instructionNodeMap and then return the instruction so that further processing
                // is done.

                AddInstruction(builder.InstructionsBuilder, instruction);
                instructionNodeMap.Add(instruction, builder);
                return instruction;
            }
            #endregion

            #region Methods
            public InstructionNode ToInstructionNode()
            {
                var childBuildersMap = new Dictionary<Builder, InstructionNode>();
                var cyclicReferences = new Dictionary<Builder, HashSet<Builder>>();

                var instructionNode = ToInstructionNode(childBuildersMap, cyclicReferences);

                foreach (var builder in cyclicReferences.Keys)
                {
                    var childBuilders = cyclicReferences[builder];
                    var fixupNode = childBuildersMap[builder];

                    foreach (var childNode in childBuildersMap.Where((childBuildersMapEntry) => childBuilders.Any((childBuilder) => (childBuilder == childBuildersMapEntry.Key)))
                                                              .Select((childBuilderMapEntry) => childBuilderMapEntry.Value))
                    {
                        if (fixupNode.ChildNodes.Contains(childNode) == false)
                        {
                            fixupNode.ChildNodes = fixupNode.ChildNodes.Add(childNode);
                        }
                    }
                }

                return instructionNode;
            }

            private InstructionNode ToInstructionNode(Dictionary<Builder, InstructionNode> childBuildersMap, Dictionary<Builder, HashSet<Builder>> cyclicReferences)
            {
                var isSelfReferencing = false;

                return new InstructionNode(
                   InstructionsBuilder.ToImmutableArray(),
                   GetChildNodes(childBuildersMap, cyclicReferences, ref isSelfReferencing),
                   isSelfReferencing
                );
            }

            private ImmutableArray<InstructionNode> GetChildNodes(Dictionary<Builder, InstructionNode> childBuildersMap, Dictionary<Builder, HashSet<Builder>> cyclicReferences, ref bool isSelfReferencing)
            {
                var childNodesBuilder = ImmutableArray.CreateBuilder<InstructionNode>();

                foreach (var childBuilder in ChildBuilders)
                {
                    if (childBuilder == this)
                    {
                        isSelfReferencing = true;
                    }
                    else
                    {
                        if (!childBuildersMap.TryGetValue(childBuilder, out var instructionNode))
                        {
                            childBuildersMap.Add(childBuilder, null);
                            instructionNode = childBuilder.ToInstructionNode(childBuildersMap, cyclicReferences);

                            childBuildersMap[childBuilder] = instructionNode;
                            AddInstructionNode(childNodesBuilder, instructionNode);
                            childNodesBuilder.Add(instructionNode);
                        }
                        else if (instructionNode == null)
                        {
                            if (!cyclicReferences.TryGetValue(this, out var childBuilders))
                            {
                                childBuilders = new HashSet<Builder>();
                                cyclicReferences.Add(this, childBuilders);
                            }

                            childBuilders.Add(childBuilder);
                        }
                        else
                        {
                            AddInstructionNode(childNodesBuilder, instructionNode);
                        }
                    }
                }

                return childNodesBuilder.ToImmutableArray();
            }
            #endregion
        }
        #endregion
    }
}
