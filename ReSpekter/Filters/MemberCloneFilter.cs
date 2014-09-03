// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MemberCloneFilter.cs" company="CyanCor GmbH">
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
//   The type cloning filter.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReSpekter.Filters
{
    using Mono.Cecil;

    /// <summary>
    /// The type cloning filter.
    /// </summary>
    public class MemberCloneFilter : IFilter<TypeDefinition>
    {
        /// <summary>
        /// Clones the specified types members.
        /// </summary>
        /// <param name="stage">The current processed element.</param>
        /// <param name="original">The original.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// The cloned type.
        /// </returns>
        public TypeDefinition Process(TypeDefinition stage, TypeDefinition original, Context context)
        {
            foreach (var method in original.Methods)
            {
                var methodDefinition = new MethodDefinition(method.Name, method.Attributes, context.ResolveType(method.ReturnType));
                foreach (var parameterDefinition in method.Parameters)
                {
                    methodDefinition.Parameters.Add(new ParameterDefinition(parameterDefinition.Name, parameterDefinition.Attributes, context.ResolveType(parameterDefinition.ParameterType)));
                }

                stage.Methods.Add(methodDefinition);
            }

            return stage;
        }
    }
}