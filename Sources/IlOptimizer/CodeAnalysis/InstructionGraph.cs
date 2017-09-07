// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using Mono.Cecil.Cil;

namespace IlOptimizer.CodeAnalysis
{
    // This is a control flow graph where each node defines a basic block of instructions.

    public class InstructionGraph
    {
        #region Constructors
        public InstructionGraph(MethodBody methodBody)
        {
            if (methodBody is null)
            {
                throw new ArgumentNullException(nameof(methodBody));
            }

            RootNode = InstructionNode.Create(methodBody.Instructions[0]);
        }
        #endregion

        #region Properties
        public MethodBody MethodBody { get; }

        public InstructionNode RootNode { get; }
        #endregion
    }
}
