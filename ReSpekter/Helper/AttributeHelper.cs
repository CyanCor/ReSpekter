// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AttributeHelper.cs" company="CyanCor GmbH">
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
//   Defines the AttributeHelper type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Mono.Cecil;

namespace CyanCor.ReSpekter.Helper
{
    static class AttributeHelper
    {
        public static CustomAttribute Duplicate(this CustomAttribute attribute, ModuleDefinition module, ResolveOperandDelegate resolver)
        {
            var newAttr = new CustomAttribute((MethodReference)resolver(module.Import(attribute.Constructor), null, null));

            foreach (var arg in attribute.ConstructorArguments)
            {
                var value = resolver( arg.Value,null,null);
                var type = module.Import((TypeReference)resolver(arg.Type, null, null));
                newAttr.ConstructorArguments.Add((CustomAttributeArgument)resolver(new CustomAttributeArgument(type, value), null, null));
            }

            return newAttr;
        }
    }
}
