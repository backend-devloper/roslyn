﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames
    Friend Class TypeSyntaxSimplifierWalker
        Inherits VisualBasicSyntaxWalker

        Private Shared ReadOnly s_predefinedTypeMetadataNames As ImmutableHashSet(Of String) = ImmutableHashSet.Create(
            CaseInsensitiveComparison.Comparer,
            NameOf([Boolean]),
            NameOf([SByte]),
            NameOf([Byte]),
            NameOf(Int16),
            NameOf(UInt16),
            NameOf(Int32),
            NameOf(UInt32),
            NameOf(Int64),
            NameOf(UInt64),
            NameOf([Single]),
            NameOf([Double]),
            NameOf([Decimal]),
            NameOf([String]),
            NameOf([Char]),
            NameOf(DateTime),
            NameOf([Object]))

        Private ReadOnly _analyzer As VisualBasicSimplifyTypeNamesDiagnosticAnalyzer
        Private ReadOnly _semanticModel As SemanticModel
        Private ReadOnly _optionSet As OptionSet
        Private ReadOnly _cancellationToken As CancellationToken

        ''' <summary>
        ''' Set of type and namespace names that have an alias associated with them.  i.e. if the
        ''' user has <c>Imports X = System.DateTime</c>, then <c>DateTime</c> will be in this set.
        ''' This is used so we can easily tell if we should try to simplify some identifier to an
        ''' alias when we encounter it.
        ''' </summary>
        Private _aliasedNames As ImmutableHashSet(Of String) = ImmutableHashSet.Create(Of String)(CaseInsensitiveComparison.Comparer)

        Public ReadOnly Property Diagnostics As List(Of Diagnostic) = New List(Of Diagnostic)()

        Public Sub New(analyzer As VisualBasicSimplifyTypeNamesDiagnosticAnalyzer, semanticModel As SemanticModel, optionSet As OptionSet, cancellationToken As CancellationToken)
            MyBase.New(SyntaxWalkerDepth.StructuredTrivia)

            _analyzer = analyzer
            _semanticModel = semanticModel
            _optionSet = optionSet
            _cancellationToken = cancellationToken

            For Each aliasSymbol In semanticModel.Compilation.AliasImports()
                _aliasedNames = _aliasedNames.Add(aliasSymbol.Target.Name)
            Next
        End Sub

        Public Overrides Sub VisitSimpleImportsClause(node As SimpleImportsClauseSyntax)
            If node.Alias IsNot Nothing Then
                Dim identifierName = TryCast(node.Name.GetRightmostName(), IdentifierNameSyntax)
                If identifierName IsNot Nothing Then
                    If Not String.IsNullOrEmpty(identifierName.Identifier.ValueText) Then
                        ImmutableInterlocked.Update(_aliasedNames, Function(names, identifier) names.Add(identifier), identifierName.Identifier.ValueText)
                    End If
                End If
            End If

            MyBase.VisitSimpleImportsClause(node)
        End Sub

        Public Overrides Sub VisitQualifiedName(node As QualifiedNameSyntax)
            If node.IsKind(SyntaxKind.QualifiedName) AndAlso TrySimplify(node) Then
                Return
            End If

            MyBase.VisitQualifiedName(node)
        End Sub

        Public Overrides Sub VisitMemberAccessExpression(node As MemberAccessExpressionSyntax)
            If node.IsKind(SyntaxKind.SimpleMemberAccessExpression) AndAlso TrySimplify(node) Then
                Return
            End If

            MyBase.VisitMemberAccessExpression(node)
        End Sub

        Public Overrides Sub VisitIdentifierName(node As IdentifierNameSyntax)
            ' Always try to simplify identifiers with an 'Attribute' suffix.
            '
            ' In other cases, don't bother looking at the right side of A.B or A!B. We will process those in
            ' one of our other top level Visit methods (Like VisitQualifiedName).
            Dim canTrySimplify = CaseInsensitiveComparison.EndsWith(node.Identifier.ValueText, "Attribute")
            If Not canTrySimplify AndAlso Not node.IsRightSideOfDotOrBang() Then
                ' The only possible simplifications to an unqualified identifier are replacement with an alias or
                ' replacement with a predefined type.
                canTrySimplify = CanReplaceIdentifierWithAlias(node.Identifier.ValueText) _
                    OrElse CanReplaceIdentifierWithPredefinedType(node.Identifier.ValueText)
            End If

            If canTrySimplify AndAlso TrySimplify(node) Then
                Return
            End If

            MyBase.VisitIdentifierName(node)
        End Sub

        Private Function CanReplaceIdentifierWithAlias(identifier As String) As Boolean
            Return _aliasedNames.Contains(identifier)
        End Function

        Private Shared Function CanReplaceIdentifierWithPredefinedType(identifier As String) As Boolean
            Return s_predefinedTypeMetadataNames.Contains(identifier)
        End Function

        Public Overrides Sub VisitGenericName(node As GenericNameSyntax)
            If node.IsKind(SyntaxKind.GenericName) AndAlso TrySimplify(node) Then
                Return
            End If

            MyBase.VisitGenericName(node)
        End Sub

        Private Function TrySimplify(node As SyntaxNode) As Boolean
            Dim diagnostic As Diagnostic = Nothing
            If Not _analyzer.TrySimplify(_semanticModel, node, diagnostic, _optionSet, _cancellationToken) Then
                Return False
            End If

            Diagnostics.Add(diagnostic)
            Return True
        End Function
    End Class
End Namespace
