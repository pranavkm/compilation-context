using System;
using Microsoft.Extensions.Primitives;

namespace CompilationContextTest
{
    class TestFile
    {
        public void Method()
        {
            // References a project and a package
            Console.WriteLine(typeof(ClassLib.Class1).Assembly + " " + typeof(StringSegment).Assembly);
        }

        public unsafe void UnsafeMethod()
        {

        }

#if DEFINED_IN_CSPROJ
// Everything is OK
#else
#error Defined in csproj is not found
#endif
    }
}
