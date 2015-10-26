﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class PatternVariableBinder : LocalScopeBinder
    {
        private readonly ExpressionSyntax expression;
        private readonly ImmutableArray<ExpressionSyntax> expressions;
        private readonly ImmutableArray<PatternSyntax> patterns;
        public readonly SyntaxNode Syntax;

        internal PatternVariableBinder(SyntaxNode syntax, ImmutableArray<ExpressionSyntax> expressions, Binder next) : base(next)
        {
            this.Syntax = syntax;
            this.expressions = expressions;
        }

        internal PatternVariableBinder(SyntaxNode syntax, IEnumerable<VariableDeclaratorSyntax> declarations, Binder next) : base(next)
        {
            this.Syntax = syntax;
            var expressions = ArrayBuilder<ExpressionSyntax>.GetInstance();
            foreach (var decl in declarations)
            {
                var value = decl.Initializer?.Value;
                if (value != null) expressions.Add(value);
            }
            this.expressions = expressions.ToImmutableAndFree();
        }

        internal PatternVariableBinder(SyntaxNode syntax, IEnumerable<ArgumentSyntax> arguments, Binder next) : base(next)
        {
            this.Syntax = syntax;
            var expressions = ArrayBuilder<ExpressionSyntax>.GetInstance();
            foreach (var arg in arguments)
            {
                var value = arg.Expression;
                if (value != null) expressions.Add(value);
            }
            this.expressions = expressions.ToImmutableAndFree();
        }

        internal PatternVariableBinder(SwitchSectionSyntax syntax, Binder next) : base(next)
        {
            this.Syntax = syntax;
            var expressions = ArrayBuilder<ExpressionSyntax>.GetInstance();
            var patterns = ArrayBuilder<PatternSyntax>.GetInstance();
            foreach (var label in syntax.Labels)
            {
                var match = label as CaseMatchLabelSyntax;
                if (match != null)
                {
                    patterns.Add(match.Pattern);
                    if (match.Condition != null)
                    {
                        expressions.Add(match.Condition);
                    }
                }
            }

            this.expressions = expressions.ToImmutableAndFree();
            this.patterns = patterns.ToImmutableAndFree();
        }

        internal PatternVariableBinder(ForStatementSyntax syntax, Binder next) : base(next)
        {
            this.Syntax = syntax;
            var expressions = ArrayBuilder<ExpressionSyntax>.GetInstance();
            if (syntax.Declaration != null) foreach(var decl in syntax.Declaration.Variables)
            {
                var value = decl.Initializer?.Value;
                if (value != null) expressions.Add(value);
            }
            if (syntax.Initializers != null) expressions.AddRange(syntax.Initializers);
            if (syntax.Condition != null) expressions.Add(syntax.Condition);
            if (syntax.Incrementors != null) expressions.AddRange(syntax.Incrementors);
            this.expressions = expressions.ToImmutableAndFree();
        }

        internal PatternVariableBinder(SyntaxNode syntax, ExpressionSyntax expression, Binder next) : base(next)
        {
            this.expression = expression;
            this.Syntax = syntax;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            var patterns = PatternVariableFinder.FindPatternVariables(expression, expressions, this.patterns);
            var builder = ArrayBuilder<LocalSymbol>.GetInstance();
            foreach (var pattern in patterns)
            {
                builder.Add(SourceLocalSymbol.MakeLocal(Next.ContainingMember(), this, pattern.Type, pattern.Identifier, LocalDeclarationKind.PatternVariable));
            }
            patterns.Free();
            return builder.ToImmutableAndFree();
        }

        internal BoundExpression WrapWithPatternVariables(BoundExpression expression)
        {
            return (Locals.Length == 0)
                ? expression
                : new BoundSequence(expression.Syntax, Locals, ImmutableArray<BoundExpression>.Empty, expression, expression.Type);
        }

        class PatternVariableFinder : CSharpSyntaxWalker
        {
            ArrayBuilder<DeclarationPatternSyntax> declarationPatterns = ArrayBuilder<DeclarationPatternSyntax>.GetInstance();
            internal static ArrayBuilder<DeclarationPatternSyntax> FindPatternVariables(
                ExpressionSyntax expression,
                ImmutableArray<ExpressionSyntax> expressions,
                ImmutableArray<PatternSyntax> patterns)
            {
                var finder = s_poolInstance.Allocate();
                finder.declarationPatterns = ArrayBuilder<DeclarationPatternSyntax>.GetInstance();
                finder.Visit(expression);
                if (!expressions.IsDefaultOrEmpty) foreach (var subExpression in expressions)
                {
                    finder.Visit(subExpression);
                }

                var result = finder.declarationPatterns;
                if (patterns != null)
                {
                    foreach (var pattern in patterns)
                    {
                        var declarationPattern = pattern as DeclarationPatternSyntax;
                        if (declarationPattern != null) result.Add(declarationPattern);
                    }
                }

                finder.declarationPatterns = null;
                s_poolInstance.Free(finder);
                return result;
            }

            public override void VisitDeclarationPattern(DeclarationPatternSyntax node)
            {
                declarationPatterns.Add(node);
                base.VisitDeclarationPattern(node);
            }
            public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) { }
            public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) { }
            public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node) { }
            public override void VisitQueryExpression(QueryExpressionSyntax node) { }

            #region pool
            private static readonly ObjectPool<PatternVariableFinder> s_poolInstance = CreatePool();

            public static ObjectPool<PatternVariableFinder> CreatePool()
            {
                return new ObjectPool<PatternVariableFinder>(() => new PatternVariableFinder(), 10);
            }
            #endregion
        }
    }
}
