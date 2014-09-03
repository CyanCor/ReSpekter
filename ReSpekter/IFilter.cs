﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IFilter.cs" company="CyanCor GmbH">
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
//   The IFilter interface describes a simple filter stage.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReSpekter
{
    /// <summary>
    /// The IFilter interface describes a simple filter stage.
    /// </summary>
    /// <typeparam name="T">The type of element that should be filtered.</typeparam>
    public interface IFilter<T>
    {
        /// <summary>
        /// Processes the specified element.
        /// </summary>
        /// <param name="stage">The stage represents the current state that the element is in.
        /// It may be reused and modified as it is no longer needed afterwards.</param>
        /// <param name="original">The original elemet for reference. Do not edit this.</param>
        /// <param name="context">The context that the element is being created in.</param>
        /// <returns>The resulting element after filtering. This may as well be the modified stage element.</returns>
        T Process(T stage, T original, Context context);
    }
}
