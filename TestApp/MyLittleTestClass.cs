// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MyLittleTestClass.cs" company="CyanCor GmbH">
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
//   Defines the MyLittleTestClass type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Security;

namespace TestApp
{
    using System;
    using CyanCor.ReSpekter.Modifiers;
    
    internal class MyLittleTestClass : IDirty, ITypeResolver
    {
        private bool _dirty;
        public string ImportantData;
        private string _testMemberUniqueIdentifier;
        private readonly WeakReference<CompositeTest> _testMemberWeakReference = new WeakReference<CompositeTest>(null);

        public CompositeTest TestMember
        {
            get
            {
                CompositeTest test;
                lock (_testMemberWeakReference)
                {
                    if (!_testMemberWeakReference.TryGetTarget(out test))
                    {
                        test = ResolveType<CompositeTest, string>(_testMemberUniqueIdentifier);
                        _testMemberWeakReference.SetTarget(test);
                    }
                }

                return test;
            }

            set
            {
                if (value != null)
                {
                    Debugger.Break();
                    _testMemberUniqueIdentifier = value.UniqueIdentifier;
                    _testMemberWeakReference.SetTarget(value);
                }
            }
        }

        public void Run()
        {
            Debugger.Break();
            TestMember.TestMember = "blubb";

            Console.WriteLine(TestMember.TestMember);
            while (true)
            {
                var s = Console.ReadLine();
                if (ImportantData != s)
                {
                    ImportantData = s;
                }
            }
        }

        public bool Dirty
        {
            get { return _dirty; }
            set { _dirty = value; }
        }

        public TT ResolveType<TT, TI>(TI identifer) where TT : class, IResolvableType<TI>
        {
            var result = new CompositeTest() as TT;
            return result;
        }
    }

    internal class CompositeTest : IResolvableType<string>
    {
        public string UniqueIdentifier { get; private set; }

        public string TestMember;
    }
}
