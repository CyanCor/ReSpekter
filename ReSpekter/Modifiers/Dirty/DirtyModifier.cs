// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DirtyModifier.cs" company="CyanCor GmbH">
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
//   Defines the IDirty type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace CyanCor.ReSpekter.Modifiers
{
    using System;
    using System.Linq;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public class DirtyModifier : BaseModifier
    {
        private readonly OpCode[] _hotCodes =
        {
            OpCodes.Starg, OpCodes.Starg_S,
            OpCodes.Stelem_Any, OpCodes.Stelem_I, OpCodes.Stelem_I1, OpCodes.Stelem_I2,
            OpCodes.Stelem_I4, OpCodes.Stelem_I8, OpCodes.Stelem_I8, OpCodes.Stelem_R4, OpCodes.Stelem_R8,
            OpCodes.Stelem_Ref,
            OpCodes.Stfld, OpCodes.Stind_I, OpCodes.Stind_I1, OpCodes.Stind_I2, OpCodes.Stind_I4, OpCodes.Stind_I8,
            OpCodes.Stind_R4, OpCodes.Stind_R8, OpCodes.Stind_Ref,
            /*OpCodes.Stloc, OpCodes.Stloc_0, OpCodes.Stloc_1,
            OpCodes.Stloc_2, OpCodes.Stloc_3, OpCodes.Stloc_S,*/
            OpCodes.Stobj,
            OpCodes.Stsfld
        };

        private PropertyDefinition _callProperty;

        bool CheckInterface(TypeDefinition type, Type iface)
        {
            if (type.BaseType != null)
            {
                if (CheckInterface(type.BaseType.Resolve(), iface))
                {
                    return true;
                }
            }

            return type.Interfaces.Any(reference => reference.FullName.Equals(iface.FullName));
        }

        protected override void Visit(TypeDefinition type)
        {
            if (CheckInterface(type, typeof(IDirty)))
            {
                _callProperty = FindProperty(type, "Dirty").Resolve();
                base.Visit(type);
            }
        }

        protected override void Visit(MethodDefinition method)
        {
            if (!method.IsStatic && !method.IsAbstract && !method.FullName.Equals(_callProperty.SetMethod.FullName))
            {
                var hotPoints = method.Body.Instructions.Where(instruction => _hotCodes.Contains(instruction.OpCode)).ToList();

                foreach (var hotPoint in hotPoints)
                {
                    var il = method.Body.GetILProcessor();

                    il.InsertAfter(hotPoint, il.Create(OpCodes.Call, _callProperty.Resolve().SetMethod));
                    il.InsertAfter(hotPoint, il.Create(OpCodes.Ldc_I4, 1));
                    il.InsertAfter(hotPoint, il.Create(OpCodes.Ldarg_0));
                }
            }

            base.Visit(method);
        }

        private MethodReference FindMethod(TypeDefinition type, string name)
        {
            var reference = type.Methods.FirstOrDefault(definition => definition.Name.Equals(name));
            if (reference == null)
            {
                return FindMethod(type.BaseType.Resolve(), name);
            }

            return reference;
        }

        private PropertyReference FindProperty(TypeDefinition type, string name)
        {
            var reference = type.Properties.FirstOrDefault(definition => definition.Name.Equals(name));
            if (reference == null)
            {
                return FindProperty(type.BaseType.Resolve(), name);
            }

            return reference;
        }
    }
}
