// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Class1.cs" company="">
//   
// </copyright>
// <summary>
//   Defines the CompositionTest type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------



namespace Playground
{
    using System;
    using CyanCor.ReSpekter.Modifiers;

    public class TestComposite : IResolvableType<string>
    {
        TestComposite()
        {
            UniqueIdentifier = Guid.NewGuid().ToString();
            CompositionTest._dictionary.Add(UniqueIdentifier, this);
        }
        public string UniqueIdentifier { get; private set; }
    }
}
