﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestBaseClass.cs" company="CyanCor GmbH">
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
//   Defines the TestBaseClass type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using CyanCor.ReSpekter.Modifiers;

namespace ReSpekter.Test
{
    /// <summary>
    /// The test base class.
    /// </summary>
    public class TestBaseClass : IDirty
    {
        private int _blob;

        public TestBaseClass Test(TestBaseClass address)
        {
            _blob++;
            return null;
        }

        public int AnotherTest()
        {
            int i = _blob;
            i = i + 8;
            return i;
        }

        public bool Dirty { get; set; }
    }
}