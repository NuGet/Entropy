using nuget_sdk_usage.Analysis.Assembly;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Xunit;

namespace nuget_sdk_usage.Tests.Analysis.Assembly
{
    public class TypeNameGeneratorTests
    {
        [Fact]
        public void FindUsedNuGetApis_AssemblyUsesOperatorMethods_ReturnsCorrectOperatorMethodNames()
        {
            UsageInformation usageInformation;
            using (var fileStream = File.OpenRead(typeof(NuGetVersioningOperators.Class1).Assembly.Location))
            {
                using (var peReader = new PEReader(fileStream))
                {
                    var metadata = peReader.GetMetadataReader();
                    usageInformation = AssemblyAnalyser.FindUsedNuGetApis(metadata);
                }
            }

            HashSet<string> expected = new HashSet<string>()
            {
                "NuGet.Versioning.NuGetVersion.Parse(string)",
                "NuGet.Versioning.SemanticVersion.operator ==(NuGet.Versioning.SemanticVersion, NuGet.Versioning.SemanticVersion)",
                "NuGet.Versioning.SemanticVersion.operator !=(NuGet.Versioning.SemanticVersion, NuGet.Versioning.SemanticVersion)",
                "NuGet.Versioning.SemanticVersion.operator >(NuGet.Versioning.SemanticVersion, NuGet.Versioning.SemanticVersion)",
                "NuGet.Versioning.SemanticVersion.operator >=(NuGet.Versioning.SemanticVersion, NuGet.Versioning.SemanticVersion)",
                "NuGet.Versioning.SemanticVersion.operator <(NuGet.Versioning.SemanticVersion, NuGet.Versioning.SemanticVersion)",
                "NuGet.Versioning.SemanticVersion.operator <=(NuGet.Versioning.SemanticVersion, NuGet.Versioning.SemanticVersion)"
            };

            var (assembly, memberReferences) = Assert.Single(usageInformation.MemberReferences);
            Assert.Equal("NuGet.Versioning", assembly);
            AssertHashSets(expected, memberReferences);
        }

        [Fact]
        public void FindUsedNuGetApis_AssemblyUsesConstructor_ReturnsCorrectMethodNames()
        {
            UsageInformation usageInformation;
            using (var fileStream = File.OpenRead(typeof(NuGetVersioningConstructor.Class1).Assembly.Location))
            {
                using (var peReader = new PEReader(fileStream))
                {
                    var metadata = peReader.GetMetadataReader();
                    usageInformation = AssemblyAnalyser.FindUsedNuGetApis(metadata);
                }
            }

            HashSet<string> expected = new HashSet<string>()
            {
                "NuGet.Versioning.SemanticVersion.SemanticVersion(int, int, int)"
            };

            var (assembly, memberReferences) = Assert.Single(usageInformation.MemberReferences);
            Assert.Equal("NuGet.Versioning", assembly);
            AssertHashSets(expected, memberReferences);
        }

        [Fact]
        public void FindUsedNuGetApis_AssemblyUsesProperty_ReturnsCorrectName()
        {
            UsageInformation usageInformation;
            using (var fileStream = File.OpenRead(typeof(NuGetVersioningProperty.Class1).Assembly.Location))
            {
                using (var peReader = new PEReader(fileStream))
                {
                    var metadata = peReader.GetMetadataReader();
                    usageInformation = AssemblyAnalyser.FindUsedNuGetApis(metadata);
                }
            }

            HashSet<string> expected = new HashSet<string>()
            {
                "NuGet.Versioning.VersionRange.All"
            };

            var (assembly, memberReferences) = Assert.Single(usageInformation.MemberReferences);
            Assert.Equal("NuGet.Versioning", assembly);
            AssertHashSets(expected, memberReferences);
        }

        [Fact]
        public void FindUsedNuGetApis_AssemblyUsesOutParameter_ReturnsCorrectName()
        {
            UsageInformation usageInformation;
            using (var fileStream = File.OpenRead(typeof(NuGetVersioningOutParameter.Class1).Assembly.Location))
            {
                using (var peReader = new PEReader(fileStream))
                {
                    var metadata = peReader.GetMetadataReader();
                    usageInformation = AssemblyAnalyser.FindUsedNuGetApis(metadata);
                }
            }

            HashSet<string> expected = new HashSet<string>()
            {
                "NuGet.Versioning.NuGetVersion.TryParse(string, out NuGet.Versioning.NuGetVersion)"
            };

            var (assembly, memberReferences) = Assert.Single(usageInformation.MemberReferences);
            Assert.Equal("NuGet.Versioning", assembly);
            AssertHashSets(expected, memberReferences);
        }

        private void AssertHashSets(HashSet<string> expected, HashSet<string> actual)
        {
            var missing = expected.ToHashSet();
            var extra = actual.ToHashSet();

            foreach (var value in expected)
            {
                extra.Remove(value);
            }

            foreach (var value in actual)
            {
                missing.Remove(value);
            }

            if (missing.Count > 0 || extra.Count > 0)
            {
                var sb = new StringBuilder();
                if (missing.Count > 0)
                {
                    sb.Append("Missing: ");
                    bool first = true;
                    foreach (var value in missing)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            sb.Append(", ");
                        }

                        sb.Append(value);
                    }
                }

                if (extra.Count > 0)
                {
                    if (missing.Count > 0)
                    {
                        sb.AppendLine();
                    }

                    sb.Append("extra: ");
                    bool first = true;
                    foreach (var value in extra)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            sb.Append(", ");
                        }

                        sb.Append(value);
                    }
                }

                throw new System.Exception(sb.ToString());
            }
        }
    }
}
