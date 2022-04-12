﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// Fix all occurrences logging.
    /// </summary>
    internal static class FixAllLogger
    {
        // correlation id of all events related to same instance of fix all
        public const string CorrelationId = nameof(CorrelationId);

        // Fix all context logging.
        private const string CodeRefactoringProvider = nameof(CodeRefactoringProvider);
        private const string CodeActionEquivalenceKey = nameof(CodeActionEquivalenceKey);
        public const string FixAllScope = nameof(FixAllScope);
        private const string LanguageName = nameof(LanguageName);
        private const string DocumentCount = nameof(DocumentCount);

        // Fix all computation result logging.
        private const string Result = nameof(Result);
        private const string Completed = nameof(Completed);
        private const string TimedOut = nameof(TimedOut);
        private const string Cancelled = nameof(Cancelled);
        private const string AllChangesApplied = nameof(AllChangesApplied);
        private const string SubsetOfChangesApplied = nameof(SubsetOfChangesApplied);

        public static void LogState(FixAllState fixAllState, bool isInternalCodeRefactoringProvider)
        {
            Logger.Log(FunctionId.Refactoring_FixAllOccurrencesContext, KeyValueLogMessage.Create(m =>
            {
                m[CorrelationId] = fixAllState.CorrelationId;

                if (isInternalCodeRefactoringProvider)
                {
                    m[CodeRefactoringProvider] = fixAllState.CodeRefactoringProvider.GetType().FullName!;
                    m[CodeActionEquivalenceKey] = fixAllState.CodeAction.EquivalenceKey;
                    m[LanguageName] = fixAllState.Project.Language;
                }
                else
                {
                    m[CodeRefactoringProvider] = fixAllState.CodeRefactoringProvider.GetType().FullName!.GetHashCode().ToString();
                    m[CodeActionEquivalenceKey] = fixAllState.CodeAction.EquivalenceKey?.GetHashCode().ToString();
                    m[LanguageName] = fixAllState.Project.Language.GetHashCode().ToString();
                }

                m[FixAllScope] = fixAllState.FixAllScope.ToString();
                switch (fixAllState.FixAllScope)
                {
                    case CodeRefactorings.FixAllScope.Project:
                        m[DocumentCount] = fixAllState.Project.DocumentIds.Count;
                        break;

                    case CodeRefactorings.FixAllScope.Solution:
                        m[DocumentCount] = fixAllState.Project.Solution.Projects.Sum(p => p.DocumentIds.Count);
                        break;
                }
            }));
        }

        public static void LogComputationResult(int correlationId, bool completed, bool timedOut = false)
        {
            Contract.ThrowIfTrue(completed && timedOut);

            string value;
            if (completed)
            {
                value = Completed;
            }
            else if (timedOut)
            {
                value = TimedOut;
            }
            else
            {
                value = Cancelled;
            }

            Logger.Log(FunctionId.Refactoring_FixAllOccurrencesComputation, KeyValueLogMessage.Create(m =>
            {
                m[CorrelationId] = correlationId;
                m[Result] = value;
            }));
        }

        public static void LogPreviewChangesResult(int? correlationId, bool applied, bool allChangesApplied = true)
        {
            string value;
            if (applied)
            {
                value = allChangesApplied ? AllChangesApplied : SubsetOfChangesApplied;
            }
            else
            {
                value = Cancelled;
            }

            Logger.Log(FunctionId.Refactoring_FixAllOccurrencesPreviewChanges, KeyValueLogMessage.Create(m =>
            {
                // we might not have this info for suppression
                if (correlationId.HasValue)
                {
                    m[CorrelationId] = correlationId;
                }

                m[Result] = value;
            }));
        }

        public static LogMessage CreateCorrelationLogMessage(int correlationId)
            => KeyValueLogMessage.Create(LogType.UserAction, m => m[CorrelationId] = correlationId);
    }
}
