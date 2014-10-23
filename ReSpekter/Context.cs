// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Context.cs" company="CyanCor GmbH">
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
//   Defines the Context type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace CyanCor.ReSpekter
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Modifiers;
    using Mono.Cecil;
    using Mono.Cecil.Pdb;
    using global::ReSpekter.Exception;

    [Serializable]
    public class Context : MarshalByRefObject
    {
        private static bool _isClone;
        private AppDomain _cloneDomain;
        private Context _clone;
        private List<IModifier> _modifiers = new List<IModifier>();
        public AcceptanceFilter<AssemblyDefinition> AssemblyFilter { get; private set; }
        private List<string> _processedAssemblies = new List<string>();
        private Dictionary<string, string> _locationLookups = new Dictionary<string, string>();

        private void AssemblyReady(byte[] assembly)
        {
            _cloneDomain.Load(assembly);
        }

        public Context()
        {
            var p = Assembly.GetCallingAssembly().Location;
            if (!string.IsNullOrEmpty(p))
            {
                var basePath = Path.GetDirectoryName(p);
                AssemblyFilter = new AcceptanceFilter<AssemblyDefinition>();
                AssemblyFilter.Whitelist.Add(subject => subject.MainModule.FullyQualifiedName.StartsWith(basePath));
                AssemblyFilter.Blacklist.Add(subject => subject.MainModule.Name.Equals("vshost32.exe"));
                AssemblyFilter.Blacklist.Add(subject => subject.MainModule.Name.Equals("Mono.Cecil.dll"));
                _modifiers.Add(new LazyCompositionModifier());
                _modifiers.Add(new DirtyModifier());
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Run(object[] parameters)
        {
            object ignored;
            return RunInternal(parameters, out ignored);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Run(object[] parameters, out object result)
        {
            return RunInternal(parameters, out result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool RunInternal(object[] parameters, out object result)
        { 
            if (_isClone)
            {
                result = null;
                return false;
            }

            var mainMethod = GetCallingMethod();
            CreateAppDomain();
            ModifyAssemblies();
            result = RunClone(parameters, mainMethod);
            AppDomain.Unload(_cloneDomain);
            return true;
        }

        private object RunClone(object[] parameters, MethodBase mainMethod)
        {
            _clone = _cloneDomain.CreateInstanceFromAndUnwrap(
                typeof (Context).Assembly.Location,
                typeof (Context).FullName) as Context;

            foreach (var locationLookup in _locationLookups)
            {
                _clone.AddResolve(locationLookup.Key, locationLookup.Value);
            }

            return _clone.InvokeClone(mainMethod.DeclaringType.Assembly.FullName, mainMethod.DeclaringType.FullName, mainMethod.Name,
                parameters);
        }

        private void ModifyAssemblies()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                ModifyAssembly(assembly);
            }
        }

        private void CreateAppDomain()
        {
            var evidence = AppDomain.CurrentDomain.Evidence;
            var setup = new AppDomainSetup
            {
                ApplicationBase = Path.GetDirectoryName(typeof (TypeDefinition).Assembly.Location)
            };

            _cloneDomain = AppDomain.CreateDomain("ReSpekted", evidence, setup);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static MethodBase GetCallingMethod()
        {
            var stackFrame = new StackTrace().GetFrames();
            var mainMethod = stackFrame[3].GetMethod();

            if (!mainMethod.IsStatic)
            {
                throw new ReSpekterException("Run has to be called from a static method.");
            }

            if (mainMethod.IsConstructor)
            {
                throw new ReSpekterException("Run cannot be called from constructors.");
            }
            return mainMethod;
        }

        private void AddResolve(string fullName, string location)
        {
            _locationLookups.Add(fullName, location);
        }

        private void ModifyAssembly(Assembly assembly)
        {
            if (_locationLookups.ContainsKey(assembly.FullName))
            {
                return;
            }

            _locationLookups.Add(assembly.FullName, assembly.Location);

            var parameters = new ReaderParameters();
            var pdbName = Path.ChangeExtension(assembly.Location, "pdb");
            if (File.Exists(pdbName))
            {
                parameters.ReadSymbols = true;
                parameters.SymbolReaderProvider = new PdbReaderProvider();
            }

            ModifyAssembly(AssemblyDefinition.ReadAssembly(assembly.Location, parameters));
        }

        private void ModifyAssembly(AssemblyDefinition assembly)
        {
            if (!assembly.FullName.Equals(typeof(Context).Assembly.FullName))
            {
                if (AssemblyFilter.Check(assembly))
                {
                    foreach (var modifier in _modifiers)
                    {
                        modifier.Visit(assembly);
                    }
                    
                    Directory.CreateDirectory("ReSpekted");
                    var parameters = new WriterParameters();
                    parameters.SymbolWriterProvider = new PdbWriterProvider();
                    parameters.WriteSymbols = true;
                    var path = "ReSpekted\\" + assembly.MainModule.Name;
                    assembly.MainModule.Import(typeof (System.WeakReference<>));
                    assembly.Write(path, parameters);
                    /*if (File.Exists(Path.ChangeExtension(_locationLookups[assembly.FullName], ".pdb")))
                    {
                        File.Copy(Path.ChangeExtension(path, ".pdb"), Path.ChangeExtension(_locationLookups[assembly.FullName], ".pdb"), true);
                    }*/

                    _locationLookups[assembly.FullName] = Path.GetFullPath(path);
                }
            }
        }

        private object InvokeClone(string assemblyName, string typeName, string methodName, object[] parameters)
        {
            _isClone = true;

            foreach (var locationLookup in _locationLookups)
            {
                Assembly.LoadFrom(locationLookup.Value);
            }

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.Equals(assemblyName))
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.FullName.Equals(typeName))
                        {
                            var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                            return method.Invoke(null, parameters);
                        }
                    }
                }
            }

            return null;
        }

        private Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (_locationLookups.ContainsKey(args.Name))
            {
                return Assembly.LoadFile(_locationLookups[args.Name]);
            }

            return null;
        }
    }
}
