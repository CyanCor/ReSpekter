// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AssemblyProxy.cs" company="CyanCor GmbH">
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
//   Defines the AssemblyProxy type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace CyanCor.ReSpekter.Proxies
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Mono.Cecil;

    public class AssemblyProxy
    {
        readonly AssemblyDefinition _assemblyDefinition;

        private List<ModuleProxy> _moduleProxies;

        private AssemblyProxy(AssemblyDefinition assemblyDefinition)
        {
            _assemblyDefinition = assemblyDefinition;
        }

        public static AssemblyProxy Load(string fileName)
        {
            return new AssemblyProxy(AssemblyDefinition.ReadAssembly(fileName));
        }

        public static AssemblyProxy Load(Assembly assembly)
        {
            return Load(assembly.Location);
        }

        public IEnumerable<ModuleProxy> Modules
        {
            get
            {
                if (_moduleProxies == null)
                {
                    _moduleProxies = new List<ModuleProxy>();
                    _moduleProxies.AddRange(_assemblyDefinition.Modules.Select(definition => new ModuleProxy(definition, this)));
                }

                return _moduleProxies;
            }
        }
    }
}
