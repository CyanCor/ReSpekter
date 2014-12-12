namespace CyanCor.ReSpekter.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public class TransformationHelper
    {
        private Dictionary<string, string> _nameTransformations = new Dictionary<string, string>();

        private Dictionary<string, PropertyDefinition> _propertyTransformations = new Dictionary<string, PropertyDefinition>();

        private Dictionary<string, FieldDefinition> _fieldTransformations = new Dictionary<string, FieldDefinition>();
        private Dictionary<string, TypeReference> _typeTransformations = new Dictionary<string, TypeReference>();

        public void DuplicateMethod(MethodDefinition source, MethodDefinition target)
        {
            source.Body.Duplicate(target.Body, Resolver);
        }

        public void AddTransformation(string source, string target)
        {
            _nameTransformations.Add(source, target);
        }

        public void AddTransformation(Type source, TypeReference target)
        {
            _typeTransformations.Add(source.Name, target);
        }

        public void AddTransformation<T>(Expression<Func<T>> sourceProperty, PropertyDefinition target)
        {
            var me = sourceProperty.Body as MemberExpression;

            if (me == null)
            {
                throw new ArgumentException("You must pass a lambda of the form: '() => Class.Property' or '() => object.Property'");
            }

            _propertyTransformations.Add(me.Member.Name, target);
        }

        private object Resolver(object subject, Instruction instruction, ILProcessor processor)
        {
            var name = subject as string;
            if (name != null)
            {
                return TransformName(name, instruction, processor);
            }

            var typeDef = subject as TypeReference;
            if (typeDef != null)
            {
                TypeReference typeRef;
                if (_typeTransformations.TryGetValue(typeDef.Name, out typeRef))
                {
                    return typeRef;
                }
            }

            return subject;
        }

        private object TransformName(string subject, Instruction instruction, ILProcessor processor)
        {
            string result;
            if (_nameTransformations.TryGetValue(subject, out result))
            {
                return result;
            }

            FieldDefinition field;
            if (_fieldTransformations.TryGetValue(subject, out field))
            {
                return field.Name;
            }

            PropertyDefinition property;
            if (_propertyTransformations.TryGetValue(subject, out property))
            {
                return property.Name;
            }

            return subject;
        }

        public void AddTransformation<T>(Expression<Func<T>> sourceField, FieldDefinition target)
        {
            var me = sourceField.Body as MemberExpression;

            if (me == null)
            {
                throw new ArgumentException("You must pass a lambda of the form: '() => Class.Field' or '() => object.Field'");
            }

            _fieldTransformations.Add(me.Member.Name, target);
        }
    }
}
