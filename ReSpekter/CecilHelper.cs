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

using System.Collections;

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
                    || tdef.NestedTypes.Any(t => HasInterface(t, iface)));
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

            ResolveOperandDelegate localResolver = (subject, instruction, processor) => ResolveAndImport(subject, instruction, il, resolver);

            foreach (var variable in variableList)
            {
                target.Variables.Add(variable.Duplicate(localResolver));
            }
            
            var instructionList = source.Instructions.ToArray();
            target.Instructions.Clear();

            foreach (var instruction in instructionList)
            {
                il.Append(instruction.Duplicate(il, localResolver));
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
                            il.Replace(il.Body.Instructions[i],
                                il.Create(instructionList[i].OpCode, il.Body.Instructions[a]));
                        }
                    }
                }
            }
        }

        private static object ResolveAndImport(object subject, Instruction instruction, ILProcessor processor,
            ResolveOperandDelegate resolver)
        {
            dynamic result = Resolve(subject, instruction, processor, resolver);
            if (result is TypeReference)
            {
                if (!(result as TypeReference).IsGenericParameter)
                {
                    processor.Body.Method.Module.Import(result as TypeReference);
                }
            }
            return result;
        }

        private static object Resolve(object subject, Instruction instruction, ILProcessor processor, ResolveOperandDelegate resolver)
        {
            var method = subject as MethodReference;
            var variable = subject as VariableDefinition;
            var typeRef = subject as GenericInstanceType;

            if (subject is Instruction)
            {
                return null;
            }

            if (method != null)
            {
                var type = (TypeReference)ResolveAndImport(method.DeclaringType, instruction, processor, resolver);
                var returnType = (TypeReference)ResolveAndImport(method.ReturnType, instruction, processor, resolver);
                return new MethodReference(method.Name, returnType, type);
            }

            if (variable != null)
            {
                var localVariable =
                    processor.Body.Variables.FirstOrDefault(definition => definition.Name.Equals(variable.Name));
                if (localVariable != null)
                {
                    return localVariable;
                }
            }

            if (typeRef != null)
            {
                var gens = typeRef.GenericArguments.Select(gen => (TypeReference)ResolveAndImport(gen, instruction, processor, resolver)).ToArray();
                if (gens.Length > 0)
                {
                    var type = (TypeReference)ResolveAndImport(typeRef.GetElementType(), instruction, processor, resolver);
                    return type.MakeGenericInstanceType(gens);
                }
                
            }

            return resolver(subject, instruction, processor);
        }

        public static VariableDefinition Duplicate(this VariableDefinition source, ResolveOperandDelegate resolver)
        {
            return new VariableDefinition(source.Name, (TypeReference)resolver(source.VariableType, null, null));
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
            
            dynamic d = resolver(source.Operand, source, processor);
            return processor.Create(source.OpCode, d);
        }
    }
}
