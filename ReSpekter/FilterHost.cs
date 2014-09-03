// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FilterHost.cs" company="CyanCor GmbH">
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
//   Defines the FilterHost type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReSpekter
{
    using Filters;
    using Mono.Cecil;

    /// <summary>
    /// The filter host contains all filter chains.
    /// </summary>
    public class FilterHost
    {
        /// <summary>
        /// The associated context.
        /// </summary>
        private readonly Context _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterHost"/> class.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        internal FilterHost(Context context)
        {
            _context = context;
            TypeFilter = new FilterChain<TypeDefinition> { new TypeCloneFilter() };
            MemberFilter = new FilterChain<TypeDefinition> { new MemberCloneFilter() };
        }

        /// <summary>
        /// Gets the type filter chain.
        /// </summary>
        /// <value>
        /// The type filter chain.
        /// </value>
        public FilterChain<TypeDefinition> TypeFilter { get; private set; }

        /// <summary>
        /// Gets the member filter chain.
        /// </summary>
        /// <value>
        /// The member filter chain.
        /// </value>
        public FilterChain<TypeDefinition> MemberFilter { get; private set; }

        /// <summary>
        /// Creates the specified type in the context.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The filtered type.</returns>
        public TypeDefinition CreateType(TypeDefinition type)
        {
            if (type.Scope.Name.Equals("CommonLanguageRuntimeLibrary"))
            {
                return type;
            }

            return TypeFilter.Process(null, type, _context);
        }

        /// <summary>
        /// Processes the specified type.
        /// </summary>
        /// <param name="stage">
        /// The stage.
        /// </param>
        /// <param name="original">
        /// The original.
        /// </param>
        /// <returns>
        /// The filtered type.
        /// </returns>
        public TypeDefinition CreateFunctions(TypeDefinition stage, TypeDefinition original)
        {
            return MemberFilter.Process(stage, original, _context);
        }
    }
}
