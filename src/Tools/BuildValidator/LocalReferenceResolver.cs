﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace BuildValidator
{
    /// <summary>
    /// Resolves references for a package by looking in local nuget and artifact
    /// directories for Roslyn
    /// </summary>
    internal class LocalReferenceResolver
    {
        private readonly Dictionary<Guid, string> _cache = new Dictionary<Guid, string>();
        private readonly HashSet<DirectoryInfo> _indexDirectories = new HashSet<DirectoryInfo>();
        private readonly ILogger _logger;

        public LocalReferenceResolver(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<LocalReferenceResolver>();
            _indexDirectories.Add(GetArtifactsDirectory());
            _indexDirectories.Add(GetNugetCacheDirectory());
        }

        public static DirectoryInfo GetNugetCacheDirectory()
        {
            var nugetPackageDirectory = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            if (nugetPackageDirectory is null)
            {
                nugetPackageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget");
            }

            return new DirectoryInfo(nugetPackageDirectory);
        }

        public Task<MetadataReference> ResolveReferenceAsync(MetadataReferenceInfo referenceInfo)
        {
            var path = Search(referenceInfo.Mvid, referenceInfo.Name);
            return Task.FromResult<MetadataReference>(MetadataReference.CreateFromFile(path));
        }

        public Task<ImmutableArray<MetadataReference>> ResolveReferencesAsync(IEnumerable<MetadataReferenceInfo> references)
        {
            var referenceArray = references.ToImmutableArray();
            CacheNames(referenceArray);

            var files = referenceArray.Select(r => _cache[r.Mvid]);

            var metadataReferences = files.Select(f => MetadataReference.CreateFromFile(f)).Cast<MetadataReference>().ToImmutableArray();
            return Task.FromResult(metadataReferences);
        }

        public string Search(Guid mvid, string name)
        {
            if (_cache.TryGetValue(mvid, out var referencePath))
            {
                return referencePath;
            }

            foreach (var directory in _indexDirectories)
            {
                var foundFile = directory.GetFiles(name, SearchOption.AllDirectories).FirstOrDefault();
                if (foundFile is null)
                {
                    continue;
                }

                referencePath = foundFile.FullName;
                _cache[mvid] = referencePath;

                _logger.LogTrace($"Caching [{mvid}, {referencePath}]");
                return referencePath;
            }

            throw new KeyNotFoundException($"[{mvid}, {name}]");
        }

        public void CacheNames(ImmutableArray<MetadataReferenceInfo> names)
        {
            foreach (var directory in _indexDirectories)
            {
                foreach (var file in directory.GetFiles("*.*", SearchOption.AllDirectories))
                {
                    // A single file name can have multiple MVID, so compare by name first then
                    // open the files to check the MVID 
                    var potentialMatches = names.Where(m => FileNameEqualityComparer.Instance.Equals(m.FileInfo, file));

                    if (!potentialMatches.Any())
                    {
                        continue;
                    }

                    var mvids = GetMvidsForFile(file);

                    foreach (var match in potentialMatches)
                    {
                        if (_cache.ContainsKey(match.Mvid))
                        {
                            continue;
                        }

                        if (mvids.Contains(match.Mvid))
                        {
                            _logger.LogTrace($"Caching [{match.Mvid}, {file.FullName}]");
                            _cache[match.Mvid] = file.FullName;
                        }
                    }
                }
            }

            var uncached = names.Where(m => !_cache.ContainsKey(m.Mvid)).ToArray();

            if (uncached.Any())
            {
                _logger.LogDebug($"Unable to find files for the following metadata references: {uncached}");
            }

            static ImmutableArray<Guid> GetMvidsForFile(FileInfo fileInfo)
            {
                var assembly = Assembly.ReflectionOnlyLoadFrom(fileInfo.FullName);
                return assembly.Modules.Select(m => m.ModuleVersionId).ToImmutableArray();
            }
        }

        public static DirectoryInfo GetArtifactsDirectory()
        {
            var assemblyLocation = typeof(LocalReferenceResolver).Assembly.Location;
            var binDir = Directory.GetParent(assemblyLocation);

            while (binDir != null && !binDir.FullName.EndsWith("artifacts\\bin", FileNameEqualityComparer.StringComparison))
            {
                binDir = binDir.Parent;
            }

            if (binDir == null)
            {
                throw new Exception();
            }

            return binDir;
        }
    }
}
