﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices
{
    [ExportLanguageService(typeof(IEmbeddedLanguagesProvider), LanguageNames.CSharp), Shared]
    internal class CSharpEmbeddedLanguagesProvider : AbstractEmbeddedLanguagesProvider
    {
        public static IEmbeddedLanguagesProvider Instance = new CSharpEmbeddedLanguagesProvider();

        public CSharpEmbeddedLanguagesProvider()
            : base((int)SyntaxKind.StringLiteralToken,
                   (int)SyntaxKind.InterpolatedStringTextToken,
                   CSharpSyntaxFactsService.Instance,
                   CSharpSemanticFactsService.Instance,
                   CSharpVirtualCharService.Instance)
        {
        }

        internal override void AddComment(SyntaxEditor editor, SyntaxToken stringLiteral, string commentContents)
        {
            var triviaList = SyntaxFactory.TriviaList(
                SyntaxFactory.Comment($"/*{commentContents}*/"),
                SyntaxFactory.ElasticSpace);

            var newStringLiteral = stringLiteral.WithLeadingTrivia(
                stringLiteral.LeadingTrivia.AddRange(triviaList));

            editor.ReplaceNode(stringLiteral.Parent, stringLiteral.Parent.ReplaceToken(stringLiteral, newStringLiteral));
        }

        internal override string EscapeText(string text, SyntaxToken token)
        {
            Debug.Assert(token.Kind() == SyntaxKind.StringLiteralToken);
            return token.IsVerbatimStringLiteral()
                ? text
                : text.Replace("\\", "\\\\");
        }
    }
}
