// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using Mono.Cecil.Cil;

namespace IlOptimizer.CodeAnalysis
{
    public static class InstructionExtensions
    {
        private static readonly InstructionFamily[] InstructionFamilies = {
            InstructionFamily.Nop,            // Nop
            InstructionFamily.Break,          // Break
            InstructionFamily.Ldarg,          // Ldarg_0
            InstructionFamily.Ldarg,          // Ldarg_1
            InstructionFamily.Ldarg,          // Ldarg_2
            InstructionFamily.Ldarg,          // Ldarg_3
            InstructionFamily.Ldloc,          // Ldloc_0
            InstructionFamily.Ldloc,          // Ldloc_1
            InstructionFamily.Ldloc,          // Ldloc_2
            InstructionFamily.Ldloc,          // Ldloc_3
            InstructionFamily.Stloc,          // Stloc_0
            InstructionFamily.Stloc,          // Stloc_1
            InstructionFamily.Stloc,          // Stloc_2
            InstructionFamily.Stloc,          // Stloc_3
            InstructionFamily.Ldarg,          // Ldarg_S
            InstructionFamily.Ldarga,         // Ldarga_S
            InstructionFamily.Starg,          // Starg_S
            InstructionFamily.Ldloc,          // Ldloc_S
            InstructionFamily.Ldloca,         // Ldloca_S
            InstructionFamily.Stloc,          // Stloc_S
            InstructionFamily.Ldnull,         // Ldnull
            InstructionFamily.Ldc,            // Ldc_I4_M1
            InstructionFamily.Ldc,            // Ldc_I4_0
            InstructionFamily.Ldc,            // Ldc_I4_1
            InstructionFamily.Ldc,            // Ldc_I4_2
            InstructionFamily.Ldc,            // Ldc_I4_3
            InstructionFamily.Ldc,            // Ldc_I4_4
            InstructionFamily.Ldc,            // Ldc_I4_5
            InstructionFamily.Ldc,            // Ldc_I4_6
            InstructionFamily.Ldc,            // Ldc_I4_7
            InstructionFamily.Ldc,            // Ldc_I4_8
            InstructionFamily.Ldc,            // Ldc_I4_S
            InstructionFamily.Ldc,            // Ldc_I4
            InstructionFamily.Ldc,            // Ldc_I8
            InstructionFamily.Ldc,            // Ldc_R4
            InstructionFamily.Ldc,            // Ldc_R8
            InstructionFamily.Dup,            // Dup
            InstructionFamily.Pop,            // Pop
            InstructionFamily.Jmp,            // Jmp
            InstructionFamily.Call,           // Call
            InstructionFamily.Calli,          // Calli
            InstructionFamily.Ret,            // Ret
            InstructionFamily.Br,             // Br_S
            InstructionFamily.Brfalse,        // Brfalse_S
            InstructionFamily.Brtrue,         // Brtrue_S
            InstructionFamily.Beq,            // Beq_S
            InstructionFamily.Bge,            // Bge_S
            InstructionFamily.Bgt,            // Bgt_S
            InstructionFamily.Ble,            // Ble_S
            InstructionFamily.Blt,            // Blt_S
            InstructionFamily.Bne,            // Bne_Un_S
            InstructionFamily.Bge,            // Bge_Un_S
            InstructionFamily.Bgt,            // Bgt_Un_S
            InstructionFamily.Ble,            // Ble_Un_S
            InstructionFamily.Blt,            // Blt_Un_S
            InstructionFamily.Br,             // Br
            InstructionFamily.Brfalse,        // Brfalse
            InstructionFamily.Brtrue,         // Brtrue
            InstructionFamily.Beq,            // Beq
            InstructionFamily.Bge,            // Bge
            InstructionFamily.Bgt,            // Bgt
            InstructionFamily.Ble,            // Ble
            InstructionFamily.Blt,            // Blt
            InstructionFamily.Bne,            // Bne_Un
            InstructionFamily.Bge,            // Bge_Un
            InstructionFamily.Bgt,            // Bgt_Un
            InstructionFamily.Ble,            // Ble_Un
            InstructionFamily.Blt,            // Blt_Un
            InstructionFamily.Switch,         // Switch
            InstructionFamily.Ldind,          // Ldind_I1
            InstructionFamily.Ldind,          // Ldind_U1
            InstructionFamily.Ldind,          // Ldind_I2
            InstructionFamily.Ldind,          // Ldind_U2
            InstructionFamily.Ldind,          // Ldind_I4
            InstructionFamily.Ldind,          // Ldind_U4
            InstructionFamily.Ldind,          // Ldind_I8
            InstructionFamily.Ldind,          // Ldind_I
            InstructionFamily.Ldind,          // Ldind_R4
            InstructionFamily.Ldind,          // Ldind_R8
            InstructionFamily.Ldind,          // Ldind_Ref
            InstructionFamily.Stind,          // Stind_Ref
            InstructionFamily.Stind,          // Stind_I1
            InstructionFamily.Stind,          // Stind_I2
            InstructionFamily.Stind,          // Stind_I4
            InstructionFamily.Stind,          // Stind_I8
            InstructionFamily.Stind,          // Stind_R4
            InstructionFamily.Stind,          // Stind_R8
            InstructionFamily.Add,            // Add
            InstructionFamily.Sub,            // Sub
            InstructionFamily.Mul,            // Mul
            InstructionFamily.Div,            // Div
            InstructionFamily.Div,            // Div_Un
            InstructionFamily.Rem,            // Rem
            InstructionFamily.Rem,            // Rem_Un
            InstructionFamily.And,            // And
            InstructionFamily.Or,             // Or
            InstructionFamily.Xor,            // Xor
            InstructionFamily.Shl,            // Shl
            InstructionFamily.Shr,            // Shr
            InstructionFamily.Shr,            // Shr_Un
            InstructionFamily.Neg,            // Neg
            InstructionFamily.Not,            // Not
            InstructionFamily.Conv,           // Conv_I1
            InstructionFamily.Conv,           // Conv_I2
            InstructionFamily.Conv,           // Conv_I4
            InstructionFamily.Conv,           // Conv_I8
            InstructionFamily.Conv,           // Conv_R4
            InstructionFamily.Conv,           // Conv_R8
            InstructionFamily.Conv,           // Conv_U4
            InstructionFamily.Conv,           // Conv_U8
            InstructionFamily.Callvirt,       // Callvirt
            InstructionFamily.Cpobj,          // Cpobj
            InstructionFamily.Ldobj,          // Ldobj
            InstructionFamily.Ldstr,          // Ldstr
            InstructionFamily.Newobj,         // Newobj
            InstructionFamily.Castclass,      // Castclass
            InstructionFamily.Isinst,         // Isinst
            InstructionFamily.Conv,           // Conv_R_Un
            InstructionFamily.Unbox,          // Unbox
            InstructionFamily.Throw,          // Throw
            InstructionFamily.Ldfld,          // Ldfld
            InstructionFamily.Ldflda,         // Ldflda
            InstructionFamily.Stfld,          // Stfld
            InstructionFamily.Ldsfld,         // Ldsfld
            InstructionFamily.Ldsflda,        // Ldsflda
            InstructionFamily.Stsfld,         // Stsfld
            InstructionFamily.Stobj,          // Stobj
            InstructionFamily.Conv,           // Conv_Ovf_I1_Un
            InstructionFamily.Conv,           // Conv_Ovf_I2_Un
            InstructionFamily.Conv,           // Conv_Ovf_I4_Un
            InstructionFamily.Conv,           // Conv_Ovf_I8_Un
            InstructionFamily.Conv,           // Conv_Ovf_U1_Un
            InstructionFamily.Conv,           // Conv_Ovf_U2_Un
            InstructionFamily.Conv,           // Conv_Ovf_U4_Un
            InstructionFamily.Conv,           // Conv_Ovf_U8_Un
            InstructionFamily.Conv,           // Conv_Ovf_I_Un
            InstructionFamily.Conv,           // Conv_Ovf_U_Un
            InstructionFamily.Box,            // Box
            InstructionFamily.Newarr,         // Newarr
            InstructionFamily.Ldlen,          // Ldlen
            InstructionFamily.Ldelema,        // Ldelema
            InstructionFamily.Ldelem,         // Ldelem_I1
            InstructionFamily.Ldelem,         // Ldelem_U1
            InstructionFamily.Ldelem,         // Ldelem_I2
            InstructionFamily.Ldelem,         // Ldelem_U2
            InstructionFamily.Ldelem,         // Ldelem_I4
            InstructionFamily.Ldelem,         // Ldelem_U4
            InstructionFamily.Ldelem,         // Ldelem_I8
            InstructionFamily.Ldelem,         // Ldelem_I
            InstructionFamily.Ldelem,         // Ldelem_R4
            InstructionFamily.Ldelem,         // Ldelem_R8
            InstructionFamily.Ldelem,         // Ldelem_Ref
            InstructionFamily.Stelem,         // Stelem_I
            InstructionFamily.Stelem,         // Stelem_I1
            InstructionFamily.Stelem,         // Stelem_I2
            InstructionFamily.Stelem,         // Stelem_I4
            InstructionFamily.Stelem,         // Stelem_I8
            InstructionFamily.Stelem,         // Stelem_R4
            InstructionFamily.Stelem,         // Stelem_R8
            InstructionFamily.Stelem,         // Stelem_Ref
            InstructionFamily.Ldelem,         // Ldelem_Any
            InstructionFamily.Stelem,         // Stelem_Any
            InstructionFamily.Unbox,          // Unbox_Any
            InstructionFamily.Conv,           // Conv_Ovf_I1
            InstructionFamily.Conv,           // Conv_Ovf_U1
            InstructionFamily.Conv,           // Conv_Ovf_I2
            InstructionFamily.Conv,           // Conv_Ovf_U2
            InstructionFamily.Conv,           // Conv_Ovf_I4
            InstructionFamily.Conv,           // Conv_Ovf_U4
            InstructionFamily.Conv,           // Conv_Ovf_I8
            InstructionFamily.Conv,           // Conv_Ovf_U8
            InstructionFamily.Refanyval,      // Refanyval
            InstructionFamily.Ckfinite,       // Ckfinite
            InstructionFamily.Mkrefany,       // Mkrefany
            InstructionFamily.Ldtoken,        // Ldtoken
            InstructionFamily.Conv,           // Conv_U2
            InstructionFamily.Conv,           // Conv_U1
            InstructionFamily.Conv,           // Conv_I
            InstructionFamily.Conv,           // Conv_Ovf_I
            InstructionFamily.Conv,           // Conv_Ovf_U
            InstructionFamily.Add,            // Add_Ovf
            InstructionFamily.Add,            // Add_Ovf_Un
            InstructionFamily.Mul,            // Mul_Ovf
            InstructionFamily.Mul,            // Mul_Ovf_Un
            InstructionFamily.Sub,            // Sub_Ovf
            InstructionFamily.Sub,            // Sub_Ovf_Un
            InstructionFamily.Endfinally,     // Endfinally
            InstructionFamily.Leave,          // Leave
            InstructionFamily.Leave,          // Leave_S
            InstructionFamily.Stind,          // Stind_I
            InstructionFamily.Conv,           // Conv_U
            InstructionFamily.Arglist,        // Arglist
            InstructionFamily.Ceq,            // Ceq
            InstructionFamily.Cgt,            // Cgt
            InstructionFamily.Cgt,            // Cgt_Un
            InstructionFamily.Clt,            // Clt
            InstructionFamily.Clt,            // Clt_Un
            InstructionFamily.Ldftn,          // Ldftn
            InstructionFamily.Ldvirtftn,      // Ldvirtftn
            InstructionFamily.Ldarg,          // Ldarg
            InstructionFamily.Ldarga,         // Ldarga
            InstructionFamily.Starg,          // Starg
            InstructionFamily.Ldloc,          // Ldloc
            InstructionFamily.Ldloca,         // Ldloca
            InstructionFamily.Stloc,          // Stloc
            InstructionFamily.Localloc,       // Localloc
            InstructionFamily.Endfilter,      // Endfilter
            InstructionFamily.Unaligned,      // Unaligned
            InstructionFamily.Volatile,       // Volatile
            InstructionFamily.Tail,           // Tail
            InstructionFamily.Initobj,        // Initobj
            InstructionFamily.Constrained,    // Constrained
            InstructionFamily.Cpblk,          // Cpblk
            InstructionFamily.Initblk,        // Initblk
            InstructionFamily.No,             // No
            InstructionFamily.Rethrow,        // Rethrow
            InstructionFamily.Sizeof,         // Sizeof
            InstructionFamily.Refanytype,     // Refanytype
            InstructionFamily.Readonly,       // Readonly
        };

