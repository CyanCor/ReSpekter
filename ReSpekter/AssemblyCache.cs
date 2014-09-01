// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AssemblyCache.cs" company="CyanCor GmbH">
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
//   Defines the AssemblyCache type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReSpekter
{
    using System.Collections.Generic;

    using Mono.Cecil;

    /// <summary>
    /// Caches Cecil AssemblyDefinitions.
    /// </summary>
    internal class AssemblyCache
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        private static readonly AssemblyCache _instance = new AssemblyCache();

        /// <summary>
        /// The assembly resolver.
        /// </summary>
        private readonly DefaultAssemblyResolver _assemblyResolver = new DefaultAssemblyResolver();

        /// <summary>
        /// The dictionary for keeping the AssemblyDefinitions.
        /// </summary>
        private readonly Dictionary<string, AssemblyDefinition> _assemblyDefinitions = new Dictionary<string, AssemblyDefinition>();

        /// <summary>
        /// Prevents a default instance of the <see cref="AssemblyCache"/> class from being created.
        /// </summary>
        private AssemblyCache()
        {
        }

        /// <summary>
        /// Gets the assembly cache instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static AssemblyCache Instance
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// Gets the AssemblyDefinition for the specified assembly.
        /// </summary>
        /// <param name="fullName">
        /// The assembly full name to get the Cecil definition for.
        /// </param>
        /// <param name="location">
        /// [optional] The location of the assembly if known. If this is not provided,
        /// the assembly cache will try to resolve the assembly.
        /// </param>
        /// <returns>
        /// The AssemblyDefinition.
        /// </returns>
        public AssemblyDefinition GetAssembly(string fullName, string location = null)
        {
            AssemblyDefinition definition;
            if (TryGetAssembly(fullName, out definition))
            {
                return definition;
            }

            if (location != null)
            {
                definition = AssemblyDefinition.ReadAssembly(location);
            }
            else
            {
                definition = _assemblyResolver.Resolve(fullName);
            }

            _assemblyDefinitions.Add(fullName, definition);

            return definition;
        }

        /// <summary>
        /// Tries the get assembly from the cache.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="assemblyDefinition">The assembly definition.</param>
        /// <returns><c>true</c> if success; <c>false</c> if not.</returns>
        private bool TryGetAssembly(string name, out AssemblyDefinition assemblyDefinition)
        {
            if (_assemblyDefinitions.TryGetValue(name, out assemblyDefinition))
            {
                return true;
            }

            foreach (var value in _assemblyDefinitions.Values)
            {
                if (value.MainModule.Name.Equals(name))
                {
                    assemblyDefinition = value;
                    return true;
                }
            }

            return false;
        }
    }
}
