using System.Collections.Generic;
using Mono.Cecil;

namespace CyanCor.ReSpekter
{
    internal class ReSpektedAssemblyResolver : IAssemblyResolver
    {
        private readonly Dictionary<string, AssemblyDefinition> _definitionLookup = new Dictionary<string, AssemblyDefinition>();
        private readonly Dictionary<string, string> _locationLookups;
        private readonly DefaultAssemblyResolver _defaultAssemblyResolver = new DefaultAssemblyResolver();

        public void UpdateAssembly(AssemblyDefinition assembly)
        {
            _definitionLookup[assembly.FullName] = assembly;
        }

        public ReSpektedAssemblyResolver(Dictionary<string, string> locationLookups)
        {
            _locationLookups = locationLookups;
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            return Resolve(name, null);
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            return Resolve(name.FullName, parameters);
        }

        public AssemblyDefinition Resolve(string fullName)
        {
            return Resolve(fullName, null);
        }

        public AssemblyDefinition Resolve(string fullName, ReaderParameters parameters)
        {
            AssemblyDefinition def;
            if (_definitionLookup.TryGetValue(fullName, out def))
            {
                return def;
            }

            if (_locationLookups.ContainsKey(fullName))
            {
                if (parameters == null)
                {
                    def = AssemblyDefinition.ReadAssembly(_locationLookups[fullName]);
                }
                else
                {
                    def = AssemblyDefinition.ReadAssembly(_locationLookups[fullName], parameters);   
                }
                _definitionLookup.Add(fullName, def);
                return def;
            }

            return _defaultAssemblyResolver.Resolve(fullName);
        }
    }
}