        public static Code GetCode(this Instruction instruction)
        {
            return instruction.OpCode.Code;
        }

        public static InstructionFamily GetInstructionFamily(this Instruction instruction)
        {
            return InstructionFamilies[(int)(instruction.OpCode.Code)];
        }

        public static VariableReference GetVariableForLdloc(this Instruction instruction, MethodBody methodBody)
        {
            if (instruction.GetInstructionFamily() != InstructionFamily.Ldloc)
            {
                throw new InvalidOperationException();
            }

            switch (instruction.GetCode())
            {
                case Code.Ldloc_0:
                    return methodBody.Variables[0];

                case Code.Ldloc_1:
                    return methodBody.Variables[1];

                case Code.Ldloc_2:
                    return methodBody.Variables[2];

                case Code.Ldloc_3:
                    return methodBody.Variables[3];

                default:
                    return (VariableReference)(instruction.Operand);
            }
        }

        public static VariableReference GetVariableForLdloca(this Instruction instruction)
        {
            if (instruction.GetInstructionFamily() != InstructionFamily.Ldloca)
            {
                throw new InvalidOperationException();
            }

            return (VariableReference)(instruction.Operand);
        }

        public static VariableReference GetVariableForStloc(this Instruction instruction, MethodBody methodBody)
        {
            if (instruction.GetInstructionFamily() != InstructionFamily.Stloc)
            {
                throw new InvalidOperationException();
            }

            switch (instruction.GetCode())
            {
                case Code.Stloc_0:
                    return methodBody.Variables[0];

                case Code.Stloc_1:
                    return methodBody.Variables[1];

                case Code.Stloc_2:
                    return methodBody.Variables[2];

                case Code.Stloc_3:
                    return methodBody.Variables[3];

                default:
                    return (VariableReference)(instruction.Operand);
            }
        }
    }
}
