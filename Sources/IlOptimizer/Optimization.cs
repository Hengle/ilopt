// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using Mono.Cecil;

namespace IlOptimizer
{
    /// <summary>Defines an optimization that can be performed by the tool.</summary>
    public sealed class Optimization
    {
        /// <summary>Initializes a new instance of the <see cref="Optimization" /> struct.</summary>
        /// <param name="name">The name of the optimization.</param>
        /// <param name="description">The description of the optimization.</param>
        /// <param name="optimizeMethod">The method to use when performing the optimization.</param>
        public Optimization(string name, string description, Func<MethodDefinition, bool> optimizeMethod)
        {
            Name = name;
            Description = description;
            OptimizeMethod = optimizeMethod;
        }

        /// <summary>The name of the optimization.</summary>
        public string Name { get; }

        /// <summary>The description of the optimization.</summary>
        public string Description { get; }

        /// <summary>The method to use when performing the optimization.</summary>
        public Func<MethodDefinition, bool> OptimizeMethod { get; }

        /// <summary>Gets or sets the number of methods that were updated by the optimization.</summary>
        public int UpdatedMethodCount { get; set; }
    }
}
