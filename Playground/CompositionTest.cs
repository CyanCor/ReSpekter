using System;
using System.Collections.Generic;
using System.Diagnostics;
using CyanCor.ReSpekter.Modifiers;

namespace Playground
{
    public class CompositionTest : ITypeResolver, IResolvableType<string>
    {
        internal static Dictionary<string, object> _dictionary = new Dictionary<string, object>();
        public CompositionTest()
        {
            UniqueIdentifier = Guid.NewGuid().ToString();
            _dictionary.Add(UniqueIdentifier, this);
        }

        public TestComposite TestMember { get; set; }

        public CompositionTest TestMember2 { get; set; }

        public TT ResolveType<TT, TI>(TI identifier) where TT : class, IResolvableType<TI>
        {
            object result;
            _dictionary.TryGetValue(identifier.ToString(), out result);
            return result as TT;
        }

        public string UniqueIdentifier { get; private set; }

        public void DoStuff()
        {
            Debugger.Break();
        }
    }
}