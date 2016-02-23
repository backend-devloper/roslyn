﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypingStyles
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpUseImplicitTypingDiagnosticAnalyzer : CSharpTypingStyleDiagnosticAnalyzerBase
    {

        private static readonly LocalizableString s_Title =
            new LocalizableResourceString(nameof(CSharpFeaturesResources.UseImplicitTypingDiagnosticTitle), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));

        private static readonly LocalizableString s_Message =
            new LocalizableResourceString(nameof(CSharpFeaturesResources.UseImplicitTyping), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));

        private static readonly DiagnosticDescriptor s_descriptorUseImplicitTyping = new DiagnosticDescriptor(
            id: IDEDiagnosticIds.UseImplicitTypingDiagnosticId,
            title: s_Title,
            messageFormat: s_Message,
            category: DiagnosticCategory.Style,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public CSharpUseImplicitTypingDiagnosticAnalyzer() : base(s_descriptorUseImplicitTyping)
        {

        }

        protected override bool IsStylePreferred(SyntaxNode declarationStatement, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken)
        {
            var stylePreferences = GetCurrentTypingStylePreferences(optionSet);

            var isTypeApparent = IsTypeApparentFromRHS(declarationStatement, semanticModel, cancellationToken);
            var isIntrinsicType = IsIntrinsicType(declarationStatement);

            return stylePreferences.HasFlag(TypingStyles.VarForIntrinsic) && isIntrinsicType
                || stylePreferences.HasFlag(TypingStyles.VarWhereApparent) && isTypeApparent
                || stylePreferences.HasFlag(TypingStyles.VarWherePossible) && !(isIntrinsicType || isTypeApparent);
        }

        protected override bool AnalyzeVariableDeclaration(TypeSyntax typeName, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken, out TextSpan issueSpan)
        {
            issueSpan = default(TextSpan);

            // If it is already var, return.
            if (typeName.IsTypeInferred(semanticModel))
            {
                return false;
            }

            var candidateReplacementNode = SyntaxFactory.IdentifierName("var")
                                                .WithLeadingTrivia(typeName.GetLeadingTrivia())
                                                .WithTrailingTrivia(typeName.GetTrailingTrivia());

            var candidateIssueSpan = typeName.Span;

            // If there exists a type named var, return.
            var conflict = semanticModel.GetSpeculativeSymbolInfo(typeName.SpanStart, candidateReplacementNode, SpeculativeBindingOption.BindAsTypeOrNamespace).Symbol;
            if (conflict != null && conflict.IsKind(SymbolKind.NamedType))
            {
                return false;
            }

            if (typeName.Parent.IsKind(SyntaxKind.VariableDeclaration) &&
                typeName.Parent.Parent.IsKind(SyntaxKind.LocalDeclarationStatement, SyntaxKind.ForStatement, SyntaxKind.UsingStatement))
            {
                var variableDeclaration = (VariableDeclarationSyntax)typeName.Parent;

                // implicitly typed variables cannot be constants.
                var localDeclarationStatement = variableDeclaration.Parent as LocalDeclarationStatementSyntax;
                if (localDeclarationStatement != null && localDeclarationStatement.IsConst)
                {
                    return false;
                }

                var variable = variableDeclaration.Variables.Single();
                if (AnalyzeAssignment(variable.Identifier, typeName, variable.Initializer, semanticModel, optionSet, cancellationToken))
                {
                    issueSpan = candidateIssueSpan;
                }
            }
            else if (typeName.Parent.IsKind(SyntaxKind.ForEachStatement))
            {
                issueSpan = candidateIssueSpan;
            }

            return issueSpan != default(TextSpan);
        }

        protected override bool AnalyzeAssignment(SyntaxToken identifier, TypeSyntax typeName, EqualsValueClauseSyntax initializer, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken)
        {
            // var cannot be assigned null
            if (initializer.Value.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return false;
            }

            // cannot use implicit typing on method group, anonymous function or on dynamic
            var declaredType = semanticModel.GetTypeInfo(typeName, cancellationToken).Type;
            if (declaredType != null
                && (declaredType.TypeKind == TypeKind.Delegate
                || declaredType.TypeKind == TypeKind.Dynamic))
            {
                return false;
            }

            // TODO: deal with ErrorTypeSymbols?

            // TODO: What to do with implicit conversions? For now, using .ConvertedType rather than .Type here.
            // This is a problem. For eg. double x = 4; changing this to var, changes its type from double to int.
            var initializerTypeInfo = semanticModel.GetTypeInfo(initializer.Value, cancellationToken);
            var initializerType = initializerTypeInfo.Type;
            var implicitlyConverted = initializerType != initializerTypeInfo.ConvertedType;

            if (implicitlyConverted)
            {
                return false;
                // TODO, based on tests.
                /*
                *    object obj = 1;
                */
            }
            else
            {
                // check for presence of casts:
                // if types don't match between left and right side of assignment, there could be explicit casts.
                // In such cases, don't replace with var or it would change the semantics.
                if (!declaredType.Equals(initializerType))
                {
                    return false;
                }
            }

            // variables declared using var cannot be used further in the same initialization expression.
            if (initializer.DescendantNodesAndSelf()
                    .Where(n => n.IsKind(SyntaxKind.IdentifierName) && ((IdentifierNameSyntax)n).Identifier.ValueText.Equals(identifier.ValueText))
                    .Any(n => semanticModel.GetSymbolInfo(n, cancellationToken).Symbol?.IsKind(SymbolKind.Local) ?? false))
            {
                return false;
            }

            return true;
        }
    }
}