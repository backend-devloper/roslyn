﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static partial class ExtensionMethodImportCompletionService
    {
        private readonly struct CacheEntry
        {
            public Checksum Checksum { get; }

            public readonly MultiDictionary<string, DeclaredSymbolInfo> SimpleExtensionMethodInfo { get; }

            public readonly ImmutableArray<DeclaredSymbolInfo> ComplexExtensionMethodInfo { get; }

            private CacheEntry(
                Checksum checksum,
                MultiDictionary<string, DeclaredSymbolInfo> simpleExtensionMethodInfo,
                ImmutableArray<DeclaredSymbolInfo> complexExtensionMethodInfo)
            {
                Checksum = checksum;
                SimpleExtensionMethodInfo = simpleExtensionMethodInfo;
                ComplexExtensionMethodInfo = complexExtensionMethodInfo;
            }

            public class Builder : IDisposable
            {
                private readonly Checksum _checksum;

                private readonly MultiDictionary<string, DeclaredSymbolInfo> _simpleItemBuilder;
                private readonly ArrayBuilder<DeclaredSymbolInfo> _complexItemBuilder;

                public Builder(Checksum checksum)
                {
                    _checksum = checksum;

                    _simpleItemBuilder = new MultiDictionary<string, DeclaredSymbolInfo>();
                    _complexItemBuilder = ArrayBuilder<DeclaredSymbolInfo>.GetInstance();
                }

                public CacheEntry ToCacheEntry()
                {
                    return new CacheEntry(
                        _checksum,
                        _simpleItemBuilder,
                        _complexItemBuilder.ToImmutable());
                }

                public void AddItem(SyntaxTreeIndex syntaxIndex)
                {
                    foreach (var (targetType, symbolInfoIndices) in syntaxIndex.SimpleExtensionMethodInfo)
                    {
                        foreach (var index in symbolInfoIndices)
                        {
                            if (syntaxIndex.TryGetDeclaredSymbolInfo(index, out var methodInfo))
                            {
                                _simpleItemBuilder.Add(targetType, methodInfo);
                            }
                        }
                    }

                    foreach (var index in syntaxIndex.ComplexExtensionMethodInfo)
                    {
                        if (syntaxIndex.TryGetDeclaredSymbolInfo(index, out var methodInfo))
                        {
                            _complexItemBuilder.Add(methodInfo);
                        }
                    }
                }

                public void Dispose()
                    => _complexItemBuilder.Free();
            }
        }

        /// <summary>
        /// We don't use PE cache from the service, so just pass in type `object` for PE entries.
        /// </summary>
        [ExportWorkspaceServiceFactory(typeof(IImportCompletionCacheService<CacheEntry, object>), ServiceLayer.Editor), Shared]
        private sealed class CacheServiceFactory : AbstractImportCompletionCacheServiceFactory<CacheEntry, object>
        {
            [ImportingConstructor]
            public CacheServiceFactory()
            {
            }
        }

        private static IImportCompletionCacheService<CacheEntry, object> GetCacheService(Workspace workspace)
            => workspace.Services.GetRequiredService<IImportCompletionCacheService<CacheEntry, object>>();

        private static async Task<CacheEntry?> GetCacheEntryAsync(
            Project project,
            bool loadOnly,
            IImportCompletionCacheService<CacheEntry, object> cacheService,
            CancellationToken cancellationToken)
        {
            var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false);

            // Cache miss, create all requested items.
            if (!cacheService.ProjectItemsCache.TryGetValue(project.Id, out var cacheEntry) ||
                cacheEntry.Checksum != checksum)
            {
                using var builder = new CacheEntry.Builder(checksum);
                foreach (var document in project.Documents)
                {
                    // Don't look for extension methods in generated code.
                    if (document.State.Attributes.IsGenerated)
                    {
                        continue;
                    }

                    var info = await document.GetSyntaxTreeIndexAsync(loadOnly, cancellationToken).ConfigureAwait(false);
                    if (info == null)
                    {
                        return null;
                    }

                    if (info.ContainsExtensionMethod)
                    {
                        builder.AddItem(info);
                    }
                }

                cacheEntry = builder.ToCacheEntry();
                cacheService.ProjectItemsCache[project.Id] = cacheEntry;
            }

            return cacheEntry;
        }
    }
}
