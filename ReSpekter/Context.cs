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

namespace ReSpekter
{
    using System;
    using System.Collections.Generic;

    using Mono.Cecil;

    /// <summary>
    /// The ReSpekter context.
    /// </summary>
    public class Context
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Context"/> class.
        /// </summary>
        public Context()
        {
            FilterHost = new FilterHost(this);
            ModuleManager = new ModuleManager(this);
        }

        /// <summary>
        /// Gets the filter host.
        /// </summary>
        /// <value>
        /// The filter host.
        /// </value>
        public FilterHost FilterHost { get; private set; }

        /// <summary>
        /// Gets the module manager.
        /// </summary>
        /// <value>
        /// The module manager.
        /// </value>
        public ModuleManager ModuleManager { get; private set; }

        /// <summary>
        /// Adds the specified class to the context.
        /// Before the type is added, it is fed through the filter chain.
        /// </summary>
        /// <param name="type">
        /// The type to add.
        /// </param>
        public void PushClass(Type type)
        {
        }

        public TypeReference GetTypeReference(TypeReference original)
        {
            return ModuleManager.FindReference(original);
        }
    }
}
