﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineHints
{
    /// <summary>
    /// The TaggerProvider that calls upon the service in order to locate the spans and names
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TagType(typeof(InlineParameterNameHintDataTag))]
    [Name(nameof(InlineParameterNameHintsDataTaggerProvider))]
    internal class InlineParameterNameHintsDataTaggerProvider : AsynchronousViewTaggerProvider<InlineParameterNameHintDataTag>
    {
        private readonly IAsynchronousOperationListener _listener;

        protected override SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeInclusive;

        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        [ImportingConstructor]
        public InlineParameterNameHintsDataTaggerProvider(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider,
            IForegroundNotificationService notificationService)
            : base(threadingContext, listenerProvider.GetListener(FeatureAttribute.InlineParameterNameHints), notificationService)
        {
            _listener = listenerProvider.GetListener(FeatureAttribute.InlineParameterNameHints);
        }

        // This option controls whether or not we run at all.
        protected override IEnumerable<PerLanguageOption2<bool>> PerLanguageOptions
            => ImmutableArray.Create(InlineHintsOptions.EnabledForParameters);

        protected override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            // The options we check here just affect the set of results we return if we run.
            return TaggerEventSources.Compose(
                TaggerEventSources.OnViewSpanChanged(ThreadingContext, textViewOpt, textChangeDelay: TaggerDelay.Short, scrollChangeDelay: TaggerDelay.NearImmediate),
                TaggerEventSources.OnWorkspaceChanged(subjectBuffer, TaggerDelay.NearImmediate, _listener),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.ForLiteralParameters, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.ForObjectCreationParameters, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, InlineHintsOptions.ForOtherParameters, TaggerDelay.NearImmediate));
        }

        protected override IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textView, ITextBuffer subjectBuffer)
        {
            this.AssertIsForeground();

            // Find the visible span some 100 lines +/- what's actually in view.  This way
            // if the user scrolls up/down, we'll already have the results.
            var visibleSpanOpt = textView.GetVisibleLinesSpan(subjectBuffer, extraLines: 100);
            if (visibleSpanOpt == null)
            {
                // Couldn't find anything visible, just fall back to tagging all hint locations
                return base.GetSpansToTag(textView, subjectBuffer);
            }

            return SpecializedCollections.SingletonEnumerable(visibleSpanOpt.Value);
        }

        protected override async Task ProduceTagsAsync(TaggerContext<InlineParameterNameHintDataTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            var cancellationToken = context.CancellationToken;
            var document = documentSnapshotSpan.Document;

            var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
            var paramNameHintsService = document.GetLanguageService<IInlineParameterNameHintsService>();
            if (paramNameHintsService != null)
            {
                var parameterHints = await paramNameHintsService.GetInlineParameterNameHintsAsync(document, snapshotSpan.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);
                foreach (var parameterHint in parameterHints)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    context.AddTag(new TagSpan<InlineParameterNameHintDataTag>(new SnapshotSpan(snapshotSpan.Snapshot, parameterHint.Position, 0), new InlineParameterNameHintDataTag(parameterHint.ParameterSymbolKey, parameterHint.Name)));
                }
            }
        }
    }
}
