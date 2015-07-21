// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TemplatePropertyTransformator.cs" company="CyanCor GmbH">
//   Copyright (c) 2015 CyanCor GmbH
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
//   Defines the TemplatePropertyTransformator type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace CyanCor.ReSpekter.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public class TemplatePropertyTransformator
    {
        private PropertyDefinition _templateProp;
        private TypeDefinition _template;
        private PropertyDefinition _target;

        private Dictionary<string, TypeReference> _typeTransformations = new Dictionary<string, TypeReference>();
        private Dictionary<string, string> _stringTransformations = new Dictionary<string, string>();

        public void DuplicateTemplate<T>(Expression<Func<T>> source, PropertyDefinition target)
        {
            var me = source.Body as MemberExpression;

            if (me == null)
            {
                throw new ArgumentException("You must pass a lambda of the form: '() => Class.Property' or '() => object.Property'");
            }

            _target = target;
            _template = target.Module.Import(me.Member.DeclaringType).Resolve();
            _templateProp = _template.Properties.Single(definition => definition.Name.Equals(me.Member.Name));
            
            if (target.GetMethod != null)
            {
                if (_templateProp.GetMethod != null)
                {
                    _templateProp.GetMethod.Body.Duplicate(target.GetMethod.Body, Resolver);
                }
            }
            if (target.SetMethod != null)
            {
                if (_templateProp.SetMethod != null)
                {
                    _templateProp.SetMethod.Body.Duplicate(target.SetMethod.Body, Resolver);
                }
            }

            AddCustomAttributes();
        }

        private void AddCustomAttributes()
        {
            foreach (var customAttribute in _templateProp.CustomAttributes)
            {
                _target.CustomAttributes.Add(customAttribute.Duplicate(_target.Module, Resolver));
            }

            if (_templateProp.GetMethod != null && _target.GetMethod != null)
            {
                foreach (var customAttribute in _templateProp.GetMethod.CustomAttributes)
                {
                    _target.GetMethod.CustomAttributes.Add(customAttribute.Duplicate(_target.Module, Resolver));
                }
            }

            if (_templateProp.SetMethod != null && _target.SetMethod != null)
            {
                foreach (var customAttribute in _templateProp.SetMethod.CustomAttributes)
                {
                    _target.SetMethod.CustomAttributes.Add(customAttribute.Duplicate(_target.Module, Resolver));
                }
            }
        }

        private object Resolver(object subject, Instruction instruction, ILProcessor processor)
        {
            var typeRef = subject as TypeReference;
            if (typeRef != null)
            {
                return _target.Module.Import(ResolveTypeReference(typeRef)).Resolve();
            }

            var fieldRef = subject as FieldReference;
            if (fieldRef == null && instruction != null)
            {
                fieldRef = instruction.Operand as FieldReference;
                if (fieldRef != null)
                {
                    if (fieldRef.DeclaringType.Equals(_template))
                    {
                        return FindField(fieldRef).Name;
                    }
                }
            }

            var name = subject as string;
            if (name != null)
            {
                if (_stringTransformations.TryGetValue(name, out name))
                {
                    return name;
                }
            }

            if (fieldRef != null)
            {
                if (fieldRef.DeclaringType.Equals(_template))
                {
                    return FindField(fieldRef);
                }
            }

            return subject;
        }

        private TypeReference ResolveTypeReference(TypeReference typeRef)
        {
            if (typeRef.Equals(_templateProp.PropertyType))
            {
                return _target.PropertyType;
            }
            else if (typeRef.Equals(_template))
            {
                return _target.DeclaringType;
            }
            else if (typeRef.Equals(_templateProp.PropertyType))
            {
                return _target.PropertyType;
            }

            TypeReference reference;
            if (_typeTransformations.TryGetValue(typeRef.Name, out reference))
            {
                return reference;
            }

            return typeRef;
        }

        private FieldReference FindField(FieldReference fieldRef)
        {
            var name = Resolver(fieldRef.Name, null, null) as string;

            var result = _target.DeclaringType.Fields.FirstOrDefault(definition => definition.Name == name);
            if (result != null)
            {
                return result;
            }

            if (result == null)
            {
                var type = _target.Module.Import(CecilHelper.ResolveAndImport(fieldRef.FieldType, null, _target.GetMethod.Body.GetILProcessor(), Resolver));
                var fieldDefinition = new FieldDefinition(fieldRef.Name + _target.Name, fieldRef.Resolve().Attributes, type);
                _target.DeclaringType.Fields.Add(fieldDefinition);

                foreach (var customAttribute in fieldRef.Resolve().CustomAttributes)
                {
                    fieldDefinition.CustomAttributes.Add(customAttribute.Duplicate(_target.Module, Resolver));
                }

                return fieldDefinition;
            }

            return fieldRef;
        }

        public void AddTransformation(Type source, TypeReference target)
        {
            _typeTransformations.Add(source.Name, target);
        }

        public void AddTransformation(string source, string target)
        {
            _stringTransformations.Add(source, target);
        }
    }
}
