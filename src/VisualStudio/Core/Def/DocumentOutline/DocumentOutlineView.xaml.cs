﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    /// <summary>
    /// Interaction logic for DocumentOutlineView.xaml
    /// All operations happen on the UI thread for visual studio
    /// </summary>
    internal sealed partial class DocumentOutlineView : UserControl, IVsCodeWindowEvents, IDisposable
    {
        private readonly IVsCodeWindow _codeWindow;
        private readonly IThreadingContext _threadingContext;
        private readonly DocumentOutlineViewModel _viewModel;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly Dictionary<IVsTextView, ITextView> _trackedTextViews = new();
        private readonly ComEventSink _codeWindowEventsSink;
        private readonly ICollectionView _collectionView;

        /// <summary>
        /// Used to suspend all event handlers for caret movement while we navigate the cursor
        /// Should only be written to within the SymbolTree_MouseDown method which always
        /// is called by WPF on the UI thread.
        /// </summary>
        private bool _isNavigating;

        public DocumentOutlineView(
            DocumentOutlineViewModel viewModel,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            IVsCodeWindow codeWindow,
            IThreadingContext threadingContext)
        {
            _codeWindow = codeWindow;
            _threadingContext = threadingContext;
            _viewModel = viewModel;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            DataContext = _viewModel;
            InitializeComponent();

            // "DocumentSymbolItems" is the key name we specified for our CollectionViewSource in the XAML file
            _collectionView = ((CollectionViewSource)FindResource("DocumentSymbolItems")).View;

            // We don't think the shell is initialized lazily, so we'll Debug.Fail(), but if it was we'd still
            // see the view created later so this will still function.
            if (ErrorHandler.Failed(codeWindow.GetPrimaryView(out var primaryTextView)))
                Debug.Fail("GetPrimaryView failed during DocumentOutlineControl initialization.");

            if (ErrorHandler.Failed(StartTrackingView(primaryTextView)))
                Debug.Fail("StartTrackingView failed during DocumentOutlineControl initialization.");

            if (ErrorHandler.Succeeded(codeWindow.GetSecondaryView(out var secondaryTextView)))
            {
                if (ErrorHandler.Failed(StartTrackingView(secondaryTextView)))
                    Debug.Fail("StartTrackingView failed during DocumentOutlineControl initialization.");
            }

            _codeWindowEventsSink = ComEventSink.Advise<IVsCodeWindowEvents>(codeWindow, this);
        }

        private int StartTrackingView(IVsTextView textView)
        {
            _threadingContext.ThrowIfNotOnUIThread();
            var wpfTextView = _editorAdaptersFactoryService.GetWpfTextView(textView);
            if (wpfTextView is null)
                return VSConstants.E_FAIL;

            _trackedTextViews.Add(textView, wpfTextView);

            // In the split window case, there's two views (each with its own caret position) but only one text buffer.
            // Subscribe to caret position changes once per view.
            wpfTextView.Caret.PositionChanged += Caret_PositionChanged;

            return VSConstants.S_OK;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
            => _viewModel.SearchText = SearchBox.Text;

        private void ExpandAll(object sender, RoutedEventArgs e)
            => _viewModel.EnqueueExpandOrCollapse(true);

        private void CollapseAll(object sender, RoutedEventArgs e)
            => _viewModel.EnqueueExpandOrCollapse(false);

        private void SortByName(object sender, EventArgs e)
            => UpdateSort(SortOption.Name);

        private void SortByOrder(object sender, EventArgs e)
            => UpdateSort(SortOption.Location);

        private void SortByType(object sender, EventArgs e)
            => UpdateSort(SortOption.Type);

        private void UpdateSort(SortOption sortOption)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            // Log which sort option was used
            Logger.Log(sortOption switch
            {
                SortOption.Name => FunctionId.DocumentOutline_SortByName,
                SortOption.Location => FunctionId.DocumentOutline_SortByOrder,
                SortOption.Type => FunctionId.DocumentOutline_SortByType,
                _ => throw new NotImplementedException(),
            }, logLevel: LogLevel.Information);

            // Defer changes until all the properties have been set
            using (var _1 = _collectionView.DeferRefresh())
            {
                // Update top-level sorting options for our tree view
                _collectionView.SortDescriptions.UpdateSortDescription(sortOption);

                // Set the sort option property to begin live-sorting
                _viewModel.SortOption = sortOption;
            }

            // Queue a refresh now that everything is set.
            _collectionView.Refresh();
        }

        /// <summary>
        /// When a symbol node in the window is selected via the keyboard, move the caret to its position in the latest active text view.
        /// </summary>
        private void SymbolTree_Selected(object sender, RoutedEventArgs e)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            // We are already navigating, this indicates we've finished selecting the item we wanted.
            if (_isNavigating)
            {
                _isNavigating = false;
                return;
            }

            // This is a user-initiated navigation
            if (e.OriginalSource is TreeViewItem { DataContext: DocumentSymbolDataViewModel symbolModel })
            {
                _isNavigating = true;
                try
                {
                    _codeWindow.GetLastActiveView(out var textView);
                    Assumes.NotNull(textView);
                    var wpfTextView = _editorAdaptersFactoryService.GetWpfTextView(textView);
                    Assumes.NotNull(wpfTextView);
                    wpfTextView.TryMoveCaretToAndEnsureVisible(
                        symbolModel.Data.SelectionRangeSpan.TranslateTo(wpfTextView.TextSnapshot, SpanTrackingMode.EdgeNegative).Start);
                }
                finally
                {
                    Debug.Assert(_isNavigating);
                    _isNavigating = false;
                }
            }
        }

        /// <summary>
        /// On caret position change, highlight the corresponding symbol node in the window and update the view.
        /// </summary>
        private void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            // Do not respond to caret changing events if we are the ones moving the caret
            if (!_isNavigating && !e.NewPosition.Equals(e.OldPosition))
            {
                // indicate that we are about to programmatically set focus
                _isNavigating = true;
                _viewModel.EnqueueSelectTreeNode(e.NewPosition);
            }
        }

        int IVsCodeWindowEvents.OnNewView(IVsTextView textView)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            return StartTrackingView(textView);
        }

        int IVsCodeWindowEvents.OnCloseView(IVsTextView textView)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            if (_trackedTextViews.TryGetValue(textView, out var view))
            {
                // In the split window case, there's two views (each with its own caret position) but only one text buffer.
                // Unsubscribe to caret position changes once per view.
                view.Caret.PositionChanged -= Caret_PositionChanged;

                _trackedTextViews.Remove(textView);
            }

            return VSConstants.S_OK;
        }

        public void Dispose()
        {
            _viewModel.Dispose();
            _codeWindowEventsSink.Unadvise();
        }
    }
}
