using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ClassLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyModel;

namespace CompilationContextTest
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                VerifyCompilation();
                VerifyDependencyGraph();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// MVC uses CompileLibraries to determine which libraries depend on MVC packages.
        /// This verification simulates this by checking against references to shared fx
        /// and references to an arbitrary package.
        /// </summary>
        static void VerifyDependencyGraph()
        {
            var dependencyContext = DependencyContext.Load(typeof(Program).Assembly);
            var classifier = new Classifier(dependencyContext.CompileLibraries);

            VerifyDependsOn(
                classifier,
                typeof(Program).Assembly,
                "Microsoft.NETCore.App",
                "Microsoft.Extensions.Primitives");

            VerifyDependsOn(
                classifier,
                typeof(Class1).Assembly,
                "Microsoft.NETCore.App",
                "Microsoft.Extensions.Primitives");

            VerifyDependsOn(
                classifier,
                typeof(Microsoft.AspNetCore.Mvc.Controller).Assembly,
                "Microsoft.Extensions.Primitives");

            VerifyDoesNotDependOn(
                classifier,
                typeof(Microsoft.AspNetCore.Mvc.Controller).Assembly,
                "Microsoft.NETCore.App");

            VerifyDoesNotDependOn(
                classifier,
                typeof(CompilationLibrary).Assembly,
                "Microsoft.Extensions.Primitives");
        }

        /// <summary>
        /// MVC views compile against the same set of references as the application. We'll try compiling a file from the project.
        /// </summary>
        private static void VerifyCompilation()
        {
            var dependencyContext = DependencyContext.Load(typeof(Program).Assembly);
            var references = dependencyContext.CompileLibraries
                .SelectMany(l => l.ResolveReferencePaths())
                .Select(p => MetadataReference.CreateFromFile(p));

            var parseOptions = new CSharpParseOptions(preprocessorSymbols: dependencyContext.CompilationOptions.Defines);
            var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(File.ReadAllText("TestFile.cs")), parseOptions);

            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: dependencyContext.CompilationOptions.AllowUnsafe == true);
            var compilation = CSharpCompilation.Create("Test.dll", new[] { syntaxTree }, references, compilationOptions);
            var emitResult = compilation.Emit(new MemoryStream());

            if (!emitResult.Success)
            {
                var errors = string.Join(Environment.NewLine, emitResult.Diagnostics.Select(d => CSharpDiagnosticFormatter.Instance.Format(d)));
                throw new Exception($"Compilation failed {Environment.NewLine}{errors}");
            }

        }

        private static void VerifyDependsOn(Classifier classifier, Assembly assembly, params string[] dependencies)
        {
            var name = assembly.GetName().Name;
            foreach (var dependencyName in dependencies)
            {
                if (!classifier.DoesLibraryReference(name, dependencyName))
                {
                    throw new Exception($"DependencyContext says library {name} does not depend on {dependencyName}");
                }
            }
        }

        private static void VerifyDoesNotDependOn(Classifier classifier, Assembly assembly, params string[] dependencies)
        {
            var name = assembly.GetName().Name;
            foreach (var dependencyName in dependencies)
            {
                if (classifier.DoesLibraryReference(name, dependencyName))
                {
                    throw new Exception($"DependencyContext says library {name} depends on {dependencyName}");
                }
            }
        }
    }
}
