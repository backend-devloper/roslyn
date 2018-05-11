﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.ExtractMethod;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching
{
    /// <summary>
    /// Looks for code of the forms:
    /// 
    ///     var x = o as Type;
    ///     if (x != null) ...
    /// 
    /// and converts it to:
    /// 
    ///     if (o is Type x) ...
    ///     
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal partial class CSharpAsAndNullCheckDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public override bool OpenFileOnly(Workspace workspace) => false;

        public CSharpAsAndNullCheckDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.InlineAsTypeCheckId,
                    new LocalizableResourceString(
                        nameof(FeaturesResources.Use_pattern_matching), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(SyntaxNodeAction,
                SyntaxKind.EqualsExpression,
                SyntaxKind.NotEqualsExpression);

        private void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext)
        {
            var node = syntaxContext.Node;
            var syntaxTree = node.SyntaxTree;

            // "x is Type y" is only available in C# 7.0 and above. Don't offer this refactoring
            // in projects targeting a lesser version.
            if (((CSharpParseOptions)syntaxTree.Options).LanguageVersion < LanguageVersion.CSharp7)
            {
                return;
            }

            var options = syntaxContext.Options;
            var cancellationToken = syntaxContext.CancellationToken;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var styleOption = optionSet.GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck);
            if (!styleOption.Value)
            {
                // Bail immediately if the user has disabled this feature.
                return;
            }

            var comparison = (BinaryExpressionSyntax)node;
            var operand = GetNullCheckOperand(comparison.Left, comparison.Right)?.WalkDownParentheses();
            if (operand == null)
            {
                return;
            }

            var semanticModel = syntaxContext.SemanticModel;
            if (operand.IsKind(SyntaxKind.CastExpression, out CastExpressionSyntax castExpression))
            {
                // Unwrap object cast
                var castType = semanticModel.GetTypeInfo(castExpression.Type).Type;
                if (castType.IsObjectType())
                {
                    operand = castExpression.Expression;
                }
            }

            if (!TryGetTypeCheckParts(
                    semanticModel,
                    operand,
                    out var declarator,
                    out var asExpression,
                    out var localSymbol))
            {
                return;
            }

            var localStatement = declarator.Parent?.Parent;
            var enclosingBlock = localStatement?.Parent;
            if (localStatement == null ||
                enclosingBlock == null)
            {
                return;
            }

            if (semanticModel.GetSymbolInfo(comparison).GetAnySymbol().IsUserDefinedOperator())
            {
                return;
            }

            var typeNode = asExpression.Right;
            var asType = semanticModel.GetTypeInfo(typeNode, cancellationToken).Type;
            if (asType.IsNullable())
            {
                // Not legal to write "x is int? y"
                return;
            }

            if (asType?.TypeKind == TypeKind.Dynamic)
            {
                // Not legal to use dynamic in a pattern.
                return;
            }

            if (!localSymbol.Type.Equals(asType))
            {
                // We have something like:
                //
                //      BaseType b = x as DerivedType;
                //      if (b != null) { ... }
                //
                // It's not necessarily safe to convert this to:
                //
                //      if (x is DerivedType b) { ... }
                //
                // That's because there may be later code that wants to do something like assign a
                // 'BaseType' into 'b'.  As we've now claimed that it must be DerivedType, that
                // won't work.  This might also cause unintended changes like changing overload
                // resolution.  So, we conservatively do not offer the change in a situation like this.
                return;
            }

            var analyzer = new Analyzer(
                semanticModel,
                localSymbol,
                comparison,
                operand,
                localStatement,
                enclosingBlock,
                cancellationToken);
            if (!analyzer.CanSafelyConvertToPatternMatching())
            {
                return;
            }

            // Looks good!
            var additionalLocations = ImmutableArray.Create(
                localStatement.GetLocation(),
                comparison.GetAncestor<StatementSyntax>().GetLocation(),
                comparison.GetLocation(),
                asExpression.GetLocation());

            // Put a diagnostic with the appropriate severity on the declaration-statement itself.
            syntaxContext.ReportDiagnostic(Diagnostic.Create(
                GetDescriptorWithSeverity(styleOption.Notification.Value),
                localStatement.GetLocation(),
                additionalLocations));
        }

        private static bool TryGetTypeCheckParts(
            SemanticModel semanticModel,
            SyntaxNode operand,
            out VariableDeclaratorSyntax declarator,
            out BinaryExpressionSyntax asExpression,
            out ILocalSymbol localSymbol)
        {
            switch (operand.Kind())
            {
                case SyntaxKind.IdentifierName:
                    {
                        // var x = e as T;
                        // if (x != null) F(x);
                        var identifier = (IdentifierNameSyntax)operand;
                        if (!TryFindVariableDeclarator(semanticModel, identifier, out localSymbol, out declarator))
                        {
                            break;
                        }

                        var initializerValue = declarator.Initializer?.Value;
                        if (!initializerValue.IsKind(SyntaxKind.AsExpression, out asExpression))
                        {
                            break;
                        }

                        return true;
                    }

                case SyntaxKind.SimpleAssignmentExpression:
                    {
                        // T x;
                        // if ((x = e as T) != null) F(x);
                        var assignment = (AssignmentExpressionSyntax)operand;
                        if (!assignment.Right.IsKind(SyntaxKind.AsExpression, out asExpression) ||
                            !assignment.Left.IsKind(SyntaxKind.IdentifierName, out IdentifierNameSyntax identifier))
                        {
                            break;
                        }

                        if (!TryFindVariableDeclarator(semanticModel, identifier, out localSymbol, out declarator))
                        {
                            break;
                        }

                        return true;
                    }
            }

            declarator = null;
            asExpression = null;
            localSymbol = null;
            return false;
        }

        private static bool TryFindVariableDeclarator(
            SemanticModel semanticModel,
            IdentifierNameSyntax identifier,
            out ILocalSymbol localSymbol,
            out VariableDeclaratorSyntax declarator)
        {
            localSymbol = semanticModel.GetSymbolInfo(identifier).Symbol as ILocalSymbol;
            declarator = localSymbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;
            return declarator != null;
        }

        private static ExpressionSyntax GetNullCheckOperand(ExpressionSyntax left, ExpressionSyntax right)
        {
            if (left.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return right;
            }

            if (right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return left;
            }

            return null;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
