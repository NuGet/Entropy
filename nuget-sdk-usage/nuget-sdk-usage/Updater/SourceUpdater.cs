using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nuget_sdk_usage.Updater
{
    internal class SourceUpdater : CSharpSyntaxRewriter
    {
        private Dictionary<string, UpdateAction> _actions;
        private SemanticModel _semanticModel;

        private static readonly AttributeListSyntax attributeListToAdd =
            SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                    SyntaxFactory.Attribute(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.QualifiedName(
                                SyntaxFactory.IdentifierName("NuGet"),
                                SyntaxFactory.IdentifierName("Shared")),
                            SyntaxFactory.IdentifierName("UsedNuGetSdkApi")))))
            .WithTrailingTrivia(SyntaxFactory.Whitespace("\r\n"));

        public SourceUpdater(Dictionary<string, UpdateAction> actions, SemanticModel semanticModel)
        {
            _actions = actions;
            _semanticModel = semanticModel;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var declaredSymbol = _semanticModel.GetDeclaredSymbol(node) ?? throw new NotSupportedException("How could a constructor declaration not have a symbol?");
            var name = declaredSymbol.ToString();

            if (!_actions.TryGetValue(name, out var updateAction))
            {
                return node;
            }

            var newNode = ApplyAction(node, declaredSymbol, updateAction);
            return newNode;
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var newNode = node;

            foreach (var variableDeclaration in node.Declaration.Variables)
            {
                var declaredSymbol = _semanticModel.GetDeclaredSymbol(variableDeclaration) ?? throw new NotSupportedException("How could a field declaration not have a symbol?");
                var name = declaredSymbol.ToString();

                if (!_actions.TryGetValue(name, out var updateAction))
                {
                    continue;
                }

                newNode = ApplyAction(node, declaredSymbol, updateAction);
            }

            return newNode;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var declaredSymbol = _semanticModel.GetDeclaredSymbol(node) ?? throw new NotSupportedException("How could a method declaration not have a symbol?");
            var name = declaredSymbol.ToString();

            if (!_actions.TryGetValue(name, out var updateAction))
            {
                return node;
            }

            var newNode = ApplyAction(node, declaredSymbol, updateAction);
            return newNode;
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var declaredSymbol = _semanticModel.GetDeclaredSymbol(node) ?? throw new NotSupportedException("How could a property declaration not have a symbol?");
            var name = declaredSymbol.ToString();

            if (!_actions.TryGetValue(name, out var action))
            {
                return node;
            }

            var newNode = ApplyAction(node, declaredSymbol, action);
            return newNode;
        }

        private T ApplyAction<T>(T node, ISymbol symbol, UpdateAction updateAction)
            where T : MemberDeclarationSyntax
        {
            var attributes = symbol.GetAttributes();
            var attribute = attributes.SingleOrDefault(a => a.AttributeClass.ToString() == UsedNuGetSdkApiAttribute.FullName);

            T newNode = null;
            if (updateAction.AddAttribute && attribute == null)
            {
                if (node.AttributeLists.Count > 0)
                {
                    var leadingTrivia = node.GetLeadingTrivia();
                    var whitespace = leadingTrivia.Last(t => t.IsKind(SyntaxKind.WhitespaceTrivia));
                    newNode = (T)node.AddAttributeLists(attributeListToAdd.WithLeadingTrivia(whitespace));
                }
                else
                {
                    var leadingTrivia = node.GetLeadingTrivia();
                    if (leadingTrivia.Count == 1 && leadingTrivia[0].IsKind(SyntaxKind.WhitespaceTrivia))
                    {
                        newNode =
                            (T)node.AddAttributeLists(attributeListToAdd.WithLeadingTrivia(leadingTrivia[0]));
                    }
                    else
                    {
                        var whitespaceTrivia = leadingTrivia.First(t => t.IsKind(SyntaxKind.WhitespaceTrivia));
                        newNode =
                            (T)node
                            .WithLeadingTrivia(whitespaceTrivia)
                            .AddAttributeLists(attributeListToAdd.WithLeadingTrivia(leadingTrivia));
                    }
                }
            }
            else if (!updateAction.AddAttribute && attribute != null)
            {
                var newAttributeLists = node.AttributeLists
                    .Select(al =>
                    {
                        AttributeSyntax toRemove = null;

                        foreach (var attr in al.Attributes)
                        {
                            var type = _semanticModel.GetTypeInfo(attr);
                            if (type.Type == null) throw new NotSupportedException("AttributeSyntax's TypeInfo doesn't have a type");
                            if (type.Type.ToString() == UsedNuGetSdkApiAttribute.FullName)
                            {
                                toRemove = attr;
                                break;
                            }
                        }

                        if (toRemove == null)
                        {
                            return al;
                        }

                        return SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(al.Attributes.Where(a => a != toRemove)));
                    })
                    .Where(al => al != null && al.Attributes.Count > 0);

                var leadingTrivia = node.GetLeadingTrivia();
                newNode = (T)node.WithAttributeLists(SyntaxFactory.List(newAttributeLists))
                    .WithLeadingTrivia(leadingTrivia);
            }

            if (newNode != null)
            {
                if (updateAction.Actioned)
                {
                    // already actioned?
                }
                updateAction.SetActioned();

                return newNode;
            }
            else
            {
                return node;
            }
        }
    }
}
