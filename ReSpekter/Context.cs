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

using System.Linq;
using System.Security.Cryptography;
using Mono.Cecil.Rocks;

namespace CyanCor.ReSpekter
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using Mono.Cecil;
    using Mono.Cecil.Pdb;
    using global::ReSpekter.Exception;

    [Serializable]
    public class Context : MarshalByRefObject
    {
        private ReSpektedAssemblyResolver _assemblyResolver;
        private static bool _isClone;
        private AppDomain _cloneDomain;
        private Context _clone;
        private List<IModifier> _modifiers = new List<IModifier>();
        private static string _basePath;

        public AcceptanceFilter<AssemblyDefinition> AssemblyFilter { get; private set; }
        private readonly Dictionary<string, string> _locationLookups = new Dictionary<string, string>();
        private readonly List<Assembly> _addedAssemblies = new List<Assembly>();

        public bool IsClone
        {
            get { return _isClone; }
        }

        public void AddAssembly(Assembly assembly)
        {
            _addedAssemblies.Add(assembly);
        }

        private void AssemblyReady(byte[] assembly)
        {
            _cloneDomain.Load(assembly);
        }

        public void AddModifier(IModifier modifier)
        {
            _modifiers.Add(modifier);
        }

        public Context()
        {
            var p = Assembly.GetCallingAssembly().Location;
            if (!string.IsNullOrEmpty(p))
            {
                if (_basePath == null)
                {
                    _basePath = Path.GetDirectoryName(p);
                }

                AssemblyFilter = new AcceptanceFilter<AssemblyDefinition>();
                AssemblyFilter.Whitelist.Add(
                    subject => subject.MainModule.FullyQualifiedName.StartsWith(_basePath));

                AssemblyFilter.Blacklist.Add(subject => subject.MainModule.Name.Equals("vshost32.exe"));
                AssemblyFilter.Blacklist.Add(subject => subject.MainModule.Name.Contains("Mono.Cecil."));
            }

            _assemblyResolver = new ReSpektedAssemblyResolver(_locationLookups);
        }

        public bool Run(object[] parameters)
        {
            object ignored;
            return RunInternal(parameters, out ignored);
        }

        public bool Run(object[] parameters, out object result)
        {
            return RunInternal(parameters, out result);
        }

        private bool RunInternal(object[] parameters, out object result)
        { 
            if (IsClone)
            {
                result = null;
                return false;
            }

            var mainMethod = GetCallingMethod();
            ModifyAssemblies();
            CreateAppDomain();
            result = RunClone(parameters, mainMethod);
            AppDomain.Unload(_cloneDomain);
            return true;
        }

        internal void Run(Assembly asm, object[] parameters)
        {
            foreach (var type in asm.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public))
                {
                    if (method.Name.Equals("Main"))
                    {
                        
                    }
                }
            }
                    
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

            return _clone.InvokeClone(_basePath, mainMethod.DeclaringType.Assembly.FullName, mainMethod.DeclaringType.FullName, mainMethod.Name, parameters);
        }

        private void ModifyAssemblies()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                ModifyAssembly(assembly);
            }

            foreach (var assembly in _addedAssemblies)
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

        public static void PrintAssemblies(AppDomain domain)
        {
            Console.WriteLine("  ----------");
            var list = domain.GetAssemblies().ToList();
            list.Sort((assembly, assembly1) => assembly.GetName().Name.CompareTo(assembly1.GetName().Name));

            foreach (var asm in list)
            {
                Console.WriteLine(asm.FullName + "\n\t" + asm.Location);
            }
            Console.WriteLine("  ----------");
            Console.WriteLine();
        }

        private static MethodBase GetCallingMethod()
        {
            var stackFrame = new StackTrace().GetFrames();
            foreach (var frame in stackFrame)
            {
                Console.WriteLine(frame.ToString());
            }

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

            foreach (var referencedAssembly in assembly.GetReferencedAssemblies())
            {
                foreach (var addedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (addedAssembly.FullName.Equals(referencedAssembly.FullName))
                    {
                        if (!_locationLookups.ContainsKey(assembly.FullName))
                        {
                            ModifyAssembly(addedAssembly);
                        }
                    }
                }
            }


            var parameters = new ReaderParameters();
            parameters.AssemblyResolver = _assemblyResolver;


            var targetDir = Path.Combine(Directory.GetCurrentDirectory(), "ReSpekted");
            Directory.CreateDirectory(targetDir);
            var path = Path.Combine(targetDir, Path.GetFileName(assembly.Location));



            if (AssemblyFilter.Check(AssemblyDefinition.ReadAssembly(assembly.Location)))
            {
#if DEBUG
                var pdbName = Path.ChangeExtension(assembly.Location, "pdb");
                if (File.Exists(pdbName))
                {
                    File.Copy(pdbName, Path.ChangeExtension(path, "pdb"), true);
                    parameters.ReadSymbols = true;
                    parameters.SymbolReaderProvider = new PdbReaderProvider();
                }
#endif
                File.Copy(assembly.Location, path, true);
                var assemblyDefinition = AssemblyDefinition.ReadAssembly(path, parameters);
                var success = ModifyAssembly(assemblyDefinition, path);
                if (!success)
                {
                    File.Delete(path);
                }
                else
                {
                    _assemblyResolver.UpdateAssembly(assemblyDefinition);
                    _locationLookups[assembly.FullName] = Path.GetFullPath(path);
                }
            }

        }

        private bool ModifyAssembly(AssemblyDefinition assembly, string destination)
        {
            if (!assembly.FullName.Equals(typeof (Context).Assembly.FullName))
            {
                Console.WriteLine("Processing assembly: " + assembly.Name);
                foreach (var module in assembly.Modules)
                {
                    foreach (var typeDefinition in module.GetTypes())
                    {
                        foreach (var method in typeDefinition.Methods.Where(definition => definition.Body != null))
                        {
                            method.Body.SimplifyMacros();
                        }
                    }
                }

                foreach (var modifier in _modifiers)
                {
                    modifier.Visit(assembly);
                }

                foreach (var module in assembly.Modules)
                {
                    foreach (var typeDefinition in module.GetTypes())
                    {
                        foreach (var method in typeDefinition.Methods.Where(definition => definition.Body != null))
                        {
                            method.Body.SimplifyMacros();
                            method.Body.OptimizeMacros();
                        }
                    }
                }

                var parameters = new WriterParameters();
#if DEBUG
                parameters.SymbolWriterProvider = new PdbWriterProvider();
                parameters.WriteSymbols = true;
#endif
                assembly.Write(destination, parameters);
                
                return true;
                /*if (File.Exists(Path.ChangeExtension(_locationLookups[assembly.FullName], ".pdb")))
                    {
                        File.Copy(Path.ChangeExtension(path, ".pdb"), Path.ChangeExtension(_locationLookups[assembly.FullName], ".pdb"), true);
                    }*/
            }

            return false;
        }

        private object InvokeClone(string basePath, string assemblyName, string typeName, string methodName, object[] parameters)
        {
            _isClone = true;
            _basePath = basePath;

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;

            foreach (var locationLookup in _locationLookups)
            {
                Assembly.LoadFrom(locationLookup.Value);
                Console.WriteLine("Loading Assembly" + locationLookup.Value);
            }

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

        public void AddModule(string path)
        {
            if (!_isClone)
            {
                _addedAssemblies.Add(Assembly.LoadFile(path));
            }
        }
    }
}
