using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace nuget_sdk_usage.Analysis.Assembly
{
    internal static class TypeNameGenerator
    {
        internal static (string assembly, string member) GetFullName(EntityHandle handle, MetadataReader metadata)
        {
            var sb = new StringBuilder();

            BuildFullName(handle, metadata, sb, out string assembly);

            return (assembly, sb.ToString());
        }

        private static void BuildFullName(EntityHandle handle, MetadataReader metadata, StringBuilder sb, out string assembly)
        {
            void BuildName(string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (sb.Length != 0)
                    {
                        sb.Append('.');
                    }

                    sb.Append(value);
                }
            }

            void BuildNameFromHandle(StringHandle stringHandle)
            {
                if (!stringHandle.IsNil)
                {
                    var value = metadata.GetString(stringHandle);
                    BuildName(value);
                }
            }

            void BuildMethodNameFromHandle(MemberReference methodReference)
            {
                if (!methodReference.Name.IsNil)
                {
                    var ilName = metadata.GetString(methodReference.Name);
                    var methodName = TransformMethodName(ilName, methodReference, metadata);
                    BuildName(methodName);
                }
            }

            switch (handle.Kind)
            {
                case HandleKind.MemberReference:
                    {
                        var memberReference = metadata.GetMemberReference((MemberReferenceHandle)handle);
                        BuildFullName(memberReference.Parent, metadata, sb, out assembly);
                        BuildMethodNameFromHandle(memberReference);
                    }
                    break;

                case HandleKind.TypeReference:
                    {
                        var typeReference = metadata.GetTypeReference((TypeReferenceHandle)handle);
                        BuildFullName(typeReference.ResolutionScope, metadata, sb, out assembly);
                        BuildNameFromHandle(typeReference.Namespace);
                        BuildNameFromHandle(typeReference.Name);
                    }
                    break;

                case HandleKind.AssemblyReference:
                    {
                        var assemblyReference = metadata.GetAssemblyReference((AssemblyReferenceHandle)handle);
                        assembly = metadata.GetString(assemblyReference.Name);
                    }
                    break;

                case HandleKind.TypeDefinition:
                    {
                        var typeDefinition = metadata.GetTypeDefinition((TypeDefinitionHandle)handle);
                        BuildNameFromHandle(typeDefinition.Namespace);
                        BuildNameFromHandle(typeDefinition.Name);
                        // I think type definitions are always defined in the containing assembly.
                        // When it appears it should belong elsewhere, I think it's an embedded (COM?) type.
                        assembly = GetCurrentAssemblyName(metadata);
                    }
                    break;

                default:
                    throw new NotImplementedException(handle.Kind.ToString());
            }
        }

        private static string GetCurrentAssemblyName(MetadataReader metadata)
        {
            var assemblyDefinition = metadata.GetAssemblyDefinition();

            var assemblyName = assemblyDefinition.GetAssemblyName();
            if (assemblyName.Name != null)
            {
                return assemblyName.Name;
            }

            if (assemblyDefinition.Name.IsNil)
            {
                throw new NotSupportedException();
            }

            return metadata.GetString(assemblyDefinition.Name);
        }

        private static readonly IReadOnlyDictionary<string, string> MethodNameStaticMapping = new Dictionary<string, string>()
        {
            { "op_Equality", "operator ==" },
            { "op_Inequality", "operator !=" },
            { "op_GreaterThan", "operator >" },
            { "op_GreaterThanOrEqual", "operator >=" },
            { "op_LessThan", "operator <" },
            { "op_LessThanOrEqual", "operator <=" }
        };

        private static string TransformMethodName(string ilName, MemberReference memberReference, MetadataReader metadata)
        {
            if (MethodNameStaticMapping.TryGetValue(ilName, out var mappedName))
            {
                return mappedName;
            }

            if (ilName.Equals(".ctor"))
            {
                var entityHandle = memberReference.Parent;
                if (entityHandle.Kind != HandleKind.TypeReference)
                {
                    throw new NotSupportedException("constructor reference was not in a type reference?");
                }
                var typeReference = metadata.GetTypeReference((TypeReferenceHandle)entityHandle);
                var name = metadata.GetString(typeReference.Name);
                return name;
            }

            return ilName;
        }
    }
}
