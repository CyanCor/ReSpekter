// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DirtyModifier.cs" company="CyanCor GmbH">
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
//   Defines the IDirty type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace CyanCor.ReSpekter.Modifiers
{
    using System;
    using System.Linq;
    using Mono.Cecil;

    public interface IResolvableType<T> 
    {
        T UniqueIdentifier { get; }
    }

    public interface ITypeResolver
    {
        TT ResolveType<TT, TI>(TI identifer) where TT : class, IResolvableType<TI>;
    }

    public class LazyCompositionModifier : BaseModifier
    {
        bool CheckInterface(TypeDefinition type, Type iface)
        {
            if (type.BaseType != null)
            {
                if (CheckInterface(type.BaseType.Resolve(), iface))
                {
                    return true;
                }
            }

            return type.Interfaces.Any(reference => reference.FullName.Equals(iface.FullName));
        }

        protected override void Visit(TypeDefinition type)
        {
            if (CheckInterface(type, typeof(ITypeResolver)))
            {
                base.Visit(type);
            }
        }

        protected override void Visit(PropertyDefinition property)
        {
            base.Visit(property);
        }

        private MethodReference FindMethod(TypeDefinition type, string name)
        {
            var reference = type.Methods.FirstOrDefault(definition => definition.Name.Equals(name));
            if (reference == null)
            {
                return FindMethod(type.BaseType.Resolve(), name);
            }

            return reference;
        }
    }
}
