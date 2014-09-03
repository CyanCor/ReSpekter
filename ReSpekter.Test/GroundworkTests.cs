// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GroundworkTests.cs" company="CyanCor GmbH">
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
//   Defines the GroundworkTests type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using ReSpekter.Filters;

namespace ReSpekter.Test
{
    using NUnit.Framework;

    /// <summary>
    /// Some basic tests for smaller stuff.
    /// </summary>
    [TestFixture]
    public class GroundworkTests
    {
        /// <summary>
        /// Tests filter chains basic functionality.
        /// </summary>
        [Test]
        public void BasicTypeCopy()
        {
            var context = new Context();
            var testFilter = new TypeCloneFilter();
            context.FilterHost.TypeFilter.Add(testFilter);
            Assert.True(context.FilterHost.TypeFilter.Count == 2);
            var filterArray = new IFilter<TypeDefinition>[context.FilterHost.TypeFilter.Count];
            context.FilterHost.TypeFilter.CopyTo(filterArray, 0);
            foreach (var filter in filterArray)
            {
                Assert.NotNull(filter);
            }

            Assert.False(((ICollection<IFilter<TypeDefinition>>)context.FilterHost.TypeFilter).IsReadOnly);

            Assert.True(context.FilterHost.TypeFilter.Contains(testFilter));
            context.FilterHost.TypeFilter.Remove(testFilter);
            Assert.False(context.FilterHost.TypeFilter.Contains(testFilter));
            Assert.True(context.FilterHost.TypeFilter.Count == 1);

            context.FilterHost.TypeFilter.Clear();

            foreach (var filter in filterArray)
            {
                Assert.False(context.FilterHost.TypeFilter.Contains(filter));
                context.FilterHost.TypeFilter.Add(filter);
            }

            var enumerator = ((IEnumerable)context.FilterHost.TypeFilter).GetEnumerator();

            while (enumerator.MoveNext())
            {
                Assert.True(filterArray.Contains(enumerator.Current));
            }
        }
    }
}
