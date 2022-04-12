﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a chain of symbols that are imported to a particular position in a source file.  Symbols may be
    /// imported, but may not necessarily be available at that location (for example, an alias symbol hidden by another
    /// symbol).  There is no guarantee that the same chain will be returned from successive calls to <see
    /// cref="SemanticModel.GetImportScopes"/>.  Each import has a reference to the location the import directive was
    /// declared at.  For the <see cref="IAliasSymbol"/> import, the location can be found using either <see
    /// cref="ISymbol.Locations"/> or <see cref="ISymbol.DeclaringSyntaxReferences"/> on the <see cref="IAliasSymbol"/>
    /// itself.  For <see cref="Imports"/> or <see cref="XmlNamespaces"/> the location is found through <see
    /// cref="ImportedNamespaceOrType.DeclaringSyntaxReference"/> or <see
    /// cref="ImportedXmlNamespace.DeclaringSyntaxReference"/> respectively.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// Scopes returned will always have at least one non-empty property value in them.
    /// <item>
    /// In C# there will be an <see cref="IImportScope"/> for every containing namespace-declarations that include any
    /// import directives.  There will also be an <see cref="IImportScope"/> for the containing compilation-unit if it
    /// includes any import directives or if there are global import directives pulled in from other files.
    /// </item>
    /// <item>
    /// In Visual Basic there will only be at most two <see cref="IImportScope"/>s returned for any position.  There
    /// will be a scope for the containing compilation unit if it includes any import directives.  There can also be a
    /// scope representing any imports specified at the project level.
    /// </item>
    /// </list>
    /// </remarks>
    public interface IImportScope
    {
        /// <summary>
        /// Aliases defined at this level of the chain.  This corresponds to <c>using X = TypeOrNamespace;</c> in C# or
        /// <c>Imports X = TypeOrNamespace</c> in Visual Basic.
        /// </summary>
        ImmutableArray<IAliasSymbol> Aliases { get; }

        /// <summary>
        /// Extern aliases defined at this level of the chain.  This corresponds to <c>extern alias X;</c> in C#.  It
        /// will be empty in Visual Basic.
        /// </summary>
        ImmutableArray<IAliasSymbol> ExternAliases { get; }

        /// <summary>
        /// Types or namespaces imported at this level of the chain.  This corresponds to <c>using Namespace;</c> or
        /// <c>using static Type;</c> in C#, or <c>Imports TypeOrNamespace</c> in Visual Basic.
        /// </summary>
        ImmutableArray<ImportedNamespaceOrType> Imports { get; }

        /// <summary>
        /// Xml namespaces imported at this level of the chain.  This corresponds to <c>Imports &lt;xmlns:prefix =
        /// "name"&gt;</c> in Visual Basic.  It will be empty in C#.
        /// </summary>
        ImmutableArray<ImportedXmlNamespace> XmlNamespaces { get; }
    }

    public readonly struct ImportedNamespaceOrType
    {
        public INamespaceOrTypeSymbol NamespaceOrType { get; }
        public SyntaxReference? DeclaringSyntaxReference { get; }

        internal ImportedNamespaceOrType(INamespaceOrTypeSymbol namespaceOrType, SyntaxReference? declaringSyntaxReference)
        {
            NamespaceOrType = namespaceOrType;
            DeclaringSyntaxReference = declaringSyntaxReference;
        }
    }

    public readonly struct ImportedXmlNamespace
    {
        public string XmlNamespace { get; }
        public SyntaxReference? DeclaringSyntaxReference { get; }

        internal ImportedXmlNamespace(string xmlNamespace, SyntaxReference? declaringSyntaxReference)
        {
            XmlNamespace = xmlNamespace;
            DeclaringSyntaxReference = declaringSyntaxReference;
        }
    }

    /// <summary>
    /// Simple POCO implementation of the import scope, usable by both C# and VB.
    /// </summary>
    internal sealed record SimpleImportScope(
        ImmutableArray<IAliasSymbol> Aliases,
        ImmutableArray<IAliasSymbol> ExternAliases,
        ImmutableArray<ImportedNamespaceOrType> Imports,
        ImmutableArray<ImportedXmlNamespace> XmlNamespaces) : IImportScope;
}
