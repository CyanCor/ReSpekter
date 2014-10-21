using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CyanCor.ReSpekter
{
    public class BaseModifier : IModifier
    {
        public AcceptanceFilter<AssemblyDefinition> AssemblyAcceptor { get; private set; }
        public AcceptanceFilter<ModuleDefinition> ModuleAcceptor { get; private set; }
        public AcceptanceFilter<TypeDefinition> TypeAcceptor { get; private set; }

        internal BaseModifier()
        {
            AssemblyAcceptor = new AcceptanceFilter<AssemblyDefinition>();
            ModuleAcceptor = new AcceptanceFilter<ModuleDefinition>();
            TypeAcceptor = new AcceptanceFilter<TypeDefinition>();

            TypeAcceptor.Whitelist.Add(new AttributeFilter(typeof(SecurityAttribute)));
        }

        public virtual void Visit(AssemblyDefinition assembly)
        {
            foreach (var moduleDefinition in assembly.Modules)
            {
                Visit(moduleDefinition);
            }
        }

        public virtual void Visit(ModuleDefinition module)
        {
            foreach (var typeDefinition in module.Types)
            {
                Visit(typeDefinition);
            }
        }

        public virtual void Visit(TypeDefinition type)
        {
            foreach (var property in type.Properties)
            {
                Visit(property);
            }

            foreach (var method in type.Methods)
            {
                Visit(method);
            }

            foreach (var field in type.Fields)
            {
                Visit(field);
            }

        }

        private void Visit(PropertyDefinition property)
        {
            
        }

        public virtual void Visit(MethodDefinition method)
        {
            if (method.Name.Equals("Printtest"))
            {
                ILProcessor processor = method.Body.GetILProcessor();
                method.Body.Instructions.Insert(0, processor.Create(OpCodes.Ret));
            }
        }

        public void Visit(FieldDefinition field)
        {
            
        }
    }
}