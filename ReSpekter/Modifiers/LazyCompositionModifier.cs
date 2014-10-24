﻿// --------------------------------------------------------------------------------------------------------------------
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

using System.Windows.Markup;

namespace CyanCor.ReSpekter.Modifiers
{
    using System;
    using System.Linq;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;

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
            if (!ValidateProperty(property))
            {
                return;
            }

            var propType = property.PropertyType.Resolve();
            var identifierType = propType.FindProperty("UniqueIdentifier").PropertyType;

            var template = property.Module.Import(typeof(LazyCompositionTemplate)).Resolve();
            var templateProp = template.Properties.Single(definition => definition.Name.Equals("TemplateProperty"));

            var uniqueIdentifier = new FieldDefinition("_uniqueIdentifier" + property.Name, FieldAttributes.Private, identifierType);
            property.DeclaringType.Fields.Add(uniqueIdentifier);

            //var weakReferenceType = property.Module.Import(typeof(WeakReference<>)).MakeGenericInstanceType(propType);
            var weakReferenceType = property.Module.Import(typeof(WeakReference<>)).MakeGenericInstanceType(propType);
            var weakReference = new FieldDefinition("_weakReference" + property.Name, FieldAttributes.Private, weakReferenceType);
            property.DeclaringType.Fields.Add(weakReference);

            templateProp.GetMethod.Body.Duplicate(property.GetMethod.Body, (subject, instruction, processor) =>
            {
                var name = subject as string;
                switch (name)
                {
                    case "_templatePropertyUniqueIdentifier":
                        {
                            return uniqueIdentifier.Name;
                        }

                    case "_templatePropertyWeakReference":
                        {
                            return weakReference.Name;
                        }
                }

                var fieldDef = subject as FieldDefinition;
                if (fieldDef != null)
                {
                    switch (fieldDef.Name)
                    {
                        case "_templatePropertyUniqueIdentifier":
                        {
                            return uniqueIdentifier;
                        }

                        case "_templatePropertyWeakReference":
                        {
                            return weakReference;
                        }
                    }
                }


                var typeDef = subject as TypeReference;
                if (typeDef != null)
                {
                    switch (typeDef.Name)
                    {
                        case "LazyCompositionTemplate":
                        {
                            return property.DeclaringType;
                        }

                        case "ResolvableTypeTemplate":
                        {
                            return propType;
                        }
                    }
                }


                return subject;
            });

            templateProp.SetMethod.Body.Duplicate(property.SetMethod.Body, (subject, instruction, processor) =>
            {
                var name = subject as string;
                switch (name)
                {
                    case "_templatePropertyUniqueIdentifier":
                        {
                            return uniqueIdentifier.Name;
                        }

                    case "_templatePropertyWeakReference":
                        {
                            return weakReference.Name;
                        }
                }

                var fieldDef = subject as FieldDefinition;
                if (fieldDef != null)
                {
                    switch (fieldDef.Name)
                    {
                        case "_templatePropertyUniqueIdentifier":
                            {
                                return uniqueIdentifier;
                            }

                        case "_templatePropertyWeakReference":
                            {
                                return weakReference;
                            }
                    }
                }


                var typeDef = subject as TypeReference;
                if (typeDef != null)
                {
                    switch (typeDef.Name)
                    {
                        case "LazyCompositionTemplate":
                            {
                                return property.DeclaringType;
                            }

                        case "ResolvableTypeTemplate":
                            {
                                return propType;
                            }
                    }
                }

                return subject;
            });
            

            var getinstructions =
                property.GetMethod.Body.Instructions.Where(instruction => instruction.OpCode != OpCodes.Nop).ToArray();
            var setinstructions =
                property.SetMethod.Body.Instructions.Where(instruction => instruction.OpCode != OpCodes.Nop).ToArray();

            
            
            //if (CheckInterface(propType, typeof(IResolvableType<>)))
            {
                base.Visit(property);
            }
        }

        private bool ValidateProperty(PropertyDefinition property)
        {
            return property.PropertyType.HasInterface(typeof(IResolvableType<>));
        }

        private MethodReference FindMethod(TypeDefinition type, string name)
        {
            MethodReference reference = type.Methods.FirstOrDefault(definition => definition.Name.Equals(name));

            if (reference == null)
            {
                return FindMethod(type.BaseType.Resolve(), name);
            }

            return reference;
        }
    }

    internal class LazyCompositionTemplate : ITypeResolver
    {
        public string UniqueIdentifier { get; private set; }

        private string _templatePropertyUniqueIdentifier;
        private WeakReference<ResolvableTypeTemplate> _templatePropertyWeakReference = new WeakReference<ResolvableTypeTemplate>(null);

        public ResolvableTypeTemplate TemplateProperty
        {
            get
            {
                ResolvableTypeTemplate rtt;
                _templatePropertyWeakReference.TryGetTarget(out rtt);
                return rtt;
            }
            set
            {
                _templatePropertyWeakReference.SetTarget(value);
            }
        }

        /*{
            get
            {
                ResolvableTypeTemplate test;
                lock (_templatePropertyWeakReference)
                {
                    if (!_templatePropertyWeakReference.TryGetTarget(out test))
                    {
                        test = ResolveType<ResolvableTypeTemplate, string>(_templatePropertyUniqueIdentifier);
                        _templatePropertyWeakReference.SetTarget(test);
                    }
                }

                return test;
            }

            set
            {
                if (value != null)
                {
                    _templatePropertyUniqueIdentifier = value.UniqueIdentifier;
                    _templatePropertyWeakReference.SetTarget(value);
                }
            }
        }*/

        public TT ResolveType<TT, TI>(TI identifer) where TT : class, IResolvableType<TI>
        {
            return null;
        }
    }

    internal class ResolvableTypeTemplate : IResolvableType<string>
    {
        public string UniqueIdentifier { get; private set; }
    }
}
