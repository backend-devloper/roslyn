// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.ReadonlyReferences)]
    public class CodeGenRefReadonlyReturnTests : CompilingTestBase
    {
        [Fact]
        public void RefReturnArrayAccess()
        {
            var text = @"
class Program
{
    static ref readonly int M()
    {
        return ref (new int[1])[0];
    }
}
";

            //PROTOTYPE(readonlyRefs): this should work for now because readonly is treated as regular ref
            var comp = CompileAndVerify(text, parseOptions: TestOptions.Regular);

            comp.VerifyIL("Program.M()", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""int""
  IL_0006:  ldc.i4.0
  IL_0007:  ldelema    ""int""
  IL_000c:  ret
}");
        }

        [Fact]
        public void BindingInvalidRefRoCombination()
        {
            var text = @"
class Program
{
    // should be a syntax error
    // just make sure binder is ok with this
    static ref readonly ref int M(int x)
    {
        return ref M(x);
    }

    // should be a syntax error
    // just make sure binder is ok with this
    static readonly int M1(int x)
    {
        return ref M(x);
    }
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,25): error CS1031: Type expected
                //     static ref readonly ref int M(int x)
                Diagnostic(ErrorCode.ERR_TypeExpected, "ref").WithLocation(6, 25),
                // (13,25): error CS0106: The modifier 'readonly' is not valid for this item
                //     static readonly int M1(int x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M1").WithArguments("readonly").WithLocation(13, 25),
                // (15,20): error CS0120: An object reference is required for the non-static field, method, or property 'Program.M(int)'
                //         return ref M(x);
                Diagnostic(ErrorCode.ERR_ObjectRequired, "M").WithArguments("Program.M(int)").WithLocation(15, 20),
                // (15,9): error CS8149: By-reference returns may only be used in methods that return by reference
                //         return ref M(x);
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "return").WithLocation(15, 9)
            );
        }

        [Fact]
        public void ReadonlyReturnCannotAssign()
        {
            var text = @"
class Program
{
    static void Test()
    {
        M() = 1;
        M1().Alice = 2;

        M() ++;
        M1().Alice --;

        M() += 1;
        M1().Alice -= 2;
    }

    static ref readonly int M() => throw null;
    static ref readonly (int Alice, int Bob) M1() => throw null;
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,9): error CS8208: Cannot assign to method 'Program.M()' because it is a readonly variable
                //         M() = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "M()").WithArguments("method", "Program.M()").WithLocation(6, 9),
                // (7,9): error CS8209: Cannot assign to a member of method 'Program.M1()' because it is a readonly variable
                //         M1().Alice = 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "M1().Alice").WithArguments("method", "Program.M1()").WithLocation(7, 9),
                // (9,9): error CS8208: Cannot assign to method 'Program.M()' because it is a readonly variable
                //         M() ++;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "M()").WithArguments("method", "Program.M()").WithLocation(9, 9),
                // (10,9): error CS8209: Cannot assign to a member of method 'Program.M1()' because it is a readonly variable
                //         M1().Alice --;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "M1().Alice").WithArguments("method", "Program.M1()").WithLocation(10, 9),
                // (12,9): error CS8208: Cannot assign to method 'Program.M()' because it is a readonly variable
                //         M() += 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "M()").WithArguments("method", "Program.M()").WithLocation(12, 9),
                // (13,9): error CS8209: Cannot assign to a member of method 'Program.M1()' because it is a readonly variable
                //         M1().Alice -= 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "M1().Alice").WithArguments("method", "Program.M1()").WithLocation(13, 9)
            );
        }

        [Fact]
        public void ReadonlyReturnCannotAssign1()
        {
            var text = @"
class Program
{
    static void Test()
    {
        P = 1;
        P1.Alice = 2;

        P ++;
        P1.Alice --;

        P += 1;
        P1.Alice -= 2;
    }

    static ref readonly int P => throw null;
    static ref readonly (int Alice, int Bob) P1 => throw null;
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,9): error CS8208: Cannot assign to property 'Program.P' because it is a readonly variable
                //         P = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "P").WithArguments("property", "Program.P").WithLocation(6, 9),
                // (7,9): error CS8209: Cannot assign to a member of property 'Program.P1' because it is a readonly variable
                //         P1.Alice = 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "P1.Alice").WithArguments("property", "Program.P1").WithLocation(7, 9),
                // (9,9): error CS8208: Cannot assign to property 'Program.P' because it is a readonly variable
                //         P ++;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "P").WithArguments("property", "Program.P").WithLocation(9, 9),
                // (10,9): error CS8209: Cannot assign to a member of property 'Program.P1' because it is a readonly variable
                //         P1.Alice --;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "P1.Alice").WithArguments("property", "Program.P1").WithLocation(10, 9),
                // (12,9): error CS8208: Cannot assign to property 'Program.P' because it is a readonly variable
                //         P += 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "P").WithArguments("property", "Program.P").WithLocation(12, 9),
                // (13,9): error CS8209: Cannot assign to a member of property 'Program.P1' because it is a readonly variable
                //         P1.Alice -= 2;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "P1.Alice").WithArguments("property", "Program.P1").WithLocation(13, 9)
            );
        }

        [Fact]
        public void ReadonlyReturnCannotAssignByref()
        {
            var text = @"
class Program
{
    static void Test()
    {
        ref var y = ref M();
        ref int a = ref M1.Alice;
        ref var y1 = ref P;
        ref int a1 = ref P1.Alice;
    }

    static ref readonly int M() => throw null;
    static ref readonly (int Alice, int Bob) M1() => throw null;
    static ref readonly int P => throw null;
    static ref readonly (int Alice, int Bob) P1 => throw null;
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (6,25): error CS8206: Cannot use method 'Program.M()' as a ref or out value because it is a readonly variable
                //         ref var y = ref M();
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "M()").WithArguments("method", "Program.M()").WithLocation(6, 25),
                // (7,25): error CS0119: 'Program.M1()' is a method, which is not valid in the given context
                //         ref int a = ref M1.Alice;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "M1").WithArguments("Program.M1()", "method").WithLocation(7, 25),
                // (8,26): error CS8206: Cannot use property 'Program.P' as a ref or out value because it is a readonly variable
                //         ref var y1 = ref P;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "P").WithArguments("property", "Program.P").WithLocation(8, 26),
                // (9,26): error CS8207: Members of property 'Program.P1' cannot be used as a ref or out value because it is a readonly variable
                //         ref int a1 = ref P1.Alice;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField2, "P1.Alice").WithArguments("property", "Program.P1").WithLocation(9, 26)
            );
        }

        [Fact]
        public void ReadonlyReturnCannotTakePtr()
        {
            var text = @"
class Program
{
    unsafe static void Test()
    {
        int* a = & M();
        int* b = & M1().Alice;

        int* a1 = & P;
        int* b2 = & P1.Alice;

        fixed(int* c = & M())
        {
        }

        fixed(int* d = & M1().Alice)
        {
        }

        fixed(int* c = & P)
        {
        }

        fixed(int* d = & P1.Alice)
        {
        }
    }

    static ref readonly int M() => throw null;
    static ref readonly (int Alice, int Bob) M1() => throw null;
    static ref readonly int P => throw null;
    static ref readonly (int Alice, int Bob) P1 => throw null;

}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (6,20): error CS0211: Cannot take the address of the given expression
                //         int* a = & M();
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "M()").WithLocation(6, 20),
                // (7,20): error CS0211: Cannot take the address of the given expression
                //         int* b = & M1().Alice;
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "M1().Alice").WithLocation(7, 20),
                // (9,21): error CS0211: Cannot take the address of the given expression
                //         int* a1 = & P;
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "P").WithLocation(9, 21),
                // (10,21): error CS0211: Cannot take the address of the given expression
                //         int* b2 = & P1.Alice;
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "P1.Alice").WithLocation(10, 21),
                // (12,26): error CS0211: Cannot take the address of the given expression
                //         fixed(int* c = & M())
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "M()").WithLocation(12, 26),
                // (16,26): error CS0211: Cannot take the address of the given expression
                //         fixed(int* d = & M1().Alice)
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "M1().Alice").WithLocation(16, 26),
                // (20,26): error CS0211: Cannot take the address of the given expression
                //         fixed(int* c = & P)
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "P").WithLocation(20, 26),
                // (24,26): error CS0211: Cannot take the address of the given expression
                //         fixed(int* d = & P1.Alice)
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "P1.Alice").WithLocation(24, 26)
            );
        }

        [Fact]
        public void ReadonlyReturnCannotReturnByOrdinaryRef()
        {
            var text = @"
class Program
{
    static ref int Test()
    {
        bool b = true;

        if (b)
        {
            if (b)
            {
                return ref M();
            }
            else
            {
                return ref M1().Alice;
            }        
        }
        else
        {
            if (b)
            {
                return ref P;
            }
            else
            {
                return ref P1.Alice;
            }        
        }
    }

    static ref readonly int M() => throw null;
    static ref readonly (int Alice, int Bob) M1() => throw null;
    static ref readonly int P => throw null;
    static ref readonly (int Alice, int Bob) P1 => throw null;
}
";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (12,28): error CS8206: Cannot use method 'Program.M()' as a ref or out value because it is a readonly variable
                //                 return ref M();
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "M()").WithArguments("method", "Program.M()").WithLocation(12, 28),
                // (16,28): error CS8207: Members of method 'Program.M1()' cannot be used as a ref or out value because it is a readonly variable
                //                 return ref M1().Alice;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField2, "M1().Alice").WithArguments("method", "Program.M1()").WithLocation(16, 28),
                // (23,28): error CS8206: Cannot use property 'Program.P' as a ref or out value because it is a readonly variable
                //                 return ref P;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "P").WithArguments("property", "Program.P").WithLocation(23, 28),
                // (27,28): error CS8207: Members of property 'Program.P1' cannot be used as a ref or out value because it is a readonly variable
                //                 return ref P1.Alice;
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField2, "P1.Alice").WithArguments("property", "Program.P1").WithLocation(27, 28)
            );
        }

        [Fact]
        public void ReadonlyReturnCanReturnByRefReadonly()
        {
            var text = @"
class Program
{
    static ref readonly int Test()
    {
        bool b = true;

        if (b)
        {
            if (b)
            {
                return ref M();
            }
            else
            {
                return ref M1().Alice;
            }        
        }
        else
        {
            if (b)
            {
                return ref P;
            }
            else
            {
                return ref P1.Alice;
            }        
        }
    }

    static ref readonly int M() => throw null;
    static ref readonly (int Alice, int Bob) M1() => throw null;
    static ref readonly int P => throw null;
    static ref readonly (int Alice, int Bob) P1 => throw null;
}

";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: false);

            comp.VerifyIL("Program.Test", @"
{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (bool V_0) //b
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  brfalse.s  IL_0019
  IL_0005:  ldloc.0
  IL_0006:  brfalse.s  IL_000e
  IL_0008:  call       ""ref readonly int Program.M()""
  IL_000d:  ret
  IL_000e:  call       ""ref readonly (int Alice, int Bob) Program.M1()""
  IL_0013:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_0018:  ret
  IL_0019:  ldloc.0
  IL_001a:  brfalse.s  IL_0022
  IL_001c:  call       ""ref readonly int Program.P.get""
  IL_0021:  ret
  IL_0022:  call       ""ref readonly (int Alice, int Bob) Program.P1.get""
  IL_0027:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_002c:  ret
}");
        }

        [Fact]
        public void ReadonlyFieldCanReturnByRefReadonly()
        {
            var text = @"
class Program
{
    ref readonly int Test()
    {
        bool b = true;

        if (b)
        {
            if (b)
            {
                return ref F;
            }
            else
            {
                return ref F1.Alice;
            }        
        }
        else
        {
            if (b)
            {
                return ref S1.F;
            }
            else
            {
                return ref S2.F1.Alice;
            }        
        }
    }

    readonly int F = 1;
    static readonly (int Alice, int Bob) F1 = (2,3);

    readonly S S1 = new S();
    static readonly S S2 = new S();

    struct S
    {
        public readonly int F;
        public readonly (int Alice, int Bob) F1;
    }
}

";

            var comp = CompileAndVerify(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.Regular, verify: false);

            //PROTOTYPE(readonlyRef): correct emit is NYI. We should not make copies when returning r/o fields
            comp.VerifyIL("Program.Test", @"
{
  // Code size       70 (0x46)
  .maxstack  1
  .locals init (bool V_0, //b
                int V_1,
                System.ValueTuple<int, int> V_2,
                int V_3,
                System.ValueTuple<int, int> V_4)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  brfalse.s  IL_0020
  IL_0005:  ldloc.0
  IL_0006:  brfalse.s  IL_0012
  IL_0008:  ldarg.0
  IL_0009:  ldfld      ""int Program.F""
  IL_000e:  stloc.1
  IL_000f:  ldloca.s   V_1
  IL_0011:  ret
  IL_0012:  ldsfld     ""(int Alice, int Bob) Program.F1""
  IL_0017:  stloc.2
  IL_0018:  ldloca.s   V_2
  IL_001a:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_001f:  ret
  IL_0020:  ldloc.0
  IL_0021:  brfalse.s  IL_0032
  IL_0023:  ldarg.0
  IL_0024:  ldfld      ""Program.S Program.S1""
  IL_0029:  ldfld      ""int Program.S.F""
  IL_002e:  stloc.3
  IL_002f:  ldloca.s   V_3
  IL_0031:  ret
  IL_0032:  ldsfld     ""Program.S Program.S2""
  IL_0037:  ldfld      ""(int Alice, int Bob) Program.S.F1""
  IL_003c:  stloc.s    V_4
  IL_003e:  ldloca.s   V_4
  IL_0040:  ldflda     ""int System.ValueTuple<int, int>.Item1""
  IL_0045:  ret
}");
        }

        [Fact]
        public void ReadonlyReturnByRefReadonlyLocalSafety()
        {
            var text = @"
class Program
{
    ref readonly int Test()
    {
        bool b = true;
        int local = 42;

        if (b)
        {
            return ref M(ref local);
        }
        else
        {
            return ref M1(out local).Alice;
        }        
    }

    static ref readonly int M(ref int x) => throw null;
    static ref readonly (int Alice, int Bob) M1(out int x) => throw null;
}

";

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (11,30): error CS8168: Cannot return local 'local' by reference because it is not a ref local
                //             return ref M(ref local);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "local").WithArguments("local").WithLocation(11, 30),
                // (11,24): error CS8164: Cannot return by reference a result of 'Program.M(ref int)' because the argument passed to parameter 'x' cannot be returned by reference
                //             return ref M(ref local);
                Diagnostic(ErrorCode.ERR_RefReturnCall, "M(ref local)").WithArguments("Program.M(ref int)", "x").WithLocation(11, 24),
                // (15,31): error CS8168: Cannot return local 'local' by reference because it is not a ref local
                //             return ref M1(out local).Alice;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "local").WithArguments("local").WithLocation(15, 31),
                // (15,24): error CS8165: Cannot return by reference a member of result of 'Program.M1(out int)' because the argument passed to parameter 'x' cannot be returned by reference
                //             return ref M1(out local).Alice;
                Diagnostic(ErrorCode.ERR_RefReturnCall2, "M1(out local)").WithArguments("Program.M1(out int)", "x").WithLocation(15, 24)
            );
        }
    }
}
