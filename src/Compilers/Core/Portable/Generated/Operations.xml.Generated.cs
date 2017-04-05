// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents an expression that creates a pointer value by taking the address of a reference.
    /// </summary>
    internal abstract partial class AddressOfExpressionBase : Operation, IAddressOfExpression
    {
        protected AddressOfExpressionBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.AddressOfExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Addressed reference.
        /// </summary>
        public abstract IOperation Reference { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitAddressOfExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAddressOfExpression(this, argument);
        }
    }
    /// <summary>
    /// Represents an expression that creates a pointer value by taking the address of a reference.
    /// </summary>
    internal sealed partial class AddressOfExpression : AddressOfExpressionBase, IAddressOfExpression
    {
        public AddressOfExpression(IOperation reference, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Reference = reference ?? throw new System.ArgumentNullException("reference");
        }
        /// <summary>
        /// Addressed reference.
        /// </summary>
        public override IOperation Reference { get; }
    }
    /// <summary>
    /// Represents an expression that creates a pointer value by taking the address of a reference.
    /// </summary>
    internal sealed partial class LazyAddressOfExpression : AddressOfExpressionBase, IAddressOfExpression
    {
        private readonly Lazy<IOperation> _lazyReference;

        public LazyAddressOfExpression(Lazy<IOperation> reference, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyReference = reference ?? throw new System.ArgumentNullException("reference");
        }
        /// <summary>
        /// Addressed reference.
        /// </summary>
        public override IOperation Reference => _lazyReference.Value;
    }

    /// <summary>
    /// Represents an argument in a method invocation.
    /// </summary>
    internal abstract partial class ArgumentBase : Operation, IArgument
    {
        protected ArgumentBase(ArgumentKind argumentKind, IParameterSymbol parameter, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.Argument, isInvalid, syntax, type, constantValue)
        {
            ArgumentKind = argumentKind;
            Parameter = parameter ?? throw new System.ArgumentNullException("parameter");
        }
        /// <summary>
        /// Kind of argument.
        /// </summary>
        public ArgumentKind ArgumentKind { get; }
        /// <summary>
        /// Parameter the argument matches.
        /// </summary>
        public IParameterSymbol Parameter { get; }
        /// <summary>
        /// Value supplied for the argument.
        /// </summary>
        public abstract IOperation Value { get; }
        /// <summary>
        /// Conversion applied to the argument value passing it into the target method. Applicable only to VB Reference arguments.
        /// </summary>
        public abstract IOperation InConversion { get; }
        /// <summary>
        /// Conversion applied to the argument value after the invocation. Applicable only to VB Reference arguments.
        /// </summary>
        public abstract IOperation OutConversion { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitArgument(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitArgument(this, argument);
        }
    }

    /// <summary>
    /// Represents an argument in a method invocation.
    /// </summary>
    internal sealed partial class Argument : ArgumentBase, IArgument
    {
        public Argument(ArgumentKind argumentKind, IParameterSymbol parameter, IOperation value, IOperation inConversion, IOperation outConversion, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(argumentKind, parameter, isInvalid, syntax, type, constantValue)
        {
            Value = value ?? throw new System.ArgumentNullException("value");
            InConversion = inConversion ?? throw new System.ArgumentNullException("inConversion");
            OutConversion = outConversion ?? throw new System.ArgumentNullException("outConversion");
        }
        /// <summary>
        /// Value supplied for the argument.
        /// </summary>
        public override IOperation Value { get; }
        /// <summary>
        /// Conversion applied to the argument value passing it into the target method. Applicable only to VB Reference arguments.
        /// </summary>
        public override IOperation InConversion { get; }
        /// <summary>
        /// Conversion applied to the argument value after the invocation. Applicable only to VB Reference arguments.
        /// </summary>
        public override IOperation OutConversion { get; }
    }

    /// <summary>
    /// Represents an argument in a method invocation.
    /// </summary>
    internal sealed partial class LazyArgument : ArgumentBase, IArgument
    {
        private readonly Lazy<IOperation> _lazyValue;
        private readonly Lazy<IOperation> _lazyInConversion;
        private readonly Lazy<IOperation> _lazyOutConversion;

        public LazyArgument(ArgumentKind argumentKind, IParameterSymbol parameter, Lazy<IOperation> value, Lazy<IOperation> inConversion, Lazy<IOperation> outConversion, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(argumentKind, parameter, isInvalid, syntax, type, constantValue)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException("value");
            _lazyInConversion = inConversion ?? throw new System.ArgumentNullException("inConversion");
            _lazyOutConversion = outConversion ?? throw new System.ArgumentNullException("outConversion");
        }
        /// <summary>
        /// Value supplied for the argument.
        /// </summary>
        public override IOperation Value => _lazyValue.Value;

        /// <summary>
        /// Conversion applied to the argument value passing it into the target method. Applicable only to VB Reference arguments.
        /// </summary>
        public override IOperation InConversion => _lazyInConversion.Value;

        /// <summary>
        /// Conversion applied to the argument value after the invocation. Applicable only to VB Reference arguments.
        /// </summary>
        public override IOperation OutConversion => _lazyOutConversion.Value;
    }

    /// <summary>
    /// Represents the creation of an array instance.
    /// </summary>
    internal abstract partial class ArrayCreationExpressionBase : Operation, IArrayCreationExpression
    {
        protected ArrayCreationExpressionBase(ITypeSymbol elementType, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ArrayCreationExpression, isInvalid, syntax, type, constantValue)
        {
            ElementType = elementType ?? throw new System.ArgumentNullException("elementType");
        }
        /// <summary>
        /// Element type of the created array instance.
        /// </summary>
        public ITypeSymbol ElementType { get; }
        /// <summary>
        /// Sizes of the dimensions of the created array instance.
        /// </summary>
        public abstract ImmutableArray<IOperation> DimensionSizes { get; }
        /// <summary>
        /// Values of elements of the created array instance.
        /// </summary>
        public abstract IArrayInitializer Initializer { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitArrayCreationExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitArrayCreationExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents the creation of an array instance.
    /// </summary>
    internal sealed partial class ArrayCreationExpression : ArrayCreationExpressionBase, IArrayCreationExpression
    {
        public ArrayCreationExpression(ITypeSymbol elementType, ImmutableArray<IOperation> dimensionSizes, IArrayInitializer initializer, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(elementType, isInvalid, syntax, type, constantValue)
        {
            DimensionSizes = dimensionSizes;
            Initializer = initializer ?? throw new System.ArgumentNullException("initializer");
        }
        /// <summary>
        /// Sizes of the dimensions of the created array instance.
        /// </summary>
        public override ImmutableArray<IOperation> DimensionSizes { get; }
        /// <summary>
        /// Values of elements of the created array instance.
        /// </summary>
        public override IArrayInitializer Initializer { get; }
    }

    /// <summary>
    /// Represents the creation of an array instance.
    /// </summary>
    internal sealed partial class LazyArrayCreationExpression : ArrayCreationExpressionBase, IArrayCreationExpression
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyDimensionSizes;
        private readonly Lazy<IArrayInitializer> _lazyInitializer;

        public LazyArrayCreationExpression(ITypeSymbol elementType, Lazy<ImmutableArray<IOperation>> dimensionSizes, Lazy<IArrayInitializer> initializer, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(elementType, isInvalid, syntax, type, constantValue)
        {
            _lazyDimensionSizes = dimensionSizes;
            _lazyInitializer = initializer ?? throw new System.ArgumentNullException("initializer");
        }
        /// <summary>
        /// Sizes of the dimensions of the created array instance.
        /// </summary>
        public override ImmutableArray<IOperation> DimensionSizes => _lazyDimensionSizes.Value;

        /// <summary>
        /// Values of elements of the created array instance.
        /// </summary>
        public override IArrayInitializer Initializer => _lazyInitializer.Value;
    }

    /// <summary>
    /// Represents a reference to an array element.
    /// </summary>
    internal abstract partial class ArrayElementReferenceExpressionBase : Operation, IArrayElementReferenceExpression
    {
        protected ArrayElementReferenceExpressionBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ArrayElementReferenceExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Array to be indexed.
        /// </summary>
        public abstract IOperation ArrayReference { get; }
        /// <summary>
        /// Indices that specify an individual element.
        /// </summary>
        public abstract ImmutableArray<IOperation> Indices { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitArrayElementReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitArrayElementReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to an array element.
    /// </summary>
    internal sealed partial class ArrayElementReferenceExpression : ArrayElementReferenceExpressionBase, IArrayElementReferenceExpression
    {
        public ArrayElementReferenceExpression(IOperation arrayReference, ImmutableArray<IOperation> indices, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            ArrayReference = arrayReference ?? throw new System.ArgumentNullException("arrayReference");
            Indices = indices;
        }
        /// <summary>
        /// Array to be indexed.
        /// </summary>
        public override IOperation ArrayReference { get; }
        /// <summary>
        /// Indices that specify an individual element.
        /// </summary>
        public override ImmutableArray<IOperation> Indices { get; }
    }

    /// <summary>
    /// Represents a reference to an array element.
    /// </summary>
    internal sealed partial class LazyArrayElementReferenceExpression : ArrayElementReferenceExpressionBase, IArrayElementReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyArrayReference;
        private readonly Lazy<ImmutableArray<IOperation>> _lazyIndices;

        public LazyArrayElementReferenceExpression(Lazy<IOperation> arrayReference, Lazy<ImmutableArray<IOperation>> indices, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyArrayReference = arrayReference ?? throw new System.ArgumentNullException("arrayReference");
            _lazyIndices = indices;
        }
        /// <summary>
        /// Array to be indexed.
        /// </summary>
        public override IOperation ArrayReference => _lazyArrayReference.Value;

        /// <summary>
        /// Indices that specify an individual element.
        /// </summary>
        public override ImmutableArray<IOperation> Indices => _lazyIndices.Value;
    }

    /// <summary>
    /// Represents the initialization of an array instance.
    /// </summary>
    internal abstract partial class ArrayInitializerBase : Operation, IArrayInitializer
    {
        protected ArrayInitializerBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ArrayInitializer, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Values to initialize array elements.
        /// </summary>
        public abstract ImmutableArray<IOperation> ElementValues { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitArrayInitializer(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitArrayInitializer(this, argument);
        }
    }

    /// <summary>
    /// Represents the initialization of an array instance.
    /// </summary>
    internal sealed partial class ArrayInitializer : ArrayInitializerBase, IArrayInitializer
    {
        public ArrayInitializer(ImmutableArray<IOperation> elementValues, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            ElementValues = elementValues;
        }
        /// <summary>
        /// Values to initialize array elements.
        /// </summary>
        public override ImmutableArray<IOperation> ElementValues { get; }
    }

    /// <summary>
    /// Represents the initialization of an array instance.
    /// </summary>
    internal sealed partial class LazyArrayInitializer : ArrayInitializerBase, IArrayInitializer
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyElementValues;

        public LazyArrayInitializer(Lazy<ImmutableArray<IOperation>> elementValues, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyElementValues = elementValues;
        }
        /// <summary>
        /// Values to initialize array elements.
        /// </summary>
        public override ImmutableArray<IOperation> ElementValues => _lazyElementValues.Value;
    }

    /// <summary>
    /// Represents an assignment expression.
    /// </summary>
    internal abstract partial class AssignmentExpressionBase : Operation, IAssignmentExpression
    {
        protected AssignmentExpressionBase(OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public abstract IOperation Target { get; }
        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        public abstract IOperation Value { get; }
    }

    /// <summary>
    /// Represents an assignment expression.
    /// </summary>
    internal sealed partial class AssignmentExpression : AssignmentExpressionBase, IAssignmentExpression
    {
        public AssignmentExpression(IOperation target, IOperation value, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.AssignmentExpression, isInvalid, syntax, type, constantValue)
        {
            Target = target ?? throw new System.ArgumentNullException("target");
            Value = value ?? throw new System.ArgumentNullException("value");
        }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public override IOperation Target { get; }
        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        public override IOperation Value { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitAssignmentExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAssignmentExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an assignment expression.
    /// </summary>
    internal sealed partial class LazyAssignmentExpression : AssignmentExpressionBase, IAssignmentExpression
    {
        private readonly Lazy<IOperation> _lazyTarget;
        private readonly Lazy<IOperation> _lazyValue;

        public LazyAssignmentExpression(Lazy<IOperation> target, Lazy<IOperation> value, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(OperationKind.AssignmentExpression, isInvalid, syntax, type, constantValue)
        {
            _lazyTarget = target ?? throw new System.ArgumentNullException("target");
            _lazyValue = value ?? throw new System.ArgumentNullException("value");
        }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public override IOperation Target => _lazyTarget.Value;

        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        public override IOperation Value => _lazyValue.Value;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitAssignmentExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAssignmentExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an await expression.
    /// </summary>
    internal abstract partial class AwaitExpressionBase : Operation, IAwaitExpression
    {
        protected AwaitExpressionBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.AwaitExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Value to be awaited.
        /// </summary>
        public abstract IOperation AwaitedValue { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitAwaitExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitAwaitExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an await expression.
    /// </summary>
    internal sealed partial class AwaitExpression : AwaitExpressionBase, IAwaitExpression
    {
        public AwaitExpression(IOperation awaitedValue, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            AwaitedValue = awaitedValue ?? throw new System.ArgumentNullException("awaitedValue");
        }
        /// <summary>
        /// Value to be awaited.
        /// </summary>
        public override IOperation AwaitedValue { get; }
    }

    /// <summary>
    /// Represents an await expression.
    /// </summary>
    internal sealed partial class LazyAwaitExpression : AwaitExpressionBase, IAwaitExpression
    {
        private readonly Lazy<IOperation> _lazyAwaitedValue;

        public LazyAwaitExpression(Lazy<IOperation> awaitedValue, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyAwaitedValue = awaitedValue ?? throw new System.ArgumentNullException("awaitedValue");
        }
        /// <summary>
        /// Value to be awaited.
        /// </summary>
        public override IOperation AwaitedValue => _lazyAwaitedValue.Value;
    }

    /// <summary>
    /// Represents an operation with two operands that produces a result with the same type as at least one of the operands.
    /// </summary>
    internal abstract partial class BinaryOperatorExpressionBase : Operation, IHasOperatorMethodExpression, IBinaryOperatorExpression
    {
        protected BinaryOperatorExpressionBase(BinaryOperationKind binaryOperationKind, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.BinaryOperatorExpression, isInvalid, syntax, type, constantValue)
        {
            BinaryOperationKind = binaryOperationKind;
            UsesOperatorMethod = usesOperatorMethod;
            OperatorMethod = operatorMethod ?? throw new System.ArgumentNullException("operatorMethod");
        }
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        public BinaryOperationKind BinaryOperationKind { get; }
        /// <summary>
        /// Left operand.
        /// </summary>
        public abstract IOperation LeftOperand { get; }
        /// <summary>
        /// Right operand.
        /// </summary>
        public abstract IOperation RightOperand { get; }
        /// <summary>
        /// True if and only if the operation is performed by an operator method.
        /// </summary>
        public bool UsesOperatorMethod { get; }
        /// <summary>
        /// Operation method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        public IMethodSymbol OperatorMethod { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitBinaryOperatorExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitBinaryOperatorExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an operation with two operands that produces a result with the same type as at least one of the operands.
    /// </summary>
    internal sealed partial class BinaryOperatorExpression : BinaryOperatorExpressionBase, IHasOperatorMethodExpression, IBinaryOperatorExpression
    {
        public BinaryOperatorExpression(BinaryOperationKind binaryOperationKind, IOperation leftOperand, IOperation rightOperand, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(binaryOperationKind, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        {
            LeftOperand = leftOperand ?? throw new System.ArgumentNullException("leftOperand");
            RightOperand = rightOperand ?? throw new System.ArgumentNullException("rightOperand");
        }
        /// <summary>
        /// Left operand.
        /// </summary>
        public override IOperation LeftOperand { get; }
        /// <summary>
        /// Right operand.
        /// </summary>
        public override IOperation RightOperand { get; }
    }

    /// <summary>
    /// Represents an operation with two operands that produces a result with the same type as at least one of the operands.
    /// </summary>
    internal sealed partial class LazyBinaryOperatorExpression : BinaryOperatorExpressionBase, IHasOperatorMethodExpression, IBinaryOperatorExpression
    {
        private readonly Lazy<IOperation> _lazyLeftOperand;
        private readonly Lazy<IOperation> _lazyRightOperand;

        public LazyBinaryOperatorExpression(BinaryOperationKind binaryOperationKind, Lazy<IOperation> leftOperand, Lazy<IOperation> rightOperand, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(binaryOperationKind, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        {
            _lazyLeftOperand = leftOperand ?? throw new System.ArgumentNullException("leftOperand");
            _lazyRightOperand = rightOperand ?? throw new System.ArgumentNullException("rightOperand");
        }
        /// <summary>
        /// Left operand.
        /// </summary>
        public override IOperation LeftOperand => _lazyLeftOperand.Value;

        /// <summary>
        /// Right operand.
        /// </summary>
        public override IOperation RightOperand => _lazyRightOperand.Value;
    }

    /// <summary>
    /// Represents a block scope.
    /// </summary>
    internal abstract partial class BlockStatementBase : Operation, IBlockStatement
    {
        protected BlockStatementBase(ImmutableArray<ILocalSymbol> locals, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.BlockStatement, isInvalid, syntax, type, constantValue)
        {
            Locals = locals;
        }
        /// <summary>
        /// Statements contained within the block.
        /// </summary>
        public abstract ImmutableArray<IOperation> Statements { get; }
        /// <summary>
        /// Local declarations contained within the block.
        /// </summary>
        public ImmutableArray<ILocalSymbol> Locals { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitBlockStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitBlockStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a block scope.
    /// </summary>
    internal sealed partial class BlockStatement : BlockStatementBase, IBlockStatement
    {
        public BlockStatement(ImmutableArray<IOperation> statements, ImmutableArray<ILocalSymbol> locals, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(locals, isInvalid, syntax, type, constantValue)
        {
            Statements = statements;
        }
        /// <summary>
        /// Statements contained within the block.
        /// </summary>
        public override ImmutableArray<IOperation> Statements { get; }
    }

    /// <summary>
    /// Represents a block scope.
    /// </summary>
    internal sealed partial class LazyBlockStatement : BlockStatementBase, IBlockStatement
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyStatements;

        public LazyBlockStatement(Lazy<ImmutableArray<IOperation>> statements, ImmutableArray<ILocalSymbol> locals, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(locals, isInvalid, syntax, type, constantValue)
        {
            _lazyStatements = statements;
        }
        /// <summary>
        /// Statements contained within the block.
        /// </summary>
        public override ImmutableArray<IOperation> Statements => _lazyStatements.Value;
    }

    /// <summary>
    /// Represents a C# goto, break, or continue statement, or a VB GoTo, Exit ***, or Continue *** statement
    /// </summary>
    internal sealed partial class BranchStatement : Operation, IBranchStatement
    {
        public BranchStatement(ILabelSymbol target, BranchKind branchKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.BranchStatement, isInvalid, syntax, type, constantValue)
        {
            Target = target ?? throw new System.ArgumentNullException("target");
            BranchKind = branchKind;
        }
        /// <summary>
        /// Label that is the target of the branch.
        /// </summary>
        public ILabelSymbol Target { get; }
        /// <summary>
        /// Kind of the branch.
        /// </summary>
        public BranchKind BranchKind { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitBranchStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitBranchStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a clause of a C# case or a VB Case.
    /// </summary>
    internal abstract partial class CaseClause : Operation, ICaseClause
    {
        protected CaseClause(CaseKind caseKind, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
            CaseKind = caseKind;
        }
        /// <summary>
        /// Kind of the clause.
        /// </summary>
        public CaseKind CaseKind { get; }
    }

    /// <summary>
    /// Represents a C# catch or VB Catch clause.
    /// </summary>
    internal abstract partial class CatchClauseBase : Operation, ICatchClause
    {
        protected CatchClauseBase(ITypeSymbol caughtType, ILocalSymbol exceptionLocal, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.CatchClause, isInvalid, syntax, type, constantValue)
        {
            CaughtType = caughtType ?? throw new System.ArgumentNullException("caughtType");
            ExceptionLocal = exceptionLocal ?? throw new System.ArgumentNullException("exceptionLocal");
        }
        /// <summary>
        /// Body of the exception handler.
        /// </summary>
        public abstract IBlockStatement Handler { get; }
        /// <summary>
        /// Type of exception to be handled.
        /// </summary>
        public ITypeSymbol CaughtType { get; }
        /// <summary>
        /// Filter expression to be executed to determine whether to handle the exception.
        /// </summary>
        public abstract IOperation Filter { get; }
        /// <summary>
        /// Symbol for the local catch variable bound to the caught exception.
        /// </summary>
        public ILocalSymbol ExceptionLocal { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitCatchClause(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitCatchClause(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# catch or VB Catch clause.
    /// </summary>
    internal sealed partial class CatchClause : CatchClauseBase, ICatchClause
    {
        public CatchClause(IBlockStatement handler, ITypeSymbol caughtType, IOperation filter, ILocalSymbol exceptionLocal, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(caughtType, exceptionLocal, isInvalid, syntax, type, constantValue)
        {
            Handler = handler ?? throw new System.ArgumentNullException("handler");
            Filter = filter ?? throw new System.ArgumentNullException("filter");
        }
        /// <summary>
        /// Body of the exception handler.
        /// </summary>
        public override IBlockStatement Handler { get; }
        /// <summary>
        /// Filter expression to be executed to determine whether to handle the exception.
        /// </summary>
        public override IOperation Filter { get; }
    }

    /// <summary>
    /// Represents a C# catch or VB Catch clause.
    /// </summary>
    internal sealed partial class LazyCatchClause : CatchClauseBase, ICatchClause
    {
        private readonly Lazy<IBlockStatement> _lazyHandler;
        private readonly Lazy<IOperation> _lazyFilter;

        public LazyCatchClause(Lazy<IBlockStatement> handler, ITypeSymbol caughtType, Lazy<IOperation> filter, ILocalSymbol exceptionLocal, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(caughtType, exceptionLocal, isInvalid, syntax, type, constantValue)
        {
            _lazyHandler = handler ?? throw new System.ArgumentNullException("handler");
            _lazyFilter = filter ?? throw new System.ArgumentNullException("filter");
        }
        /// <summary>
        /// Body of the exception handler.
        /// </summary>
        public override IBlockStatement Handler => _lazyHandler.Value;

        /// <summary>
        /// Filter expression to be executed to determine whether to handle the exception.
        /// </summary>
        public override IOperation Filter => _lazyFilter.Value;
    }

    /// <summary>
    /// Represents an assignment expression that includes a binary operation.
    /// </summary>
    internal abstract partial class CompoundAssignmentExpressionBase : AssignmentExpressionBase, IHasOperatorMethodExpression, ICompoundAssignmentExpression
    {
        protected CompoundAssignmentExpressionBase(BinaryOperationKind binaryOperationKind, bool usesOperatorMethod, IMethodSymbol operatorMethod, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
            BinaryOperationKind = binaryOperationKind;
            UsesOperatorMethod = usesOperatorMethod;
            OperatorMethod = operatorMethod ?? throw new System.ArgumentNullException("operatorMethod");
        }
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        public BinaryOperationKind BinaryOperationKind { get; }
        /// <summary>
        /// True if and only if the operation is performed by an operator method.
        /// </summary>
        public bool UsesOperatorMethod { get; }
        /// <summary>
        /// Operation method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        public IMethodSymbol OperatorMethod { get; }
    }

    /// <summary>
    /// Represents an assignment expression that includes a binary operation.
    /// </summary>
    internal sealed partial class CompoundAssignmentExpression : CompoundAssignmentExpressionBase, IHasOperatorMethodExpression, ICompoundAssignmentExpression
    {
        public CompoundAssignmentExpression(BinaryOperationKind binaryOperationKind, IOperation target, IOperation value, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(binaryOperationKind, usesOperatorMethod, operatorMethod, OperationKind.CompoundAssignmentExpression, isInvalid, syntax, type, constantValue)
        {
            Target = target ?? throw new System.ArgumentNullException("target");
            Value = value ?? throw new System.ArgumentNullException("value");
        }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public override IOperation Target { get; }
        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        public override IOperation Value { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitCompoundAssignmentExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitCompoundAssignmentExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an assignment expression that includes a binary operation.
    /// </summary>
    internal sealed partial class LazyCompoundAssignmentExpression : CompoundAssignmentExpressionBase, IHasOperatorMethodExpression, ICompoundAssignmentExpression
    {
        private readonly Lazy<IOperation> _lazyTarget;
        private readonly Lazy<IOperation> _lazyValue;

        public LazyCompoundAssignmentExpression(BinaryOperationKind binaryOperationKind, Lazy<IOperation> target, Lazy<IOperation> value, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(binaryOperationKind, usesOperatorMethod, operatorMethod, OperationKind.CompoundAssignmentExpression, isInvalid, syntax, type, constantValue)
        {
            _lazyTarget = target ?? throw new System.ArgumentNullException("target");
            _lazyValue = value ?? throw new System.ArgumentNullException("value");
        }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public override IOperation Target => _lazyTarget.Value;

        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        public override IOperation Value => _lazyValue.Value;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitCompoundAssignmentExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitCompoundAssignmentExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an expression that includes a ? or ?. conditional access instance expression.
    /// </summary>
    internal abstract partial class ConditionalAccessExpressionBase : Operation, IConditionalAccessExpression
    {
        protected ConditionalAccessExpressionBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ConditionalAccessExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Expression to be evaluated if the conditional instance is non null.
        /// </summary>
        public abstract IOperation ConditionalValue { get; }
        /// <summary>
        /// Expresson that is conditionally accessed.
        /// </summary>
        public abstract IOperation ConditionalInstance { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConditionalAccessExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConditionalAccessExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an expression that includes a ? or ?. conditional access instance expression.
    /// </summary>
    internal sealed partial class ConditionalAccessExpression : ConditionalAccessExpressionBase, IConditionalAccessExpression
    {
        public ConditionalAccessExpression(IOperation conditionalValue, IOperation conditionalInstance, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            ConditionalValue = conditionalValue ?? throw new System.ArgumentNullException("conditionalValue");
            ConditionalInstance = conditionalInstance ?? throw new System.ArgumentNullException("conditionalInstance");
        }
        /// <summary>
        /// Expression to be evaluated if the conditional instance is non null.
        /// </summary>
        public override IOperation ConditionalValue { get; }
        /// <summary>
        /// Expresson that is conditionally accessed.
        /// </summary>
        public override IOperation ConditionalInstance { get; }
    }

    /// <summary>
    /// Represents an expression that includes a ? or ?. conditional access instance expression.
    /// </summary>
    internal sealed partial class LazyConditionalAccessExpression : ConditionalAccessExpressionBase, IConditionalAccessExpression
    {
        private readonly Lazy<IOperation> _lazyConditionalValue;
        private readonly Lazy<IOperation> _lazyConditionalInstance;

        public LazyConditionalAccessExpression(Lazy<IOperation> conditionalValue, Lazy<IOperation> conditionalInstance, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyConditionalValue = conditionalValue ?? throw new System.ArgumentNullException("conditionalValue");
            _lazyConditionalInstance = conditionalInstance ?? throw new System.ArgumentNullException("conditionalInstance");
        }
        /// <summary>
        /// Expression to be evaluated if the conditional instance is non null.
        /// </summary>
        public override IOperation ConditionalValue => _lazyConditionalValue.Value;

        /// <summary>
        /// Expresson that is conditionally accessed.
        /// </summary>
        public override IOperation ConditionalInstance => _lazyConditionalInstance.Value;
    }

    /// <summary>
    /// Represents the value of a conditionally-accessed expression within an expression containing a conditional access.
    /// </summary>
    internal sealed partial class ConditionalAccessInstanceExpression : Operation, IConditionalAccessInstanceExpression
    {
        public ConditionalAccessInstanceExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.ConditionalAccessInstanceExpression, isInvalid, syntax, type, constantValue)
        {
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConditionalAccessInstanceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConditionalAccessInstanceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# ?: or VB If expression.
    /// </summary>
    internal abstract partial class ConditionalChoiceExpressionBase : Operation, IConditionalChoiceExpression
    {
        protected ConditionalChoiceExpressionBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ConditionalChoiceExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Condition to be tested.
        /// </summary>
        public abstract IOperation Condition { get; }
        /// <summary>
        /// Value evaluated if the Condition is true.
        /// </summary>
        public abstract IOperation IfTrueValue { get; }
        /// <summary>
        /// Value evaluated if the Condition is false.
        /// </summary>
        public abstract IOperation IfFalseValue { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConditionalChoiceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConditionalChoiceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# ?: or VB If expression.
    /// </summary>
    internal sealed partial class ConditionalChoiceExpression : ConditionalChoiceExpressionBase, IConditionalChoiceExpression
    {
        public ConditionalChoiceExpression(IOperation condition, IOperation ifTrueValue, IOperation ifFalseValue, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Condition = condition ?? throw new System.ArgumentNullException("condition");
            IfTrueValue = ifTrueValue ?? throw new System.ArgumentNullException("ifTrueValue");
            IfFalseValue = ifFalseValue ?? throw new System.ArgumentNullException("ifFalseValue");
        }
        /// <summary>
        /// Condition to be tested.
        /// </summary>
        public override IOperation Condition { get; }
        /// <summary>
        /// Value evaluated if the Condition is true.
        /// </summary>
        public override IOperation IfTrueValue { get; }
        /// <summary>
        /// Value evaluated if the Condition is false.
        /// </summary>
        public override IOperation IfFalseValue { get; }
    }

    /// <summary>
    /// Represents a C# ?: or VB If expression.
    /// </summary>
    internal sealed partial class LazyConditionalChoiceExpression : ConditionalChoiceExpressionBase, IConditionalChoiceExpression
    {
        private readonly Lazy<IOperation> _lazyCondition;
        private readonly Lazy<IOperation> _lazyIfTrueValue;
        private readonly Lazy<IOperation> _lazyIfFalseValue;

        public LazyConditionalChoiceExpression(Lazy<IOperation> condition, Lazy<IOperation> ifTrueValue, Lazy<IOperation> ifFalseValue, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyCondition = condition ?? throw new System.ArgumentNullException("condition");
            _lazyIfTrueValue = ifTrueValue ?? throw new System.ArgumentNullException("ifTrueValue");
            _lazyIfFalseValue = ifFalseValue ?? throw new System.ArgumentNullException("ifFalseValue");
        }
        /// <summary>
        /// Condition to be tested.
        /// </summary>
        public override IOperation Condition => _lazyCondition.Value;

        /// <summary>
        /// Value evaluated if the Condition is true.
        /// </summary>
        public override IOperation IfTrueValue => _lazyIfTrueValue.Value;

        /// <summary>
        /// Value evaluated if the Condition is false.
        /// </summary>
        public override IOperation IfFalseValue => _lazyIfFalseValue.Value;
    }

    /// <summary>
    /// Represents a conversion operation.
    /// </summary>
    internal abstract partial class ConversionExpressionBase : Operation, IHasOperatorMethodExpression, IConversionExpression
    {
        protected ConversionExpressionBase(ConversionKind conversionKind, bool isExplicit, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ConversionExpression, isInvalid, syntax, type, constantValue)
        {
            ConversionKind = conversionKind;
            IsExplicit = isExplicit;
            UsesOperatorMethod = usesOperatorMethod;
            OperatorMethod = operatorMethod ?? throw new System.ArgumentNullException("operatorMethod");
        }
        /// <summary>
        /// Value to be converted.
        /// </summary>
        public abstract IOperation Operand { get; }
        /// <summary>
        /// Kind of conversion.
        /// </summary>
        public ConversionKind ConversionKind { get; }
        /// <summary>
        /// True if and only if the conversion is indicated explicity by a cast operation in the source code.
        /// </summary>
        public bool IsExplicit { get; }
        /// <summary>
        /// True if and only if the operation is performed by an operator method.
        /// </summary>
        public bool UsesOperatorMethod { get; }
        /// <summary>
        /// Operation method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        public IMethodSymbol OperatorMethod { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitConversionExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitConversionExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a conversion operation.
    /// </summary>
    internal sealed partial class ConversionExpression : ConversionExpressionBase, IHasOperatorMethodExpression, IConversionExpression
    {
        public ConversionExpression(IOperation operand, ConversionKind conversionKind, bool isExplicit, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(conversionKind, isExplicit, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        {
            Operand = operand ?? throw new System.ArgumentNullException("operand");
        }
        /// <summary>
        /// Value to be converted.
        /// </summary>
        public override IOperation Operand { get; }
    }

    /// <summary>
    /// Represents a conversion operation.
    /// </summary>
    internal sealed partial class LazyConversionExpression : ConversionExpressionBase, IHasOperatorMethodExpression, IConversionExpression
    {
        private readonly Lazy<IOperation> _lazyOperand;

        public LazyConversionExpression(Lazy<IOperation> operand, ConversionKind conversionKind, bool isExplicit, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(conversionKind, isExplicit, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        {
            _lazyOperand = operand ?? throw new System.ArgumentNullException("operand");
        }
        /// <summary>
        /// Value to be converted.
        /// </summary>
        public override IOperation Operand => _lazyOperand.Value;
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal sealed partial class DefaultValueExpression : Operation, IDefaultValueExpression
    {
        public DefaultValueExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.DefaultValueExpression, isInvalid, syntax, type, constantValue)
        {
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitDefaultValueExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDefaultValueExpression(this, argument);
        }
    }

    /// <summary>
    /// Reprsents an empty statement.
    /// </summary>
    internal sealed partial class EmptyStatement : Operation, IEmptyStatement
    {
        public EmptyStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.EmptyStatement, isInvalid, syntax, type, constantValue)
        {
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitEmptyStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitEmptyStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a VB End statemnt.
    /// </summary>
    internal sealed partial class EndStatement : Operation, IEndStatement
    {
        public EndStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.EndStatement, isInvalid, syntax, type, constantValue)
        {
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitEndStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitEndStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a binding of an event.
    /// </summary>
    internal abstract partial class EventAssignmentExpressionBase : Operation, IEventAssignmentExpression
    {
        protected EventAssignmentExpressionBase(IEventSymbol @event, bool adds, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.EventAssignmentExpression, isInvalid, syntax, type, constantValue)
        {
            Event = @event ?? throw new System.ArgumentNullException("@event");
            Adds = adds;
        }
        /// <summary>
        /// Event being bound.
        /// </summary>
        public IEventSymbol Event { get; }

        /// <summary>
        /// Instance used to refer to the event being bound.
        /// </summary>
        public abstract IOperation EventInstance { get; }

        /// <summary>
        /// Handler supplied for the event.
        /// </summary>
        public abstract IOperation HandlerValue { get; }

        /// <summary>
        /// True for adding a binding, false for removing one.
        /// </summary>
        public bool Adds { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitEventAssignmentExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitEventAssignmentExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a binding of an event.
    /// </summary>
    internal sealed partial class EventAssignmentExpression : EventAssignmentExpressionBase, IEventAssignmentExpression
    {
        public EventAssignmentExpression(IEventSymbol @event, IOperation eventInstance, IOperation handlerValue, bool adds, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(@event, adds, isInvalid, syntax, type, constantValue)
        {
            EventInstance = eventInstance ?? throw new System.ArgumentNullException("eventInstance");
            HandlerValue = handlerValue ?? throw new System.ArgumentNullException("handlerValue");
        }

        /// <summary>
        /// Instance used to refer to the event being bound.
        /// </summary>
        public override IOperation EventInstance { get; }

        /// <summary>
        /// Handler supplied for the event.
        /// </summary>
        public override IOperation HandlerValue { get; }
    }

    /// <summary>
    /// Represents a binding of an event.
    /// </summary>
    internal sealed partial class LazyEventAssignmentExpression : EventAssignmentExpressionBase, IEventAssignmentExpression
    {
        private readonly Lazy<IOperation> _lazyEventInstance;
        private readonly Lazy<IOperation> _lazyHandlerValue;

        public LazyEventAssignmentExpression(IEventSymbol @event, Lazy<IOperation> eventInstance, Lazy<IOperation> handlerValue, bool adds, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(@event, adds, isInvalid, syntax, type, constantValue)
        {
            _lazyEventInstance = eventInstance ?? throw new System.ArgumentNullException("eventInstance");
            _lazyHandlerValue = handlerValue ?? throw new System.ArgumentNullException("handlerValue");
        }

        /// <summary>
        /// Instance used to refer to the event being bound.
        /// </summary>
        public override IOperation EventInstance => _lazyEventInstance.Value;

        /// <summary>
        /// Handler supplied for the event.
        /// </summary>
        public override IOperation HandlerValue => _lazyHandlerValue.Value;
    }

    /// <summary>
    /// Represents a reference to an event.
    /// </summary>
    internal sealed partial class EventReferenceExpression : MemberReferenceExpression, IEventReferenceExpression
    {
        public EventReferenceExpression(IEventSymbol @event, IOperation instance, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(member, OperationKind.EventReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            Event = @event ?? throw new System.ArgumentNullException("@event");
            Instance = instance ?? throw new System.ArgumentNullException("instance");
        }
        /// <summary>
        /// Referenced event.
        /// </summary>
        public IEventSymbol Event { get; }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitEventReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitEventReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to an event.
    /// </summary>
    internal sealed partial class LazyEventReferenceExpression : MemberReferenceExpression, IEventReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;

        public LazyEventReferenceExpression(IEventSymbol @event, Lazy<IOperation> instance, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(member, OperationKind.EventReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            Event = @event ?? throw new System.ArgumentNullException("@event");
            _lazyInstance = instance ?? throw new System.ArgumentNullException("instance");
        }
        /// <summary>
        /// Referenced event.
        /// </summary>
        public IEventSymbol Event { get; }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance => _lazyInstance.Value;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitEventReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitEventReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# or VB statement that consists solely of an expression.
    /// </summary>
    internal abstract partial class ExpressionStatementBase : Operation, IExpressionStatement
    {
        protected ExpressionStatementBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ExpressionStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Expression of the statement.
        /// </summary>
        public abstract IOperation Expression { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitExpressionStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitExpressionStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# or VB statement that consists solely of an expression.
    /// </summary>
    internal sealed partial class ExpressionStatement : ExpressionStatementBase, IExpressionStatement
    {
        public ExpressionStatement(IOperation expression, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Expression = expression ?? throw new System.ArgumentNullException("expression");
        }
        /// <summary>
        /// Expression of the statement.
        /// </summary>
        public override IOperation Expression { get; }
    }

    /// <summary>
    /// Represents a C# or VB statement that consists solely of an expression.
    /// </summary>
    internal sealed partial class LazyExpressionStatement : ExpressionStatementBase, IExpressionStatement
    {
        private readonly Lazy<IOperation> _lazyExpression;

        public LazyExpressionStatement(Lazy<IOperation> expression, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyExpression = expression ?? throw new System.ArgumentNullException("expression");
        }
        /// <summary>
        /// Expression of the statement.
        /// </summary>
        public override IOperation Expression => _lazyExpression.Value;
    }

    /// <summary>
    /// Represents an initialization of a field.
    /// </summary>
    internal sealed partial class FieldInitializer : SymbolInitializer, IFieldInitializer
    {
        public FieldInitializer(ImmutableArray<IFieldSymbol> initializedFields, IOperation value, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
            InitializedFields = initializedFields;
            Value = value ?? throw new System.ArgumentNullException("value");
        }
        /// <summary>
        /// Initialized fields. There can be multiple fields for Visual Basic fields declared with As New.
        /// </summary>
        public ImmutableArray<IFieldSymbol> InitializedFields { get; }
        public override IOperation Value { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFieldInitializer(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitFieldInitializer(this, argument);
        }
    }

    /// <summary>
    /// Represents an initialization of a field.
    /// </summary>
    internal sealed partial class LazyFieldInitializer : SymbolInitializer, IFieldInitializer
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazyFieldInitializer(ImmutableArray<IFieldSymbol> initializedFields, Lazy<IOperation> value, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(kind, isInvalid, syntax, type, constantValue)
        {
            InitializedFields = initializedFields;
            _lazyValue = value ?? throw new System.ArgumentNullException("value");
        }
        /// <summary>
        /// Initialized fields. There can be multiple fields for Visual Basic fields declared with As New.
        /// </summary>
        public ImmutableArray<IFieldSymbol> InitializedFields { get; }
        public override IOperation Value => _lazyValue.Value;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFieldInitializer(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitFieldInitializer(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a field.
    /// </summary>
    internal sealed partial class FieldReferenceExpression : MemberReferenceExpression, IFieldReferenceExpression
    {
        public FieldReferenceExpression(IFieldSymbol field, IOperation instance, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(member, OperationKind.FieldReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            Field = field ?? throw new System.ArgumentNullException("field");
            Instance = instance ?? throw new System.ArgumentNullException("instance");
        }
        /// <summary>
        /// Referenced field.
        /// </summary>
        public IFieldSymbol Field { get; }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFieldReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitFieldReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a field.
    /// </summary>
    internal sealed partial class LazyFieldReferenceExpression : MemberReferenceExpression, IFieldReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;

        public LazyFieldReferenceExpression(IFieldSymbol field, Lazy<IOperation> instance, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(member, OperationKind.FieldReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            Field = field ?? throw new System.ArgumentNullException("field");
            _lazyInstance = instance ?? throw new System.ArgumentNullException("instance");
        }
        /// <summary>
        /// Referenced field.
        /// </summary>
        public IFieldSymbol Field { get; }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance => _lazyInstance.Value;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFieldReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitFieldReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# fixed staement.
    /// </summary>
    internal abstract partial class FixedStatementBase : Operation, IFixedStatement
    {
        protected FixedStatementBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.FixedStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Variables to be fixed.
        /// </summary>
        public abstract IVariableDeclarationStatement Variables { get; }
        /// <summary>
        /// Body of the fixed, over which the variables are fixed.
        /// </summary>
        public abstract IOperation Body { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitFixedStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitFixedStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# fixed staement.
    /// </summary>
    internal sealed partial class FixedStatement : FixedStatementBase, IFixedStatement
    {
        public FixedStatement(IVariableDeclarationStatement variables, IOperation body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Variables = variables ?? throw new System.ArgumentNullException("variables");
            Body = body ?? throw new System.ArgumentNullException("body");
        }
        /// <summary>
        /// Variables to be fixed.
        /// </summary>
        public override IVariableDeclarationStatement Variables { get; }
        /// <summary>
        /// Body of the fixed, over which the variables are fixed.
        /// </summary>
        public override IOperation Body { get; }
    }

    /// <summary>
    /// Represents a C# fixed staement.
    /// </summary>
    internal sealed partial class LazyFixedStatement : FixedStatementBase, IFixedStatement
    {
        private readonly Lazy<IVariableDeclarationStatement> _lazyVariables;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyFixedStatement(Lazy<IVariableDeclarationStatement> variables, Lazy<IOperation> body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyVariables = variables ?? throw new System.ArgumentNullException("variables");
            _lazyBody = body ?? throw new System.ArgumentNullException("body");
        }
        /// <summary>
        /// Variables to be fixed.
        /// </summary>
        public override IVariableDeclarationStatement Variables => _lazyVariables.Value;

        /// <summary>
        /// Body of the fixed, over which the variables are fixed.
        /// </summary>
        public override IOperation Body => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# foreach statement or a VB For Each staement.
    /// </summary>
    internal sealed partial class ForEachLoopStatement : LoopStatement, IForEachLoopStatement
    {
        public ForEachLoopStatement(ILocalSymbol iterationVariable, IOperation collection, LoopKind loopKind, IOperation body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(loopKind, OperationKind.LoopStatement, isInvalid, syntax, type, constantValue)
        {
            IterationVariable = iterationVariable ?? throw new System.ArgumentNullException("iterationVariable");
            Collection = collection ?? throw new System.ArgumentNullException("collection");
            Body = body ?? throw new System.ArgumentNullException("body");
        }
        /// <summary>
        /// Iteration variable of the loop.
        /// </summary>
        public ILocalSymbol IterationVariable { get; }
        /// <summary>
        /// Collection value over which the loop iterates.
        /// </summary>
        public IOperation Collection { get; }
        /// <summary>
        /// Body of the loop.
        /// </summary>
        public override IOperation Body { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitForEachLoopStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitForEachLoopStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# foreach statement or a VB For Each staement.
    /// </summary>
    internal sealed partial class LazyForEachLoopStatement : LoopStatement, IForEachLoopStatement
    {
        private readonly Lazy<IOperation> _lazyCollection;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyForEachLoopStatement(ILocalSymbol iterationVariable, Lazy<IOperation> collection, LoopKind loopKind, Lazy<IOperation> body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(loopKind, OperationKind.LoopStatement, isInvalid, syntax, type, constantValue)
        {
            IterationVariable = iterationVariable ?? throw new System.ArgumentNullException("iterationVariable");
            _lazyCollection = collection ?? throw new System.ArgumentNullException("collection");
            _lazyBody = body ?? throw new System.ArgumentNullException("body");
        }
        /// <summary>
        /// Iteration variable of the loop.
        /// </summary>
        public ILocalSymbol IterationVariable { get; }
        /// <summary>
        /// Collection value over which the loop iterates.
        /// </summary>
        public IOperation Collection => _lazyCollection.Value;

        /// <summary>
        /// Body of the loop.
        /// </summary>
        public override IOperation Body => _lazyBody.Value;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitForEachLoopStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitForEachLoopStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# for statement or a VB For statement.
    /// </summary>
    internal sealed partial class ForLoopStatement : ForWhileUntilLoopStatement, IForLoopStatement
    {
        public ForLoopStatement(ImmutableArray<IOperation> before, ImmutableArray<IOperation> atLoopBottom, ImmutableArray<ILocalSymbol> locals, IOperation condition, LoopKind loopKind, IOperation body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(loopKind, OperationKind.LoopStatement, isInvalid, syntax, type, constantValue)
        {
            Before = before;
            AtLoopBottom = atLoopBottom;
            Locals = locals;
            Condition = condition ?? throw new System.ArgumentNullException("condition");
            Body = body ?? throw new System.ArgumentNullException("body");
        }
        /// <summary>
        /// Statements to execute before entry to the loop. For C# these come from the first clause of the for statement. For VB these initialize the index variable of the For statement.
        /// </summary>
        public ImmutableArray<IOperation> Before { get; }
        /// <summary>
        /// Statements to execute at the bottom of the loop. For C# these come from the third clause of the for statement. For VB these increment the index variable of the For statement.
        /// </summary>
        public ImmutableArray<IOperation> AtLoopBottom { get; }
        /// <summary>
        /// Declarations local to the loop.
        /// </summary>
        public ImmutableArray<ILocalSymbol> Locals { get; }
        /// <summary>
        /// Condition of the loop.
        /// </summary>
        public override IOperation Condition { get; }
        /// <summary>
        /// Body of the loop.
        /// </summary>
        public override IOperation Body { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitForLoopStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitForLoopStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# for statement or a VB For statement.
    /// </summary>
    internal sealed partial class LazyForLoopStatement : ForWhileUntilLoopStatement, IForLoopStatement
    {
        private readonly Lazy<ImmutableArray<IOperation>> _lazyBefore;
        private readonly Lazy<ImmutableArray<IOperation>> _lazyAtLoopBottom;
        private readonly Lazy<IOperation> _lazyCondition;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyForLoopStatement(Lazy<ImmutableArray<IOperation>> before, Lazy<ImmutableArray<IOperation>> atLoopBottom, ImmutableArray<ILocalSymbol> locals, Lazy<IOperation> condition, LoopKind loopKind, Lazy<IOperation> body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(loopKind, OperationKind.LoopStatement, isInvalid, syntax, type, constantValue)
        {
            _lazyBefore = before;
            _lazyAtLoopBottom = atLoopBottom;
            Locals = locals;
            _lazyCondition = condition ?? throw new System.ArgumentNullException("condition");
            _lazyBody = body ?? throw new System.ArgumentNullException("body");
        }
        /// <summary>
        /// Statements to execute before entry to the loop. For C# these come from the first clause of the for statement. For VB these initialize the index variable of the For statement.
        /// </summary>
        public ImmutableArray<IOperation> Before => _lazyBefore.Value;

        /// <summary>
        /// Statements to execute at the bottom of the loop. For C# these come from the third clause of the for statement. For VB these increment the index variable of the For statement.
        /// </summary>
        public ImmutableArray<IOperation> AtLoopBottom => _lazyAtLoopBottom.Value;

        /// <summary>
        /// Declarations local to the loop.
        /// </summary>
        public ImmutableArray<ILocalSymbol> Locals { get; }
        /// <summary>
        /// Condition of the loop.
        /// </summary>
        public override IOperation Condition => _lazyCondition.Value;

        /// <summary>
        /// Body of the loop.
        /// </summary>
        public override IOperation Body => _lazyBody.Value;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitForLoopStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitForLoopStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# while, for, or do statement, or a VB While, For, or Do statement.
    /// </summary>
    internal abstract partial class ForWhileUntilLoopStatement : LoopStatement, IForWhileUntilLoopStatement
    {
        protected ForWhileUntilLoopStatement(LoopKind loopKind, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(loopKind, kind, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Condition of the loop.
        /// </summary>
        public abstract IOperation Condition { get; }
    }

    /// <summary>
    /// Represents an if statement in C# or an If statement in VB.
    /// </summary>
    internal abstract partial class IfStatementBase : Operation, IIfStatement
    {
        protected IfStatementBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.IfStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Condition of the if statement. For C# there is naturally one clause per if, but for VB If statements with multiple clauses are rewritten to have only one.
        /// </summary>
        public abstract IOperation Condition { get; }
        /// <summary>
        /// Statement executed if the condition is true.
        /// </summary>
        public abstract IOperation IfTrueStatement { get; }
        /// <summary>
        /// Statement executed if the condition is false.
        /// </summary>
        public abstract IOperation IfFalseStatement { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIfStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIfStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents an if statement in C# or an If statement in VB.
    /// </summary>
    internal sealed partial class IfStatement : IfStatementBase, IIfStatement
    {
        public IfStatement(IOperation condition, IOperation ifTrueStatement, IOperation ifFalseStatement, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Condition = condition ?? throw new System.ArgumentNullException("condition");
            IfTrueStatement = ifTrueStatement ?? throw new System.ArgumentNullException("ifTrueStatement");
            IfFalseStatement = ifFalseStatement ?? throw new System.ArgumentNullException("ifFalseStatement");
        }
        /// <summary>
        /// Condition of the if statement. For C# there is naturally one clause per if, but for VB If statements with multiple clauses are rewritten to have only one.
        /// </summary>
        public override IOperation Condition { get; }
        /// <summary>
        /// Statement executed if the condition is true.
        /// </summary>
        public override IOperation IfTrueStatement { get; }
        /// <summary>
        /// Statement executed if the condition is false.
        /// </summary>
        public override IOperation IfFalseStatement { get; }
    }

    /// <summary>
    /// Represents an if statement in C# or an If statement in VB.
    /// </summary>
    internal sealed partial class LazyIfStatement : IfStatementBase, IIfStatement
    {
        private readonly Lazy<IOperation> _lazyCondition;
        private readonly Lazy<IOperation> _lazyIfTrueStatement;
        private readonly Lazy<IOperation> _lazyIfFalseStatement;

        public LazyIfStatement(Lazy<IOperation> condition, Lazy<IOperation> ifTrueStatement, Lazy<IOperation> ifFalseStatement, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyCondition = condition ?? throw new System.ArgumentNullException("condition");
            _lazyIfTrueStatement = ifTrueStatement ?? throw new System.ArgumentNullException("ifTrueStatement");
            _lazyIfFalseStatement = ifFalseStatement ?? throw new System.ArgumentNullException("ifFalseStatement");
        }
        /// <summary>
        /// Condition of the if statement. For C# there is naturally one clause per if, but for VB If statements with multiple clauses are rewritten to have only one.
        /// </summary>
        public override IOperation Condition => _lazyCondition.Value;

        /// <summary>
        /// Statement executed if the condition is true.
        /// </summary>
        public override IOperation IfTrueStatement => _lazyIfTrueStatement.Value;

        /// <summary>
        /// Statement executed if the condition is false.
        /// </summary>
        public override IOperation IfFalseStatement => _lazyIfFalseStatement.Value;
    }

    /// <summary>
    /// Represents an increment expression.
    /// </summary>
    internal sealed partial class IncrementExpression : CompoundAssignmentExpressionBase, IIncrementExpression
    {
        public IncrementExpression(UnaryOperationKind incrementOperationKind, BinaryOperationKind binaryOperationKind, IOperation target, IOperation value, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(binaryOperationKind, usesOperatorMethod, operatorMethod, OperationKind.IncrementExpression, isInvalid, syntax, type, constantValue)
        {
            IncrementOperationKind = incrementOperationKind;
            Target = target ?? throw new System.ArgumentNullException("target");
            Value = value ?? throw new System.ArgumentNullException("value");
        }
        /// <summary>
        /// Kind of increment.
        /// </summary>
        public UnaryOperationKind IncrementOperationKind { get; }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public override IOperation Target { get; }
        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        public override IOperation Value { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIncrementExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIncrementExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an increment expression.
    /// </summary>
    internal sealed partial class LazyIncrementExpression : CompoundAssignmentExpressionBase, IIncrementExpression
    {
        private readonly Lazy<IOperation> _lazyTarget;
        private readonly Lazy<IOperation> _lazyValue;

        public LazyIncrementExpression(UnaryOperationKind incrementOperationKind, BinaryOperationKind binaryOperationKind, Lazy<IOperation> target, Lazy<IOperation> value, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(binaryOperationKind, usesOperatorMethod, operatorMethod, OperationKind.IncrementExpression, isInvalid, syntax, type, constantValue)
        {
            IncrementOperationKind = incrementOperationKind;
            _lazyTarget = target ?? throw new System.ArgumentNullException("target");
            _lazyValue = value ?? throw new System.ArgumentNullException("value");
        }
        /// <summary>
        /// Kind of increment.
        /// </summary>
        public UnaryOperationKind IncrementOperationKind { get; }
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        public override IOperation Target => _lazyTarget.Value;

        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        public override IOperation Value => _lazyValue.Value;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIncrementExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIncrementExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to an indexed property.
    /// </summary>
    internal sealed partial class IndexedPropertyReferenceExpression : PropertyReferenceExpressionBase, IHasArgumentsExpression, IIndexedPropertyReferenceExpression
    {
        public IndexedPropertyReferenceExpression(IPropertySymbol property, IOperation instance, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(property, member, OperationKind.IndexedPropertyReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            Instance = instance ?? throw new System.ArgumentNullException("instance");
        }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIndexedPropertyReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIndexedPropertyReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to an indexed property.
    /// </summary>
    internal sealed partial class LazyIndexedPropertyReferenceExpression : PropertyReferenceExpressionBase, IHasArgumentsExpression, IIndexedPropertyReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;

        public LazyIndexedPropertyReferenceExpression(IPropertySymbol property, Lazy<IOperation> instance, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(property, member, OperationKind.IndexedPropertyReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException("instance");
        }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance => _lazyInstance.Value;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIndexedPropertyReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIndexedPropertyReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# this or base expression, or a VB Me, MyClass, or MyBase expression.
    /// </summary>
    internal sealed partial class InstanceReferenceExpression : Operation, IInstanceReferenceExpression
    {
        public InstanceReferenceExpression(InstanceReferenceKind instanceReferenceKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.InstanceReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            InstanceReferenceKind = instanceReferenceKind;
        }
        ///
        /// <summary>
        /// Kind of instance reference.
        /// </summary>
        public InstanceReferenceKind InstanceReferenceKind { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInstanceReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInstanceReferenceExpression(this, argument);
        }
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal sealed partial class InvalidExpression : Operation, IInvalidExpression
    {
        public InvalidExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.InvalidExpression, isInvalid, syntax, type, constantValue)
        {
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInvalidExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInvalidExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a syntactically or semantically invalid C# or VB statement.
    /// </summary>
    internal sealed partial class InvalidStatement : Operation, IInvalidStatement
    {
        public InvalidStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.InvalidStatement, isInvalid, syntax, type, constantValue)
        {
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInvalidStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInvalidStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# or VB method invocation.
    /// </summary>
    internal abstract partial class InvocationExpressionBase : Operation, IHasArgumentsExpression, IInvocationExpression
    {
        protected InvocationExpressionBase(IMethodSymbol targetMethod, bool isVirtual, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.InvocationExpression, isInvalid, syntax, type, constantValue)
        {
            TargetMethod = targetMethod ?? throw new System.ArgumentNullException("targetMethod");
            IsVirtual = isVirtual;
        }
        /// <summary>
        /// Method to be invoked.
        /// </summary>
        public IMethodSymbol TargetMethod { get; }
        /// <summary>
        /// 'This' or 'Me' instance to be supplied to the method, or null if the method is static.
        /// </summary>
        public abstract IOperation Instance { get; }
        /// <summary>
        /// True if the invocation uses a virtual mechanism, and false otherwise.
        /// </summary>
        public bool IsVirtual { get; }
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in the order specified in source,
        /// and params/ParamArray arguments have been collected into arrays. Arguments are not present
        /// unless supplied in source.
        /// </summary>
        public abstract ImmutableArray<IArgument> ArgumentsInSourceOrder { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitInvocationExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitInvocationExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# or VB method invocation.
    /// </summary>
    internal sealed partial class InvocationExpression : InvocationExpressionBase, IHasArgumentsExpression, IInvocationExpression
    {
        public InvocationExpression(IMethodSymbol targetMethod, IOperation instance, bool isVirtual, ImmutableArray<IArgument> argumentsInSourceOrder, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(targetMethod, isVirtual, isInvalid, syntax, type, constantValue)
        {
            Instance = instance ?? throw new System.ArgumentNullException("instance");
            ArgumentsInSourceOrder = argumentsInSourceOrder;
        }
        /// <summary>
        /// 'This' or 'Me' instance to be supplied to the method, or null if the method is static.
        /// </summary>
        public override IOperation Instance { get; }
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in the order specified in source,
        /// and params/ParamArray arguments have been collected into arrays. Arguments are not present
        /// unless supplied in source.
        /// </summary>
        public override ImmutableArray<IArgument> ArgumentsInSourceOrder { get; }
    }

    /// <summary>
    /// Represents a C# or VB method invocation.
    /// </summary>
    internal sealed partial class LazyInvocationExpression : InvocationExpressionBase, IHasArgumentsExpression, IInvocationExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;
        private readonly Lazy<ImmutableArray<IArgument>> _lazyArgumentsInSourceOrder;

        public LazyInvocationExpression(IMethodSymbol targetMethod, Lazy<IOperation> instance, bool isVirtual, Lazy<ImmutableArray<IArgument>> argumentsInSourceOrder, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(targetMethod, isVirtual, isInvalid, syntax, type, constantValue)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException("instance");
            _lazyArgumentsInSourceOrder = argumentsInSourceOrder;
        }
        /// <summary>
        /// 'This' or 'Me' instance to be supplied to the method, or null if the method is static.
        /// </summary>
        public override IOperation Instance => _lazyInstance.Value;

        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in the order specified in source,
        /// and params/ParamArray arguments have been collected into arrays. Arguments are not present
        /// unless supplied in source.
        /// </summary>
        public override ImmutableArray<IArgument> ArgumentsInSourceOrder => _lazyArgumentsInSourceOrder.Value;
    }

    /// <summary>
    /// Represents an expression that tests if a value is of a specific type.
    /// </summary>
    internal abstract partial class IsTypeExpressionBase : Operation, IIsTypeExpression
    {
        protected IsTypeExpressionBase(ITypeSymbol isType, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.IsTypeExpression, isInvalid, syntax, type, constantValue)
        {
            IsType = isType ?? throw new System.ArgumentNullException("isType");
        }
        /// <summary>
        /// Value to test.
        /// </summary>
        public abstract IOperation Operand { get; }
        /// <summary>
        /// Type for which to test.
        /// </summary>
        public ITypeSymbol IsType { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitIsTypeExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitIsTypeExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an expression that tests if a value is of a specific type.
    /// </summary>
    internal sealed partial class IsTypeExpression : IsTypeExpressionBase, IIsTypeExpression
    {
        public IsTypeExpression(IOperation operand, ITypeSymbol isType, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isType, isInvalid, syntax, type, constantValue)
        {
            Operand = operand ?? throw new System.ArgumentNullException("operand");
        }
        /// <summary>
        /// Value to test.
        /// </summary>
        public override IOperation Operand { get; }
    }

    /// <summary>
    /// Represents an expression that tests if a value is of a specific type.
    /// </summary>
    internal sealed partial class LazyIsTypeExpression : IsTypeExpressionBase, IIsTypeExpression
    {
        private readonly Lazy<IOperation> _lazyOperand;

        public LazyIsTypeExpression(Lazy<IOperation> operand, ITypeSymbol isType, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isType, isInvalid, syntax, type, constantValue)
        {
            _lazyOperand = operand ?? throw new System.ArgumentNullException("operand");
        }
        /// <summary>
        /// Value to test.
        /// </summary>
        public override IOperation Operand => _lazyOperand.Value;
    }

    /// <summary>
    /// Represents a C# or VB label statement.
    /// </summary>
    internal abstract partial class LabelStatementBase : Operation, ILabelStatement
    {
        protected LabelStatementBase(ILabelSymbol label, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.LabelStatement, isInvalid, syntax, type, constantValue)
        {
            Label = label ?? throw new System.ArgumentNullException("label");
        }
        /// <summary>
        ///  Label that can be the target of branches.
        /// </summary>
        public ILabelSymbol Label { get; }
        /// <summary>
        /// Statement that has been labeled.
        /// </summary>
        public abstract IOperation LabeledStatement { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLabelStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLabelStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# or VB label statement.
    /// </summary>
    internal sealed partial class LabelStatement : LabelStatementBase, ILabelStatement
    {
        public LabelStatement(ILabelSymbol label, IOperation labeledStatement, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(label, isInvalid, syntax, type, constantValue)
        {
            LabeledStatement = labeledStatement ?? throw new System.ArgumentNullException("labeledStatement");
        }
        /// <summary>
        /// Statement that has been labeled.
        /// </summary>
        public override IOperation LabeledStatement { get; }
    }

    /// <summary>
    /// Represents a C# or VB label statement.
    /// </summary>
    internal sealed partial class LazyLabelStatement : LabelStatementBase, ILabelStatement
    {
        private readonly Lazy<IOperation> _lazyLabeledStatement;

        public LazyLabelStatement(ILabelSymbol label, Lazy<IOperation> labeledStatement, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(label, isInvalid, syntax, type, constantValue)
        {
            _lazyLabeledStatement = labeledStatement ?? throw new System.ArgumentNullException("labeledStatement");
        }
        /// <summary>
        /// Statement that has been labeled.
        /// </summary>
        public override IOperation LabeledStatement => _lazyLabeledStatement.Value;
    }

    /// <summary>
    /// Represents a lambda expression.
    /// </summary>
    internal abstract partial class LambdaExpressionBase : Operation, ILambdaExpression
    {
        protected LambdaExpressionBase(IMethodSymbol signature, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.LambdaExpression, isInvalid, syntax, type, constantValue)
        {
            Signature = signature ?? throw new System.ArgumentNullException("signature");
        }
        /// <summary>
        /// Signature of the lambda.
        /// </summary>
        public IMethodSymbol Signature { get; }
        /// <summary>
        /// Body of the lambda.
        /// </summary>
        public abstract IBlockStatement Body { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLambdaExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLambdaExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a lambda expression.
    /// </summary>
    internal sealed partial class LambdaExpression : LambdaExpressionBase, ILambdaExpression
    {
        public LambdaExpression(IMethodSymbol signature, IBlockStatement body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(signature, isInvalid, syntax, type, constantValue)
        {
            Body = body ?? throw new System.ArgumentNullException("body");
        }
        /// <summary>
        /// Body of the lambda.
        /// </summary>
        public override IBlockStatement Body { get; }
    }

    /// <summary>
    /// Represents a lambda expression.
    /// </summary>
    internal sealed partial class LazyLambdaExpression : LambdaExpressionBase, ILambdaExpression
    {
        private readonly Lazy<IBlockStatement> _lazyBody;

        public LazyLambdaExpression(IMethodSymbol signature, Lazy<IBlockStatement> body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(signature, isInvalid, syntax, type, constantValue)
        {
            _lazyBody = body ?? throw new System.ArgumentNullException("body");
        }
        /// <summary>
        /// Body of the lambda.
        /// </summary>
        public override IBlockStatement Body => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a late-bound reference to a member of a class or struct.
    /// </summary>
    internal abstract partial class LateBoundMemberReferenceExpressionBase : Operation, ILateBoundMemberReferenceExpression
    {
        protected LateBoundMemberReferenceExpressionBase(string memberName, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.LateBoundMemberReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            MemberName = memberName ?? throw new System.ArgumentNullException("memberName");
        }
        /// <summary>
        /// Instance used to bind the member reference.
        /// </summary>
        public abstract IOperation Instance { get; }
        /// <summary>
        /// Name of the member.
        /// </summary>
        public string MemberName { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLateBoundMemberReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLateBoundMemberReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a late-bound reference to a member of a class or struct.
    /// </summary>
    internal sealed partial class LateBoundMemberReferenceExpression : LateBoundMemberReferenceExpressionBase, ILateBoundMemberReferenceExpression
    {
        public LateBoundMemberReferenceExpression(IOperation instance, string memberName, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(memberName, isInvalid, syntax, type, constantValue)
        {
            Instance = instance ?? throw new System.ArgumentNullException("instance");
        }
        /// <summary>
        /// Instance used to bind the member reference.
        /// </summary>
        public override IOperation Instance { get; }
    }

    /// <summary>
    /// Represents a late-bound reference to a member of a class or struct.
    /// </summary>
    internal sealed partial class LazyLateBoundMemberReferenceExpression : LateBoundMemberReferenceExpressionBase, ILateBoundMemberReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;

        public LazyLateBoundMemberReferenceExpression(Lazy<IOperation> instance, string memberName, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(memberName, isInvalid, syntax, type, constantValue)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException("instance");
        }
        /// <summary>
        /// Instance used to bind the member reference.
        /// </summary>
        public override IOperation Instance => _lazyInstance.Value;
    }

    /// <summary>
    /// Represents a textual literal numeric, string, etc. expression.
    /// </summary>
    internal sealed partial class LiteralExpression : Operation, ILiteralExpression
    {
        public LiteralExpression(string text, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.LiteralExpression, isInvalid, syntax, type, constantValue)
        {
            Text = text ?? throw new System.ArgumentNullException("text");
        }
        /// <summary>
        /// Textual representation of the literal.
        /// </summary>
        public string Text { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLiteralExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLiteralExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a declared local variable.
    /// </summary>
    internal sealed partial class LocalReferenceExpression : Operation, ILocalReferenceExpression
    {
        public LocalReferenceExpression(ILocalSymbol local, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.LocalReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            Local = local ?? throw new System.ArgumentNullException("local");
        }
        /// <summary>
        /// Referenced local variable.
        /// </summary>
        public ILocalSymbol Local { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLocalReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLocalReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# lock or a VB SyncLock statement.
    /// </summary>
    internal abstract partial class LockStatementBase : Operation, ILockStatement
    {
        protected LockStatementBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.LockStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Value to be locked.
        /// </summary>
        public abstract IOperation LockedObject { get; }
        /// <summary>
        /// Body of the lock, to be executed while holding the lock.
        /// </summary>
        public abstract IOperation Body { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLockStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLockStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# lock or a VB SyncLock statement.
    /// </summary>
    internal sealed partial class LockStatement : LockStatementBase, ILockStatement
    {
        public LockStatement(IOperation lockedObject, IOperation body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            LockedObject = lockedObject ?? throw new System.ArgumentNullException("lockedObject");
            Body = body ?? throw new System.ArgumentNullException("body");
        }
        /// <summary>
        /// Value to be locked.
        /// </summary>
        public override IOperation LockedObject { get; }
        /// <summary>
        /// Body of the lock, to be executed while holding the lock.
        /// </summary>
        public override IOperation Body { get; }
    }

    /// <summary>
    /// Represents a C# lock or a VB SyncLock statement.
    /// </summary>
    internal sealed partial class LazyLockStatement : LockStatementBase, ILockStatement
    {
        private readonly Lazy<IOperation> _lazyLockedObject;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyLockStatement(Lazy<IOperation> lockedObject, Lazy<IOperation> body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyLockedObject = lockedObject ?? throw new System.ArgumentNullException("lockedObject");
            _lazyBody = body ?? throw new System.ArgumentNullException("body");
        }
        /// <summary>
        /// Value to be locked.
        /// </summary>
        public override IOperation LockedObject => _lazyLockedObject.Value;

        /// <summary>
        /// Body of the lock, to be executed while holding the lock.
        /// </summary>
        public override IOperation Body => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# while, for, foreach, or do statement, or a VB While, For, For Each, or Do statement.
    /// </summary>
    internal abstract partial class LoopStatement : Operation, ILoopStatement
    {
        protected LoopStatement(LoopKind loopKind, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
            LoopKind = loopKind;
        }
        /// <summary>
        /// Kind of the loop.
        /// </summary>
        public LoopKind LoopKind { get; }
        /// <summary>
        /// Body of the loop.
        /// </summary>
        public abstract IOperation Body { get; }
    }

    /// <summary>
    /// Represents a reference to a member of a class, struct, or interface.
    /// </summary>
    internal abstract partial class MemberReferenceExpression : Operation, IMemberReferenceExpression
    {
        protected MemberReferenceExpression(ISymbol member, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
            Member = member ?? throw new System.ArgumentNullException("member");
        }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public abstract IOperation Instance { get; }

        /// <summary>
        /// Referenced member.
        /// </summary>
        public ISymbol Member { get; }
    }

    /// <summary>
    /// Represents a reference to a method other than as the target of an invocation.
    /// </summary>
    internal sealed partial class MethodBindingExpression : MemberReferenceExpression, IMethodBindingExpression
    {
        public MethodBindingExpression(IMethodSymbol method, bool isVirtual, IOperation instance, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(member, OperationKind.MethodBindingExpression, isInvalid, syntax, type, constantValue)
        {
            Method = method ?? throw new System.ArgumentNullException("method");
            IsVirtual = isVirtual;
            Instance = instance ?? throw new System.ArgumentNullException("instance");
        }
        /// <summary>
        /// Referenced method.
        /// </summary>
        public IMethodSymbol Method { get; }

        /// <summary>
        /// Indicates whether the reference uses virtual semantics.
        /// </summary>
        public bool IsVirtual { get; }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitMethodBindingExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitMethodBindingExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a method other than as the target of an invocation.
    /// </summary>
    internal sealed partial class LazyMethodBindingExpression : MemberReferenceExpression, IMethodBindingExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;

        public LazyMethodBindingExpression(IMethodSymbol method, bool isVirtual, Lazy<IOperation> instance, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(member, OperationKind.MethodBindingExpression, isInvalid, syntax, type, constantValue)
        {
            Method = method ?? throw new System.ArgumentNullException("method");
            IsVirtual = isVirtual;
            _lazyInstance = instance ?? throw new System.ArgumentNullException("instance");
        }
        /// <summary>
        /// Referenced method.
        /// </summary>
        public IMethodSymbol Method { get; }

        /// <summary>
        /// Indicates whether the reference uses virtual semantics.
        /// </summary>
        public bool IsVirtual { get; }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance => _lazyInstance.Value;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitMethodBindingExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitMethodBindingExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a null-coalescing expression.
    /// </summary>
    internal abstract partial class NullCoalescingExpressionBase : Operation, INullCoalescingExpression
    {
        protected NullCoalescingExpressionBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.NullCoalescingExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Value to be unconditionally evaluated.
        /// </summary>
        public abstract IOperation PrimaryOperand { get; }
        /// <summary>
        /// Value to be evaluated if Primary evaluates to null/Nothing.
        /// </summary>
        public abstract IOperation SecondaryOperand { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitNullCoalescingExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitNullCoalescingExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a null-coalescing expression.
    /// </summary>
    internal sealed partial class NullCoalescingExpression : NullCoalescingExpressionBase, INullCoalescingExpression
    {
        public NullCoalescingExpression(IOperation primaryOperand, IOperation secondaryOperand, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            PrimaryOperand = primaryOperand ?? throw new System.ArgumentNullException("primaryOperand");
            SecondaryOperand = secondaryOperand ?? throw new System.ArgumentNullException("secondaryOperand");
        }
        /// <summary>
        /// Value to be unconditionally evaluated.
        /// </summary>
        public override IOperation PrimaryOperand { get; }
        /// <summary>
        /// Value to be evaluated if Primary evaluates to null/Nothing.
        /// </summary>
        public override IOperation SecondaryOperand { get; }
    }

    /// <summary>
    /// Represents a null-coalescing expression.
    /// </summary>
    internal sealed partial class LazyNullCoalescingExpression : NullCoalescingExpressionBase, INullCoalescingExpression
    {
        private readonly Lazy<IOperation> _lazyPrimaryOperand;
        private readonly Lazy<IOperation> _lazySecondaryOperand;

        public LazyNullCoalescingExpression(Lazy<IOperation> primaryOperand, Lazy<IOperation> secondaryOperand, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyPrimaryOperand = primaryOperand ?? throw new System.ArgumentNullException("primaryOperand");
            _lazySecondaryOperand = secondaryOperand ?? throw new System.ArgumentNullException("secondaryOperand");
        }
        /// <summary>
        /// Value to be unconditionally evaluated.
        /// </summary>
        public override IOperation PrimaryOperand => _lazyPrimaryOperand.Value;

        /// <summary>
        /// Value to be evaluated if Primary evaluates to null/Nothing.
        /// </summary>
        public override IOperation SecondaryOperand => _lazySecondaryOperand.Value;
    }

    /// <summary>
    /// Represents a new/New expression.
    /// </summary>
    internal abstract partial class ObjectCreationExpressionBase : Operation, IHasArgumentsExpression, IObjectCreationExpression
    {
        protected ObjectCreationExpressionBase(IMethodSymbol constructor, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ObjectCreationExpression, isInvalid, syntax, type, constantValue)
        {
            Constructor = constructor ?? throw new System.ArgumentNullException("constructor");
        }
        /// <summary>
        /// Constructor to be invoked on the created instance.
        /// </summary>
        public IMethodSymbol Constructor { get; }
        /// <summary>
        /// Explicitly-specified member initializers.
        /// </summary>
        public abstract ImmutableArray<ISymbolInitializer> MemberInitializers { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitObjectCreationExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitObjectCreationExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a new/New expression.
    /// </summary>
    internal sealed partial class ObjectCreationExpression : ObjectCreationExpressionBase, IHasArgumentsExpression, IObjectCreationExpression
    {
        public ObjectCreationExpression(IMethodSymbol constructor, ImmutableArray<ISymbolInitializer> memberInitializers, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(constructor, isInvalid, syntax, type, constantValue)
        {
            MemberInitializers = memberInitializers;
        }
        /// <summary>
        /// Explicitly-specified member initializers.
        /// </summary>
        public override ImmutableArray<ISymbolInitializer> MemberInitializers { get; }
    }

    /// <summary>
    /// Represents a new/New expression.
    /// </summary>
    internal sealed partial class LazyObjectCreationExpression : ObjectCreationExpressionBase, IHasArgumentsExpression, IObjectCreationExpression
    {
        private readonly Lazy<ImmutableArray<ISymbolInitializer>> _lazyMemberInitializers;

        public LazyObjectCreationExpression(IMethodSymbol constructor, Lazy<ImmutableArray<ISymbolInitializer>> memberInitializers, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(constructor, isInvalid, syntax, type, constantValue)
        {
            _lazyMemberInitializers = memberInitializers;
        }
        /// <summary>
        /// Explicitly-specified member initializers.
        /// </summary>
        public override ImmutableArray<ISymbolInitializer> MemberInitializers => _lazyMemberInitializers.Value;
    }

    /// <summary>
    /// Represents an argument value that has been omitted in an invocation.
    /// </summary>
    internal sealed partial class OmittedArgumentExpression : Operation, IOmittedArgumentExpression
    {
        public OmittedArgumentExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.OmittedArgumentExpression, isInvalid, syntax, type, constantValue)
        {
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitOmittedArgumentExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitOmittedArgumentExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an initialization of a parameter at the point of declaration.
    /// </summary>
    internal sealed partial class ParameterInitializer : SymbolInitializer, IParameterInitializer
    {
        public ParameterInitializer(IParameterSymbol parameter, IOperation value, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
            Parameter = parameter ?? throw new System.ArgumentNullException("parameter");
            Value = value ?? throw new System.ArgumentNullException("value");
        }
        /// <summary>
        /// Initialized parameter.
        /// </summary>
        public IParameterSymbol Parameter { get; }
        public override IOperation Value { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitParameterInitializer(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitParameterInitializer(this, argument);
        }
    }

    /// <summary>
    /// Represents an initialization of a parameter at the point of declaration.
    /// </summary>
    internal sealed partial class LazyParameterInitializer : SymbolInitializer, IParameterInitializer
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazyParameterInitializer(IParameterSymbol parameter, Lazy<IOperation> value, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(kind, isInvalid, syntax, type, constantValue)
        {
            Parameter = parameter ?? throw new System.ArgumentNullException("parameter");
            _lazyValue = value ?? throw new System.ArgumentNullException("value");
        }
        /// <summary>
        /// Initialized parameter.
        /// </summary>
        public IParameterSymbol Parameter { get; }
        public override IOperation Value => _lazyValue.Value;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitParameterInitializer(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitParameterInitializer(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a parameter.
    /// </summary>
    internal sealed partial class ParameterReferenceExpression : Operation, IParameterReferenceExpression
    {
        public ParameterReferenceExpression(IParameterSymbol parameter, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.ParameterReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            Parameter = parameter ?? throw new System.ArgumentNullException("parameter");
        }
        /// <summary>
        /// Referenced parameter.
        /// </summary>
        public IParameterSymbol Parameter { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitParameterReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitParameterReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a parenthesized expression.
    /// </summary>
    internal abstract partial class ParenthesizedExpressionBase : Operation, IParenthesizedExpression
    {
        protected ParenthesizedExpressionBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ParenthesizedExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Operand enclosed in parentheses.
        /// </summary>
        public abstract IOperation Operand { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitParenthesizedExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitParenthesizedExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a parenthesized expression.
    /// </summary>
    internal sealed partial class ParenthesizedExpression : ParenthesizedExpressionBase, IParenthesizedExpression
    {
        public ParenthesizedExpression(IOperation operand, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Operand = operand ?? throw new System.ArgumentNullException("operand");
        }
        /// <summary>
        /// Operand enclosed in parentheses.
        /// </summary>
        public override IOperation Operand { get; }
    }

    /// <summary>
    /// Represents a parenthesized expression.
    /// </summary>
    internal sealed partial class LazyParenthesizedExpression : ParenthesizedExpressionBase, IParenthesizedExpression
    {
        private readonly Lazy<IOperation> _lazyOperand;

        public LazyParenthesizedExpression(Lazy<IOperation> operand, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyOperand = operand ?? throw new System.ArgumentNullException("operand");
        }
        /// <summary>
        /// Operand enclosed in parentheses.
        /// </summary>
        public override IOperation Operand => _lazyOperand.Value;
    }

    /// <summary>
    /// Represents a general placeholder when no more specific kind of placeholder is available.
    /// A placeholder is an expression whose meaning is inferred from context.
    /// </summary>
    internal sealed partial class PlaceholderExpression : Operation, IPlaceholderExpression
    {
        public PlaceholderExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.PlaceholderExpression, isInvalid, syntax, type, constantValue)
        {
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPlaceholderExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPlaceholderExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference through a pointer.
    /// </summary>
    internal abstract partial class PointerIndirectionReferenceExpressionBase : Operation, IPointerIndirectionReferenceExpression
    {
        protected PointerIndirectionReferenceExpressionBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.PointerIndirectionReferenceExpression, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Pointer to be dereferenced.
        /// </summary>
        public abstract IOperation Pointer { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPointerIndirectionReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPointerIndirectionReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference through a pointer.
    /// </summary>
    internal sealed partial class PointerIndirectionReferenceExpression : PointerIndirectionReferenceExpressionBase, IPointerIndirectionReferenceExpression
    {
        public PointerIndirectionReferenceExpression(IOperation pointer, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Pointer = pointer ?? throw new System.ArgumentNullException("pointer");
        }
        /// <summary>
        /// Pointer to be dereferenced.
        /// </summary>
        public override IOperation Pointer { get; }
    }

    /// <summary>
    /// Represents a reference through a pointer.
    /// </summary>
    internal sealed partial class LazyPointerIndirectionReferenceExpression : PointerIndirectionReferenceExpressionBase, IPointerIndirectionReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyPointer;

        public LazyPointerIndirectionReferenceExpression(Lazy<IOperation> pointer, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyPointer = pointer ?? throw new System.ArgumentNullException("pointer");
        }
        /// <summary>
        /// Pointer to be dereferenced.
        /// </summary>
        public override IOperation Pointer => _lazyPointer.Value;
    }

    /// <summary>
    /// Represents an initialization of a property.
    /// </summary>
    internal sealed partial class PropertyInitializer : SymbolInitializer, IPropertyInitializer
    {
        public PropertyInitializer(IPropertySymbol initializedProperty, IOperation value, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
            InitializedProperty = initializedProperty ?? throw new System.ArgumentNullException("initializedProperty");
            Value = value ?? throw new System.ArgumentNullException("value");
        }
        /// <summary>
        /// Set method used to initialize the property.
        /// </summary>
        public IPropertySymbol InitializedProperty { get; }
        public override IOperation Value { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPropertyInitializer(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPropertyInitializer(this, argument);
        }
    }

    /// <summary>
    /// Represents an initialization of a property.
    /// </summary>
    internal sealed partial class LazyPropertyInitializer : SymbolInitializer, IPropertyInitializer
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazyPropertyInitializer(IPropertySymbol initializedProperty, Lazy<IOperation> value, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(kind, isInvalid, syntax, type, constantValue)
        {
            InitializedProperty = initializedProperty ?? throw new System.ArgumentNullException("initializedProperty");
            _lazyValue = value ?? throw new System.ArgumentNullException("value");
        }
        /// <summary>
        /// Set method used to initialize the property.
        /// </summary>
        public IPropertySymbol InitializedProperty { get; }
        public override IOperation Value => _lazyValue.Value;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPropertyInitializer(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPropertyInitializer(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a property.
    /// </summary>
    internal abstract partial class PropertyReferenceExpressionBase : MemberReferenceExpression, IPropertyReferenceExpression
    {
        protected PropertyReferenceExpressionBase(IPropertySymbol property, ISymbol member, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(member, kind, isInvalid, syntax, type, constantValue)
        {
            Property = property ?? throw new System.ArgumentNullException("property");
        }
        /// <summary>
        /// Referenced property.
        /// </summary>
        public IPropertySymbol Property { get; }
    }

    /// <summary>
    /// Represents a reference to a property.
    /// </summary>
    internal sealed partial class PropertyReferenceExpression : PropertyReferenceExpressionBase, IPropertyReferenceExpression
    {
        public PropertyReferenceExpression(IPropertySymbol property, IOperation instance, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(property, member, OperationKind.PropertyReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            Instance = instance ?? throw new System.ArgumentNullException("instance");
        }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPropertyReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPropertyReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a property.
    /// </summary>
    internal sealed partial class LazyPropertyReferenceExpression : PropertyReferenceExpressionBase, IPropertyReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyInstance;

        public LazyPropertyReferenceExpression(IPropertySymbol property, Lazy<IOperation> instance, ISymbol member, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(property, member, OperationKind.PropertyReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            _lazyInstance = instance ?? throw new System.ArgumentNullException("instance");
        }
        /// <summary>
        /// Instance of the type. Null if the reference is to a static/shared member.
        /// </summary>
        public override IOperation Instance => _lazyInstance.Value;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitPropertyReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitPropertyReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents Case x To y in VB.
    /// </summary>
    internal sealed partial class RangeCaseClause : CaseClause, IRangeCaseClause
    {
        public RangeCaseClause(IOperation minimumValue, IOperation maximumValue, CaseKind caseKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(caseKind, OperationKind.RangeCaseClause, isInvalid, syntax, type, constantValue)
        {
            MinimumValue = minimumValue ?? throw new System.ArgumentNullException("minimumValue");
            MaximumValue = maximumValue ?? throw new System.ArgumentNullException("maximumValue");
        }
        /// <summary>
        /// Minimum value of the case range.
        /// </summary>
        public IOperation MinimumValue { get; }
        /// <summary>
        /// Maximum value of the case range.
        /// </summary>
        public IOperation MaximumValue { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitRangeCaseClause(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitRangeCaseClause(this, argument);
        }
    }

    /// <summary>
    /// Represents Case x To y in VB.
    /// </summary>
    internal sealed partial class LazyRangeCaseClause : CaseClause, IRangeCaseClause
    {
        private readonly Lazy<IOperation> _lazyMinimumValue;
        private readonly Lazy<IOperation> _lazyMaximumValue;

        public LazyRangeCaseClause(Lazy<IOperation> minimumValue, Lazy<IOperation> maximumValue, CaseKind caseKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(caseKind, OperationKind.RangeCaseClause, isInvalid, syntax, type, constantValue)
        {
            _lazyMinimumValue = minimumValue ?? throw new System.ArgumentNullException("minimumValue");
            _lazyMaximumValue = maximumValue ?? throw new System.ArgumentNullException("maximumValue");
        }
        /// <summary>
        /// Minimum value of the case range.
        /// </summary>
        public IOperation MinimumValue => _lazyMinimumValue.Value;

        /// <summary>
        /// Maximum value of the case range.
        /// </summary>
        public IOperation MaximumValue => _lazyMaximumValue.Value;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitRangeCaseClause(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitRangeCaseClause(this, argument);
        }
    }

    /// <summary>
    /// Represents Case Is op x in VB.
    /// </summary>
    internal sealed partial class RelationalCaseClause : CaseClause, IRelationalCaseClause
    {
        public RelationalCaseClause(IOperation value, BinaryOperationKind relation, CaseKind caseKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(caseKind, OperationKind.RelationalCaseClause, isInvalid, syntax, type, constantValue)
        {
            Value = value ?? throw new System.ArgumentNullException("value");
            Relation = relation;
        }
        /// <summary>
        /// Case value.
        /// </summary>
        public IOperation Value { get; }
        /// <summary>
        /// Relational operator used to compare the switch value with the case value.
        /// </summary>
        public BinaryOperationKind Relation { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitRelationalCaseClause(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitRelationalCaseClause(this, argument);
        }
    }

    /// <summary>
    /// Represents Case Is op x in VB.
    /// </summary>
    internal sealed partial class LazyRelationalCaseClause : CaseClause, IRelationalCaseClause
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazyRelationalCaseClause(Lazy<IOperation> value, BinaryOperationKind relation, CaseKind caseKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(caseKind, OperationKind.RelationalCaseClause, isInvalid, syntax, type, constantValue)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException("value");
            Relation = relation;
        }
        /// <summary>
        /// Case value.
        /// </summary>
        public IOperation Value => _lazyValue.Value;

        /// <summary>
        /// Relational operator used to compare the switch value with the case value.
        /// </summary>
        public BinaryOperationKind Relation { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitRelationalCaseClause(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitRelationalCaseClause(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# return or a VB Return statement.
    /// </summary>
    internal abstract partial class ReturnStatementBase : Operation, IReturnStatement
    {
        protected ReturnStatementBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ReturnStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Value to be returned.
        /// </summary>
        public abstract IOperation ReturnedValue { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitReturnStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitReturnStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# return or a VB Return statement.
    /// </summary>
    internal sealed partial class ReturnStatement : ReturnStatementBase, IReturnStatement
    {
        public ReturnStatement(IOperation returnedValue, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            ReturnedValue = returnedValue ?? throw new System.ArgumentNullException("returnedValue");
        }
        /// <summary>
        /// Value to be returned.
        /// </summary>
        public override IOperation ReturnedValue { get; }
    }

    /// <summary>
    /// Represents a C# return or a VB Return statement.
    /// </summary>
    internal sealed partial class LazyReturnStatement : ReturnStatementBase, IReturnStatement
    {
        private readonly Lazy<IOperation> _lazyReturnedValue;

        public LazyReturnStatement(Lazy<IOperation> returnedValue, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyReturnedValue = returnedValue ?? throw new System.ArgumentNullException("returnedValue");
        }
        /// <summary>
        /// Value to be returned.
        /// </summary>
        public override IOperation ReturnedValue => _lazyReturnedValue.Value;
    }

    /// <summary>
    /// Represents case x in C# or Case x in VB.
    /// </summary>
    internal sealed partial class SingleValueCaseClause : CaseClause, ISingleValueCaseClause
    {
        public SingleValueCaseClause(IOperation value, BinaryOperationKind equality, CaseKind caseKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(caseKind, OperationKind.SingleValueCaseClause, isInvalid, syntax, type, constantValue)
        {
            Value = value ?? throw new System.ArgumentNullException("value");
            Equality = equality;
        }
        /// <summary>
        /// Case value.
        /// </summary>
        public IOperation Value { get; }
        /// <summary>
        /// Relational operator used to compare the switch value with the case value.
        /// </summary>
        public BinaryOperationKind Equality { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSingleValueCaseClause(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSingleValueCaseClause(this, argument);
        }
    }

    /// <summary>
    /// Represents case x in C# or Case x in VB.
    /// </summary>
    internal sealed partial class LazySingleValueCaseClause : CaseClause, ISingleValueCaseClause
    {
        private readonly Lazy<IOperation> _lazyValue;

        public LazySingleValueCaseClause(Lazy<IOperation> value, BinaryOperationKind equality, CaseKind caseKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(caseKind, OperationKind.SingleValueCaseClause, isInvalid, syntax, type, constantValue)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException("value");
            Equality = equality;
        }
        /// <summary>
        /// Case value.
        /// </summary>
        public IOperation Value => _lazyValue.Value;

        /// <summary>
        /// Relational operator used to compare the switch value with the case value.
        /// </summary>
        public BinaryOperationKind Equality { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSingleValueCaseClause(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSingleValueCaseClause(this, argument);
        }
    }

    /// <summary>
    /// Represents a SizeOf expression.
    /// </summary>
    internal sealed partial class SizeOfExpression : TypeOperationExpression, ISizeOfExpression
    {
        public SizeOfExpression(ITypeSymbol typeOperand, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(typeOperand, OperationKind.SizeOfExpression, isInvalid, syntax, type, constantValue)
        {
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSizeOfExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSizeOfExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a VB Stop statement.
    /// </summary>
    internal sealed partial class StopStatement : Operation, IStopStatement
    {
        public StopStatement(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.StopStatement, isInvalid, syntax, type, constantValue)
        {
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitStopStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitStopStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# case or VB Case statement.
    /// </summary>
    internal abstract partial class SwitchCaseBase : Operation, ISwitchCase
    {
        protected SwitchCaseBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.SwitchCase, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Clauses of the case. For C# there is one clause per case, but for VB there can be multiple.
        /// </summary>
        public abstract ImmutableArray<ICaseClause> Clauses { get; }
        /// <summary>
        /// Statements of the case.
        /// </summary>
        public abstract ImmutableArray<IOperation> Body { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSwitchCase(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSwitchCase(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# case or VB Case statement.
    /// </summary>
    internal sealed partial class SwitchCase : SwitchCaseBase, ISwitchCase
    {
        public SwitchCase(ImmutableArray<ICaseClause> clauses, ImmutableArray<IOperation> body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Clauses = clauses;
            Body = body;
        }
        /// <summary>
        /// Clauses of the case. For C# there is one clause per case, but for VB there can be multiple.
        /// </summary>
        public override ImmutableArray<ICaseClause> Clauses { get; }
        /// <summary>
        /// Statements of the case.
        /// </summary>
        public override ImmutableArray<IOperation> Body { get; }
    }

    /// <summary>
    /// Represents a C# case or VB Case statement.
    /// </summary>
    internal sealed partial class LazySwitchCase : SwitchCaseBase, ISwitchCase
    {
        private readonly Lazy<ImmutableArray<ICaseClause>> _lazyClauses;
        private readonly Lazy<ImmutableArray<IOperation>> _lazyBody;

        public LazySwitchCase(Lazy<ImmutableArray<ICaseClause>> clauses, Lazy<ImmutableArray<IOperation>> body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyClauses = clauses;
            _lazyBody = body;
        }
        /// <summary>
        /// Clauses of the case. For C# there is one clause per case, but for VB there can be multiple.
        /// </summary>
        public override ImmutableArray<ICaseClause> Clauses => _lazyClauses.Value;

        /// <summary>
        /// Statements of the case.
        /// </summary>
        public override ImmutableArray<IOperation> Body => _lazyBody.Value;
    }

    /// <summary>
    /// Represents a C# switch or VB Select Case statement.
    /// </summary>
    internal abstract partial class SwitchStatementBase : Operation, ISwitchStatement
    {
        protected SwitchStatementBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.SwitchStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Value to be switched upon.
        /// </summary>
        public abstract IOperation Value { get; }
        /// <summary>
        /// Cases of the switch.
        /// </summary>
        public abstract ImmutableArray<ISwitchCase> Cases { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSwitchStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSwitchStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# switch or VB Select Case statement.
    /// </summary>
    internal sealed partial class SwitchStatement : SwitchStatementBase, ISwitchStatement
    {
        public SwitchStatement(IOperation value, ImmutableArray<ISwitchCase> cases, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Value = value ?? throw new System.ArgumentNullException("value");
            Cases = cases;
        }
        /// <summary>
        /// Value to be switched upon.
        /// </summary>
        public override IOperation Value { get; }
        /// <summary>
        /// Cases of the switch.
        /// </summary>
        public override ImmutableArray<ISwitchCase> Cases { get; }
    }

    /// <summary>
    /// Represents a C# switch or VB Select Case statement.
    /// </summary>
    internal sealed partial class LazySwitchStatement : SwitchStatementBase, ISwitchStatement
    {
        private readonly Lazy<IOperation> _lazyValue;
        private readonly Lazy<ImmutableArray<ISwitchCase>> _lazyCases;

        public LazySwitchStatement(Lazy<IOperation> value, Lazy<ImmutableArray<ISwitchCase>> cases, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyValue = value ?? throw new System.ArgumentNullException("value");
            _lazyCases = cases;
        }
        /// <summary>
        /// Value to be switched upon.
        /// </summary>
        public override IOperation Value => _lazyValue.Value;

        /// <summary>
        /// Cases of the switch.
        /// </summary>
        public override ImmutableArray<ISwitchCase> Cases => _lazyCases.Value;
    }

    /// <summary>
    /// Represents an initializer for a field, property, or parameter.
    /// </summary>
    internal abstract partial class SymbolInitializer : Operation, ISymbolInitializer
    {
        protected SymbolInitializer(OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
        }
        public abstract IOperation Value { get; }
    }

    /// <summary>
    /// Represents a reference to a local variable synthesized by language analysis.
    /// </summary>
    internal abstract partial class SyntheticLocalReferenceExpressionBase : Operation, ISyntheticLocalReferenceExpression
    {
        protected SyntheticLocalReferenceExpressionBase(SyntheticLocalKind syntheticLocalKind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.SyntheticLocalReferenceExpression, isInvalid, syntax, type, constantValue)
        {
            SyntheticLocalKind = syntheticLocalKind;
        }
        /// <summary>
        /// Kind of the synthetic local.
        /// </summary>
        public SyntheticLocalKind SyntheticLocalKind { get; }
        /// <summary>
        /// Statement defining the lifetime of the synthetic local.
        /// </summary>
        public abstract IOperation ContainingStatement { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitSyntheticLocalReferenceExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitSyntheticLocalReferenceExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a reference to a local variable synthesized by language analysis.
    /// </summary>
    internal sealed partial class SyntheticLocalReferenceExpression : SyntheticLocalReferenceExpressionBase, ISyntheticLocalReferenceExpression
    {
        public SyntheticLocalReferenceExpression(SyntheticLocalKind syntheticLocalKind, IOperation containingStatement, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(syntheticLocalKind, isInvalid, syntax, type, constantValue)
        {
            ContainingStatement = containingStatement ?? throw new System.ArgumentNullException("containingStatement");
        }
        /// <summary>
        /// Statement defining the lifetime of the synthetic local.
        /// </summary>
        public override IOperation ContainingStatement { get; }
    }

    /// <summary>
    /// Represents a reference to a local variable synthesized by language analysis.
    /// </summary>
    internal sealed partial class LazySyntheticLocalReferenceExpression : SyntheticLocalReferenceExpressionBase, ISyntheticLocalReferenceExpression
    {
        private readonly Lazy<IOperation> _lazyContainingStatement;

        public LazySyntheticLocalReferenceExpression(SyntheticLocalKind syntheticLocalKind, Lazy<IOperation> containingStatement, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(syntheticLocalKind, isInvalid, syntax, type, constantValue)
        {
            _lazyContainingStatement = containingStatement ?? throw new System.ArgumentNullException("containingStatement");
        }
        /// <summary>
        /// Statement defining the lifetime of the synthetic local.
        /// </summary>
        public override IOperation ContainingStatement => _lazyContainingStatement.Value;
    }

    /// <summary>
    /// Represents a C# throw or a VB Throw statement.
    /// </summary>
    internal abstract partial class ThrowStatementBase : Operation, IThrowStatement
    {
        protected ThrowStatementBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.ThrowStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Value to be thrown.
        /// </summary>
        public abstract IOperation ThrownObject { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitThrowStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitThrowStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# throw or a VB Throw statement.
    /// </summary>
    internal sealed partial class ThrowStatement : ThrowStatementBase, IThrowStatement
    {
        public ThrowStatement(IOperation thrownObject, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            ThrownObject = thrownObject ?? throw new System.ArgumentNullException("thrownObject");
        }
        /// <summary>
        /// Value to be thrown.
        /// </summary>
        public override IOperation ThrownObject { get; }
    }

    /// <summary>
    /// Represents a C# throw or a VB Throw statement.
    /// </summary>
    internal sealed partial class LazyThrowStatement : ThrowStatementBase, IThrowStatement
    {
        private readonly Lazy<IOperation> _lazyThrownObject;

        public LazyThrowStatement(Lazy<IOperation> thrownObject, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyThrownObject = thrownObject ?? throw new System.ArgumentNullException("thrownObject");
        }
        /// <summary>
        /// Value to be thrown.
        /// </summary>
        public override IOperation ThrownObject => _lazyThrownObject.Value;
    }

    /// <summary>
    /// Represents a C# try or a VB Try statement.
    /// </summary>
    internal abstract partial class TryStatementBase : Operation, ITryStatement
    {
        protected TryStatementBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.TryStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Body of the try, over which the handlers are active.
        /// </summary>
        public abstract IBlockStatement Body { get; }
        /// <summary>
        /// Catch clauses of the try.
        /// </summary>
        public abstract ImmutableArray<ICatchClause> Catches { get; }
        /// <summary>
        /// Finally handler of the try.
        /// </summary>
        public abstract IBlockStatement FinallyHandler { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitTryStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitTryStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# try or a VB Try statement.
    /// </summary>
    internal sealed partial class TryStatement : TryStatementBase, ITryStatement
    {
        public TryStatement(IBlockStatement body, ImmutableArray<ICatchClause> catches, IBlockStatement finallyHandler, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Body = body ?? throw new System.ArgumentNullException("body");
            Catches = catches;
            FinallyHandler = finallyHandler ?? throw new System.ArgumentNullException("finallyHandler");
        }
        /// <summary>
        /// Body of the try, over which the handlers are active.
        /// </summary>
        public override IBlockStatement Body { get; }
        /// <summary>
        /// Catch clauses of the try.
        /// </summary>
        public override ImmutableArray<ICatchClause> Catches { get; }
        /// <summary>
        /// Finally handler of the try.
        /// </summary>
        public override IBlockStatement FinallyHandler { get; }
    }

    /// <summary>
    /// Represents a C# try or a VB Try statement.
    /// </summary>
    internal sealed partial class LazyTryStatement : TryStatementBase, ITryStatement
    {
        private readonly Lazy<IBlockStatement> _lazyBody;
        private readonly Lazy<ImmutableArray<ICatchClause>> _lazyCatches;
        private readonly Lazy<IBlockStatement> _lazyFinallyHandler;

        public LazyTryStatement(Lazy<IBlockStatement> body, Lazy<ImmutableArray<ICatchClause>> catches, Lazy<IBlockStatement> finallyHandler, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyBody = body ?? throw new System.ArgumentNullException("body");
            _lazyCatches = catches;
            _lazyFinallyHandler = finallyHandler ?? throw new System.ArgumentNullException("finallyHandler");
        }
        /// <summary>
        /// Body of the try, over which the handlers are active.
        /// </summary>
        public override IBlockStatement Body => _lazyBody.Value;

        /// <summary>
        /// Catch clauses of the try.
        /// </summary>
        public override ImmutableArray<ICatchClause> Catches => _lazyCatches.Value;

        /// <summary>
        /// Finally handler of the try.
        /// </summary>
        public override IBlockStatement FinallyHandler => _lazyFinallyHandler.Value;
    }

    /// <summary>
    /// Represents a TypeOf expression.
    /// </summary>
    internal sealed partial class TypeOfExpression : TypeOperationExpression, ITypeOfExpression
    {
        public TypeOfExpression(ITypeSymbol typeOperand, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(typeOperand, OperationKind.TypeOfExpression, isInvalid, syntax, type, constantValue)
        {
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitTypeOfExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitTypeOfExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an expression operating on a type.
    /// </summary>
    internal abstract partial class TypeOperationExpression : Operation, ITypeOperationExpression
    {
        protected TypeOperationExpression(ITypeSymbol typeOperand, OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(kind, isInvalid, syntax, type, constantValue)
        {
            TypeOperand = typeOperand ?? throw new System.ArgumentNullException("typeOperand");
        }
        /// <summary>
        /// Type operand.
        /// </summary>
        public ITypeSymbol TypeOperand { get; }
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal sealed partial class TypeParameterObjectCreationExpression : Operation, ITypeParameterObjectCreationExpression
    {
        public TypeParameterObjectCreationExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.TypeParameterObjectCreationExpression, isInvalid, syntax, type, constantValue)
        {
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitTypeParameterObjectCreationExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitTypeParameterObjectCreationExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an operation with one operand.
    /// </summary>
    internal abstract partial class UnaryOperatorExpressionBase : Operation, IHasOperatorMethodExpression, IUnaryOperatorExpression
    {
        protected UnaryOperatorExpressionBase(UnaryOperationKind unaryOperationKind, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.UnaryOperatorExpression, isInvalid, syntax, type, constantValue)
        {
            UnaryOperationKind = unaryOperationKind;
            UsesOperatorMethod = usesOperatorMethod;
            OperatorMethod = operatorMethod ?? throw new System.ArgumentNullException("operatorMethod");
        }
        /// <summary>
        /// Kind of unary operation.
        /// </summary>
        public UnaryOperationKind UnaryOperationKind { get; }
        /// <summary>
        /// Single operand.
        /// </summary>
        public abstract IOperation Operand { get; }
        /// <summary>
        /// True if and only if the operation is performed by an operator method.
        /// </summary>
        public bool UsesOperatorMethod { get; }
        /// <summary>
        /// Operation method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        public IMethodSymbol OperatorMethod { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitUnaryOperatorExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitUnaryOperatorExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents an operation with one operand.
    /// </summary>
    internal sealed partial class UnaryOperatorExpression : UnaryOperatorExpressionBase, IHasOperatorMethodExpression, IUnaryOperatorExpression
    {
        public UnaryOperatorExpression(UnaryOperationKind unaryOperationKind, IOperation operand, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(unaryOperationKind, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        {
            Operand = operand ?? throw new System.ArgumentNullException("operand");
        }
        /// <summary>
        /// Single operand.
        /// </summary>
        public override IOperation Operand { get; }
    }

    /// <summary>
    /// Represents an operation with one operand.
    /// </summary>
    internal sealed partial class LazyUnaryOperatorExpression : UnaryOperatorExpressionBase, IHasOperatorMethodExpression, IUnaryOperatorExpression
    {
        private readonly Lazy<IOperation> _lazyOperand;

        public LazyUnaryOperatorExpression(UnaryOperationKind unaryOperationKind, Lazy<IOperation> operand, bool usesOperatorMethod, IMethodSymbol operatorMethod, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(unaryOperationKind, usesOperatorMethod, operatorMethod, isInvalid, syntax, type, constantValue)
        {
            _lazyOperand = operand ?? throw new System.ArgumentNullException("operand");
        }
        /// <summary>
        /// Single operand.
        /// </summary>
        public override IOperation Operand => _lazyOperand.Value;
    }

    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal sealed partial class UnboundLambdaExpression : Operation, IUnboundLambdaExpression
    {
        public UnboundLambdaExpression(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(OperationKind.UnboundLambdaExpression, isInvalid, syntax, type, constantValue)
        {
        }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitUnboundLambdaExpression(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitUnboundLambdaExpression(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# using or VB Using statement.
    /// </summary>
    internal abstract partial class UsingStatementBase : Operation, IUsingStatement
    {
        protected UsingStatementBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.UsingStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Body of the using, over which the resources of the using are maintained.
        /// </summary>
        public abstract IOperation Body { get; }

        /// <summary>
        /// Declaration introduced by the using statement. Null if the using statement does not declare any variables.
        /// </summary>
        public abstract IVariableDeclarationStatement Declaration { get; }

        /// <summary>
        /// Resource held by the using. Can be null if Declaration is not null.
        /// </summary>
        public abstract IOperation Value { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitUsingStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitUsingStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# using or VB Using statement.
    /// </summary>
    internal sealed partial class UsingStatement : UsingStatementBase, IUsingStatement
    {
        public UsingStatement(IOperation body, IVariableDeclarationStatement declaration, IOperation value, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Body = body ?? throw new System.ArgumentNullException("body");
            Declaration = declaration ?? throw new System.ArgumentNullException("declaration");
            Value = value ?? throw new System.ArgumentNullException("value");
        }
        /// <summary>
        /// Body of the using, over which the resources of the using are maintained.
        /// </summary>
        public override IOperation Body { get; }

        /// <summary>
        /// Declaration introduced by the using statement. Null if the using statement does not declare any variables.
        /// </summary>
        public override IVariableDeclarationStatement Declaration { get; }

        /// <summary>
        /// Resource held by the using. Can be null if Declaration is not null.
        /// </summary>
        public override IOperation Value { get; }
    }

    /// <summary>
    /// Represents a C# using or VB Using statement.
    /// </summary>
    internal sealed partial class LazyUsingStatement : UsingStatementBase, IUsingStatement
    {
        private readonly Lazy<IOperation> _lazyBody;
        private readonly Lazy<IVariableDeclarationStatement> _lazyDeclaration;
        private readonly Lazy<IOperation> _lazyValue;

        public LazyUsingStatement(Lazy<IOperation> body, Lazy<IVariableDeclarationStatement> declaration, Lazy<IOperation> value, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyBody = body ?? throw new System.ArgumentNullException("body");
            _lazyDeclaration = declaration ?? throw new System.ArgumentNullException("declaration");
            _lazyValue = value ?? throw new System.ArgumentNullException("value");
        }
        /// <summary>
        /// Body of the using, over which the resources of the using are maintained.
        /// </summary>
        public override IOperation Body => _lazyBody.Value;

        /// <summary>
        /// Declaration introduced by the using statement. Null if the using statement does not declare any variables.
        /// </summary>
        public override IVariableDeclarationStatement Declaration => _lazyDeclaration.Value;

        /// <summary>
        /// Resource held by the using. Can be null if Declaration is not null.
        /// </summary>
        public override IOperation Value => _lazyValue.Value;
    }

    /// <summary>
    /// Represents a local variable declaration.
    /// </summary>
    internal abstract partial class VariableDeclarationBase : Operation, IVariableDeclaration
    {
        protected VariableDeclarationBase(ILocalSymbol variable, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.VariableDeclaration, isInvalid, syntax, type, constantValue)
        {
            Variable = variable ?? throw new System.ArgumentNullException("variable");
        }
        /// <summary>
        /// Variable declared by the declaration.
        /// </summary>
        public ILocalSymbol Variable { get; }
        /// <summary>
        /// Initializer of the variable.
        /// </summary>
        public abstract IOperation InitialValue { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitVariableDeclaration(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitVariableDeclaration(this, argument);
        }
    }

    /// <summary>
    /// Represents a local variable declaration.
    /// </summary>
    internal sealed partial class VariableDeclaration : VariableDeclarationBase, IVariableDeclaration
    {
        public VariableDeclaration(ILocalSymbol variable, IOperation initialValue, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(variable, isInvalid, syntax, type, constantValue)
        {
            InitialValue = initialValue ?? throw new System.ArgumentNullException("initialValue");
        }
        /// <summary>
        /// Initializer of the variable.
        /// </summary>
        public override IOperation InitialValue { get; }
    }

    /// <summary>
    /// Represents a local variable declaration.
    /// </summary>
    internal sealed partial class LazyVariableDeclaration : VariableDeclarationBase, IVariableDeclaration
    {
        private readonly Lazy<IOperation> _lazyInitialValue;

        public LazyVariableDeclaration(ILocalSymbol variable, Lazy<IOperation> initialValue, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(variable, isInvalid, syntax, type, constantValue)
        {
            _lazyInitialValue = initialValue ?? throw new System.ArgumentNullException("initialValue");
        }
        /// <summary>
        /// Initializer of the variable.
        /// </summary>
        public override IOperation InitialValue => _lazyInitialValue.Value;
    }

    /// <summary>
    /// Represents a local variable declaration statement.
    /// </summary>
    internal abstract partial class VariableDeclarationStatementBase : Operation, IVariableDeclarationStatement
    {
        protected VariableDeclarationStatementBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.VariableDeclarationStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Variables declared by the statement.
        /// </summary>
        public abstract ImmutableArray<IVariableDeclaration> Variables { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitVariableDeclarationStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitVariableDeclarationStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a local variable declaration statement.
    /// </summary>
    internal sealed partial class VariableDeclarationStatement : VariableDeclarationStatementBase, IVariableDeclarationStatement
    {
        public VariableDeclarationStatement(ImmutableArray<IVariableDeclaration> variables, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Variables = variables;
        }
        /// <summary>
        /// Variables declared by the statement.
        /// </summary>
        public override ImmutableArray<IVariableDeclaration> Variables { get; }
    }

    /// <summary>
    /// Represents a local variable declaration statement.
    /// </summary>
    internal sealed partial class LazyVariableDeclarationStatement : VariableDeclarationStatementBase, IVariableDeclarationStatement
    {
        private readonly Lazy<ImmutableArray<IVariableDeclaration>> _lazyVariables;

        public LazyVariableDeclarationStatement(Lazy<ImmutableArray<IVariableDeclaration>> variables, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyVariables = variables;
        }
        /// <summary>
        /// Variables declared by the statement.
        /// </summary>
        public override ImmutableArray<IVariableDeclaration> Variables => _lazyVariables.Value;
    }

    /// <summary>
    /// Represents a C# while or do statement, or a VB While or Do statement.
    /// </summary>
    internal sealed partial class WhileUntilLoopStatement : ForWhileUntilLoopStatement, IWhileUntilLoopStatement
    {
        public WhileUntilLoopStatement(bool isTopTest, bool isWhile, IOperation condition, LoopKind loopKind, IOperation body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(loopKind, OperationKind.LoopStatement, isInvalid, syntax, type, constantValue)
        {
            IsTopTest = isTopTest;
            IsWhile = isWhile;
            Condition = condition ?? throw new System.ArgumentNullException("condition");
            Body = body ?? throw new System.ArgumentNullException("body");
        }
        /// <summary>
        /// True if the loop test executes at the top of the loop; false if the loop test executes at the bottom of the loop.
        /// </summary>
        public bool IsTopTest { get; }
        /// <summary>
        /// True if the loop is a while loop; false if the loop is an until loop.
        /// </summary>
        public bool IsWhile { get; }
        /// <summary>
        /// Condition of the loop.
        /// </summary>
        public override IOperation Condition { get; }
        /// <summary>
        /// Body of the loop.
        /// </summary>
        public override IOperation Body { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitWhileUntilLoopStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitWhileUntilLoopStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a C# while or do statement, or a VB While or Do statement.
    /// </summary>
    internal sealed partial class LazyWhileUntilLoopStatement : ForWhileUntilLoopStatement, IWhileUntilLoopStatement
    {
        private readonly Lazy<IOperation> _lazyCondition;
        private readonly Lazy<IOperation> _lazyBody;

        public LazyWhileUntilLoopStatement(bool isTopTest, bool isWhile, Lazy<IOperation> condition, LoopKind loopKind, Lazy<IOperation> body, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(loopKind, OperationKind.LoopStatement, isInvalid, syntax, type, constantValue)
        {
            IsTopTest = isTopTest;
            IsWhile = isWhile;
            _lazyCondition = condition ?? throw new System.ArgumentNullException("condition");
            _lazyBody = body ?? throw new System.ArgumentNullException("body");
        }
        /// <summary>
        /// True if the loop test executes at the top of the loop; false if the loop test executes at the bottom of the loop.
        /// </summary>
        public bool IsTopTest { get; }
        /// <summary>
        /// True if the loop is a while loop; false if the loop is an until loop.
        /// </summary>
        public bool IsWhile { get; }
        /// <summary>
        /// Condition of the loop.
        /// </summary>
        public override IOperation Condition => _lazyCondition.Value;

        /// <summary>
        /// Body of the loop.
        /// </summary>
        public override IOperation Body => _lazyBody.Value;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitWhileUntilLoopStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitWhileUntilLoopStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a VB With statement.
    /// </summary>
    internal abstract partial class WithStatementBase : Operation, IWithStatement
    {
        protected WithStatementBase(bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
                    base(OperationKind.WithStatement, isInvalid, syntax, type, constantValue)
        {
        }
        /// <summary>
        /// Body of the with.
        /// </summary>
        public abstract IOperation Body { get; }
        /// <summary>
        /// Value to whose members leading-dot-qualified references within the with body bind.
        /// </summary>
        public abstract IOperation Value { get; }
        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitWithStatement(this);
        }
        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitWithStatement(this, argument);
        }
    }

    /// <summary>
    /// Represents a VB With statement.
    /// </summary>
    internal sealed partial class WithStatement : WithStatementBase, IWithStatement
    {
        public WithStatement(IOperation body, IOperation value, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) :
            base(isInvalid, syntax, type, constantValue)
        {
            Body = body ?? throw new System.ArgumentNullException("body");
            Value = value ?? throw new System.ArgumentNullException("value");
        }
        /// <summary>
        /// Body of the with.
        /// </summary>
        public override IOperation Body { get; }
        /// <summary>
        /// Value to whose members leading-dot-qualified references within the with body bind.
        /// </summary>
        public override IOperation Value { get; }
    }

    /// <summary>
    /// Represents a VB With statement.
    /// </summary>
    internal sealed partial class LazyWithStatement : WithStatementBase, IWithStatement
    {
        private readonly Lazy<IOperation> _lazyBody;
        private readonly Lazy<IOperation> _lazyValue;

        public LazyWithStatement(Lazy<IOperation> body, Lazy<IOperation> value, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue) : base(isInvalid, syntax, type, constantValue)
        {
            _lazyBody = body ?? throw new System.ArgumentNullException("body");
            _lazyValue = value ?? throw new System.ArgumentNullException("value");
        }
        /// <summary>
        /// Body of the with.
        /// </summary>
        public override IOperation Body => _lazyBody.Value;

        /// <summary>
        /// Value to whose members leading-dot-qualified references within the with body bind.
        /// </summary>
        public override IOperation Value => _lazyValue.Value;
    }

}
