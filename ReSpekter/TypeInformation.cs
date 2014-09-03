// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TypeInformation.cs" company="CyanCor GmbH">
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
//   The type information.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReSpekter
{
    using Mono.Cecil;

    /// <summary>
    /// The type information.
    /// </summary>
    internal class TypeInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeInformation"/> class.
        /// </summary>
        /// <param name="fullName">The full name.</param>
        /// <param name="originalType">Type of the original.</param>
        /// <param name="stagedType">Type of the staged.</param>
        public TypeInformation(string fullName, TypeDefinition originalType, TypeDefinition stagedType)
        {
            FullName = fullName;
            OriginalType = originalType;
            StagedType = stagedType;
        }

        public bool Built { get; set; }

        public TypeDefinition StagedType { get; set; }

        public TypeDefinition OriginalType { get; private set; }

        public string FullName { get; private set; }
    }
}