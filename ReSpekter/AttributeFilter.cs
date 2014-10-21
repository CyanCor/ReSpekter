using System;
using System.Linq;
using Mono.Cecil;

namespace CyanCor.ReSpekter
{
    internal class AttributeFilter : IFilter<TypeDefinition>
    {
        private readonly Type[] _types;

        public AttributeFilter(params Type[] types)
        {
            _types = types;
        }

        public bool Check(TypeDefinition subject)
        {
            foreach (var attribute in subject.CustomAttributes)
            {
                if (_types.Any(type => attribute.AttributeType.FullName.Equals(type.FullName)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}