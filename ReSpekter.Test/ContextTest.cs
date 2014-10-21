// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ContextTest.cs" company="CyanCor GmbH">
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
//   Defines the ContextTest type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReSpekter.Test
{
    using CyanCor.ReSpekter;
    using NUnit.Framework;

    /// <summary>
    /// Tests the context.
    /// </summary>
    [TestFixture]
    public class ContextTest
    {
        /// <summary>
        /// Tests basic type copying.
        /// </summary>
        [Test]
        public static void BasicTypeCopy()
        {
            var context = new Context();
            if (context.Run(new object[0]))
            {
                return;
            }

            var c = new BasicTestClass();
            c.MakeMeDirty();
            Assert.True(c.Dirty);
            c.Dirty = false;
            Assert.False(c.Dirty);
            c.Test(null);
            Assert.True(c.Dirty);
            c.Dirty = false;
            c.AnotherTest();
            Assert.False(c.Dirty);
        }
    }
}