﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact]
        public void ILambdaExpression_BoundLambda_HasValidLambdaExpressionTree()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Action x /*<bind>*/= () => F()/*</bind>*/;
    }

    static void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Action x /* ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'Action x /* ... *</bind>*/;')
    Variables: Local_1: System.Action x
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Action) (Syntax: '() => F()')
        ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: '() => F()')
          IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'F()')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'F()')
              IInvocationExpression (static void Program.F()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'F()')
            IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'F()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ILambdaExpression_UnboundLambdaAsVar_HasValidLambdaExpressionTree()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x /*<bind>*/= () => F()/*</bind>*/;
    }

    static void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'var x /*<bi ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var x /*<bi ... *</bind>*/;')
    Variables: Local_1: var x
    Initializer: ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: '() => F()')
        IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'F()')
          IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'F()')
            IInvocationExpression (static void Program.F()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'F()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0815: Cannot assign lambda expression to an implicitly-typed variable
                //         var x /*<bind>*/= () => F()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "x /*<bind>*/= () => F()").WithArguments("lambda expression").WithLocation(8, 13),
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ILambdaExpression_UnboundLambdaAsDelegate_HasValidLambdaExpressionTree()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Action<int> x /*<bind>*/= () => F()/*</bind>*/;
    }

    static void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Action<int> ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'Action<int> ... *</bind>*/;')
    Variables: Local_1: System.Action<System.Int32> x
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Action<System.Int32>, IsInvalid) (Syntax: '() => F()')
        ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: '() => F()')
          IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'F()')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'F()')
              IInvocationExpression (static void Program.F()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'F()')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1593: Delegate 'Action<int>' does not take 0 arguments
                //         Action<int> x /*<bind>*/= () => F()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "() => F()").WithArguments("System.Action<int>", "0").WithLocation(8, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
