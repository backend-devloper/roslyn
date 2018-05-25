﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CodeRefactorings.InvertIf
{
    internal abstract partial class AbstractInvertIfCodeRefactoringProvider<TIfStatementSyntax, TEmbeddedStatement> : CodeRefactoringProvider
        where TIfStatementSyntax : SyntaxNode
        where TEmbeddedStatement : class
    {
        protected enum InvertIfStyle
        {
            IfWithElse_SwapIfBodyWithElseBody,
            IfWithoutElse_SwapIfBodyWithSubsequentStatements,
            IfWithoutElse_MoveSubsequentStatementsToIfBody,
            IfWithoutElse_WithElseClause,
            IfWithoutElse_MoveIfBodyToElseClause,
            IfWithoutElse_WithSubsequentExitPointStatement,
            IfWithoutElse_WithNearmostJumpStatement,
            IfWithoutElse_WithNegatedCondition,
        }

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var textSpan = context.Span;
            if (!textSpan.IsEmpty)
            {
                return;
            }

            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(textSpan.Start);

            var ifNode = token.GetAncestor<TIfStatementSyntax>();
            if (ifNode == null)
            {
                return;
            }

            if (ifNode.OverlapsHiddenPosition(cancellationToken))
            {
                return;
            }

            var headerSpan = GetHeaderSpan(ifNode);
            if (!headerSpan.IntersectsWith(textSpan))
            {
                return;
            }

            if (!CanInvert(ifNode))
            {
                return;
            }

            // Keep the subsequent exit-point to be used in case (5) below.
            SyntaxNode subsequentSingleExitPointOpt = null;

            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
            var invertIfStyle = IsElseless(ifNode)
                ? GetInvertIfStyle(
                    ifNode,
                    semanticModel,
                    ref subsequentSingleExitPointOpt)
                : InvertIfStyle.IfWithElse_SwapIfBodyWithElseBody;

            context.RegisterRefactoring(new MyCodeAction(GetTitle(),
                c => InvertIfAsync(root, document, semanticModel, ifNode, invertIfStyle, subsequentSingleExitPointOpt, c)));
        }

        private InvertIfStyle GetInvertIfStyle(
            TIfStatementSyntax ifNode,
            SemanticModel semanticModel,
            ref SyntaxNode subsequentSingleExitPointOpt)
        {
            var ifBodyStatementRange = GetIfBodyStatementRange(ifNode);
            if (IsEmptyStatementRange(ifBodyStatementRange))
            {
                // (1) An empty if-statement: just negate the condition
                //  
                //  if (condition) { }
                //
                // ->
                //
                //  if (!condition) { }
                //
                return InvertIfStyle.IfWithoutElse_WithNegatedCondition;
            }

            var subsequentStatementRanges = GetSubsequentStatementRanges(ifNode);
            if (subsequentStatementRanges.All(IsEmptyStatementRange))
            {
                // (2) No statements after if-statement, invert with the nearmost parent jump-statement
                //
                //  void M() {
                //    if (condition) {
                //      Body();
                //    }
                //  }
                //
                // ->
                //
                //  void M() {
                //    if (!condition) {
                //      return;
                //    }
                //    Body();
                //  }
                //
                return InvertIfStyle.IfWithoutElse_WithNearmostJumpStatement;
            }

            AnalyzeControlFlow(
                semanticModel, ifBodyStatementRange,
                out var ifBodyEndPointIsReachable,
                out var ifBodySingleExitPointOpt);

            AnalyzeSubsequentControlFlow(
                semanticModel, subsequentStatementRanges,
                out var subsequentEndPointIsReachable,
                out subsequentSingleExitPointOpt);

            if (subsequentEndPointIsReachable)
            {
                if (!ifBodyEndPointIsReachable)
                {
                    if (ifBodyStatementRange.IsSingleStatement &&
                        SubsequentStatementsAreInTheSameBlock(ifNode, subsequentStatementRanges) &&
                        ifBodySingleExitPointOpt?.RawKind == GetNearmostParentJumpStatementRawKind(ifNode))
                    {
                        // (3) Invese of the case (2). Safe to move all subsequent statements to if-body.
                        // 
                        //  while (condition) {
                        //    if (condition) {
                        //      continue;
                        //    }
                        //    f();
                        //  }
                        //
                        // ->
                        //
                        //  while (condition) {
                        //    if (!condition) {
                        //      f();
                        //    }
                        //  }
                        //
                        return InvertIfStyle.IfWithoutElse_MoveSubsequentStatementsToIfBody;
                    }
                    else
                    {
                        // (4) Otherwise, we generate the else and swap blocks to keep flow intact.
                        // 
                        //  while (condition) {
                        //    if (condition) {
                        //      return;
                        //    }
                        //    f();
                        //  }
                        //
                        // ->
                        //
                        //  while (condition) {
                        //    if (!condition) {
                        //      f();
                        //    } else {
                        //      return;
                        //    }
                        //  }
                        //
                        return InvertIfStyle.IfWithoutElse_WithElseClause;
                    }
                }
            }
            else if (ifBodyEndPointIsReachable)
            {
                if (subsequentSingleExitPointOpt != null &&
                    SingleSubsequentStatement(subsequentStatementRanges))
                {
                    // (5) if-body end-point is reachable but the next statement is a only jump-statement.
                    //     This usually happens in a switch-statement. We invert and use that jump-statement.
                    // 
                    //  case constant:
                    //    if (condition) {
                    //      f();
                    //    }
                    //    break;
                    //
                    // ->
                    //
                    //  case constant:
                    //    if (!condition) {
                    //      break;
                    //    }
                    //    f();
                    //    break; // we always keep this so that we don't end up with invalid code.
                    //
                    return InvertIfStyle.IfWithoutElse_WithSubsequentExitPointStatement;
                }
            }
            else if (SubsequentStatementsAreInTheSameBlock(ifNode, subsequentStatementRanges))
            {
                // (6) If both if-body and subsequent statements have an unreachable end-point,
                //     it would be safe to just swap the two.
                //
                //    if (condition) {
                //      return;
                //    }
                //    break;
                //
                // ->
                //
                //  case constant:
                //    if (!condition) {
                //      break;
                //    }
                //    return;
                //
                return InvertIfStyle.IfWithoutElse_SwapIfBodyWithSubsequentStatements;
            }

            // (7) If none of the above worked, as the last resort we invert and generate an empty if-body.
            // 
            //  {
            //    if (condition) {
            //      f();
            //    }
            //    f();
            //  }
            //
            // ->
            //
            //  {
            //    if (!condition) {
            //    } else {
            //      f();
            //    }
            //    f();
            //  }
            //  
            return InvertIfStyle.IfWithoutElse_MoveIfBodyToElseClause;
        }

        private static bool SingleSubsequentStatement(ImmutableArray<StatementRange> subsequentStatementRanges)
        {
            return subsequentStatementRanges.Length == 1 && subsequentStatementRanges[0].IsSingleStatement;
        }

        private Task<Document> InvertIfAsync(
            SyntaxNode root,
            Document document,
            SemanticModel semanticModel,
            TIfStatementSyntax ifNode,
            InvertIfStyle invertIfStyle,
            SyntaxNode subsequentSingleExitPointOpt,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                document.WithSyntaxRoot(
                    GetRootWithInvertIfStatement(
                        root,
                        ifNode,
                        invertIfStyle,
                        subsequentSingleExitPointOpt,
                        negatedExpression: Negate(
                            GetCondition(ifNode),
                            document.GetLanguageService<SyntaxGenerator>(),
                            document.GetLanguageService<ISyntaxFactsService>(),
                            semanticModel,
                            cancellationToken))));
        }

        private static void AnalyzeSubsequentControlFlow(
            SemanticModel semanticModel,
            ImmutableArray<StatementRange> subsequentStatementRanges,
            out bool subsequentEndPointIsReachable,
            out SyntaxNode subsequentSingleExitPointOpt)
        {
            subsequentEndPointIsReachable = true;
            subsequentSingleExitPointOpt = null;

            foreach (var statementRange in subsequentStatementRanges)
            {
                AnalyzeControlFlow(
                    semanticModel,
                    statementRange,
                    out subsequentEndPointIsReachable,
                    out subsequentSingleExitPointOpt);
                if (!subsequentEndPointIsReachable)
                {
                    return;
                }
            }
        }

        private static void AnalyzeControlFlow(
            SemanticModel semanticModel,
            StatementRange statementRange,
            out bool endPointIsReachable,
            out SyntaxNode singleExitPointOpt)
        {
            var flow = semanticModel.AnalyzeControlFlow(
                statementRange.FirstStatement,
                statementRange.LastStatement);
            endPointIsReachable = flow.EndPointIsReachable;
            singleExitPointOpt = flow.ExitPoints.Length == 1 ? flow.ExitPoints[0] : null;
        }

        private static bool SubsequentStatementsAreInTheSameBlock(
            TIfStatementSyntax ifNode,
            ImmutableArray<StatementRange> subsequentStatementRanges)
        {
            Debug.Assert(subsequentStatementRanges.Length > 0);
            return ifNode.Parent == subsequentStatementRanges[0].FirstStatement.Parent;
        }

        private int GetNearmostParentJumpStatementRawKind(SyntaxNode ifNode)
        {
            foreach (var node in ifNode.Ancestors())
            {
                var jumpStatementRawKind = GetJumpStatementRawKind(node);
                if (jumpStatementRawKind != -1)
                {
                    return jumpStatementRawKind;
                }
            }

            throw ExceptionUtilities.Unreachable;
        }

        private bool IsEmptyStatementRange(StatementRange statementRange)
        {
            if (!statementRange.IsEmpty)
            {
                var parent = statementRange.Parent;
                if (!IsStatementContainer(parent))
                {
                    Debug.Assert(statementRange.IsSingleStatement);
                    return statementRange.FirstStatement.DescendantNodesAndSelf().All(IsNoOpSyntaxNode);
                }

                var statements = GetStatements(parent);
                var firstIndex = statements.IndexOf(statementRange.FirstStatement);
                var lastIndex = statements.IndexOf(statementRange.LastStatement);
                for (var i = firstIndex; i <= lastIndex; i++)
                {
                    if (!statements[i].DescendantNodesAndSelf().All(IsNoOpSyntaxNode))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private ImmutableArray<StatementRange> GetSubsequentStatementRanges(SyntaxNode ifNode)
        {
            var builder = ArrayBuilder<StatementRange>.GetInstance();

            var innerStatement = ifNode;
            foreach (var node in ifNode.Ancestors())
            {
                var nextStatement = GetNextExecutableStatement(innerStatement);
                if (nextStatement != null && IsStatementContainer(node))
                {
                    builder.Add(new StatementRange(nextStatement, GetStatements(node).Last()));
                }

                if (!CanControlFlowOut(node))
                {
                    // We no longer need to continue since other statements
                    // are out of reach, as far as this analysis concerned.
                    break;
                }

                if (IsStatement(node))
                {
                    innerStatement = node;
                }
            }

            return builder.ToImmutableAndFree();
        }

        protected abstract int GetJumpStatementRawKind(SyntaxNode node);
        protected abstract SyntaxNode GetJumpStatement(int rawKind);
        protected abstract bool IsNoOpSyntaxNode(SyntaxNode node);
        protected abstract bool IsStatement(SyntaxNode node);
        protected abstract bool IsStatementContainer(SyntaxNode node);
        protected abstract SyntaxList<SyntaxNode> GetStatements(SyntaxNode node);
        protected abstract SyntaxNode GetNextExecutableStatement(SyntaxNode node);
        protected abstract bool CanControlFlowOut(SyntaxNode node);
        protected abstract StatementRange GetIfBodyStatementRange(TIfStatementSyntax ifNode);
        protected abstract bool CanInvert(TIfStatementSyntax ifNode);
        protected abstract bool IsElseless(TIfStatementSyntax ifNode);
        protected abstract SyntaxNode GetCondition(TIfStatementSyntax ifNode);
        protected abstract TextSpan GetHeaderSpan(TIfStatementSyntax ifNode);
        protected abstract string GetTitle();

        protected abstract IEnumerable<SyntaxNode> UnwrapBlock(TEmbeddedStatement ifBody);
        protected abstract TEmbeddedStatement GetIfBody(TIfStatementSyntax ifNode);
        protected abstract TEmbeddedStatement GetElseBody(TIfStatementSyntax ifNode);
        protected abstract TEmbeddedStatement GetEmptyEmbeddedStatement();

        protected abstract TEmbeddedStatement AsEmbeddedStatement(
            TEmbeddedStatement originalStatement,
            IEnumerable<SyntaxNode> newStatements);

        protected abstract SyntaxNode UpdateIf(
            TIfStatementSyntax ifNode,
            SyntaxNode condition,
            TEmbeddedStatement trueStatement = null,
            TEmbeddedStatement falseStatement = null);

        protected abstract SyntaxNode WithStatements(
            SyntaxNode node,
            IEnumerable<SyntaxNode> statements);

        private SyntaxNode GetRootWithInvertIfStatement(
            SyntaxNode root,
            TIfStatementSyntax ifNode,
            InvertIfStyle invertIfStyle,
            SyntaxNode subsequentSingleExitPointOpt,
            SyntaxNode negatedExpression)
        {
            switch (invertIfStyle)
            {
                case InvertIfStyle.IfWithElse_SwapIfBodyWithElseBody:
                    {
                        var updatedIf = UpdateIf(
                           ifNode: ifNode,
                           condition: negatedExpression,
                           trueStatement: GetElseBody(ifNode),
                           falseStatement: GetIfBody(ifNode));

                        return root.ReplaceNode(ifNode, updatedIf);
                    }

                case InvertIfStyle.IfWithoutElse_MoveIfBodyToElseClause:
                    {
                        var ifBody = GetIfBody(ifNode);

                        var updatedIf = UpdateIf(
                            ifNode: ifNode,
                            condition: negatedExpression,
                            trueStatement: GetEmptyEmbeddedStatement(),
                            falseStatement: ifBody);

                        return root.ReplaceNode(ifNode, updatedIf);
                    }

                case InvertIfStyle.IfWithoutElse_WithNegatedCondition:
                    {
                        var updatedIf = UpdateIf(
                            ifNode: ifNode,
                            condition: negatedExpression);

                        return root.ReplaceNode(ifNode, updatedIf);
                    }

                case InvertIfStyle.IfWithoutElse_SwapIfBodyWithSubsequentStatements:
                    {
                        var currentParent = ifNode.Parent;
                        var statements = GetStatements(currentParent);
                        var index = statements.IndexOf(ifNode);

                        var statementsBeforeIf = statements.Take(index);
                        var statementsAfterIf = statements.Skip(index + 1);

                        var ifBody = GetIfBody(ifNode);

                        var updatedIf = UpdateIf(
                            ifNode: ifNode,
                            condition: negatedExpression,
                            trueStatement: AsEmbeddedStatement(ifBody, statementsAfterIf));

                        var updatedParent = WithStatements(
                            currentParent,
                            statementsBeforeIf.Concat(updatedIf).Concat(UnwrapBlock(ifBody)));

                        return root.ReplaceNode(currentParent, updatedParent.WithAdditionalAnnotations(Formatter.Annotation));
                    }

                case InvertIfStyle.IfWithoutElse_WithNearmostJumpStatement:
                    {
                        var currentParent = ifNode.Parent;
                        var statements = GetStatements(currentParent);
                        var index = statements.IndexOf(ifNode);

                        var ifBody = GetIfBody(ifNode);
                        var newIfBody = GetJumpStatement(GetNearmostParentJumpStatementRawKind(ifNode));

                        var updatedIf = UpdateIf(
                            ifNode: ifNode,
                            condition: negatedExpression,
                            trueStatement: AsEmbeddedStatement(ifBody, new[] { newIfBody }));

                        var statementsBeforeIf = statements.Take(index);

                        var updatedParent = WithStatements(
                            currentParent,
                            statementsBeforeIf.Concat(updatedIf).Concat(UnwrapBlock(ifBody)));

                        return root.ReplaceNode(currentParent, updatedParent .WithAdditionalAnnotations(Formatter.Annotation));
                    }

                case InvertIfStyle.IfWithoutElse_WithSubsequentExitPointStatement:
                    {
                        var currentParent = ifNode.Parent;
                        var statements = GetStatements(currentParent);
                        var index = statements.IndexOf(ifNode);

                        var ifBody = GetIfBody(ifNode);
                        var newIfBody = subsequentSingleExitPointOpt;

                        var updatedIf = UpdateIf(
                            ifNode: ifNode,
                            condition: negatedExpression,
                            trueStatement: AsEmbeddedStatement(ifBody, new[] { newIfBody }));

                        var statementsBeforeIf = statements.Take(index);

                        var updatedParent = WithStatements(
                            currentParent,
                            statementsBeforeIf.Concat(updatedIf).Concat(UnwrapBlock(ifBody)).Concat(newIfBody));

                        return root.ReplaceNode(currentParent, updatedParent.WithAdditionalAnnotations(Formatter.Annotation));
                    }

                case InvertIfStyle.IfWithoutElse_MoveSubsequentStatementsToIfBody:
                    {
                        var currentParent = ifNode.Parent;
                        var statements = GetStatements(currentParent);
                        var index = statements.IndexOf(ifNode);

                        var statementsBeforeIf = statements.Take(index);
                        var statementsAfterIf = statements.Skip(index + 1);
                        var ifBody = GetIfBody(ifNode);

                        var updatedIf = UpdateIf(
                            ifNode: ifNode,
                            condition: negatedExpression,
                            trueStatement: AsEmbeddedStatement(ifBody, statementsAfterIf));

                        var updatedParent = WithStatements(
                            currentParent,
                            statementsBeforeIf.Concat(updatedIf));

                        return root.ReplaceNode(currentParent, updatedParent.WithAdditionalAnnotations(Formatter.Annotation));
                    }

                case InvertIfStyle.IfWithoutElse_WithElseClause:
                    {
                        var currentParent = ifNode.Parent;
                        var statements = GetStatements(currentParent);
                        var index = statements.IndexOf(ifNode);

                        var statementsBeforeIf = statements.Take(index);
                        var statementsAfterIf = statements.Skip(index + 1);

                        var ifBody = GetIfBody(ifNode);

                        var updatedIf = UpdateIf(
                            ifNode: ifNode,
                            condition: negatedExpression,
                            trueStatement: AsEmbeddedStatement(ifBody, statementsAfterIf),
                            falseStatement: ifBody);

                        var updatedParent = WithStatements(
                            currentParent,
                            statementsBeforeIf.Concat(updatedIf));

                        return root.ReplaceNode(currentParent, updatedParent.WithAdditionalAnnotations(Formatter.Annotation));
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(invertIfStyle);
            }
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
