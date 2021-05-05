﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    // PROTOTYPE(extended-property-patterns) Split BoundPropertySubpattern and remove this (requires IOperation changes)
    partial class BoundSubpattern
    {
        internal BoundSubpattern(SyntaxNode syntax, Symbol? symbol, BoundPattern pattern, bool hasErrors = false)
            : this(syntax, symbol is null ? ImmutableArray<Symbol>.Empty : ImmutableArray.Create(symbol), pattern, hasErrors)
        {
        }

        internal Symbol? Symbol
        {
            get
            {
                if (this.Symbols.IsEmpty)
                    return null;
                if (this.Symbols.Length == 1)
                    return this.Symbols[0];
                return null;
            }
        }
    }
}
