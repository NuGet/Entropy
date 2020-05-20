using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;

namespace nuget_sdk_usage.Analysis.Assembly
{
    internal class MethodSignatureDecoder : ISignatureTypeProvider<string, object>
    {
        public static MethodSignatureDecoder Default { get; } = new MethodSignatureDecoder();

        public string GetArrayType(string elementType, ArrayShape shape)
        {
            throw new System.NotImplementedException();
        }

        public string GetByReferenceType(string elementType)
        {
            // I think IL doesn't have a difference between what C# calls `out` and `ref`. `out` is just `ref`
            // with the convention that it always sets a value. Currently, NuGet.Client doesn't use `ref` in
            // public APIs, but if it does one day, then nuget-sdk-usage needs to be updated to deal with it.
            return "out " + elementType;
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
        private static readonly string BooleanName = "bool";
        private static readonly string StringName = "string";
        private static readonly string VoidName = "void";
        private static readonly string Int32Name = "int";
        private static readonly string ByteName = "byte";
        private static readonly string ObjectName = "object";
        private static readonly string Int64Name = "long";
        private static readonly string IntPtrName = typeof(System.IntPtr).FullName;
        private static readonly string DoubleName = "double";
        private static readonly string CharName = "char";
#pragma warning restore CS8601 // Possible null reference assignment.
    }
}