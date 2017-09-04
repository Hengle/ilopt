// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using IlOptimizer.CodeAnalysis;
using Mono.Cecil;

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

                if (methodBody.InitLocals == false)
                {
                    return false;
                }

                methodBody.InitLocals = false;
                {
                    var rootNode = InstructionNode.CreateGraph(methodBody);

                    // TODO: We need to go through the instruction graph for each variable
                    // and determine whether or not the code naturally initializes it before
                    // its first access.
                }
                return true;
            }

            return false;
        }
    }
}
