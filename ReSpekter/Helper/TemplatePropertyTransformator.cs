using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CyanCor.ReSpekter.Helper
{
    public class TemplatePropertyTransformator
    {
        private PropertyDefinition _templateProp;
        private TypeDefinition _template;
        private PropertyDefinition _target;

        private Dictionary<string, TypeReference> _typeTransformations = new Dictionary<string, TypeReference>();

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

            _templateProp.GetMethod.Body.Duplicate(target.GetMethod.Body, Resolver);
            _templateProp.SetMethod.Body.Duplicate(target.SetMethod.Body, Resolver);
        }

        private object Resolver(object subject, Instruction instruction, ILProcessor processor)
        {
            var typeRef = subject as TypeReference;
            if (typeRef != null)
            {
                return ResolveTypeReference(typeRef);
            }

            var name = subject as string;
            if (name != null)
            {
                
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

            return _target.Module.Import(typeRef);
        }

        private FieldReference FindField(FieldReference fieldRef)
        {
            var result = _target.DeclaringType.Fields.FirstOrDefault(definition => definition.Name == fieldRef.Name);
            if (result != null)
            {
                return result;
            }

            result = _target.DeclaringType.Fields.FirstOrDefault(definition => definition.Name == fieldRef.Name + _target.Name);
            if (result != null)
            {
                return result;
            }

            if (result == null)
            {
                var fieldDefinition = new FieldDefinition(fieldRef.Name + _target.Name, fieldRef.Resolve().Attributes, ResolveTypeReference(fieldRef.FieldType));
                _target.DeclaringType.Fields.Add(fieldDefinition);
                return fieldDefinition;
            }

            return fieldRef;
        }

        public void AddTransformation(Type source, TypeReference target)
        {
            _typeTransformations.Add(source.Name, target);
        }
    }
}
