﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    using SymbolKind = LanguageServer.Protocol.SymbolKind;

    internal sealed record DocumentSymbolData(
        string Name,
        SymbolKind SymbolKind,
        Glyph Glyph,
        SnapshotSpan RangeSpan,
        SnapshotSpan SelectionRangeSpan,
        ImmutableArray<DocumentSymbolData> Children);
}
