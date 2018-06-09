﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.VirtualChars
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices
    <ExportLanguageService(GetType(IEmbeddedLanguageProvider), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEmbeddedLanguageProvider
        Inherits AbstractEmbeddedLanguageProvider

        Public Shared Instance As New VisualBasicEmbeddedLanguageProvider()

        Private Sub New()
            MyBase.New(SyntaxKind.StringLiteralToken,
                       VisualBasicSyntaxFactsService.Instance,
                       VisualBasicSemanticFactsService.Instance,
                       VisualBasicVirtualCharService.Instance)
        End Sub

        Friend Overrides Sub AddComment(editor As SyntaxEditor, stringLiteral As SyntaxToken, commentContents As String)
            Dim trivia = SyntaxFactory.TriviaList(
                SyntaxFactory.CommentTrivia($"' {commentContents}"),
                SyntaxFactory.ElasticCarriageReturnLineFeed)

            Dim containingStatement = stringLiteral.Parent.GetAncestor(Of StatementSyntax)

            Dim leadingBlankLines = containingStatement.GetLeadingBlankLines()

            Dim newStatement = containingStatement.GetNodeWithoutLeadingBlankLines().
                                                   WithPrependedLeadingTrivia(leadingBlankLines.AddRange(trivia))

            editor.ReplaceNode(containingStatement, newStatement)
        End Sub

        Friend Overrides Function EscapeText(text As String, token As SyntaxToken) As String
            Return text
        End Function
    End Class
End Namespace
