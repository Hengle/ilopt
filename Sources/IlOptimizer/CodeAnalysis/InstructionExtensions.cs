// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using Mono.Cecil.Cil;

namespace IlOptimizer.CodeAnalysis
{
    public static class InstructionExtensions
    {
        public static int GetLoadLocalIndex(this Instruction instruction)
        {
            var code = instruction.OpCode.Code;

            switch (code)
            {
                case Code.Ldloc_0:
                    return 0;

                case Code.Ldloc_1:
                    return 1;

                case Code.Ldloc_2:
                    return 2;

                case Code.Ldloc_3:
                    return 3;

                case Code.Ldloca_S:
                case Code.Ldloc:
                case Code.Ldloca:
                    return ((VariableDefinition)(instruction.Operand)).Index;

                default:
                    throw new InvalidOperationException();
            }
        }

        public static int GetStoreLocalIndex(this Instruction instruction)
        {
            var code = instruction.OpCode.Code;

            switch (code)
            {
                case Code.Stloc_0:
                    return 0;

                case Code.Stloc_1:
                    return 1;

                case Code.Stloc_2:
                    return 2;

                case Code.Stloc_3:
                    return 3;

                case Code.Stloc_S:
                case Code.Stloc:
                    return ((VariableDefinition)(instruction.Operand)).Index;

                default:
                    throw new InvalidOperationException();
            }
        }

        public static bool IsBranchInstruction(this Instruction instruction)
        {
            var flowControl = instruction.OpCode.FlowControl;
            return (flowControl == FlowControl.Cond_Branch) || (flowControl == FlowControl.Branch);
        }

        public static bool IsLoadLocalInstruction(this Instruction instruction)
        {
            var code = instruction.OpCode.Code;
            return (code == Code.Ldloc_0)
                || (code == Code.Ldloc_1)
                || (code == Code.Ldloc_2)
                || (code == Code.Ldloc_3)
                || (code == Code.Ldloc_S)
                || (code == Code.Ldloc)
                || (code == Code.Ldloca_S)
                || (code == Code.Ldloca);
        }

        public static bool IsStoreLocalInstruction(this Instruction instruction)
        {
            var code = instruction.OpCode.Code;
            return (code == Code.Stloc_0)
                || (code == Code.Stloc_1)
                || (code == Code.Stloc_2)
                || (code == Code.Stloc_3)
                || (code == Code.Stloc_S)
                || (code == Code.Stloc);
        }
    }
}
