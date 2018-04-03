﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions
{
    using RegexToken = EmbeddedSyntaxToken<RegexKind>;
    using RegexTrivia = EmbeddedSyntaxTrivia<RegexKind>;

    internal static class RegexHelpers
    {
        public static bool HasOption(RegexOptions options, RegexOptions val)
            => (options & val) != 0;

        public static RegexToken CreateToken(RegexKind kind, ImmutableArray<RegexTrivia> leadingTrivia, ImmutableArray<VirtualChar> virtualChars)
            => new RegexToken(kind, leadingTrivia, virtualChars, ImmutableArray<RegexTrivia>.Empty, ImmutableArray<EmbeddedDiagnostic>.Empty, value: null);

        public static RegexToken CreateMissingToken(RegexKind kind)
            => CreateToken(kind, ImmutableArray<RegexTrivia>.Empty, ImmutableArray<VirtualChar>.Empty);

        public static RegexTrivia CreateTrivia(RegexKind kind, ImmutableArray<VirtualChar> virtualChars)
            => CreateTrivia(kind, virtualChars, ImmutableArray<EmbeddedDiagnostic>.Empty);

        public static RegexTrivia CreateTrivia(RegexKind kind, ImmutableArray<VirtualChar> virtualChars, ImmutableArray<EmbeddedDiagnostic> diagnostics)
            => new RegexTrivia(kind, virtualChars, diagnostics);

        public static bool IsSelfEscape(this RegexSimpleEscapeNode node)
        {
            if (node.TypeToken.VirtualChars.Length > 0)
            {
                switch (node.TypeToken.VirtualChars[0].Char)
                {
                    case 'a':
                    case 'b':
                    case 'e':
                    case 'f':
                    case 'n':
                    case 'r':
                    case 't':
                    case 'v':
                        return false;
                }
            }

            return true;
        }
    }
}
