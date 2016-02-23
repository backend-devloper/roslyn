﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.UseImplicitTyping
{
    /* TODO :
    *   1. pipe through options
    *   2. Design an options page to support tweaks to settings
    *       e.g: use var 'except' on primitive types, do not use var 'except' when type is apparent from rhs.
    *   3. Refactoring to common base class.
    *       a. UseImplicitType and UseExplicitType : AbstractCSharpUseTypingStyle
    *       b. CSharp and VB implementations to AbstractUseTypingStyle
    */

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpUseImplicitTypingDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        // TODO: 
        // 1. localize title and message
        // 2. tweak severity and custom tags 
        //      a. need to have various levels of diagnostics to report based on option settings.
        private static readonly DiagnosticDescriptor s_descriptorUseImplicitTyping = new DiagnosticDescriptor(
            id: IDEDiagnosticIds.UseImplicitTypingDiagnosticId,
            title: "Use implicit typing",
            messageFormat: "Use var instead of explicit type name",
            category: DiagnosticCategory.Style,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            customTags: DiagnosticCustomTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(s_descriptorUseImplicitTyping);


        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        {
            return DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
        }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: check for generatedcode and bail.
            // context.ConfigureGeneratedCodeAnalysis() See https://github.com/dotnet/roslyn/pull/7526

            context.RegisterSyntaxNodeAction(HandleVariableDeclaration, SyntaxKind.VariableDeclaration);
            context.RegisterSyntaxNodeAction(HandleForEachStatement, SyntaxKind.ForEachStatement);
        }

        private void HandleVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            var variableDeclaration = (VariableDeclarationSyntax)context.Node;

            // var is applicable only for local variables.
            if (variableDeclaration.Parent.IsKind(SyntaxKind.FieldDeclaration) ||
                variableDeclaration.Parent.IsKind(SyntaxKind.EventFieldDeclaration))
            {
                return;
            }

            // implicitly typed variables cannot have multiple declarators and
            // must have an initializer.
            if (variableDeclaration.Variables.Count > 1 ||
                !variableDeclaration.Variables.Single().Initializer.IsKind(SyntaxKind.EqualsValueClause))
            {
                return;
            }

            // TODO: Check options and bail.
            var optionSet = GetOptionSet(context.Options);
            var diagnostic = AnalyzeVariableDeclaration(variableDeclaration, context.SemanticModel, context.CancellationToken);

            if (diagnostic != null)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        // TODO: move this helper to a common place.
        private bool IsTypeApparentFromRHS()
        {
            // constructors of form = new TypeSomething();
            // object creation expression that contains a typename and not an anonymous object creation expression.

            // invocation expression
            // a. int.Parse, TextSpan.From static methods? 
            // return type or 1 ref/out type matches some part of identifier name within a dotted name.
            // also consider Generic method invocation with type parameters *and* not inferred
            // c. Factory Methods

            throw new NotImplementedException();
        }

        private bool IntrinsicTypeInDeclaration()
        {
            // Add support to not use var in place of intrinsic types
            throw new NotImplementedException();
        }

        private void HandleForEachStatement(SyntaxNodeAnalysisContext context)
        {
            var forEachStatement = (ForEachStatementSyntax)context.Node;
            var diagnostic = AnalyzeVariableDeclaration(forEachStatement, context.SemanticModel, context.CancellationToken);

            // TODO: Check options and bail.
            if (diagnostic != null)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        private Diagnostic AnalyzeVariableDeclaration(SyntaxNode declarationStatement,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            TextSpan diagnosticSpan;
            TypeSyntax declaredType;

            if (declarationStatement.IsKind(SyntaxKind.VariableDeclaration))
            {
                declaredType = ((VariableDeclarationSyntax)declarationStatement).Type;
            }
            else if (declarationStatement.IsKind(SyntaxKind.ForEachStatement))
            {
                declaredType = ((ForEachStatementSyntax)declarationStatement).Type;
            }
            else
            {
                Debug.Assert(false, $"unhandled kind {declarationStatement.Kind().ToString()}");
                return null;
            }

            return IsReplaceableByVar(declaredType, semanticModel, cancellationToken, out diagnosticSpan)
                ? Diagnostic.Create(s_descriptorUseImplicitTyping, declarationStatement.SyntaxTree.GetLocation(diagnosticSpan))
                : null;
        }

        private bool IsReplaceableByVar(TypeSyntax typeName, SemanticModel semanticModel, CancellationToken cancellationToken, out TextSpan issueSpan)
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
                Debug.Assert(variableDeclaration.Variables.Count == 1);

                // implicitly typed variables cannot be constants.
                var localDeclarationStatement = variableDeclaration.Parent as LocalDeclarationStatementSyntax;
                if (localDeclarationStatement != null && localDeclarationStatement.IsConst)
                {
                    return false;
                }

                var variable = variableDeclaration.Variables.Single();
                if (CheckAssignment(variable.Identifier, typeName, variable.Initializer, semanticModel, cancellationToken))
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

        private bool CheckAssignment(SyntaxToken identifier, TypeSyntax typeName, EqualsValueClauseSyntax initializer, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // var cannot be assigned null
            if (initializer.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return false;
            }

            // cannot use implicit typing on method group, anonymous function or on dynamic
            var declaredType = semanticModel.GetTypeInfo(typeName, cancellationToken).Type;
            if (declaredType != null &&
                    (declaredType.TypeKind == TypeKind.Delegate ||
                     declaredType.TypeKind == TypeKind.Dynamic))
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

        private OptionSet GetOptionSet(AnalyzerOptions analyzerOptions)
        {
            var workspaceOptions = analyzerOptions as WorkspaceAnalyzerOptions;
            if (workspaceOptions != null)
            {
                return workspaceOptions.Workspace.Options;
            }

            return null;
        }
    }
}