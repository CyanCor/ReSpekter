using Mono.Cecil;

namespace CyanCor.ReSpekter.Proxies
{
    public class ModuleProxy
    {
        private readonly AssemblyProxy _assembly;

        internal ModuleProxy(ModuleDefinition moduleDefinition, AssemblyProxy assembly)
        {
            ModuleDefinition = moduleDefinition;
            _assembly = assembly;
        }

        internal ModuleDefinition ModuleDefinition { get; set; }
    }

    public class TypeProxy
    {
    }
}
