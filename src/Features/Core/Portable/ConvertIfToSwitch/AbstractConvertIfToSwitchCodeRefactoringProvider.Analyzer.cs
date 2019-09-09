﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConvertIfToSwitch
{
    using static BinaryOperatorKind;

    internal abstract partial class AbstractConvertIfToSwitchCodeRefactoringProvider<
        TIfStatementSyntax, TExpressionSyntax, TIsExpressionSyntax, TPatternSyntax>
    {
        // Match the following pattern which can be safely converted to switch statement
        //
        //    <if-statement-sequence>
        //        : if (<section-expr>) { <unreachable-end-point> }, <if-statement-sequence>
        //        : if (<section-expr>) { <unreachable-end-point> }, ( return | throw )
        //        | <if-statement>
        //
        //    <if-statement>
        //        : if (<section-expr>) { _ } else <if-statement>
        //        | if (<section-expr>) { _ } else { _ }
        //        | if (<section-expr>) { _ }
        //        | { <if-statement-sequence> }
        //
        //    <section-expr>
        //        : <section-expr> || <pattern-expr>
        //        | <pattern-expr>
        //
        //    <pattern-expr>
        //        : <pattern-expr> && <expr>                         // C#
        //        | <expr0> is <pattern>                             // C#
        //        | <expr0> is <type>                                // C#
        //        | <expr0> == <const-expr>                          // C#, VB
        //        | <expr0> <comparison-op> <const>                  //     VB
        //        | ( <expr0> >= <const> | <const> <= <expr0> )
        //           && ( <expr0> <= <const> | <const> >= <expr0> )  //     VB
        //        | ( <expr0> <= <const> | <const> >= <expr0> )
        //           && ( <expr0> >= <const> | <const> <= <expr0> )  //     VB
        //
        internal abstract class Analyzer
        {
            private SyntaxNode? _switchTargetExpression;
            private readonly ISyntaxFactsService _syntaxFacts;

            protected Analyzer(ISyntaxFactsService syntaxFacts)
            {
                _syntaxFacts = syntaxFacts;
            }

            public (ImmutableArray<AnalyzedSwitchSection>, SyntaxNode) AnalyzeIfStatementSequence(ReadOnlySpan<IOperation> operations)
            {
                var sections = ArrayBuilder<AnalyzedSwitchSection>.GetInstance();
                if (!ParseIfStatementSequence(operations, sections, out var defaultBodyOpt))
                {
                    sections.Free();
                    return default;
                }

                if (defaultBodyOpt is object)
                {
                    sections.Add(new AnalyzedSwitchSection(labels: default, defaultBodyOpt, defaultBodyOpt.Syntax));
                }

                Debug.Assert(_switchTargetExpression is object);
                // UNDONE: Null-suppression should be removed once Assert is annotated
                return (sections.ToImmutableAndFree(), _switchTargetExpression!);
            }

            // Tree to parse:
            //
            //    <if-statement-sequence>
            //        : if (<section-expr>) { <unreachable-end-point> }, <if-statement-sequence>
            //        : if (<section-expr>) { <unreachable-end-point> }, ( return | throw )
            //        | <if-statement>
            //
            private bool ParseIfStatementSequence(ReadOnlySpan<IOperation> operations, ArrayBuilder<AnalyzedSwitchSection> sections, out IOperation? defaultBodyOpt)
            {
                if (operations.Length > 1 &&
                    operations[0] is IConditionalOperation { WhenFalse: null } op &&
                    HasUnreachableEndPoint(op.WhenTrue))
                {
                    if (!ParseIfStatement(op, sections, out defaultBodyOpt))
                    {
                        return false;
                    }

                    if (!ParseIfStatementSequence(operations.Slice(1), sections, out defaultBodyOpt))
                    {
                        var nextStatement = operations[1];
                        if (nextStatement is IReturnOperation { ReturnedValue: { } } ||
                            nextStatement is IThrowOperation { Exception: { } })
                        {
                            defaultBodyOpt = nextStatement;
                        }
                    }

                    return true;
                }

                if (operations.Length > 0)
                {
                    return ParseIfStatement(operations[0], sections, out defaultBodyOpt);
                }

                defaultBodyOpt = null;
                return false;
            }

            // Tree to parse:
            //
            //    <if-statement>
            //        : if (<section-expr>) { _ } else <if-statement>
            //        | if (<section-expr>) { _ } else { _ }
            //        | if (<section-expr>) { _ }
            //        | { <if-statement-sequence> }
            //
            private bool ParseIfStatement(IOperation operation, ArrayBuilder<AnalyzedSwitchSection> sections, out IOperation? defaultBodyOpt)
            {
                switch (operation)
                {
                    case IBlockOperation op:
                        return ParseIfStatementSequence(op.Operations.AsSpan(), sections, out defaultBodyOpt);

                    case IConditionalOperation op when CanConvert(op):
                        var section = ParseSwitchSection(op);
                        if (section is null)
                        {
                            defaultBodyOpt = null;
                            return false;
                        }

                        sections.Add(section);

                        if (op.WhenFalse is null)
                        {
                            defaultBodyOpt = null;
                        }
                        else if (!ParseIfStatement(op.WhenFalse, sections, out defaultBodyOpt))
                        {
                            defaultBodyOpt = op.WhenFalse;
                        }

                        return true;
                }

                defaultBodyOpt = null;
                return false;
            }

            private AnalyzedSwitchSection? ParseSwitchSection(IConditionalOperation operation)
            {
                var labels = ArrayBuilder<AnalyzedSwitchLabel>.GetInstance();
                if (!ParseSwitchLabels(operation.Condition, labels))
                {
                    labels.Free();
                    return null;
                }

                return new AnalyzedSwitchSection(labels.ToImmutableAndFree(), operation.WhenTrue, operation.Syntax);
            }

            // Tree to parse:
            //
            //    <section-expr>
            //        : <section-expr> || <pattern-expr>
            //        | <pattern-expr>
            //
            private bool ParseSwitchLabels(IOperation operation, ArrayBuilder<AnalyzedSwitchLabel> labels)
            {
                if (operation is IBinaryOperation { OperatorKind: ConditionalOr } op)
                {
                    if (!ParseSwitchLabels(op.LeftOperand, labels))
                    {
                        return false;
                    }

                    operation = op.RightOperand;
                }

                var label = ParseSwitchLabel(operation);
                if (label is null)
                {
                    return false;
                }

                labels.Add(label);
                return true;
            }

            private AnalyzedSwitchLabel? ParseSwitchLabel(IOperation operation)
            {
                var guards = ArrayBuilder<TExpressionSyntax>.GetInstance();
                var pattern = ParsePattern(operation, guards);
                if (pattern is null)
                {
                    guards.Free();
                    return null;
                }

                return new AnalyzedSwitchLabel(pattern, guards.ToImmutableAndFree());
            }

            private enum ConstantResult
            {
                None,
                Left,
                Right,
            }

            private ConstantResult DetermineConstant(IBinaryOperation op)
            {
                return (op.LeftOperand, op.RightOperand) switch
                {
                    var (e, v) when IsConstant(v) && CheckTargetExpression(e) => ConstantResult.Right,
                    var (v, e) when IsConstant(v) && CheckTargetExpression(e) => ConstantResult.Left,
                    _ => ConstantResult.None,
                };
            }

            // Tree to parse:
            //
            //    <pattern-expr>
            //        : <pattern-expr> && <expr>                         // C#
            //        | <expr0> is <pattern>                             // C#
            //        | <expr0> is <type>                                // C#
            //        | <expr0> == <const-expr>                          // C#, VB
            //        | <expr0> <comparison-op> <const>                  //     VB
            //        | ( <expr0> >= <const> | <const> <= <expr0> )
            //           && ( <expr0> <= <const> | <const> >= <expr0> )  //     VB
            //        | ( <expr0> <= <const> | <const> >= <expr0> )
            //           && ( <expr0> >= <const> | <const> <= <expr0> )  //     VB
            //
            private AnalyzedPattern? ParsePattern(IOperation operation, ArrayBuilder<TExpressionSyntax> guards)
            {
                switch (operation)
                {
                    case IBinaryOperation { OperatorKind: ConditionalAnd } op
                        when SupportsCaseGuard && op.RightOperand.Syntax is TExpressionSyntax node:
                        guards.Add(node);
                        return ParsePattern(op.LeftOperand, guards);

                    case IBinaryOperation { OperatorKind: ConditionalAnd } op
                        when SupportsRangePattern && GetRangeBounds(op) is (TExpressionSyntax lower, TExpressionSyntax higher):
                        return new AnalyzedPattern.Range(lower, higher);

                    case IBinaryOperation { OperatorKind: BinaryOperatorKind.Equals } op:
                        return DetermineConstant(op) switch
                        {
                            ConstantResult.Left when op.LeftOperand.Syntax is TExpressionSyntax left
                                => new AnalyzedPattern.Constant(left),
                            ConstantResult.Right when op.RightOperand.Syntax is TExpressionSyntax right
                                => new AnalyzedPattern.Constant(right),
                            _ => null
                        };

                    case IBinaryOperation op when SupportsRelationalPattern && IsComparisonOperator(op.OperatorKind):
                        return DetermineConstant(op) switch
                        {
                            ConstantResult.Left when op.LeftOperand.Syntax is TExpressionSyntax left
                                => new AnalyzedPattern.Relational(Negate(op.OperatorKind), left),
                            ConstantResult.Right when op.RightOperand.Syntax is TExpressionSyntax right
                                => new AnalyzedPattern.Relational(op.OperatorKind, right),
                            _ => null
                        };

                    case IIsTypeOperation op
                        when SupportsTypePattern && CheckTargetExpression(op.ValueOperand) && op.Syntax is TIsExpressionSyntax node:
                        return new AnalyzedPattern.Type(node);

                    case IIsPatternOperation op
                        when SupportsSourcePattern && CheckTargetExpression(op.Value) && op.Pattern.Syntax is TPatternSyntax pattern:
                        return new AnalyzedPattern.Source(pattern);

                    case IParenthesizedOperation op:
                        return ParsePattern(op.Operand, guards);
                }

                return null;
            }

            private enum BoundKind
            {
                None,
                Lower,
                Higher,
            }

            private (SyntaxNode Lower, SyntaxNode Higher) GetRangeBounds(IBinaryOperation op)
            {
                if (!(op is { LeftOperand: IBinaryOperation left, RightOperand: IBinaryOperation right }))
                {
                    return default;
                }

                return (GetRangeBound(left), GetRangeBound(right)) switch
                {
                    ({ Kind: BoundKind.Lower } low, { Kind: BoundKind.Higher } high)
                        when CheckTargetExpression(low.Expression, high.Expression) => (low.Value.Syntax, high.Value.Syntax),
                    ({ Kind: BoundKind.Higher } high, { Kind: BoundKind.Lower } low)
                        when CheckTargetExpression(low.Expression, high.Expression) => (low.Value.Syntax, high.Value.Syntax),
                    _ => default
                };

                bool CheckTargetExpression(IOperation left, IOperation right)
                    => _syntaxFacts.AreEquivalent(left.Syntax, right.Syntax) && this.CheckTargetExpression(left);
            }

            private static (BoundKind Kind, IOperation Expression, IOperation Value) GetRangeBound(IBinaryOperation op)
            {
                return op.OperatorKind switch
                {
                    // 5 <= i
                    LessThanOrEqual when IsConstant(op.LeftOperand) => (BoundKind.Lower, op.RightOperand, op.LeftOperand),
                    // i <= 5
                    LessThanOrEqual when IsConstant(op.RightOperand) => (BoundKind.Higher, op.LeftOperand, op.RightOperand),
                    // 5 >= i
                    GreaterThanOrEqual when IsConstant(op.LeftOperand) => (BoundKind.Higher, op.RightOperand, op.LeftOperand),
                    // i >= 5
                    GreaterThanOrEqual when IsConstant(op.RightOperand) => (BoundKind.Lower, op.LeftOperand, op.RightOperand),
                    _ => default
                };
            }

            private static BinaryOperatorKind Negate(BinaryOperatorKind operatorKind)
            {
                return operatorKind switch
                {
                    LessThan => GreaterThan,
                    LessThanOrEqual => GreaterThanOrEqual,
                    GreaterThanOrEqual => LessThanOrEqual,
                    GreaterThan => LessThan,
                    NotEquals => NotEquals,
                    var v => throw ExceptionUtilities.UnexpectedValue(v)
                };
            }

            private static bool IsComparisonOperator(BinaryOperatorKind operatorKind)
            {
                switch (operatorKind)
                {
                    case LessThan:
                    case LessThanOrEqual:
                    case GreaterThanOrEqual:
                    case GreaterThan:
                    case NotEquals:
                        return true;
                    default:
                        return false;
                }
            }

            private static bool IsConstant(IOperation operation)
            {
                // Use syntax to skip possible conversion operation
                return operation.SemanticModel.GetConstantValue(operation.Syntax).HasValue;
            }

            private bool CheckTargetExpression(IOperation operation)
            {
                if (operation is IConversionOperation { IsImplicit: false } op)
                {
                    // Unwrap explicit casts because switch will emit those anyways
                    operation = op.Operand;
                }

                var expression = operation.Syntax;
                // If we have not figured the switch expression yet,
                // we will assume that the first expression is the one.
                if (_switchTargetExpression is null)
                {
                    _switchTargetExpression = expression;
                    return true;
                }

                return _syntaxFacts.AreEquivalent(expression, _switchTargetExpression);
            }

            public abstract bool CanConvert(IConditionalOperation operation);
            public abstract bool HasUnreachableEndPoint(IOperation operation);

            public abstract bool SupportsSwitchExpression { get; }
            public abstract bool SupportsRelationalPattern { get; }
            public abstract bool SupportsSourcePattern { get; }
            public abstract bool SupportsRangePattern { get; }
            public abstract bool SupportsTypePattern { get; }
            public abstract bool SupportsCaseGuard { get; }
        }
    }
}
