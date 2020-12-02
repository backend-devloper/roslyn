﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion
{
    [Export(LanguageNames.CSharp, typeof(IBraceCompletionService)), Shared]
    internal class CurlyBraceCompletionService : AbstractBraceCompletionService
    {
        /// <summary>
        /// Annotation used to find the closing brace location after formatting changes are applied.
        /// The closing brace location is then used as the caret location.
        /// </summary>
        private static readonly SyntaxAnnotation s_closingBraceSyntaxAnnotation = new(nameof(s_closingBraceSyntaxAnnotation));

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CurlyBraceCompletionService()
        {
        }

        protected override char OpeningBrace => CurlyBrace.OpenCharacter;

        protected override char ClosingBrace => CurlyBrace.CloseCharacter;

        public override Task<bool> AllowOverTypeAsync(BraceCompletionContext context, CancellationToken cancellationToken)
            => AllowOverTypeInUserCodeWithValidClosingTokenAsync(context, cancellationToken);

        public override async Task<BraceCompletionResult?> GetTextChangesAfterCompletionAsync(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken)
        {
            var documentOptions = await braceCompletionContext.Document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            // After the closing brace is completed we need to format the span from the opening point to the closing point.
            // E.g. when the user triggers completion for an if statement ($$ is the caret location) we insert braces to get
            // if (true){$$}
            // We then need to format this to
            // if (true) { $$}
            var (formattingChanges, finalCurlyBraceEnd) = await FormatTrackingSpanAsync(
                braceCompletionContext.Document,
                documentOptions,
                braceCompletionContext.OpeningPoint,
                braceCompletionContext.ClosingPoint,
                shouldHonorAutoFormattingOnCloseBraceOption: true,
                // We're not trying to format the indented block here, so no need to pass in additional rules.
                braceFormattingIndentationRules: ImmutableArray<AbstractFormattingRule>.Empty,
                cancellationToken).ConfigureAwait(false);

            if (formattingChanges.IsEmpty)
            {
                return null;
            }

            // The caret location should be at the start of the closing brace character.
            var originalText = await braceCompletionContext.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var formattedText = originalText.WithChanges(formattingChanges);
            var caretLocation = formattedText.Lines.GetLinePosition(finalCurlyBraceEnd - 1);

            return new BraceCompletionResult(formattingChanges, caretLocation);
        }

        public override async Task<BraceCompletionResult?> GetTextChangeAfterReturnAsync(BraceCompletionContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            var closingPoint = context.ClosingPoint;
            var openingPoint = context.OpeningPoint;
            var originalDocumentText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            // check whether shape of the braces are what we support
            // shape must be either "{|}" or "{ }". | is where caret is. otherwise, we don't do any special behavior
            if (!ContainsOnlyWhitespace(originalDocumentText, openingPoint, closingPoint))
            {
                return null;
            }

            // If there is not already an empty line inserted between the braces, insert one.
            TextChange? newLineEdit = null;
            var textToFormat = originalDocumentText;
            if (!HasExistingEmptyLineBetweenBraces(openingPoint, closingPoint, originalDocumentText))
            {
                var newLineString = documentOptions.GetOption(FormattingOptions2.NewLine);
                newLineEdit = new TextChange(new TextSpan(closingPoint - 1, 0), newLineString);
                textToFormat = originalDocumentText.WithChanges(newLineEdit.Value);

                // Modify the closing point location to adjust for the newly inserted line.
                closingPoint += newLineString.Length;
            }

            // Format the text that contains the newly inserted line.
            var (formattingChanges, newClosingPoint) = await FormatTrackingSpanAsync(
                document.WithText(textToFormat),
                documentOptions,
                openingPoint,
                closingPoint,
                shouldHonorAutoFormattingOnCloseBraceOption: false,
                braceFormattingIndentationRules: GetBraceIndentationFormattingRules(documentOptions),
                cancellationToken).ConfigureAwait(false);
            closingPoint = newClosingPoint;
            var formattedText = textToFormat.WithChanges(formattingChanges);

            // Get the empty line between the curly braces.
            var desiredCaretLine = GetLineBetweenCurlys(closingPoint, formattedText);
            Debug.Assert(desiredCaretLine.GetFirstNonWhitespacePosition() == null, "the line between the formatted braces is not empty");

            // Set the caret position to the properly indented column in the desired line.
            var newDocument = document.WithText(formattedText);
            var newDocumentText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var caretPosition = GetIndentedLinePosition(newDocument, newDocumentText, desiredCaretLine.LineNumber, cancellationToken);

            // The new line edit is calculated against the original text, d0, to get text d1.
            // The formatting edits are calculated against d1 to get text d2.
            // Merge the formatting and new line edits into a set of whitespace only text edits that all apply to d0.
            var overallChanges = newLineEdit != null ? MergeFormatChangesIntoNewLineChange(newLineEdit.Value, formattingChanges) : formattingChanges;
            return new BraceCompletionResult(overallChanges, caretPosition);

            static bool HasExistingEmptyLineBetweenBraces(int openingPoint, int closingPoint, SourceText sourceText)
            {
                // Check if there is an empty new line between the braces already.  If not insert one.
                // This handles razor cases where they insert an additional empty line before calling brace completion.
                var openingPointLine = sourceText.Lines.GetLineFromPosition(openingPoint).LineNumber;
                var closingPointLine = sourceText.Lines.GetLineFromPosition(closingPoint).LineNumber;

                return closingPointLine - 1 > openingPointLine && sourceText.Lines[closingPointLine - 1].IsEmptyOrWhitespace();
            }

            static TextLine GetLineBetweenCurlys(int closingPosition, SourceText text)
            {
                var closingBraceLineNumber = text.Lines.GetLineFromPosition(closingPosition - 1).LineNumber;
                return text.Lines[closingBraceLineNumber - 1];
            }

            static LinePosition GetIndentedLinePosition(Document document, SourceText sourceText, int lineNumber, CancellationToken cancellationToken)
            {
                var indentationService = document.GetRequiredLanguageService<IIndentationService>();
                var indentation = indentationService.GetIndentation(document, lineNumber, cancellationToken);

                var baseLinePosition = sourceText.Lines.GetLinePosition(indentation.BasePosition);
                var offsetOfBacePosition = baseLinePosition.Character;
                var totalOffset = offsetOfBacePosition + indentation.Offset;
                var indentedLinePosition = new LinePosition(lineNumber, totalOffset);
                return indentedLinePosition;
            }
        }

        public override async Task<bool> CanProvideBraceCompletionAsync(char brace, int openingPosition, Document document, CancellationToken cancellationToken)
        {
            // Only potentially valid for curly brace completion if not in an interpolation brace completion context.
            if (OpeningBrace == brace && await InterpolationBraceCompletionService.IsPositionInInterpolationContextAsync(document, openingPosition, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            return await base.CanProvideBraceCompletionAsync(brace, openingPosition, document, cancellationToken).ConfigureAwait(false);
        }

        protected override bool IsValidOpeningBraceToken(SyntaxToken token)
            => token.IsKind(SyntaxKind.OpenBraceToken) && !token.Parent.IsKind(SyntaxKind.Interpolation);

        protected override bool IsValidClosingBraceToken(SyntaxToken token)
            => token.IsKind(SyntaxKind.CloseBraceToken);

        /// <summary>
        /// Given the original text, a text edit to insert a new line to the original text, and
        /// a set of formatting text edits that apply to the text with the new line inserted.
        /// Modify the formatting edits to be relative to the original text and return the set
        /// of edits that can be applied to the original text to get the new line + formatted text.
        /// 
        /// Visible for testing.
        /// </summary>
        internal static ImmutableArray<TextChange> MergeFormatChangesIntoNewLineChange(TextChange newLineEdit, ImmutableArray<TextChange> formattingEdits)
        {
            using var _ = ArrayBuilder<TextChange>.GetInstance(out var overallChanges);

            // There is always text in the new line edit as we construct it above.
            var newLineText = newLineEdit.NewText!;
            var newLineTextStart = newLineEdit.Span.Start;
            var newLineTextEnd = newLineEdit.Span.End + newLineText.Length;
            var newLineTextAfterMerge = newLineText;
            foreach (var formattingEdit in formattingEdits)
            {
                var formattingEditText = formattingEdit.NewText ?? string.Empty;
                if (formattingEdit.Span.End < newLineTextStart)
                {
                    // The formatting change replacement span is entirely before where we added the new line, just take the change
                    // since the spans are already relative to the original text.
                    overallChanges.Add(formattingEdit);
                }
                else if (formattingEdit.Span.Start > newLineTextEnd)
                {
                    // The formatting change replacement span is entirely after the text inserted by the new line change.
                    // We need to adjust the span by the amount that was inserted by the new line to make the change relative to the original text.
                    var adjustedFormatChange = new TextChange(new TextSpan(formattingEdit.Span.Start - newLineText.Length, formattingEdit.Span.Length), formattingEditText);
                    overallChanges.Add(adjustedFormatChange);
                }
                else
                {
                    // The formatting change modifies locations that were inserted by the new line edit.
                    // There are three cases that cover the different types of overlap.

                    // Case 1: The new line text is entirely contained within the formatting change replacement span.
                    // The formatting change text therefore has all of the new line text that should be inserted.
                    // Remove the new line edit and modify the formatting change so that the end is relative to the original text.
                    if (newLineTextStart >= formattingEdit.Span.Start && newLineTextEnd <= formattingEdit.Span.End)
                    {
                        newLineTextAfterMerge = string.Empty;
                        var adjustedFormatChange = new TextChange(new TextSpan(formattingEdit.Span.Start, formattingEdit.Span.Length - newLineText.Length), formattingEditText);
                        overallChanges.Add(adjustedFormatChange);
                    }
                    // Case 2: The end of the formatting change span overlaps with the beginning of the new line text.
                    // Remove the overlapping text from the new line edit (the text is included in the formatting change).
                    // Modify the formatting change span to be everything up to the new line edit start so there is not overlap.
                    else if (newLineTextStart >= formattingEdit.Span.Start)
                    {
                        var overlappingAmount = formattingEdit.Span.End - newLineTextStart;
                        var adjustedFormatChange = new TextChange(new TextSpan(formattingEdit.Span.Start, formattingEdit.Span.Length - overlappingAmount), formattingEditText);

                        // Remove the overlap at the beginning of the new line text.
                        newLineTextAfterMerge = newLineTextAfterMerge.Remove(0, overlappingAmount);
                        overallChanges.Add(adjustedFormatChange);
                    }
                    // Case 3: The beginning of the formatting change span overlaps with the end of the new line text.
                    // Remove the overlapping text from the new line edit (the text is included in the formatting change).
                    // Modify the formatting change span to be everything after the new line edit end (and make the endpoint relative to the original text).
                    else
                    {
                        var overlappingAmount = newLineTextEnd - formattingEdit.Span.Start;
                        var adjustedFormatChange = new TextChange(new TextSpan(newLineEdit.Span.End, formattingEdit.Span.Length - overlappingAmount), formattingEditText);

                        // Remove the overlap at the end of the new line text.
                        newLineTextAfterMerge = newLineTextAfterMerge.Substring(0, newLineTextAfterMerge.Length - overlappingAmount);
                        overallChanges.Add(adjustedFormatChange);
                    }
                }
            }

            if (newLineTextAfterMerge != string.Empty)
            {
                // Ensure the new line change comes before formatting changes in case of ties.
                overallChanges.Insert(0, new TextChange(newLineEdit.Span, newLineTextAfterMerge));
            }

            return overallChanges.ToImmutable();
        }

        private static bool ContainsOnlyWhitespace(SourceText text, int openingPosition, int closingBraceEndPoint)
        {
            // Set the start point to the character after the opening brace.
            var start = openingPosition + 1;
            // Set the end point to the closing brace start character position.
            var end = closingBraceEndPoint - 1;

            for (var i = start; i < end; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Formats the span between the opening and closing points, options permitting.
        /// Returns the text changes that should be applied to the input document to 
        /// get the formatted text and the end of the close curly brace in the formatted text.
        /// </summary>
        private static async Task<(ImmutableArray<TextChange> textChanges, int finalCurlyBraceEnd)> FormatTrackingSpanAsync(
            Document document,
            DocumentOptionSet documentOptions,
            int openingPoint,
            int closingPoint,
            bool shouldHonorAutoFormattingOnCloseBraceOption,
            ImmutableArray<AbstractFormattingRule> braceFormattingIndentationRules,
            CancellationToken cancellationToken)
        {
            var option = document.Project.Solution.Options.GetOption(BraceCompletionOptions.AutoFormattingOnCloseBrace, document.Project.Language);
            if (!option && shouldHonorAutoFormattingOnCloseBraceOption)
            {
                return (ImmutableArray<TextChange>.Empty, closingPoint);
            }

            // Annotate the original closing brace so we can find it after formatting.
            document = await GetDocumentWithAnnotatedClosingBraceAsync(document, closingPoint, cancellationToken).ConfigureAwait(false);

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var startPoint = openingPoint;
            var endPoint = closingPoint;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Only format outside of the completed braces if they're on the same line for array/collection/object initializer expressions.
            // Example:   `var x = new int[]{}`:
            // Correct:   `var x = new int[] {}`
            // Incorrect: `var x = new int[] { }`
            // This is a heuristic to prevent brace completion from breaking user expectation/muscle memory in common scenarios.
            // see bug Devdiv:823958
            if (text.Lines.GetLineFromPosition(startPoint) == text.Lines.GetLineFromPosition(endPoint))
            {
                var startToken = root.FindToken(startPoint, findInsideTrivia: true);
                if (startToken.IsKind(SyntaxKind.OpenBraceToken) &&
                    (startToken.Parent?.IsInitializerForArrayOrCollectionCreationExpression() == true ||
                     startToken.Parent is AnonymousObjectCreationExpressionSyntax))
                {
                    // Since the braces are next to each other the span to format is everything up to the opening brace start.
                    endPoint = startToken.SpanStart;
                }
            }

            var style = documentOptions.GetOption(FormattingOptions.SmartIndent);
            if (style == FormattingOptions.IndentStyle.Smart)
            {
                // Set the formatting start point to be the beginning of the first word to the left 
                // of the opening brace location.
                // skip whitespace
                while (startPoint >= 0 && char.IsWhiteSpace(text[startPoint]))
                {
                    startPoint--;
                }

                // skip tokens in the first word to the left.
                startPoint--;
                while (startPoint >= 0 && !char.IsWhiteSpace(text[startPoint]))
                {
                    startPoint--;
                }
            }

            var spanToFormat = TextSpan.FromBounds(Math.Max(startPoint, 0), endPoint);
            var rules = document.GetFormattingRules(braceFormattingIndentationRules, spanToFormat);
            var result = Formatter.GetFormattingResult(root, SpecializedCollections.SingletonEnumerable(spanToFormat), document.Project.Solution.Workspace, documentOptions, rules, cancellationToken);
            if (result == null)
            {
                return (ImmutableArray<TextChange>.Empty, closingPoint);
            }

            var newRoot = result.GetFormattedRoot(cancellationToken);
            var newClosingPoint = newRoot.GetAnnotatedTokens(s_closingBraceSyntaxAnnotation).Single().SpanStart + 1;

            var textChanges = result.GetTextChanges(cancellationToken).ToImmutableArray();
            return (textChanges, newClosingPoint);

            static async Task<Document> GetDocumentWithAnnotatedClosingBraceAsync(Document document, int closingBraceEndPoint, CancellationToken cancellationToken)
            {
                var originalRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var closeBraceToken = originalRoot.FindToken(closingBraceEndPoint - 1);
                var newCloseBraceToken = closeBraceToken.WithAdditionalAnnotations(s_closingBraceSyntaxAnnotation);
                var root = originalRoot.ReplaceToken(closeBraceToken, newCloseBraceToken);
                return document.WithSyntaxRoot(root);
            }
        }

        private static ImmutableArray<AbstractFormattingRule> GetBraceIndentationFormattingRules(DocumentOptionSet documentOptions)
        {
            var indentStyle = documentOptions.GetOption(FormattingOptions.SmartIndent);
            return ImmutableArray.Create(BraceCompletionFormattingRule.ForIndentStyle(indentStyle));
        }

        private sealed class BraceCompletionFormattingRule : BaseFormattingRule
        {
            private static readonly Predicate<SuppressOperation> s_predicate = o => o == null || o.Option.IsOn(SuppressOption.NoWrapping);

            private static readonly ImmutableArray<BraceCompletionFormattingRule> s_instances = ImmutableArray.Create(
                new BraceCompletionFormattingRule(FormattingOptions.IndentStyle.None),
                new BraceCompletionFormattingRule(FormattingOptions.IndentStyle.Block),
                new BraceCompletionFormattingRule(FormattingOptions.IndentStyle.Smart));

            private readonly FormattingOptions.IndentStyle _indentStyle;
            private readonly CachedOptions _options;

            public BraceCompletionFormattingRule(FormattingOptions.IndentStyle indentStyle)
                : this(indentStyle, new CachedOptions(null))
            {
            }

            private BraceCompletionFormattingRule(FormattingOptions.IndentStyle indentStyle, CachedOptions options)
            {
                _indentStyle = indentStyle;
                _options = options;
            }

            public static AbstractFormattingRule ForIndentStyle(FormattingOptions.IndentStyle indentStyle)
            {
                Debug.Assert(s_instances[(int)indentStyle]._indentStyle == indentStyle);
                return s_instances[(int)indentStyle];
            }

            public override AbstractFormattingRule WithOptions(AnalyzerConfigOptions options)
            {
                var cachedOptions = new CachedOptions(options);

                if (cachedOptions == _options)
                {
                    return this;
                }

                return new BraceCompletionFormattingRule(_indentStyle, cachedOptions);
            }

            public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
            {
                // If we're inside any of the following expressions check if the option for
                // braces on new lines in object / array initializers is set before we attempt
                // to move the open brace location to a new line.
                // new MyObject {
                // new List<int> {
                // int[] arr = {
                //           = new[] {
                //           = new int[] {
                if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent.IsKind(
                    SyntaxKind.ObjectInitializerExpression,
                    SyntaxKind.CollectionInitializerExpression,
                    SyntaxKind.ArrayInitializerExpression,
                    SyntaxKind.ImplicitArrayCreationExpression))
                {
                    if (_options.NewLinesForBracesInObjectCollectionArrayInitializers)
                    {
                        return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                    }
                    else
                    {
                        return null;
                    }
                }

                return base.GetAdjustNewLinesOperation(in previousToken, in currentToken, in nextOperation);
            }

            public override void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode node, in NextAlignTokensOperationAction nextOperation)
            {
                base.AddAlignTokensOperations(list, node, in nextOperation);
                if (_indentStyle == FormattingOptions.IndentStyle.Block)
                {
                    var bracePair = node.GetBracePair();
                    if (bracePair.IsValidBracePair())
                    {
                        // If the user has set block style indentation and we're in a valid brace pair
                        // then make sure we align the close brace to the open brace.
                        AddAlignIndentationOfTokensToBaseTokenOperation(list, node, bracePair.openBrace,
                            SpecializedCollections.SingletonEnumerable(bracePair.closeBrace), AlignTokensOption.AlignIndentationOfTokensToFirstTokenOfBaseTokenLine);
                    }
                }
            }

            public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, in NextSuppressOperationAction nextOperation)
            {
                base.AddSuppressOperations(list, node, in nextOperation);

                // not sure exactly what is happening here, but removing the bellow causesthe indentation to be wrong.

                // remove suppression rules for array and collection initializer
                if (node.IsInitializerForArrayOrCollectionCreationExpression())
                {
                    // remove any suppression operation
                    list.RemoveAll(s_predicate);
                }
            }

            private readonly struct CachedOptions : IEquatable<CachedOptions>
            {
                public readonly bool NewLinesForBracesInObjectCollectionArrayInitializers;

                public CachedOptions(AnalyzerConfigOptions? options)
                {
                    NewLinesForBracesInObjectCollectionArrayInitializers = GetOptionOrDefault(options, CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers);
                }

                public static bool operator ==(CachedOptions left, CachedOptions right)
                    => left.Equals(right);

                public static bool operator !=(CachedOptions left, CachedOptions right)
                    => !(left == right);

                private static T GetOptionOrDefault<T>(AnalyzerConfigOptions? options, Option2<T> option)
                {
                    if (options is null)
                        return option.DefaultValue;

                    return options.GetOption(option);
                }

                public override bool Equals(object? obj)
                    => obj is CachedOptions options && Equals(options);

                public bool Equals(CachedOptions other)
                {
                    return NewLinesForBracesInObjectCollectionArrayInitializers == other.NewLinesForBracesInObjectCollectionArrayInitializers;
                }

                public override int GetHashCode()
                {
                    var hashCode = 0;
                    hashCode = (hashCode << 1) + (NewLinesForBracesInObjectCollectionArrayInitializers ? 1 : 0);
                    return hashCode;
                }
            }
        }
    }
}
