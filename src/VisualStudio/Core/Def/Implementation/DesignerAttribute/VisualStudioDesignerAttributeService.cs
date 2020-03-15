﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Services;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    internal class VisualStudioDesignerAttributeService
        : ForegroundThreadAffinitizedObject, IDesignerAttributeService, IDesignerAttributeListener
    {
        private readonly VisualStudioWorkspaceImpl _workspace;

        /// <summary>
        /// Used so we can switch over to the UI thread for communicating with legacy projects that
        /// require that.
        /// </summary>
        private readonly IThreadingContext _threadingContext;

        /// <summary>
        /// Used to acquire the legacy project designer service.
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Our connections to the remote OOP server. Created on demand when we startup and then
        /// kept around for the lifetime of this service.
        /// </summary>
        private KeepAliveSession? _keepAliveSession;

        /// <summary>
        /// Cache from project to the CPS designer service for it.  Computed on demand (which
        /// requires using the UI thread), but then cached for all subsequent notifications about
        /// that project.
        /// </summary>
        private readonly ConcurrentDictionary<ProjectId, IProjectItemDesignerTypeUpdateService?> _cpsProjects
            = new ConcurrentDictionary<ProjectId, IProjectItemDesignerTypeUpdateService?>();

        /// <summary>
        /// Cached designer service for notifying legacy projects about designer atttributes.
        /// </summary>
        private IVSMDDesignerService? _legacyDesignerService;

        // We'll get notifications from the OOP server about new attribute arguments. Batch those
        // notifications up and deliver them to VS every second.
        #region protected by lock

        /// <summary>
        /// Lock we will use to ensure the remainder of these fields can be accessed in a threadsafe
        /// manner.  When OOP calls back into us, we'll place the data it produced into
        /// <see cref="_updatedInfos"/>.  We'll then kick of a task to process this in the future if
        /// we don't already have an existing task in flight for that.
        /// </summary>
        private readonly object _gate = new object();

        /// <summary>
        /// Data produced by OOP that we want to process in our next update task.
        /// </summary>
        private readonly List<DesignerInfo> _updatedInfos = new List<DesignerInfo>();

        /// <summary>
        /// Task kicked off to do the next batch of processing of <see cref="_updatedInfos"/>. These
        /// tasks form a chain so that the next task only processes when the previous one completes.
        /// </summary>
        private Task _updateTask = Task.CompletedTask;

        /// <summary>
        /// Whether or not there is an existing task in flight that will process the current batch
        /// of <see cref="_updatedInfos"/>.  If there is an existing in flight task, we don't need 
        /// to kick off a new one if we receive more notifications before it runs.
        /// </summary>
        private bool _taskInFlight = false;

        #endregion

        public VisualStudioDesignerAttributeService(
            VisualStudioWorkspaceImpl workspace,
            IThreadingContext threadingContext,
            IServiceProvider serviceProvider)
            : base(threadingContext)
        {
            _workspace = workspace;
            _threadingContext = threadingContext;
            _serviceProvider = serviceProvider;

            _workspace.WorkspaceChanged += OnWorkspaceChanged;
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind == WorkspaceChangeKind.ProjectRemoved)
                _cpsProjects.TryRemove(e.ProjectId, out _);
        }

        void IDesignerAttributeService.Start(CancellationToken cancellationToken)
            => _ = StartAsync(cancellationToken);

        private async Task StartAsync(CancellationToken cancellationToken)
        {
            // Have to catch all exceptions coming through here as this is called from a
            // fire-and-forget method and we want to make sure nothing leaks out.
            try
            {
                await StartWorkerAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is normal (during VS closing).  Just ignore.
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
                // Otherwise report a watson for any other exception.  Don't bring down VS.  This is
                // a BG service we don't want impacting the user experience.
            }
        }

        private async Task StartWorkerAsync(CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
                return;

            // Pass ourselves in as the callback target for the OOP service.  As it discovers
            // designer attributes it will call back into us to notify VS about it.
            _keepAliveSession = await client.TryCreateKeepAliveSessionAsync(
                WellKnownServiceHubServices.RemoteDesignerAttributeService,
                callbackTarget: this, cancellationToken).ConfigureAwait(false);
            if (_keepAliveSession == null)
                return;

            // Now kick off scanning in the OOP process.
            var success = await _keepAliveSession.TryInvokeAsync(
                nameof(IRemoteDesignerAttributeService.StartScanningForDesignerAttributesAsync),
                solution: null,
                arguments: Array.Empty<object>(),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Callback from the OOP service back into us.
        /// </summary>
        public Task RegisterDesignerAttributesAsync(
            IList<DesignerInfo> attributeInfos, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                // add our work to the set we'll process in the next batch.
                _updatedInfos.AddRange(attributeInfos);

                if (!_taskInFlight)
                {
                    // No in-flight task.  Kick one off to process these messages a second from now.
                    // We always attach the task to the previous one so that notifications to the ui
                    // follow the same order as the notification the OOP server sent to us.
                    _updateTask = _updateTask.ContinueWithAfterDelayFromAsync(
                        _ => NotifyProjectSystemAsync(cancellationToken),
                        cancellationToken,
                        1000/*ms*/,
                        TaskContinuationOptions.RunContinuationsAsynchronously,
                        TaskScheduler.Default);
                    _taskInFlight = true;
                }
            }

            return Task.CompletedTask;
        }

        private async Task NotifyProjectSystemAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var _1 = ArrayBuilder<DesignerInfo>.GetInstance(out var attributeInfos);
            AddInfosAndResetQueue(attributeInfos);

            // Now, group all the notifications by project and update all the projects in parallel.
            using var _2 = ArrayBuilder<Task>.GetInstance(out var tasks);
            foreach (var group in attributeInfos.GroupBy(a => a.DocumentId.ProjectId))
            {
                cancellationToken.ThrowIfCancellationRequested();
                tasks.Add(NotifyProjectSystemAsync(group.Key, group, cancellationToken));
            }

            // Wait until all project updates have happened before processing the next batch.
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private void AddInfosAndResetQueue(ArrayBuilder<DesignerInfo> attributeInfos)
        {
            using var _ = PooledHashSet<DocumentId>.GetInstance(out var seenDocumentIds);

            lock (_gate)
            {
                // walk the set of updates in reverse, and ignore documents if we see them a second
                // time.  This ensures that if we're batching up multiple notifications for the same
                // document, that we only bother processing the last one since it should beat out
                // all the prior ones.
                for (var i = _updatedInfos.Count - 1; i >= 0; i--)
                {
                    var designerArg = _updatedInfos[i];
                    if (seenDocumentIds.Add(designerArg.DocumentId))
                        attributeInfos.Add(designerArg);
                }

                // mark there being no existing update task so that the next OOP notification will
                // kick one off.
                _updatedInfos.Clear();
                _taskInFlight = false;
            }
        }

        private async Task NotifyProjectSystemAsync(
            ProjectId projectId,
            IEnumerable<DesignerInfo> attributeInfos,
            CancellationToken cancellationToken)
        {
            // Delegate to the CPS or legacy notification services as necessary.
            var cpsUpdateService = await GetUpdateServiceIfCpsProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
            var task = cpsUpdateService == null
                ? NotifyLegacyProjectSystemAsync(projectId, attributeInfos, cancellationToken)
                : NotifyCpsProjectSystemAsync(cpsUpdateService, attributeInfos, cancellationToken);

            await task.ConfigureAwait(false);
        }

        private async Task NotifyLegacyProjectSystemAsync(
            ProjectId projectId,
            IEnumerable<DesignerInfo> attributeInfos,
            CancellationToken cancellationToken)
        {
            // legacy project system can only be talked to on the UI thread.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            AssertIsForeground();

            var designerService = _legacyDesignerService ??= (IVSMDDesignerService)_serviceProvider.GetService(typeof(SVSMDDesignerService));
            if (designerService == null)
                return;

            var hierarchy = _workspace.GetHierarchy(projectId);
            if (hierarchy == null)
                return;

            foreach (var info in attributeInfos)
            {
                cancellationToken.ThrowIfCancellationRequested();
                NotifyLegacyProjectSystemOnUIThread(designerService, hierarchy, info);
            }
        }

        private void NotifyLegacyProjectSystemOnUIThread(
            IVSMDDesignerService designerService,
            IVsHierarchy hierarchy,
            DesignerInfo info)
        {
            this.AssertIsForeground();

            var itemId = hierarchy.TryGetItemId(info.FilePath);
            if (itemId == VSConstants.VSITEMID_NIL)
                return;

            // PERF: Avoid sending the message if the project system already has the current value.
            if (ErrorHandler.Succeeded(hierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_ItemSubType, out var currentValue)))
            {
                var currentStringValue = string.IsNullOrEmpty(currentValue as string) ? null : (string)currentValue;
                if (string.Equals(currentStringValue, info.Category, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            try
            {
                designerService.RegisterDesignViewAttribute(
                    hierarchy, (int)itemId, dwClass: 0,
                    pwszAttributeValue: info.Category);
            }
            catch
            {
                // DevDiv # 933717
                // turns out RegisterDesignViewAttribute can throw in certain cases such as a file failed to be checked out by source control
                // or IVSHierarchy failed to set a property for this project
                //
                // just swallow it. don't crash VS.
            }
        }

        private async Task NotifyCpsProjectSystemAsync(
            IProjectItemDesignerTypeUpdateService updateService,
            IEnumerable<DesignerInfo> attributeInfos,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Broadcast all the information about all the documents in parallel to CPS.

            using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

            foreach (var info in attributeInfos)
            {
                cancellationToken.ThrowIfCancellationRequested();
                tasks.Add(NotifyCpsProjectSystemAsync(updateService, info, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task NotifyCpsProjectSystemAsync(
            IProjectItemDesignerTypeUpdateService updateService,
            DesignerInfo info,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await updateService.SetProjectItemDesignerTypeAsync(info.FilePath, info.Category).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // we might call update service after project is already removed and get object disposed exception.
                // we will catch the exception and ignore. 
                // see this PR for more detail - https://github.com/dotnet/roslyn/pull/35383
            }
        }

        private async Task<IProjectItemDesignerTypeUpdateService?> GetUpdateServiceIfCpsProjectAsync(
            ProjectId projectId, CancellationToken cancellationToken)
        {
            if (!_cpsProjects.TryGetValue(projectId, out var updateService))
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                this.AssertIsForeground();

                updateService = ComputeUpdateService();
                _cpsProjects.TryAdd(projectId, updateService);
            }

            return updateService;

            IProjectItemDesignerTypeUpdateService? ComputeUpdateService()
            {
                if (!_workspace.IsCPSProject(projectId))
                    return null;

                var vsProject = (IVsProject?)_workspace.GetHierarchy(projectId);
                if (vsProject == null)
                    return null;

                if (ErrorHandler.Failed(vsProject.GetItemContext((uint)VSConstants.VSITEMID.Root, out var projectServiceProvider)))
                    return null;

                var serviceProvider = new Shell.ServiceProvider(projectServiceProvider);
                return serviceProvider.GetService(typeof(IProjectItemDesignerTypeUpdateService)) as IProjectItemDesignerTypeUpdateService;
            }
        }
    }
}
