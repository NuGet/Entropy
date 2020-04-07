using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace nuget_sdk_usage.Updater
{
    internal class AttributeUsageFinder : CSharpSyntaxRewriter
    {
        private SemanticModel _semanticModel;
        private List<string> _foundMembers;

        internal AttributeUsageFinder(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
            _foundMembers = new List<string>();
        }

        internal IReadOnlyList<string> FoundMembers => _foundMembers;

        public override SyntaxNode VisitAttribute(AttributeSyntax node)
        {
            var attributeType = _semanticModel.GetTypeInfo(node);
            if (attributeType.Type == null)
            {
                throw new NotSupportedException("How could an attribute not have a type?");
            }

            var displayString = attributeType.Type.ToString();

            if (displayString != UsedNuGetSdkApiAttribute.FullName)
            {
                return base.VisitAttribute(node);
            }

            var attributeList = node.Parent ?? throw new NotSupportedException("How can an attribute not have a parent?");
            var member = attributeList.Parent ?? throw new NotSupportedException("How can an attribute list not have a parent?");
            switch (member)
            {
                case FieldDeclarationSyntax fieldDeclarationSyntax:
                    {
                        foreach (var field in fieldDeclarationSyntax.Declaration.Variables)
                        {
                            var declaredSymbol = _semanticModel.GetDeclaredSymbol(field) ?? throw new NotSupportedException("How can a variable not have a declared symbol?");
                            var name = declaredSymbol.ToDisplayString();
                            _foundMembers.Add(name);
                        }
                    }
                    return base.VisitAttribute(node);

                case MethodDeclarationSyntax methodDeclarationSyntax:
                    {
                        var symbol = _semanticModel.GetDeclaredSymbol(methodDeclarationSyntax) ?? throw new NotSupportedException("How could a declared method not have a declared symbol?");
                        var name = symbol.ToDisplayString();
                        _foundMembers.Add(name);
                    }
                    return base.VisitAttribute(node);

                case ClassDeclarationSyntax classDeclarationSyntax:
                    {
                        var symbol = _semanticModel.GetDeclaredSymbol(classDeclarationSyntax) ?? throw new NotSupportedException("How could a declared class not have a declared symbol?");
                        var name = symbol.ToDisplayString();
                        _foundMembers.Add(name);
                    }
                    return base.VisitAttribute(node);

                case ConstructorDeclarationSyntax constructorDeclarationSyntax:
                    {
                        var symbol = _semanticModel.GetDeclaredSymbol(constructorDeclarationSyntax) ?? throw new NotSupportedException("How could a declared class not have a declared symbol?");
                        var name = symbol.ToDisplayString();
                        _foundMembers.Add(name);
                    }
                    return base.VisitAttribute(node);

                case PropertyDeclarationSyntax propertyDeclarationSyntax:
                    {
                        var symbol = _semanticModel.GetDeclaredSymbol(propertyDeclarationSyntax) ?? throw new NotSupportedException("How could a declared property not have a declared symbol?");
                        var name = symbol.ToDisplayString();
                        _foundMembers.Add(name);
                    }
                    return base.VisitAttribute(node);

                default:
                    throw new NotSupportedException(member.GetType().Name);
            }
        }
    }
}
