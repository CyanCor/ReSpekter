// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IModifier.cs" company="CyanCor GmbH">
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
//   Defines the IModifier type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace CyanCor.ReSpekter
{
    using Mono.Cecil;

    public interface IModifier
    {
        AcceptanceFilter<AssemblyDefinition> AssemblyAcceptor { get; }
        AcceptanceFilter<ModuleDefinition> ModuleAcceptor { get; }
        AcceptanceFilter<TypeDefinition> TypeAcceptor { get; }
        void Visit(AssemblyDefinition assembly);
    }
}
