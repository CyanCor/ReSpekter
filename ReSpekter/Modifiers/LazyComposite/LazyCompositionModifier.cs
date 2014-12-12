// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LazyCompositionModifier.cs" company="CyanCor GmbH">
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

using CyanCor.ReSpekter.Helper;

namespace CyanCor.ReSpekter.Modifiers
{
    using System;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public class LazyCompositionModifier : BaseModifier
    {
        protected override void Visit(TypeDefinition type)
        {
            // Check if type derives from ITypeResolver - modifier is only applied to those.
            if (type.HasInterface(typeof(ITypeResolver)))
            {
                base.Visit(type);
            }
        }

        private bool ValidateProperty(PropertyDefinition property)
        {
            return property.PropertyType.HasInterface(typeof(IResolvableType<>));
        }

        protected override void Visit(PropertyDefinition property)
        {
            // Check if property has a supported type.
            if (!ValidateProperty(property))
            {
                return;
            }

            var transformator = new TemplatePropertyTransformator();
            var propType = property.PropertyType.Resolve();
            var identifierType = propType.FindProperty("UniqueIdentifier").PropertyType;
            transformator.AddTransformation(typeof(ReferenceIdentifierTemplate), identifierType);
            transformator.DuplicateTemplate(() => new LazyCompositionTemplate().TemplateProperty, property);
        }

    }

    internal class LazyCompositionTemplate : ITypeResolver
    {
        public ReferenceIdentifierTemplate UniqueIdentifier;
        private WeakReference _weakReference;

        public ResolvableTypeTemplate TemplateProperty
        {
            get
            {
                if (_weakReference == null)
                {
                    _weakReference = new WeakReference(null);
                }

                var obj = _weakReference.Target as ResolvableTypeTemplate;
                if (obj != null)
                {
                    return obj;
                }

                obj = ResolveType<ResolvableTypeTemplate, ReferenceIdentifierTemplate>(UniqueIdentifier);
                if (obj != null)
                {
                    UniqueIdentifier = obj.UniqueIdentifier;
                }

                _weakReference.Target = obj;
                return obj;
            }

            set
            {
                if (_weakReference == null)
                {
                    _weakReference = new WeakReference(value);
                }
                else
                {
                    _weakReference.Target = value;
                }

                if (value != null)
                {
                    UniqueIdentifier = value.UniqueIdentifier;
                }
                else
                {
                    UniqueIdentifier = null;
                }
            }
        }

        public TT ResolveType<TT, TI>(TI identifier) where TT : class, IResolvableType<TI>
        {
            return null;
        }
    }

    internal class ReferenceIdentifierTemplate
    {
    }

    internal class ResolvableTypeTemplate : IResolvableType<ReferenceIdentifierTemplate>
    {
        public ReferenceIdentifierTemplate UniqueIdentifier { get; private set; }

        public string TestMember;
    }
}
