// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ModuleManager.cs" company="CyanCor GmbH">
//   Copyright (c) 2014 CyanCor GmbH
//   
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//   
//   http://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and limitations under the License.
// </copyright>
// <summary>
//   Manages all <see cref="ModuleDefinition" />s for this context.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReSpekter
{
    using System;
    using System.Collections.Generic;
    using Exception;
    using Mono.Cecil;

    /// <summary>
    /// Manages all <see cref="ModuleDefinition"/>s for this context.
    /// </summary>
    internal class ModuleManager
    {
        /// <summary>
        /// The assembly cache
        /// </summary>
        private readonly AssemblyCache _assemblyCache = AssemblyCache.Instance;

        /// <summary>
        /// The type lookup table.
        /// </summary>
        private readonly Dictionary<string, TypeDefinition> _types = new Dictionary<string, TypeDefinition>();

        /// <summary>
        /// The context for this manager.
        /// </summary>
        private readonly Context _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleManager"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public ModuleManager(Context context)
        {
            _context = context;
        }

        /// <summary>
        /// Resolves the specified type in the context.
        /// </summary>
        /// <param name="originalType">
        /// The type to look up.
        /// </param>
        /// <returns>
        /// the resolved type.
        /// </returns>
        /// <exception cref="TypeNotFoundException">
        /// thrown if the type cannot be resolved.
        /// </exception>
        public TypeDefinition ResolveType(Type originalType)
        {
            return ResolveType(originalType.FullName, originalType.Assembly.FullName, originalType.Assembly.Location);
        }

        /// <summary>
        /// Resolves the specified type in the context.
        /// </summary>
        /// <param name="original">
        /// The reference to the type to look up.
        /// </param>
        /// <returns>
        /// the resolved type.
        /// </returns>
        /// <exception cref="TypeNotFoundException">
        /// thrown if the type cannot be resolved.
        /// </exception>
        public TypeDefinition ResolveType(TypeReference original)
        {
            return ResolveType(original.FullName, original.Scope.Name);
        }

        /// <summary>
        /// Resolves the specified type in the context.
        /// </summary>
        /// <param name="fullName">The fully qualified name for the type.</param>
        /// <param name="assemblyName">The assembly to search the original type in.</param>
        /// <param name="assemblyLocation">[optional] The assembly location if known.</param>
        /// <returns>the resolved type.</returns>
        /// <exception cref="TypeNotFoundException">thrown if the type cannot be resolved.</exception>
        private TypeDefinition ResolveType(string fullName, string assemblyName, string assemblyLocation = null)
        {
            var assembly = _assemblyCache.GetAssembly(assemblyName, assemblyLocation);

            TypeDefinition typeReference;
            if (_types.TryGetValue(fullName, out typeReference))
            {
                return typeReference;
            }

            foreach (var module in assembly.Modules)
            {
                var type = module.GetType(fullName);
                if (type != null)
                {
                    var newType = _context.FilterHost.Process(type);
                    _types.Add(fullName, newType);
                    return newType;
                }
            }

            throw new TypeNotFoundException(string.Format("Unable to resolve type '{0}' from assembly definition.", fullName));
        }
    }
}