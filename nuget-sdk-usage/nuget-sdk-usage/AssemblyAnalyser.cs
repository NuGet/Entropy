using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;

namespace nuget_sdk_usage
{
    // There isn't much documentation on System.Reflection.Metadata.
    // Apparently it follows the CLI spec fairly closely, so maybe that helps: https://www.ecma-international.org/publications/standards/Ecma-335.htm
    // This app does a very similar job to the .NET API Portability Analyzer, which also uses System.Reflection.Metadata: https://github.com/microsoft/dotnet-apiport
    internal static class AssemblyAnalyser
    {
        internal static bool HasReferenceToNuGetAssembly(MetadataReader metadata)
        {
            foreach (var assemblyReferenceHandle in metadata.AssemblyReferences)
            {
                var assemblyReference = metadata.GetAssemblyReference(assemblyReferenceHandle);

                var assemblyName = metadata.GetString(assemblyReference.Name);

                if (NuGetAssembly.MatchesName(assemblyName) && !assemblyReference.PublicKeyOrToken.IsNil)
                {
                    var publicToken = metadata.GetBlobBytes(assemblyReference.PublicKeyOrToken);
                    if (NuGetAssembly.IsMicrosoftPublicToken(publicToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static UsageInformation FindUsedNuGetApis(MetadataReader metadata)
        {
            var result = new UsageInformation();

            void AddMember(string assembly, string member)
            {
                if (!result.MemberReferences.TryGetValue(assembly, out HashSet<string>? apis))
                {
                    apis = new HashSet<string>();
                    result.MemberReferences[assembly] = apis;
                }

                apis.Add(member);
            }

            foreach (var memberReferenceHandle in metadata.MemberReferences)
            {
                var memberReference = metadata.GetMemberReference(memberReferenceHandle);
                var foundAssemblyReference = TryFindAssemblyReference(memberReferenceHandle, metadata, out var assemblyReference);

                if (foundAssemblyReference && DefinedInNuGetAssembly(assemblyReference))
                {
                    var assemblyName = assemblyReference.GetAssemblyName();
                    var version = assemblyName.Version?.ToString();
                    if (version != null)
                    {
                        result.Versions.Add(version);
                    }

                    var kind = memberReference.GetKind();
                    switch (kind)
                    {
                        case MemberReferenceKind.Method:
                            {
                                var (assembly, member) = TypeNameGenerator.GetFullName(memberReferenceHandle, metadata);
                                var sig = memberReference.DecodeMethodSignature(MethodSignatureDecoder.Default, null);
                                var methodSignature = GetMethodSignature(member, sig);
                                AddMember(assembly, methodSignature);
                            }
                            break;

                        case MemberReferenceKind.Field:
                            {
                                var (assembly, member) = TypeNameGenerator.GetFullName(memberReferenceHandle, metadata);
                                AddMember(assembly, member);
                            }
                            break;

                        default:
                            throw new NotImplementedException(kind.ToString());
                    }
                }
            }

            if (result.MemberReferences.SelectMany(r => r.Value).Any())
            {
                if (TryGetTargetFramework(metadata, out string? targetFramework))
                {
                    result.TargetFrameworks.Add(targetFramework);
                }
            }

            return result;
        }

        private static string GetMethodSignature(string member, MethodSignature<string> sig)
        {
            var sb = new StringBuilder();

            sb.Append(member);
            sb.Append('(');
            for (int i = 0; i < sig.ParameterTypes.Length; i++)
            {
                if (i != 0)
                {
                    sb.Append(", ");
                }

                sb.Append(sig.ParameterTypes[i]);
            }
            sb.Append(')');

            return sb.ToString();
        }

        private static bool DefinedInNuGetAssembly(AssemblyReference assemblyReference)
        {
            var assemblyName = assemblyReference.GetAssemblyName();

            if (assemblyName.Name != null && !NuGetAssembly.MatchesName(assemblyName.Name))
            {
                return false;
            }

            // The strong name keys NuGet uses is in the repo, as is good practise, so we can't be sure
            // that the 
            var publicKeyToken = assemblyName.GetPublicKeyToken();
            if (publicKeyToken == null)
            {
                return false;
            }

            return NuGetAssembly.IsMicrosoftPublicToken(publicKeyToken);
        }

        private static bool TryFindAssemblyReference(EntityHandle handle, MetadataReader metadata, out AssemblyReference assemblyReference)
        {
            assemblyReference = default;
            switch (handle.Kind)
            {
                case HandleKind.AssemblyReference:
                    {
                        assemblyReference = metadata.GetAssemblyReference((AssemblyReferenceHandle)handle);
                        return true;
                    }

                case HandleKind.TypeReference:
                    {
                        var typeReference = metadata.GetTypeReference((TypeReferenceHandle)handle);
                        return TryFindAssemblyReference(typeReference.ResolutionScope, metadata, out assemblyReference);
                    }

                case HandleKind.TypeSpecification:
                    {
                        // I don't know what this is, or how to find the assembly the type is defined in.
                        return false;
                    }

                case HandleKind.MemberReference:
                    {
                        var memberReference = metadata.GetMemberReference((MemberReferenceHandle)handle);
                        return TryFindAssemblyReference(memberReference.Parent, metadata, out assemblyReference);
                    }

                default:
                    throw new NotImplementedException(handle.Kind.ToString());
            }
        }

        private static bool TryGetTargetFramework(MetadataReader metadata, [NotNullWhen(true)] out string? targetFramework)
        {
            var assembly = metadata.GetAssemblyDefinition();
            var attributeHandles = assembly.GetCustomAttributes();

            foreach (var attributeHandle in attributeHandles)
            {
                var attribute = metadata.GetCustomAttribute(attributeHandle);
                var (assemblyName, name) = TypeNameGenerator.GetFullName(attribute.Constructor, metadata);
                if (assemblyName == "mscorlib" && name == "System.Runtime.Versioning.TargetFrameworkAttribute..ctor")
                {
                    var decoded = attribute.DecodeValue(TargetFrameworkAttributeDecoder.Default);
                    var frameworkDisplayName = decoded.NamedArguments.SingleOrDefault(arg => arg.Name == "FrameworkDisplayName");
                    if (frameworkDisplayName.Name == "FrameworkDisplayName")
                    {
                        targetFramework = (string)frameworkDisplayName.Value;
                        return true;
                    }
                }
            }

            targetFramework = null;
            return false;
        }
    }
}
