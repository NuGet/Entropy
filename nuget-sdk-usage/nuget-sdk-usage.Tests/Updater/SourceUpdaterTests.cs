using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using nuget_sdk_usage.Updater;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace nuget_sdk_usage.Tests.Updater
{
    public class SourceUpdaterTests
    {
        [Theory]
        [InlineData(SourceType.Constructor, SourceType.ConstructorWithAttribute, true)]
        [InlineData(SourceType.ConstructorWithDoc, SourceType.ConstructorWithDocAndAttribute, true)]
        [InlineData(SourceType.ConstructorWithAttribute, SourceType.Constructor, false)]
        [InlineData(SourceType.ConstructorWithDocAndAttribute, SourceType.ConstructorWithDoc, false)]
        public void Visit_Constructor(SourceType inputSource, SourceType expectedSource, bool add)
        {
            var input = GetSource(inputSource);
            var expected = GetSource(expectedSource);
            var memberName = "ClassLibrary1.Class1.Class1()";

            var action = new KeyValuePair<string, UpdateAction>(memberName, new UpdateAction(add));

            AssertAction(input, action, expected);
        }

        [Theory]
        [InlineData(SourceType.Field, SourceType.FieldWithAttribute, true)]
        [InlineData(SourceType.FieldWithDoc, SourceType.FieldWithDocAndAttribute, true)]
        [InlineData(SourceType.FieldWithAttribute, SourceType.Field, false)]
        [InlineData(SourceType.FieldWithDocAndAttribute, SourceType.FieldWithDoc, false)]
        public void Visit_Field(SourceType inputSource, SourceType expectedSource, bool add)
        {
            var input = GetSource(inputSource);
            var expected = GetSource(expectedSource);
            var memberName = "ClassLibrary1.Class1.Field";

            var action = new KeyValuePair<string, UpdateAction>(memberName, new UpdateAction(add));

            AssertAction(input, action, expected);
        }

        [Theory]
        [InlineData(SourceType.Method, SourceType.MethodWithAttribute, true)]
        [InlineData(SourceType.MethodWithDoc, SourceType.MethodWithDocAndAttribute, true)]
        [InlineData(SourceType.MethodWithAttribute, SourceType.Method, false)]
        [InlineData(SourceType.MethodWithDocAndAttribute, SourceType.MethodWithDoc, false)]
        public void Visit_Method(SourceType inputSource, SourceType expectedSource, bool add)
        {
            var input = GetSource(inputSource);
            var expected = GetSource(expectedSource);
            var memberName = "ClassLibrary1.Class1.Method()";

            var action = new KeyValuePair<string, UpdateAction>(memberName, new UpdateAction(add));

            AssertAction(input, action, expected);
        }

        [Theory]
        [InlineData(SourceType.Property, SourceType.PropertyWithAttribute, true)]
        [InlineData(SourceType.PropertyWithDoc, SourceType.PropertyWithDocAndAttribute, true)]
        [InlineData(SourceType.PropertyWithAttribute, SourceType.Property, false)]
        [InlineData(SourceType.PropertyWithDocAndAttribute, SourceType.PropertyWithDoc, false)]
        public void Visit_Property(SourceType inputSource, SourceType expectedSource, bool add)
        {
            var input = GetSource(inputSource);
            var expected = GetSource(expectedSource);
            var memberName = "ClassLibrary1.Class1.Property";

            var action = new KeyValuePair<string, UpdateAction>(memberName, new UpdateAction(add));

            AssertAction(input, action, expected);
        }

        [Fact]
        public void Visit_FieldWithAnotherAttribute()
        {
            var input = @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        /// <Summary>Doc</Summary>
        [Obsolete]
        public string Field;
    }
}";

            var expected = @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        /// <Summary>Doc</Summary>
        [Obsolete]
        [NuGet.Shared.UsedNuGetSdkApi]
        public string Field;
    }
}";

            var memberName = "ClassLibrary1.Class1.Field";

            var action = new KeyValuePair<string, UpdateAction>(memberName, new UpdateAction(addAttribute: true));

            AssertAction(input, action, expected);
        }

        private void AssertAction(string original, KeyValuePair<string, UpdateAction> action, string expected)
        {
            // Arrange
            var syntaxTree = CSharpSyntaxTree.ParseText(original);
            var assemblyName = Path.GetRandomFileName();
            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree, AttributeSyntax },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var semanticModel = compilation.GetSemanticModel(syntaxTree, false);

            var actions = new Dictionary<string, UpdateAction>()
            {
                { action.Key, action.Value }
            };

            var target = new SourceUpdater(actions, semanticModel);

            // Act
            var result = target.Visit(syntaxTree.GetRoot());

            // Assert
            Assert.True(action.Value.Actioned);

            var text = result.ToFullString();
            Assert.Equal(expected, text);
        }

        private static readonly SyntaxTree AttributeSyntax = CSharpSyntaxTree.ParseText(@"
namespace NuGet.Shared
{
    public class UsedNuGetSdkApiAttribute : Attribute
    {
    }
}
");

        public enum SourceType
        {
            Constructor,
            ConstructorWithAttribute,
            ConstructorWithDoc,
            ConstructorWithDocAndAttribute,
            Field,
            FieldWithAttribute,
            FieldWithDoc,
            FieldWithDocAndAttribute,
            Method,
            MethodWithAttribute,
            MethodWithDoc,
            MethodWithDocAndAttribute,
            Property,
            PropertyWithAttribute,
            PropertyWithDoc,
            PropertyWithDocAndAttribute,
        }

        private string GetSource(SourceType sourceType)
        {
            return sourceType switch
            {
                SourceType.Constructor => @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        public Class1()
        {
        }
    }
}",

                SourceType.ConstructorWithAttribute => @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        [NuGet.Shared.UsedNuGetSdkApi]
        public Class1()
        {
        }
    }
}",

                SourceType.ConstructorWithDoc => @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        /// <Summary>Doc</Summary>
        public Class1()
        {
        }
    }
}",

                SourceType.ConstructorWithDocAndAttribute => @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        /// <Summary>Doc</Summary>
        [NuGet.Shared.UsedNuGetSdkApi]
        public Class1()
        {
        }
    }
}",

                SourceType.Field => @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        public string Field;
    }
}",

                SourceType.FieldWithAttribute => @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        [NuGet.Shared.UsedNuGetSdkApi]
        public string Field;
    }
}",

                SourceType.FieldWithDoc => @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        /// <Summary>Doc</Summary>
        public string Field;
    }
}",

                SourceType.FieldWithDocAndAttribute => @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        /// <Summary>Doc</Summary>
        [NuGet.Shared.UsedNuGetSdkApi]
        public string Field;
    }
}",

                SourceType.Method => @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        public void Method()
        {
        }
    }
}",

                SourceType.MethodWithAttribute => @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        [NuGet.Shared.UsedNuGetSdkApi]
        public void Method()
        {
        }
    }
}",

                SourceType.MethodWithDoc => @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        /// <Summary>Doc</Summary>
        public void Method()
        {
        }
    }
}",

                SourceType.MethodWithDocAndAttribute => @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        /// <Summary>Doc</Summary>
        [NuGet.Shared.UsedNuGetSdkApi]
        public void Method()
        {
        }
    }
}",

                SourceType.Property => @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        public int Property { get; set; }
    }
}",

                SourceType.PropertyWithAttribute => @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        [NuGet.Shared.UsedNuGetSdkApi]
        public int Property { get; set; }
    }
}",

                SourceType.PropertyWithDoc => @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        /// <Summary>Doc</Summary>
        public int Property { get; set; }
    }
}",

                SourceType.PropertyWithDocAndAttribute => @"using System;

namespace ClassLibrary1
{
    class Class1
    {
        /// <Summary>Doc</Summary>
        [NuGet.Shared.UsedNuGetSdkApi]
        public int Property { get; set; }
    }
}",

                _ => throw new NotImplementedException(sourceType.ToString())
            };
        }
    }
}
