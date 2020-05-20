using System.Reflection.Metadata;

namespace nuget_sdk_usage.Analysis.Assembly
{
    internal class TargetFrameworkAttributeDecoder : ICustomAttributeTypeProvider<string>
    {
        public static TargetFrameworkAttributeDecoder Default { get; } = new TargetFrameworkAttributeDecoder();

        public string GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            switch (typeCode)
            {
                case PrimitiveTypeCode.String:
                    return "string";

                default:
                    throw new System.NotImplementedException();
            }
        }

        public string GetSystemType()
        {
            throw new System.NotImplementedException();
        }

        public string GetSZArrayType(string elementType)
        {
            throw new System.NotImplementedException();
        }

        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            throw new System.NotImplementedException();
        }

        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            throw new System.NotImplementedException();
        }

        public string GetTypeFromSerializedName(string name)
        {
            throw new System.NotImplementedException();
        }

        public PrimitiveTypeCode GetUnderlyingEnumType(string type)
        {
            throw new System.NotImplementedException();
        }

        public bool IsSystemType(string type)
        {
            throw new System.NotImplementedException();
        }
    }
}