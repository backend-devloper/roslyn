﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InvertIf
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertIf), [Shared]>
    Friend NotInheritable Class VisualBasicInvertMultiLineIfCodeRefactoringProvider
        Inherits VisualBasicInvertIfCodeRefactoringProvider(Of MultiLineIfBlockSyntax)

        Protected Overrides Function GetHeaderSpan(ifNode As MultiLineIfBlockSyntax) As TextSpan
            Return TextSpan.FromBounds(
                    ifNode.IfStatement.IfKeyword.SpanStart,
                    ifNode.IfStatement.Condition.Span.End)
        End Function

        Protected Overrides Function IsElseless(ifNode As MultiLineIfBlockSyntax) As Boolean
            Return ifNode.ElseBlock Is Nothing
        End Function

        Protected Overrides Function CanInvert(ifNode As MultiLineIfBlockSyntax) As Boolean
            Return ifNode.ElseIfBlocks.IsEmpty
        End Function

        Protected Overrides Function GetCondition(ifNode As MultiLineIfBlockSyntax) As SyntaxNode
            Return ifNode.IfStatement.Condition
        End Function

        Protected Overrides Function GetIfBody(ifNode As MultiLineIfBlockSyntax) As SyntaxList(Of StatementSyntax)
            Return ifNode.Statements
        End Function

        Protected Overrides Function GetElseBody(ifNode As MultiLineIfBlockSyntax) As SyntaxList(Of StatementSyntax)
            Return ifNode.ElseBlock.Statements
        End Function

        Protected Overrides Function UpdateIf(ifNode As MultiLineIfBlockSyntax, condition As SyntaxNode, Optional trueStatement As SyntaxList(Of StatementSyntax) = Nothing, Optional falseStatement As SyntaxList(Of StatementSyntax) = Nothing) As SyntaxNode
            Dim updatedIf = ifNode.WithIfStatement(ifNode.IfStatement.WithCondition(DirectCast(condition, ExpressionSyntax)))

            If Not trueStatement.IsEmpty Then
                updatedIf = updatedIf.WithStatements(trueStatement)
            End If

            If Not falseStatement.IsEmpty Then
                updatedIf = updatedIf.WithElseBlock(SyntaxFactory.ElseBlock(falseStatement))
            End If

            Return updatedIf
        End Function
    End Class
End Namespace

