using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyModel;

namespace CompilationContextTest
{
    internal class Classifier
    {
        private readonly IReadOnlyList<Library> _libraries;

        public Classifier(IReadOnlyList<Library> libraries) => _libraries = libraries;

        public bool DoesLibraryReference(string library, string dependency)
        {
            var classification = ComputeClassification(new Dictionary<string, DependencyClassification>(StringComparer.Ordinal), library, dependency);
            return classification == DependencyClassification.References;
        }

        private DependencyClassification ComputeClassification(Dictionary<string, DependencyClassification> state, string libraryName, string dependency)
        {
            if (dependency == libraryName)
            {
                return DependencyClassification.IsReference;
            }

            if (state.TryGetValue(libraryName, out var result))
            {
                return result;
            }
            else
            {
                var library = _libraries.FirstOrDefault(l => l.Name == libraryName);
                if (library == null)
                {
                    throw new Exception($"Cannot find library {libraryName} in dependency graph");
                }

                var classification = DependencyClassification.DoesNotReference;
                foreach (var candidateDependency in library.Dependencies)
                {
                    var dependencyClassification = ComputeClassification(state, candidateDependency.Name, dependency);
                    if (dependencyClassification == DependencyClassification.References ||
                        dependencyClassification == DependencyClassification.IsReference)
                    {
                        classification = DependencyClassification.References;
                        break;
                    }
                }

                state[libraryName] = classification;
                return classification;
            }
        }

        private enum DependencyClassification
        {
            References = 1,

            DoesNotReference = 2,

            IsReference = 3,
        }
    }
}
