// (c) gfoidl, all rights reserved

namespace System.Runtime.CompilerServices
{
    // It's a .NET Standard 2.0 project, but for records this is needed to make the compiler happy :-)
    internal class IsExternalInit { }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class NotNullWhenAttribute : Attribute
    {
        public bool ReturnValue { get; }

        public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
    }
}
