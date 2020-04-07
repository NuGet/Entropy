using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;

namespace nuget_sdk_usage.Analysis.Assembly
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

                if (NuGetAssembly.IsNuGetAssembly(assemblyReference))
                {
                    return true;
                }
            }

            return false;
        }

        internal static UsageInformation FindUsedNuGetApis(MetadataReader metadata)
        {
            var result = new UsageInformation();

            void AddMember(string assembly, string member)
            {
                if (!result.MemberReferences.TryGetValue(assembly, out HashSet<string> apis))
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

                if (foundAssemblyReference && NuGetAssembly.IsNuGetAssembly(assemblyReference))
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
                                if (TryGetPropertyName(member, out string propertyName))
                                {
                                    AddMember(assembly, propertyName);
                                }
                                else
                                {
                                    var sig = memberReference.DecodeMethodSignature(MethodSignatureDecoder.Default, null);
                                    var methodSignature = GetMethodSignature(member, sig);
                                    AddMember(assembly, methodSignature);
                                }
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
                if (TryGetTargetFramework(metadata, out string targetFramework))
                {
                    result.TargetFrameworks.Add(targetFramework);
                }
            }

            return result;
        }

        private static bool TryGetPropertyName(string member, out string propertyFullName)
        {
            var lastPeriodIndex = member.LastIndexOf('.');
            string propertyMethodPrefix = member.Length >= lastPeriodIndex + 5
                ? member.Substring(lastPeriodIndex + 1, 4)
                : null;

            bool IsPropertyMethodPrefix(string prefix)
            {
                return prefix != null && (prefix == "get_" || prefix == "set_");
            }

            if (!IsPropertyMethodPrefix(propertyMethodPrefix))
            {
                propertyFullName = null;
                return false;
            }

            string namespaceAndClass = member.Substring(0, lastPeriodIndex + 1);
            string propertyName = member.Substring(lastPeriodIndex + 5);
            propertyFullName = namespaceAndClass + propertyName;
            return true;
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
                        // To do this properly, we need to call TypeSpecification.DecodeSignature, but use an
                        // ISignatureTypeProvider that allows us to get the assembly, rather than the type name.
                        // Something to do later.
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

        private static bool TryGetTargetFramework(MetadataReader metadata, out string targetFramework)
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
