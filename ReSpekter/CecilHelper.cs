// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CecilHelper.cs" company="CyanCor GmbH">
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
//   Defines the CecilHelper type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace CyanCor.ReSpekter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;

    public delegate object ResolveOperandDelegate(object subject, Instruction instruction, ILProcessor processor);

    public static class CecilHelper
    {
        
        private delegate Instruction InstructionCloneDelegate(Instruction source, ILProcessor processor, ResolveOperandDelegate resolver);

        private static Dictionary<OpCode, InstructionCloneDelegate> _cloneDelegates;

        public static bool HasInterface(this TypeReference type, Type iface)
        {

            var iref = type.Module.Import(iface);
            return HasInterface(type, iref);
        }

        public static bool HasInterface(this TypeReference type, TypeReference iface)
        {
            var tdef = type.Resolve();
            return (tdef.Interfaces.Any(i => CompareInterfaces(i, iface))
                    || ((tdef.BaseType != null) && (tdef.BaseType.HasInterface(iface))));
        }

        private static bool CompareInterfaces(TypeReference i, TypeReference iref)
        {
            if (iref.HasGenericParameters)
            {
                if (!i.HasGenericParameters)
                {
                    return i.GetElementType().FullName.Equals(iref.FullName);
                }
            }

            return i.FullName.Equals(iref.FullName);
        }

        public static PropertyReference FindProperty(this TypeDefinition type, string name)
        {
            var reference = type.Properties.FirstOrDefault(definition => definition.Name.Equals(name));
            if (reference == null)
            {
                return FindProperty(type.BaseType.Resolve(), name);
            }

            return reference;
        }

        public static void Duplicate(this MethodBody source, MethodBody target, ResolveOperandDelegate resolver)
        {
            var variableList = source.Variables.ToArray();

            target.InitLocals = source.InitLocals;
            target.Variables.Clear();

            var il = target.GetILProcessor();

            ResolveOperandDelegate localResolver = resolver;

            foreach (var variable in variableList)
            {
                target.Variables.Add(new VariableDefinition(variable.Name, ResolveAndImport(variable.VariableType, null, il, resolver)));
            }
            
            var instructionList = source.Instructions.ToArray();
            target.Instructions.Clear();

            foreach (var instruction in instructionList)
            {
                var duplicate = instruction.Duplicate(il, localResolver);
                duplicate.Offset = instruction.Offset;
                if (duplicate.ToString().Substring(8) != instruction.ToString().Substring(8))
                {

                }
                il.Append(duplicate);
            }

            for (var i = 0; i < instructionList.Length; i++)
            {
                var jumpTarget = instructionList[i].Operand as Instruction;
                if (jumpTarget != null)
                {
                    for (var a = 0; a < instructionList.Length; a++)
                    {
                        if (instructionList[a] == jumpTarget)
                        {
                            var instruction = il.Create(instructionList[i].OpCode, il.Body.Instructions[a]);
                            instruction.Offset = il.Body.Instructions[i].Offset;
                            il.Replace(il.Body.Instructions[i], instruction);
                        }
                    }
                }
            }
        }

        private static object ResolveAndImport(FieldReference subject, Instruction instruction, ILProcessor processor, ResolveOperandDelegate resolver)
        {
            var name = ResolveAndImport(subject.Name, instruction, processor, resolver);
            var declaringType = ResolveAndImport(subject.DeclaringType, instruction, processor, resolver).Resolve();

            var field = declaringType.Resolve().Fields.Single(definition => definition.Name.Equals(name)).Resolve();

            return processor.Body.Method.Module.Import(field);
        }

        private static VariableDefinition ResolveAndImport(VariableDefinition subject, Instruction instruction, ILProcessor processor, ResolveOperandDelegate resolver)
        {
            var localVariable = processor.Body.Variables.FirstOrDefault(definition => definition.Name.Equals(subject.Name));
            if (localVariable != null)
            {
                return localVariable;
            }

            var result = (VariableDefinition)resolver(subject, instruction, processor);

            return new VariableDefinition(result.Name, processor.Body.Method.Module.Import(result.VariableType));
        }

        private static TypeReference ResolveAndImport(TypeReference subject, Instruction instruction, ILProcessor processor, ResolveOperandDelegate resolver)
        {
            var genType = subject as GenericInstanceType;

            if (genType != null)
            {
                var gens = genType.GenericArguments.Select(gen => ResolveAndImport(gen, instruction, processor, resolver)).ToArray();
                if (gens.Length > 0)
                {
                    var type = ResolveAndImport(genType.GetElementType(), instruction, processor, resolver);
                    var newType = type.MakeGenericInstanceType(gens);
                    return processor.Body.Method.Module.Import(newType);
                }
            }

            var result = ((TypeReference)(resolver(subject, instruction, processor))).Resolve();
            return processor.Body.Method.Module.Import(result.Resolve());
        }

        private static MethodReference ResolveAndImport(MethodReference subject, Instruction instruction, ILProcessor processor, ResolveOperandDelegate resolver)
        {
            var declaringType = ResolveAndImport(subject.DeclaringType, instruction, processor, resolver);
            var declaringTypeResolved = declaringType.Resolve();
            var resolved = subject.Resolve();
            var name = ResolveAndImport(subject.Name, instruction, processor, resolver);

            MethodDefinition method = null;
            if (resolved.IsConstructor)
            {
                foreach (var methodDefinition in declaringTypeResolved.GetConstructors())
                {
                    if (methodDefinition.Parameters.Count == resolved.Parameters.Count)
                    {
                        var equal = true;
                        for (var i = 0; i < methodDefinition.Parameters.Count; i++)
                        {
                            if (!methodDefinition.Parameters[i].Equals(resolved.Parameters[i]))
                            {
                                equal = false;
                            }
                        }

                        if (equal)
                        {
                            method = methodDefinition;
                        }
                    }
                }
            }
            else
            {
                method = ResolveMethod(declaringTypeResolved, name);
            }

            var baseMethodReference = processor.Body.Method.Module.Import(method);

            if (declaringType.IsGenericInstance)
            {
                var instanceType = (GenericInstanceType)declaringType;
                baseMethodReference =
                    baseMethodReference.MakeGeneric(
                        instanceType.GenericArguments.Select(
                            (reference => ResolveAndImport(reference, instruction, processor, resolver))).ToArray());

            }

            var genericMethod = subject as GenericInstanceMethod;

            if (genericMethod != null)
            {
                var genericArguments = genericMethod.GenericArguments.Select(
                    reference => ResolveAndImport(reference, instruction, processor, resolver));
                var genMethod = new GenericInstanceMethod(baseMethodReference);
                foreach (var genericArgument in genericArguments)
                {
                    genMethod.GenericArguments.Add(genericArgument);
                }

                baseMethodReference = genMethod;
            }

            return processor.Body.Method.Module.Import(baseMethodReference);
        }

        private static MethodDefinition ResolveMethod(TypeDefinition declaringTypeResolved, string name)
        {
            var method = declaringTypeResolved.GetMethods().FirstOrDefault(definition => definition.Name.Equals(name));
            if (method == null)
            {
                if (declaringTypeResolved.BaseType != null)
                {
                    method = ResolveMethod(declaringTypeResolved.BaseType.Resolve(), name);
                }
            }
            return method.Resolve();
        }

        public static TypeReference MakeGenericType(this TypeReference self, params TypeReference[] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
            {
                throw new ArgumentException();
            }

            var instance = new GenericInstanceType(self);
            foreach (var argument in arguments)
            {
                instance.GenericArguments.Add(argument);
            }

            return instance;
        }

        public static MethodReference MakeGeneric(this MethodReference self, params TypeReference[] classArguments)
        {
            var reference = new MethodReference(self.Name, self.ReturnType)
            {
                DeclaringType = self.DeclaringType.MakeGenericType(classArguments),
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention,
            };

            foreach (var parameter in self.Parameters)
            {
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            }

            foreach (var genericParameter in self.GenericParameters)
            {
                reference.GenericParameters.Add(new GenericParameter(genericParameter.Name, reference));
            }

            return reference;
        }

        private static string ResolveAndImport(string subject, Instruction instruction, ILProcessor processor, ResolveOperandDelegate resolver)
        {
            return (string)resolver(subject, instruction, processor);
        }


        public static Instruction Duplicate(this Instruction source, ILProcessor processor, ResolveOperandDelegate resolver)
        {
            if (source.Operand == null)
            {
                return processor.Create(source.OpCode);
            }

            if (source.Operand is Instruction)
            {
                return processor.Create(OpCodes.Nop);
            }

            dynamic d = source.Operand;
            d = ResolveAndImport(d, source, processor, resolver);
            return processor.Create(source.OpCode, d);
        }
    }
}
