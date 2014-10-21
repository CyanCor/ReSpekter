// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="CyanCor GmbH">
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
//   Defines the Program type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace TestApp
{
    using System;
    using System.Threading;
    using CyanCor.ReSpekter;

    class Program 
    {
        private static bool _check;
        private string _test;
        private bool _dirty;

        static void Main(string[] args)
        {
            var context = new Context();
            if (context.Run(new object[] { args }))
            {
                return;
            }

            var test = new MyLittleTestClass();
            var timer = new Timer(OnTimer, test, new TimeSpan(0, 0, 0, 0, 100), new TimeSpan(0, 0, 0, 0, 100));
            
            test.Run();
        }

        private static void OnTimer(object state)
        {
            var test = (MyLittleTestClass)state;
            if (test.Dirty)
            {
                test.Dirty = false;
                Console.WriteLine(test.ImportantData + " persisted.");
            }
        }
    }
}
