using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;

namespace nuget_sdk_usage
{
    internal class MethodSignatureDecoder : ISignatureTypeProvider<string, object?>
    {
        public static MethodSignatureDecoder Default { get; } = new MethodSignatureDecoder();

        public string GetArrayType(string elementType, ArrayShape shape)
        {
            throw new System.NotImplementedException();
        }

        public string GetByReferenceType(string elementType)
        {
            return elementType;
        }

        public string GetFunctionPointerType(MethodSignature<string> signature)
        {
            throw new System.NotImplementedException();
        }

        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
        {
            var sb = new StringBuilder();

            sb.Append(genericType);
            sb.Append('<');
            sb.Append(typeArguments[0]);

            for (int i = 1; i < typeArguments.Length; i++)
            {
                sb.Append(", ");
                sb.Append(typeArguments[i]);
            }

            sb.Append('>');

            return sb.ToString();
        }

        public string GetGenericMethodParameter(object genericContext, int index)
        {
            return "T" + index;
        }

        public string GetGenericTypeParameter(object genericContext, int index)
        {
            throw new System.NotImplementedException();
        }

        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired)
        {
            throw new System.NotImplementedException();
        }

        public string GetPinnedType(string elementType)
        {
            throw new System.NotImplementedException();
        }

        public string GetPointerType(string elementType)
        {
            throw new System.NotImplementedException();
        }

        public string GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            switch (typeCode)
            {
                case PrimitiveTypeCode.Boolean: return BooleanName;
                case PrimitiveTypeCode.String: return StringName;
                case PrimitiveTypeCode.Void: return VoidName;
                case PrimitiveTypeCode.Int32: return Int32Name;
                case PrimitiveTypeCode.Byte: return ByteName;
                case PrimitiveTypeCode.Object: return ObjectName;
                case PrimitiveTypeCode.Int64: return Int64Name;
                case PrimitiveTypeCode.IntPtr: return IntPtrName;
                case PrimitiveTypeCode.Double: return DoubleName;
                case PrimitiveTypeCode.Char: return CharName;
                default: throw new System.NotImplementedException(typeCode.ToString());
            }
        }

        public string GetSZArrayType(string elementType)
        {
            return elementType + "[]";
        }

        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            var (_, typeName) = TypeNameGenerator.GetFullName(handle, reader);
            return typeName;
        }

        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            var (_, typeName) = TypeNameGenerator.GetFullName(handle, reader);
            return typeName;
        }

        public string GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            throw new System.NotImplementedException();
        }

#pragma warning disable CS8601 // Possible null reference assignment.
        private static readonly string BooleanName = typeof(bool).FullName;
        private static readonly string StringName = typeof(string).FullName;
        private static readonly string VoidName = typeof(void).FullName;
        private static readonly string Int32Name = typeof(int).FullName;
        private static readonly string ByteName = typeof(byte).FullName;
        private static readonly string ObjectName = typeof(object).FullName;
        private static readonly string Int64Name = typeof(long).FullName;
        private static readonly string IntPtrName = typeof(System.IntPtr).FullName;
        private static readonly string DoubleName = typeof(double).FullName;
        private static readonly string CharName = typeof(char).FullName;
#pragma warning restore CS8601 // Possible null reference assignment.
    }
}