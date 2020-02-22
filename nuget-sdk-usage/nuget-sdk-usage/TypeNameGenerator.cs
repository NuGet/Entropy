using System;
using System.Reflection.Metadata;
using System.Text;

namespace nuget_sdk_usage
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
            void BuildName(StringHandle stringHandle)
            {
                if (!stringHandle.IsNil)
                {
                    if (sb.Length != 0)
                    {
                        sb.Append('.');
                    }

                    var value = metadata.GetString(stringHandle);
                    sb.Append(value);
                }
            }

            switch (handle.Kind)
            {
                case HandleKind.MemberReference:
                    {
                        var memberReference = metadata.GetMemberReference((MemberReferenceHandle)handle);
                        BuildFullName(memberReference.Parent, metadata, sb, out assembly);
                        BuildName(memberReference.Name);
                    }
                    break;

                case HandleKind.TypeReference:
                    {
                        var typeReference = metadata.GetTypeReference((TypeReferenceHandle)handle);
                        BuildFullName(typeReference.ResolutionScope, metadata, sb, out assembly);
                        BuildName(typeReference.Namespace);
                        BuildName(typeReference.Name);
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
                        var typeDefintion = metadata.GetTypeDefinition((TypeDefinitionHandle)handle);
                        BuildName(typeDefintion.Namespace);
                        BuildName(typeDefintion.Name);
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
            var assemblyDefition = metadata.GetAssemblyDefinition();

            var assemblyName = assemblyDefition.GetAssemblyName();
            if (assemblyName.Name != null)
            {
                return assemblyName.Name;
            }

            if (assemblyDefition.Name.IsNil)
            {
                throw new NotSupportedException();
            }

            return metadata.GetString(assemblyDefition.Name);
        }
    }
}
