﻿//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//using System;
//using System.IO.Pipelines;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.CodeAnalysis.Storage;
//using Microsoft.VisualStudio.RpcContracts.Caching;
//using Roslyn.Utilities;

//namespace Microsoft.VisualStudio.LanguageServices.Storage
//{
//    /// <summary>
//    /// Base implementation of <see cref="IRoslynCloudCacheService"/> that sits on top of the VS platform's <see
//    /// cref="ICacheService"/>, taking care of all common logic.
//    /// </summary>
//    internal abstract class AbstractCloudCacheService : IRoslynCloudCacheService
//    {
//        protected readonly ICacheService CacheService;

//        protected AbstractCloudCacheService(ICacheService cacheService)
//        {
//            CacheService = cacheService;
//        }

//        private static CacheItemKey Convert(RoslynCloudCacheItemKey key)
//            => new(Convert(key.ContainerKey), key.ItemName) { Version = key.Version };

//        private static CacheContainerKey Convert(RoslynCloudCacheContainerKey containerKey)
//            => new(containerKey.Component, containerKey.Dimensions);

//        public abstract void Dispose();

//        public ValueTask DisposeAsync()
//        {
//            if (this.CacheService is IAsyncDisposable asyncDisposable)
//            {
//                return asyncDisposable.DisposeAsync();
//            }
//            else if (this.CacheService is IDisposable disposable)
//            {
//                disposable.Dispose();
//                return ValueTaskFactory.CompletedTask;
//            }

//            return ValueTaskFactory.CompletedTask;
//        }

//        public Task<bool> CheckExistsAsync(RoslynCloudCacheItemKey key, CancellationToken cancellationToken)
//            => this.CacheService.CheckExistsAsync(Convert(key), cancellationToken);

//        public ValueTask<string> GetRelativePathBaseAsync(CancellationToken cancellationToken)
//            => this.CacheService.GetRelativePathBaseAsync(cancellationToken);

//        public Task SetItemAsync(RoslynCloudCacheItemKey key, PipeReader reader, bool shareable, CancellationToken cancellationToken)
//            => this.CacheService.SetItemAsync(Convert(key), reader, shareable, cancellationToken);

//        public Task<bool> TryGetItemAsync(RoslynCloudCacheItemKey key, PipeWriter writer, CancellationToken cancellationToken)
//            => this.CacheService.TryGetItemAsync(Convert(key), writer, cancellationToken);
//    }
//}
