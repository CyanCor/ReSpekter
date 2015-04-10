using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CyanCor.ReSpekter
{
    public class BaseModifier : IModifier
    {
        private delegate void InstructionCloneDelegate(Instruction source, ILProcessor processor);
        public AcceptanceFilter<AssemblyDefinition> AssemblyAcceptor { get; private set; }
        public AcceptanceFilter<ModuleDefinition> ModuleAcceptor { get; private set; }
        public AcceptanceFilter<TypeDefinition> TypeAcceptor { get; private set; }

        private Dictionary<OpCode, InstructionCloneDelegate> _cloneDelegates = new Dictionary<OpCode, InstructionCloneDelegate>();

        private readonly List<PropertyDefinition> _newProperties = new List<PropertyDefinition>();

        protected List<PropertyDefinition> NewProperties
        {
            get { return _newProperties; }
        }

        protected BaseModifier()
        {
            AssemblyAcceptor = new AcceptanceFilter<AssemblyDefinition>();
            ModuleAcceptor = new AcceptanceFilter<ModuleDefinition>();
            TypeAcceptor = new AcceptanceFilter<TypeDefinition>();

            TypeAcceptor.Whitelist.Add(new AttributeFilter(typeof(SecurityAttribute)));
        }

        void BuildCloneDelegates()
        {
            
        }

        public virtual void Visit(AssemblyDefinition assembly)
        {
            foreach (var moduleDefinition in assembly.Modules)
            {
                Visit(moduleDefinition);
            }
        }

        protected virtual void Visit(ModuleDefinition module)
        {
            foreach (var typeDefinition in module.Types)
            {
                if (!typeDefinition.CustomAttributes.Any(
                        attribute => attribute.AttributeType.FullName.Equals(typeof (NoReSpekterAttribute).FullName)))
                {
                    Visit(typeDefinition);
                }
            }
        }

        protected virtual void Visit(TypeDefinition type)
        {
            foreach (var t in type.NestedTypes)
            {
                if (!t.CustomAttributes.Any(
                        attribute => attribute.AttributeType.FullName.Equals(typeof(NoReSpekterAttribute).FullName)))
                {
                    Visit(t);
                }
            }

            foreach (var property in type.Properties)
            {
                if (!property.CustomAttributes.Any(
                        attribute => attribute.AttributeType.FullName.Equals(typeof(NoReSpekterAttribute).FullName)))
                {
                    Visit(property);
                }
            }

            foreach (var propertyDefinition in _newProperties)
            {
                propertyDefinition.DeclaringType = null;
                type.Properties.Add(propertyDefinition);
            }
            _newProperties.Clear();

            foreach (var method in type.Methods)
            {
                if (!method.CustomAttributes.Any(
                        attribute => attribute.AttributeType.FullName.Equals(typeof(NoReSpekterAttribute).FullName)))
                {
                    Visit(method);
                }
            }

            foreach (var field in type.Fields)
            {
                if (!field.CustomAttributes.Any(
                        attribute => attribute.AttributeType.FullName.Equals(typeof(NoReSpekterAttribute).FullName)))
                {
                    Visit(field);
                }
            }

        }

        protected virtual void Visit(PropertyDefinition property)
        {
            
        }

        protected virtual void Visit(MethodDefinition method)
        {
            if (method.Name.Equals("Printtest"))
            {
                ILProcessor processor = method.Body.GetILProcessor();
                method.Body.Instructions.Insert(0, processor.Create(OpCodes.Ret));
            }
        }

        protected virtual void Visit(FieldDefinition field)
        {
            
        }

    }
}