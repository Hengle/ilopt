// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using Mono.Cecil;

namespace IlOptimizer
{
    /// <summary>Provides methods for stripping the 'init' flag from the '.locals' directive for a method.</summary>
    public static class StripLocalsInit
    {
        public static bool Optimize(MethodDefinition method)
        {
            if (method.HasBody && method.Body.InitLocals)
            {
                method.Body.InitLocals = false;
                return true;
            }

            return false;
        }
    }
}
