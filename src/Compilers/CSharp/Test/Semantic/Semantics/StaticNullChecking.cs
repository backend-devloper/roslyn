﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class StaticNullChecking : CompilingTestBase
    {
        [Fact]
        public void Test0()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
        string? x = null;
    }
}
");

            c.VerifyDiagnostics(
    // (6,9): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
    //         string? x = null;
    Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "string?").WithArguments("System.Nullable<T>", "T", "string").WithLocation(6, 9),
    // (6,17): warning CS0219: The variable 'x' is assigned but its value is never used
    //         string? x = null;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(6, 17)
                );
        }

        [Fact]
        public void MissingNullableType_01()
        {
            CSharpCompilation core = CreateCompilation(@"
namespace System
{
    public class Object {}
    public struct Int32 {}
    public struct Void {}
}
");


            CSharpCompilation c = CreateCompilation(@"
class C
{
    static void Main()
    {
        int? x = null;
    }

    static void Test(int? x) {}
}
", new[] { core.ToMetadataReference() }, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (9,22): error CS0518: Predefined type 'System.Nullable`1' is not defined or imported
    //     static void Test(int? x) {}
    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "int?").WithArguments("System.Nullable`1").WithLocation(9, 22),
    // (6,9): error CS0518: Predefined type 'System.Nullable`1' is not defined or imported
    //         int? x = null;
    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "int?").WithArguments("System.Nullable`1").WithLocation(6, 9)
                );
        }

        [Fact]
        public void MissingNullableType_02()
        {
            CSharpCompilation core = CreateCompilation(@"
namespace System
{
    public class Object {}
    public struct Void {}
}
");


            CSharpCompilation c = CreateCompilation(@"
class C
{
    static void Main()
    {
        object? x = null;
    }

    static void Test(object? x) {}
}
", new[] { core.ToMetadataReference() }, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (6,17): warning CS0219: The variable 'x' is assigned but its value is never used
    //         object? x = null;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(6, 17)
                );
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_01()
        {
            var source = @"
class A
{
    public virtual T? Foo<T>() where T : struct 
    { 
        return null; 
    }
}

class B : A
{
    public override T? Foo<T>()
    {
        return null;
    }
} 
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            //var a = compilation.GetTypeByMetadataName("A");
            //var aFoo = a.GetMember<MethodSymbol>("Foo");
            //Assert.Equal("T? A.Foo<T>()", aFoo.ToTestDisplayString());

            //var b = compilation.GetTypeByMetadataName("B");
            //var bFoo = b.GetMember<MethodSymbol>("Foo");
            //Assert.Equal("T? A.Foo<T>()", bFoo.OverriddenMethod.ToTestDisplayString());

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_02()
        {
            var source = @"
class A
{
    public virtual void Foo<T>(T? x) where T : struct 
    { 
    }
}

class B : A
{
    public override void Foo<T>(T? x)
    {
    }
} 
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_03()
        {
            var source = @"
class A
{
    public virtual System.Nullable<T> Foo<T>() where T : struct 
    { 
        return null; 
    }
}

class B : A
{
    public override T? Foo<T>()
    {
        return null;
    }
} 
";
            CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true")).VerifyDiagnostics();
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_04()
        {
            var source = @"
class A
{
    public virtual void Foo<T>(System.Nullable<T> x) where T : struct 
    { 
    }
}

class B : A
{
    public override void Foo<T>(T? x)
    {
    }
} 
";
            CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true")).VerifyDiagnostics();
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_05()
        {
            var source = @"
class A
{
    public virtual T? Foo<T>() where T : struct 
    { 
        return null; 
    }
}

class B : A
{
    public override System.Nullable<T> Foo<T>()
    {
        return null;
    }
} 
";
            CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true")).VerifyDiagnostics();
        }

        [Fact]
        public void InheritedValueConstraintForNullable1_06()
        {
            var source = @"
class A
{
    public virtual void Foo<T>(T? x) where T : struct 
    { 
    }
}

class B : A
{
    public override void Foo<T>(System.Nullable<T> x)
    {
    }
} 
";
            CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true")).VerifyDiagnostics();
        }

        [Fact]
        public void InheritedClassConstraintForNullable1_01()
        {
            var source = @"
class A
{
    public virtual T? Foo<T>() where T : class 
    { 
        return null; 
    }
}

class B : A
{
    public override T? Foo<T>()
    {
        return null;
    }
} 
";
            CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true")).VerifyDiagnostics();
        }

        [Fact(Skip = "Unexpected errors!")]
        public void InheritedClassConstraintForNullable1_02()
        {
            var source = @"
class A
{
    public virtual void Foo<T>(T? x) where T : class 
    { 
    }

    void Test1(string? x1) {}
    void Test1(string x2) {}

    string Test2(string y1) { return y1; }
    string? Test2(string y2) { return y2; }
}

class B : A
{
    public override void Foo<T>(T? x)
    {
    }
} 
";
            CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true")).VerifyDiagnostics();
        }

        [Fact()]
        public void Test1()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void Test1()
    {
        string? x1 = null;
        string? y1 = x1; 
        string z1 = x1; 
    }

    void Test2()
    {
        string? x2 = """";
        string z2 = x2; 
    }

    void Test3()
    {
        string? x3;
        string z3 = x3; 
    }

    void Test4()
    {
        string x4;
        string z4 = x4; 
    }

    void Test5()
    {
        string? x5 = """";
        x5 = null;
        string? y5;
        y5 = x5; 
        string z5;
        z5 = x5; 
    }

    void Test6()
    {
        string? x6 = """";
        string z6;
        z6 = x6; 
    }

    void Test7()
    {
        CL1? x7 = null;
        CL1 y7 = x7.P1; 
        CL1 z7 = x7?.P1;
        x7 = new CL1();
        CL1 u7 = x7.P1; 
    }

    void Test8()
    {
        CL1? x8 = new CL1();
        CL1 y8 = x8.M1(); 
        x8 = null;
        CL1 u8 = x8.M1(); 
        CL1 z8 = x8?.M1();
    }

    void Test9(CL1? x9, CL1 y9)
    {
        CL1 u9; 
        u9 = x9;
        u9 = y9;
        x9 = y9;
        CL1 v9; 
        v9 = x9;
        y9 = null;
    }

    void Test10(CL1 x10)
    {
        CL1 u10; 
        u10 = x10.P1;
        u10 = x10.P2;
        u10 = x10.M1();
        u10 = x10.M2();
        CL1? v10;
        v10 = x10.P2;
        v10 = x10.M2();
    }

    void Test11(CL1 x11, CL1? y11)
    {
        CL1 u11; 
        u11 = x11.F1;
        u11 = x11.F2;
        CL1? v11;
        v11 = x11.F2;
        x11.F2 = x11.F1;
        u11 = x11.F2;

        v11 = y11.F1;
    }

    void Test12(CL1 x12)
    {
        S1 y12;
        CL1 u12; 
        u12 = y12.F3;
        u12 = y12.F4;
    }

    void Test13(CL1 x13)
    {
        S1 y13;
        CL1? u13; 
        u13 = y13.F3;
        u13 = y13.F4;
    }

    void Test14(CL1 x14)
    {
        S1 y14;
        y14.F3 = null;
        y14.F4 = null;
        y14.F3 = x14;
        y14.F4 = x14;
    }

    void Test15(CL1 x15)
    {
        S1 y15;
        CL1 u15; 
        y15.F3 = null;
        y15.F4 = null;
        u15 = y15.F3;
        u15 = y15.F4;

        CL1? v15;
        v15 = y15.F4;
        y15.F4 = x15;
        u15 = y15.F4;
    }

    void Test16()
    {
        S1 y16;
        CL1 u16; 
        y16 = new S1();
        u16 = y16.F3;
        u16 = y16.F4;
    }

    void Test17(CL1 z17)
    {
        S1 x17;
        x17.F4 = z17;
        S1 y17 = new S1();
        CL1 u17; 
        u17 = y17.F4;

        y17 = x17;
        CL1 v17; 
        v17 = y17.F4;
    }

    void Test18(CL1 z18)
    {
        S1 x18;
        x18.F4 = z18;
        S1 y18 = x18;
        CL1 u18; 
        u18 = y18.F4;
    }

    void Test19(S1 x19, CL1 z19)
    {
        S1 y19;
        y19.F4 = null; 
        CL1 u19; 
        u19 = y19.F4;

        x19.F4 = z19;
        y19 = x19;
        CL1 v19;
        v19 = y19.F4;
    }

    void Test20(S1 x20, CL1 z20)
    {
        S1 y20;
        y20.F4 = z20;
        CL1 u20; 
        u20 = y20.F4;

        y20 = x20;
        CL1 v20;
        v20 = y20.F4;
    }

    S1 GetS1()
    {
        return new S1();
    }
    void Test21(CL1 z21)
    {
        S1 y21;
        y21.F4 = z21;
        CL1 u21; 
        u21 = y21.F4;

        y21 = GetS1();
        CL1 v21;
        v21 = y21.F4;
    }

    void Test22()
    {
        S1 y22;
        CL1 u22; 
        u22 = y22.F4;

        y22 = GetS1();
        CL1 v22;
        v22 = y22.F4;
    }

    void Test23(CL1 z23)
    {
        S2 y23;
        y23.F5.F4 = z23;
        CL1 u23; 
        u23 = y23.F5.F4;

        y23 = GetS2();
        CL1 v23;
        v23 = y23.F5.F4;
    }

    S2 GetS2()
    {
        return new S2();
    }

    void Test24()
    {
        S2 y24;
        CL1 u24; 
        u24 = y24.F5.F4; // 1
        u24 = y24.F5.F4; // 2

        y24 = GetS2();
        CL1 v24;
        v24 = y24.F5.F4;
    }

    void Test25(CL1 z25)
    {
        S2 y25;
        S2 x25 = GetS2();
        x25.F5.F4 = z25;
        y25 = x25;
        CL1 v25;
        v25 = y25.F5.F4;
    }

    void Test26(CL1 x26, CL1? y26, CL1 z26)
    {
        x26.P1 = y26;
        x26.P1 = z26;
    }

    void Test27(CL1 x27, CL1? y27, CL1 z27)
    {
        x27[x27] = y27;
        x27[x27] = z27;
    }

    void Test28(CL1 x28, CL1? y28, CL1 z28)
    {
        x28[y28] = z28;
    }

    void Test29(CL1 x29, CL1 y29, CL1 z29)
    {
        z29 = x29[y29];
        z29 = x29[1];
    }

    void Test30(CL1? x30, CL1 y30, CL1 z30)
    {
        z30 = x30[y30];
    }

    void Test31(CL1 x31)
    {
        x31 = default(CL1);
    }

    void Test32(CL1 x32)
    {
        var y32 = new CL1() ?? x32;
    }

    void Test33(object x33)
    {
        var y33 = new { p = (object)null } ?? x33;
    }
}

class CL1
{
    public CL1()
    {
        F1 = this;
    }

    public CL1 F1;
    public CL1? F2;

    public CL1 P1 { get; set; }
    public CL1? P2 { get; set; }

    public CL1 M1() { return new CL1(); }
    public CL1? M2() { return null; }

    public CL1 this[CL1 x]
    {
        get { return x; }
        set { }
    }

    public CL1? this[int x]
    {
        get { return null; }
        set { }
    }
}

struct S1
{
    public CL1 F3;
    public CL1? F4;
}

struct S2
{
    public S1 F5;
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (12,21): warning CS8201: Possible null reference assignment.
    //         string z1 = x1; 
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1").WithLocation(12, 21),
    // (24,21): error CS0165: Use of unassigned local variable 'x3'
    //         string z3 = x3; 
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x3").WithArguments("x3").WithLocation(24, 21),
    // (30,21): error CS0165: Use of unassigned local variable 'x4'
    //         string z4 = x4; 
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x4").WithArguments("x4").WithLocation(30, 21),
    // (40,14): warning CS8201: Possible null reference assignment.
    //         z5 = x5; 
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x5").WithLocation(40, 14),
    // (53,18): warning CS8202: Possible dereference of a null reference.
    //         CL1 y7 = x7.P1; 
    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x7").WithLocation(53, 18),
    // (54,18): warning CS8201: Possible null reference assignment.
    //         CL1 z7 = x7?.P1;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x7?.P1").WithLocation(54, 18),
    // (64,18): warning CS8202: Possible dereference of a null reference.
    //         CL1 u8 = x8.M1(); 
    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x8").WithLocation(64, 18),
    // (65,18): warning CS8201: Possible null reference assignment.
    //         CL1 z8 = x8?.M1();
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x8?.M1()").WithLocation(65, 18),
    // (71,14): warning CS8201: Possible null reference assignment.
    //         u9 = x9;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x9").WithLocation(71, 14),
    // (76,14): warning CS8201: Possible null reference assignment.
    //         y9 = null;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "null").WithLocation(76, 14),
    // (83,15): warning CS8201: Possible null reference assignment.
    //         u10 = x10.P2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x10.P2").WithLocation(83, 15),
    // (85,15): warning CS8201: Possible null reference assignment.
    //         u10 = x10.M2();
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x10.M2()").WithLocation(85, 15),
    // (95,15): warning CS8201: Possible null reference assignment.
    //         u11 = x11.F2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x11.F2").WithLocation(95, 15),
    // (99,15): warning CS8201: Possible null reference assignment.
    //         u11 = x11.F2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x11.F2").WithLocation(99, 15),
    // (101,15): warning CS8202: Possible dereference of a null reference.
    //         v11 = y11.F1;
    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "y11").WithLocation(101, 15),
    // (108,15): error CS0170: Use of possibly unassigned field 'F3'
    //         u12 = y12.F3;
    Diagnostic(ErrorCode.ERR_UseDefViolationField, "y12.F3").WithArguments("F3").WithLocation(108, 15),
    // (109,15): error CS0170: Use of possibly unassigned field 'F4'
    //         u12 = y12.F4;
    Diagnostic(ErrorCode.ERR_UseDefViolationField, "y12.F4").WithArguments("F4").WithLocation(109, 15),
    // (116,15): error CS0170: Use of possibly unassigned field 'F3'
    //         u13 = y13.F3;
    Diagnostic(ErrorCode.ERR_UseDefViolationField, "y13.F3").WithArguments("F3").WithLocation(116, 15),
    // (117,15): error CS0170: Use of possibly unassigned field 'F4'
    //         u13 = y13.F4;
    Diagnostic(ErrorCode.ERR_UseDefViolationField, "y13.F4").WithArguments("F4").WithLocation(117, 15),
    // (123,18): warning CS8201: Possible null reference assignment.
    //         y14.F3 = null;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "null").WithLocation(123, 18),
    // (133,18): warning CS8201: Possible null reference assignment.
    //         y15.F3 = null;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "null").WithLocation(133, 18),
    // (136,15): warning CS8201: Possible null reference assignment.
    //         u15 = y15.F4;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y15.F4").WithLocation(136, 15),
    // (150,15): warning CS8201: Possible null reference assignment.
    //         u16 = y16.F4;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y16.F4").WithLocation(150, 15),
    // (159,15): warning CS8201: Possible null reference assignment.
    //         u17 = y17.F4;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y17.F4").WithLocation(159, 15),
    // (161,15): error CS0165: Use of unassigned local variable 'x17'
    //         y17 = x17;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x17").WithArguments("x17").WithLocation(161, 15),
    // (170,18): error CS0165: Use of unassigned local variable 'x18'
    //         S1 y18 = x18;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x18").WithArguments("x18").WithLocation(170, 18),
    // (180,15): warning CS8201: Possible null reference assignment.
    //         u19 = y19.F4;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y19.F4").WithLocation(180, 15),
    // (197,15): warning CS8201: Possible null reference assignment.
    //         v20 = y20.F4;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y20.F4").WithLocation(197, 15),
    // (213,15): warning CS8201: Possible null reference assignment.
    //         v21 = y21.F4;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y21.F4").WithLocation(213, 15),
    // (220,15): error CS0170: Use of possibly unassigned field 'F4'
    //         u22 = y22.F4;
    Diagnostic(ErrorCode.ERR_UseDefViolationField, "y22.F4").WithArguments("F4").WithLocation(220, 15),
    // (224,15): warning CS8201: Possible null reference assignment.
    //         v22 = y22.F4;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y22.F4").WithLocation(224, 15),
    // (236,15): warning CS8201: Possible null reference assignment.
    //         v23 = y23.F5.F4;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y23.F5.F4").WithLocation(236, 15),
    // (248,15): error CS0170: Use of possibly unassigned field 'F4'
    //         u24 = y24.F5.F4; // 1
    Diagnostic(ErrorCode.ERR_UseDefViolationField, "y24.F5.F4").WithArguments("F4").WithLocation(248, 15),
    // (253,15): warning CS8201: Possible null reference assignment.
    //         v24 = y24.F5.F4;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y24.F5.F4").WithLocation(253, 15),
    // (268,18): warning CS8201: Possible null reference assignment.
    //         x26.P1 = y26;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y26").WithLocation(268, 18),
    // (274,20): warning CS8201: Possible null reference assignment.
    //         x27[x27] = y27;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y27").WithLocation(274, 20),
    // (280,13): warning CS8204: Possible null reference argument.
    //         x28[y28] = z28;
    Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y28").WithLocation(280, 13),
    // (286,15): warning CS8201: Possible null reference assignment.
    //         z29 = x29[1];
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x29[1]").WithLocation(286, 15),
    // (291,15): warning CS8202: Possible dereference of a null reference.
    //         z30 = x30[y30];
    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x30").WithLocation(291, 15),
    // (296,15): warning CS8201: Possible null reference assignment.
    //         x31 = default(CL1);
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "default(CL1)").WithLocation(296, 15),
    // (301,19): warning CS8207: Expression is probably never null.
    //         var y32 = new CL1() ?? x32;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "new CL1()").WithLocation(301, 19),
    // (306,19): warning CS8207: Expression is probably never null.
    //         var y33 = new { p = (object)null } ?? x33;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "new { p = (object)null }").WithLocation(306, 19)
                );
        }

        [Fact]
        public void PassingParameters_1()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void M1(CL1 p) {}

    void Test1(CL1? x1, CL1 y1)
    {
        M1(x1);
        M1(y1);
    }

    void Test2()
    {
        CL1? x2;
        M1(x2);
    }

    void M2(ref CL1? p) {}

    void Test3()
    {
        CL1 x3;
        M2(ref x3);
    }

    void Test4(CL1 x4)
    {
        M2(ref x4);
    }

    void M3(out CL1? p) { p = null; }

    void Test5()
    {
        CL1 x5;
        M3(out x5);
    }

    void M4(ref CL1 p) {}

    void Test6()
    {
        CL1? x6 = null;
        M4(ref x6);
    }

    void M5(out CL1 p) { p = new CL1(); }

    void Test7()
    {
        CL1? x7 = null;
        CL1 u7 = x7;
        M5(out x7);
        CL1 v7 = x7;
    }

    void M6(CL1 p1, CL1? p2) {}

    void Test8(CL1? x8, CL1? y8)
    {
        M6(p2: x8, p1: y8);
    }

    void M7(params CL1[] p1) {}

    void Test9(CL1 x9, CL1? y9)
    {
        M7(x9, y9);
    }

    void Test10(CL1? x10, CL1 y10)
    {
        M7(x10, y10);
    }

    void M8(CL1 p1, params CL1[] p2) {}

    void Test11(CL1? x11, CL1 y11, CL1? z11)
    {
        M8(x11, y11, z11);
    }

    void Test12(CL1? x12, CL1 y12)
    {
        M8(p2: x12, p1: y12);
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (12,12): warning CS8204: Possible null reference argument.
    //         M1(x1);
    Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x1").WithLocation(12, 12),
    // (19,12): error CS0165: Use of unassigned local variable 'x2'
    //         M1(x2);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x2").WithArguments("x2").WithLocation(19, 12),
    // (27,16): error CS0165: Use of unassigned local variable 'x3'
    //         M2(ref x3);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x3").WithArguments("x3").WithLocation(27, 16),
    // (27,16): warning CS8201: Possible null reference assignment.
    //         M2(ref x3);
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3").WithLocation(27, 16),
    // (32,16): warning CS8201: Possible null reference assignment.
    //         M2(ref x4);
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x4").WithLocation(32, 16),
    // (40,16): warning CS8201: Possible null reference assignment.
    //         M3(out x5);
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x5").WithLocation(40, 16),
    // (48,16): warning CS8204: Possible null reference argument.
    //         M4(ref x6);
    Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x6").WithLocation(48, 16),
    // (56,18): warning CS8201: Possible null reference assignment.
    //         CL1 u7 = x7;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x7").WithLocation(56, 18),
    // (65,24): warning CS8204: Possible null reference argument.
    //         M6(p2: x8, p1: y8);
    Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y8").WithLocation(65, 24),
    // (72,16): warning CS8204: Possible null reference argument.
    //         M7(x9, y9);
    Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y9").WithLocation(72, 16),
    // (77,12): warning CS8204: Possible null reference argument.
    //         M7(x10, y10);
    Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x10").WithLocation(77, 12),
    // (84,12): warning CS8204: Possible null reference argument.
    //         M8(x11, y11, z11);
    Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x11").WithLocation(84, 12),
    // (84,22): warning CS8204: Possible null reference argument.
    //         M8(x11, y11, z11);
    Diagnostic(ErrorCode.WRN_NullReferenceArgument, "z11").WithLocation(84, 22),
    // (89,16): warning CS8204: Possible null reference argument.
    //         M8(p2: x12, p1: y12);
    Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x12").WithLocation(89, 16)
                );
        }

        [Fact]
        public void RefOutParameters_01()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void Test1(ref CL1 x1, CL1 y1)
    {
        y1 = x1;
    }

    void Test2(ref CL1? x2, CL1 y2)
    {
        y2 = x2;
    }

    void Test3(ref CL1? x3, CL1 y3)
    {
        x3 = y3;
        y3 = x3;
    }

    void Test4(out CL1 x4, CL1 y4)
    {
        y4 = x4;
        x4 = y4;
    }

    void Test5(out CL1? x5, CL1 y5)
    {
        y5 = x5;
        x5 = y5;
    }

    void Test6(out CL1? x6, CL1 y6)
    {
        x6 = y6;
        y6 = x6;
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (15,14): warning CS8201: Possible null reference assignment.
    //         y2 = x2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2").WithLocation(15, 14),
    // (21,14): warning CS8201: Possible null reference assignment.
    //         y3 = x3;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3").WithLocation(21, 14),
    // (26,14): error CS0269: Use of unassigned out parameter 'x4'
    //         y4 = x4;
    Diagnostic(ErrorCode.ERR_UseDefViolationOut, "x4").WithArguments("x4").WithLocation(26, 14),
    // (32,14): error CS0269: Use of unassigned out parameter 'x5'
    //         y5 = x5;
    Diagnostic(ErrorCode.ERR_UseDefViolationOut, "x5").WithArguments("x5").WithLocation(32, 14),
    // (39,14): warning CS8201: Possible null reference assignment.
    //         y6 = x6;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x6").WithLocation(39, 14)
                );
        }

        [Fact]
        public void RefOutParameters_02()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void Test1(ref S1 x1, CL1 y1)
    {
        y1 = x1.F1;
    }

    void Test2(ref S1 x2, CL1 y2)
    {
        y2 = x2.F2;
    }

    void Test3(ref S1 x3, CL1 y3)
    {
        x3.F2 = y3;
        y3 = x3.F2;
    }

    void Test4(out S1 x4, CL1 y4)
    {
        y4 = x4.F1;
        x4.F1 = y4;
        x4.F2 = y4;
    }

    void Test5(out S1 x5, CL1 y5)
    {
        y5 = x5.F2;
        x5.F1 = y5;
        x5.F2 = y5;
    }

    void Test6(out S1 x6, CL1 y6)
    {
        x6.F1 = y6;
        x6.F2 = y6;
        y6 = x6.F2;
    }
}

class CL1
{
}

struct S1
{
    public CL1 F1;
    public CL1? F2;
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (15,14): warning CS8201: Possible null reference assignment.
    //         y2 = x2.F2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2.F2").WithLocation(15, 14),
    // (21,14): warning CS8201: Possible null reference assignment.
    //         y3 = x3.F2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3.F2").WithLocation(21, 14),
    // (26,14): error CS0170: Use of possibly unassigned field 'F1'
    //         y4 = x4.F1;
    Diagnostic(ErrorCode.ERR_UseDefViolationField, "x4.F1").WithArguments("F1").WithLocation(26, 14),
    // (33,14): error CS0170: Use of possibly unassigned field 'F2'
    //         y5 = x5.F2;
    Diagnostic(ErrorCode.ERR_UseDefViolationField, "x5.F2").WithArguments("F2").WithLocation(33, 14),
    // (42,14): warning CS8201: Possible null reference assignment.
    //         y6 = x6.F2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x6.F2").WithLocation(42, 14)
                );
        }

        [Fact]
        public void RefOutParameters_03()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void Test3(ref S1 x3, CL1 y3)
    {
        S1 z3;
        z3.F1 = y3;
        z3.F2 = y3;
        x3 = z3;
        y3 = x3.F2;
    }

    void Test6(out S1 x6, CL1 y6)
    {
        S1 z6;
        z6.F1 = y6;
        z6.F2 = y6;
        x6 = z6;
        y6 = x6.F2;
    }
}

class CL1
{
}

struct S1
{
    public CL1 F1;
    public CL1? F2;
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (14,14): warning CS8201: Possible null reference assignment.
    //         y3 = x3.F2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3.F2").WithLocation(14, 14),
    // (23,14): warning CS8201: Possible null reference assignment.
    //         y6 = x6.F2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x6.F2").WithLocation(23, 14)
                );
        }

        [Fact]
        public void TargetingUnannotatedAPIs()
        {
            CSharpCompilation c0 = CreateCompilationWithMscorlib45(@"
public class CL0 
{
    public object F1;

    public object P1 { get; set;}

    public object this[object x]
    {
        get { return null; }
        set { }
    }

    public S1 M1() { return new S1(); }
}

public struct S1
{
    public CL0 F1;
}
", options: TestOptions.DebugDll);

            CSharpCompilation c = CreateCompilationWithMscorlib45(@"
class C 
{
    static void Main()
    {
    }

    bool Test1(string? x1, string y1)
    {
        return string.Equals(x1, y1);
    }

    object Test2(ref object? x2, object? y2)
    {
        System.Threading.Interlocked.Exchange(ref x2, y2);
        return x2 ?? new object(); 
    }    

    object Test3(ref object? x3, object? y3)
    {
        return System.Threading.Interlocked.Exchange(ref x3, y3) ?? new object(); 
    }    

    object Test4(System.Delegate x4)
    {
        return x4.Target ?? new object(); 
    }    

    object Test5(CL0 x5)
    {
        return x5.F1 ?? new object(); 
    }    

    void Test6(CL0 x6, object? y6)
    {
        x6.F1 = y6;
    }    

    void Test7(CL0 x7, object? y7)
    {
        x7.P1 = y7;
    }    

    void Test8(CL0 x8, object? y8, object? z8)
    {
        x8[y8] = z8;
    }    

    object Test9(CL0 x9)
    {
        return x9[1] ?? new object(); 
    }    

    object Test10(CL0 x10)
    {
        return x10.M1().F1 ?? new object(); 
    }    

    object Test11(CL0 x11, CL0? z11)
    {
        S1 y11 = x11.M1();
        y11.F1 = z11;
        return y11.F1; 
    }    

    object Test12(CL0 x12)
    {
        S1 y12 = x12.M1();
        y12.F1 = x12;
        return y12.F1 ?? new object(); 
    }    

    void Test13(CL0 x13, object? y13)
    {
        y13 = x13.F1;
        object z13 = y13;
        z13 = y13 ?? new object();
    }    

    void Test14(CL0 x14)
    {
        object? y14 = x14.F1;
        object z14 = y14;
        z14 = y14 ?? new object();
    }    

    void Test15(CL0 x15)
    {
        S2 y15;
        y15.F2 = x15.F1;
        object z15 = y15.F2;
        z15 = y15.F2 ?? new object();
    }    

    struct Test16
    {
        object? y16 {get;}

        public Test16(CL0 x16)
        {
            y16 = x16.F1;
            object z16 = y16;
            z16 = y16 ?? new object();
        }    
    }

    void Test17(CL0 x17)
    {
        var y17 = new { F2 = x17.F1 };
        object z17 = y17.F2;
        z17 = y17.F2 ?? new object();
    }    
}

public struct S2
{
    public object? F2;
}

", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"), references: new[] { c0.EmitToImageReference() });

            c.VerifyDiagnostics(
    // (63,16): warning CS8203: Possible null reference return.
    //         return y11.F1; 
    Diagnostic(ErrorCode.WRN_NullReferenceReturn, "y11.F1").WithLocation(63, 16),
    // (70,16): warning CS8207: Expression is probably never null.
    //         return y12.F1 ?? new object(); 
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "y12.F1").WithLocation(70, 16)
                );
        }

        [Fact]
        public void ReturningValues()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    CL1 Test1(CL1? x1)
    {
        return x1;
    }

    CL1? Test2(CL1? x2)
    {
        return x2;
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (10,16): warning CS8203: Possible null reference return.
    //         return x1;
    Diagnostic(ErrorCode.WRN_NullReferenceReturn, "x1").WithLocation(10, 16)
                );
        }

        [Fact]
        public void ConditionalBranching_01()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1 x1, CL1? y1, CL1 z1)
    {
        if (y1 != null)
        {
            x1 = y1;
        }
        else
        {
            z1 = y1;
        }
    }

    void Test2(CL1 x2, CL1? y2, CL1 z2)
    {
        if (y2 == null)
        {
            x2 = y2;
        }
        else
        {
            z2 = y2;
        }
    }

    void Test3(CL2 x3, CL2? y3, CL2 z3)
    {
        if (y3 != null)
        {
            x3 = y3;
        }
        else
        {
            z3 = y3;
        }
    }

    void Test4(CL2 x4, CL2? y4, CL2 z4)
    {
        if (y4 == null)
        {
            x4 = y4;
        }
        else
        {
            z4 = y4;
        }
    }

    void Test5(CL1 x5, CL1 y5, CL1 z5)
    {
        if (y5 != null)
        {
            x5 = y5;
        }
        else
        {
            z5 = y5;
        }
    }
}

class CL1
{
}

class CL2
{
    public static bool operator == (CL2 x, CL2 y) { return false; }
    public static bool operator != (CL2 x, CL2 y) { return false; }
    public override bool Equals(object obj) { return false; }
    public override int GetHashCode() { return 0; }
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (16,18): warning CS8201: Possible null reference assignment.
    //             z1 = y1;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y1").WithLocation(16, 18),
    // (24,18): warning CS8201: Possible null reference assignment.
    //             x2 = y2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y2").WithLocation(24, 18),
    // (40,18): warning CS8201: Possible null reference assignment.
    //             z3 = y3;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y3").WithLocation(40, 18),
    // (48,18): warning CS8201: Possible null reference assignment.
    //             x4 = y4;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y4").WithLocation(48, 18),
    // (58,13): warning CS8205: Result of the comparison is possibly always true.
    //         if (y5 != null)
    Diagnostic(ErrorCode.WRN_NullCheckIsProbablyAlwaysTrue, "y5 != null").WithLocation(58, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_02()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1 x1, CL1? y1, CL1 z1)
    {
        if (null != y1)
        {
            x1 = y1;
        }
        else
        {
            z1 = y1;
        }
    }

    void Test2(CL1 x2, CL1? y2, CL1 z2)
    {
        if (null == y2)
        {
            x2 = y2;
        }
        else
        {
            z2 = y2;
        }
    }

    void Test3(CL2 x3, CL2? y3, CL2 z3)
    {
        if (null != y3)
        {
            x3 = y3;
        }
        else
        {
            z3 = y3;
        }
    }

    void Test4(CL2 x4, CL2? y4, CL2 z4)
    {
        if (null == y4)
        {
            x4 = y4;
        }
        else
        {
            z4 = y4;
        }
    }

    void Test5(CL1 x5, CL1 y5, CL1 z5)
    {
        if (null == y5)
        {
            x5 = y5;
        }
        else
        {
            z5 = y5;
        }
    }
}

class CL1
{
}

class CL2
{
    public static bool operator == (CL2 x, CL2 y) { return false; }
    public static bool operator != (CL2 x, CL2 y) { return false; }
    public override bool Equals(object obj) { return false; }
    public override int GetHashCode() { return 0; }
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (16,18): warning CS8201: Possible null reference assignment.
    //             z1 = y1;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y1").WithLocation(16, 18),
    // (24,18): warning CS8201: Possible null reference assignment.
    //             x2 = y2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y2").WithLocation(24, 18),
    // (40,18): warning CS8201: Possible null reference assignment.
    //             z3 = y3;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y3").WithLocation(40, 18),
    // (48,18): warning CS8201: Possible null reference assignment.
    //             x4 = y4;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y4").WithLocation(48, 18),
    // (58,13): warning CS8206: Result of the comparison is possibly always false.
    //         if (null == y5)
    Diagnostic(ErrorCode.WRN_NullCheckIsProbablyAlwaysFalse, "null == y5").WithLocation(58, 13)
                );
        }

        [Fact]
        public void ConditionalBranching_03()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1 x1, CL1? y1, CL1 z1, bool u1)
    {
        if (null != y1 || u1)
        {
            x1 = y1;
        }
        else
        {
            z1 = y1;
        }
    }

    void Test2(CL1 x2, CL1? y2, CL1 z2, bool u2)
    {
        if (y2 != null && u2)
        {
            x2 = y2;
        }
        else
        {
            z2 = y2;
        }
    }

    bool Test3(CL1? x3)
    {
        return x3.M1();
    }

    bool Test4(CL1? x4)
    {
        return x4 != null && x4.M1();
    }

    bool Test5(CL1? x5)
    {
        return x5 == null && x5.M1();
    }
}

class CL1
{
    public bool M1() { return true; }
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (12,18): warning CS8201: Possible null reference assignment.
    //             x1 = y1;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y1").WithLocation(12, 18),
    // (16,18): warning CS8201: Possible null reference assignment.
    //             z1 = y1;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y1").WithLocation(16, 18),
    // (28,18): warning CS8201: Possible null reference assignment.
    //             z2 = y2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y2").WithLocation(28, 18),
    // (34,16): warning CS8202: Possible dereference of a null reference.
    //         return x3.M1();
    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x3").WithLocation(34, 16),
    // (44,30): warning CS8202: Possible dereference of a null reference.
    //         return x5 == null && x5.M1();
    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x5").WithLocation(44, 30)
                );
        }

        [Fact]
        public void ConditionalBranching_04()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1 x1, CL1? y1)
    {
        CL1 z1 = y1 ?? x1;
    }

    void Test2(CL1? x2, CL1? y2)
    {
        CL1 z2 = y2 ?? x2;
    }

    void Test3(CL1 x3, CL1? y3)
    {
        CL1 z3 = x3 ?? y3;
    }

    void Test4(CL1? x4, CL1 y4)
    {
        x4 = y4;
        CL1 z4 = x4 ?? x4.M1();
    }

    void Test5(CL1 x5)
    {
        const CL1? y5 = null;
        CL1 z5 = y5 ?? x5;
    }

    void Test6(CL1 x6)
    {
        const string? y6 = """";
        string z6 = y6 ?? x6.M2();
    }
}

class CL1
{
    public CL1 M1() { return new CL1(); }
    public string? M2() { return null; }
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (15,18): warning CS8201: Possible null reference assignment.
    //         CL1 z2 = y2 ?? x2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y2 ?? x2").WithLocation(15, 18),
    // (20,18): warning CS8207: Expression is probably never null.
    //         CL1 z3 = x3 ?? y3;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "x3").WithLocation(20, 18),
    // (26,18): warning CS8207: Expression is probably never null.
    //         CL1 z4 = x4 ?? x4.M1();
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "x4").WithLocation(26, 18),
    // (26,24): warning CS8202: Possible dereference of a null reference.
    //         CL1 z4 = x4 ?? x4.M1();
    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x4").WithLocation(26, 24)
                );
        }

        [Fact]
        public void ConditionalBranching_05()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1? x1)
    {
        CL1 z1 = x1?.M1();
    }

    void Test2(CL1? x2, CL1 y2)
    {
        x2 = y2;
        CL1 z2 = x2?.M1();
    }

    void Test3(CL1? x3, CL1 y3)
    {
        x3 = y3;
        CL1 z3 = x3?.M2();
    }

    void Test4(CL1? x4)
    {
        x4?.M3(x4);
    }
}

class CL1
{
    public CL1 M1() { return new CL1(); }
    public CL1? M2() { return null; }
    public void M3(CL1 x) { }
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (10,18): warning CS8201: Possible null reference assignment.
    //         CL1 z1 = x1?.M1();
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1?.M1()").WithLocation(10, 18),
    // (16,18): warning CS8207: Expression is probably never null.
    //         CL1 z2 = x2?.M1();
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "x2").WithLocation(16, 18),
    // (22,18): warning CS8207: Expression is probably never null.
    //         CL1 z3 = x3?.M2();
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "x3").WithLocation(22, 18),
    // (22,18): warning CS8201: Possible null reference assignment.
    //         CL1 z3 = x3?.M2();
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3?.M2()").WithLocation(22, 18)
                );
        }

        [Fact]
        public void ConditionalBranching_06()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1 x1, CL1? y1)
    {
        CL1 z1 = y1 != null ? y1 : x1;
    }

    void Test2(CL1? x2, CL1? y2)
    {
        CL1 z2 = y2 != null ? y2 : x2;
    }

    void Test3(CL1 x3, CL1? y3)
    {
        CL1 z3 = x3 != null ? x3 : y3;
    }

    void Test4(CL1? x4, CL1 y4)
    {
        x4 = y4;
        CL1 z4 = x4 != null ? x4 : x4.M1();
    }

    void Test5(CL1 x5)
    {
        const CL1? y5 = null;
        CL1 z5 = y5 != null ? y5 : x5;
    }

    void Test6(CL1 x6)
    {
        const string? y6 = """";
        string z6 = y6 != null ? y6 : x6.M2();
    }
}

class CL1
{
    public CL1 M1() { return new CL1(); }
    public string? M2() { return null; }
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (15,18): warning CS8201: Possible null reference assignment.
    //         CL1 z2 = y2 != null ? y2 : x2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y2 != null ? y2 : x2").WithLocation(15, 18),
    // (20,18): warning CS8205: Result of the comparison is possibly always true.
    //         CL1 z3 = x3 != null ? x3 : y3;
    Diagnostic(ErrorCode.WRN_NullCheckIsProbablyAlwaysTrue, "x3 != null").WithLocation(20, 18),
    // (20,18): warning CS8201: Possible null reference assignment.
    //         CL1 z3 = x3 != null ? x3 : y3;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3 != null ? x3 : y3").WithLocation(20, 18),
    // (26,18): warning CS8205: Result of the comparison is possibly always true.
    //         CL1 z4 = x4 != null ? x4 : x4.M1();
    Diagnostic(ErrorCode.WRN_NullCheckIsProbablyAlwaysTrue, "x4 != null").WithLocation(26, 18),
    // (26,36): warning CS8202: Possible dereference of a null reference.
    //         CL1 z4 = x4 != null ? x4 : x4.M1();
    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x4").WithLocation(26, 36),
    // (38,21): warning CS8205: Result of the comparison is possibly always true.
    //         string z6 = y6 != null ? y6 : x6.M2();
    Diagnostic(ErrorCode.WRN_NullCheckIsProbablyAlwaysTrue, "y6 != null").WithLocation(38, 21),
    // (38,21): warning CS8201: Possible null reference assignment.
    //         string z6 = y6 != null ? y6 : x6.M2();
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y6 != null ? y6 : x6.M2()").WithLocation(38, 21)
                );
        }

        [Fact]
        public void ConditionalBranching_07()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1 x1, CL1? y1)
    {
        CL1 z1 = y1 == null ? x1 : y1;
    }

    void Test2(CL1? x2, CL1? y2)
    {
        CL1 z2 = y2 == null ? x2 : y2;
    }

    void Test3(CL1 x3, CL1? y3)
    {
        CL1 z3 = x3 == null ? y3 : x3;
    }

    void Test4(CL1? x4, CL1 y4)
    {
        x4 = y4;
        CL1 z4 = x4 == null ? x4.M1() : x4;
    }

    void Test5(CL1 x5)
    {
        const CL1? y5 = null;
        CL1 z5 = y5 == null ? x5 : y5;
    }

    void Test6(CL1 x6)
    {
        const string? y6 = """";
        string z6 = y6 == null ? x6.M2() : y6;
    }
}

class CL1
{
    public CL1 M1() { return new CL1(); }
    public string? M2() { return null; }
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (15,18): warning CS8201: Possible null reference assignment.
    //         CL1 z2 = y2 == null ? x2 : y2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y2 == null ? x2 : y2").WithLocation(15, 18),
    // (20,18): warning CS8206: Result of the comparison is possibly always false.
    //         CL1 z3 = x3 == null ? y3 : x3;
    Diagnostic(ErrorCode.WRN_NullCheckIsProbablyAlwaysFalse, "x3 == null").WithLocation(20, 18),
    // (20,18): warning CS8201: Possible null reference assignment.
    //         CL1 z3 = x3 == null ? y3 : x3;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x3 == null ? y3 : x3").WithLocation(20, 18),
    // (26,18): warning CS8206: Result of the comparison is possibly always false.
    //         CL1 z4 = x4 == null ? x4.M1() : x4;
    Diagnostic(ErrorCode.WRN_NullCheckIsProbablyAlwaysFalse, "x4 == null").WithLocation(26, 18),
    // (26,31): warning CS8202: Possible dereference of a null reference.
    //         CL1 z4 = x4 == null ? x4.M1() : x4;
    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x4").WithLocation(26, 31),
    // (38,21): warning CS8206: Result of the comparison is possibly always false.
    //         string z6 = y6 == null ? x6.M2() : y6;
    Diagnostic(ErrorCode.WRN_NullCheckIsProbablyAlwaysFalse, "y6 == null").WithLocation(38, 21),
    // (38,21): warning CS8201: Possible null reference assignment.
    //         string z6 = y6 == null ? x6.M2() : y6;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y6 == null ? x6.M2() : y6").WithLocation(38, 21)
                );
        }

        [Fact(Skip = "Unexpected warning")]
        public void ConditionalBranching_08()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    bool Test1(CL1? x1)
    {
        if (x1?.P1 == true)
        {
            return x1.P2;
        }

        return false;
    }
}

class CL1
{
    public bool P1 { get { return true;} }
    public bool P2 { get { return true;} }
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void Loop_1()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1? x1, CL1 y1, CL1? z1)
    {
        x1 = y1;
        x1.M1(); // 1

        for (int i = 0; i < 2; i++)
        {
            x1.M1(); // 2
            x1 = z1;
        }
    }

    CL1 Test2(CL1? x2, CL1 y2, CL1? z2)
    {
        x2 = y2;
        x2.M1(); // 1

        for (int i = 0; i < 2; i++)
        {
            x2 = z2;
            x2.M1(); // 2
            y2 = x2;
            y2.M2(x2);

            if (i == 1)
            {
                return x2;
            }
        }

        return y2;
    }
}

class CL1
{
    public void M1() { }
    public void M2(CL1 x) { }
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (15,13): warning CS8202: Possible dereference of a null reference.
    //             x1.M1(); // 2
    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x1").WithLocation(15, 13),
    // (28,13): warning CS8202: Possible dereference of a null reference.
    //             x2.M1(); // 2
    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x2").WithLocation(28, 13),
    // (29,18): warning CS8201: Possible null reference assignment.
    //             y2 = x2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2").WithLocation(29, 18),
    // (30,19): warning CS8204: Possible null reference argument.
    //             y2.M2(x2);
    Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x2").WithLocation(30, 19),
    // (34,24): warning CS8203: Possible null reference return.
    //                 return x2;
    Diagnostic(ErrorCode.WRN_NullReferenceReturn, "x2").WithLocation(34, 24)
                );
        }

        [Fact]
        public void Loop_2()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1? x1, CL1 y1, CL1? z1)
    {
        x1 = y1;
        if (x1 == null) {} // 1

        for (int i = 0; i < 2; i++)
        {
            if (x1 == null) {} // 2
            x1 = z1;
        }
    }

    void Test2(CL1? x2, CL1 y2, CL1? z2)
    {
        x2 = y2;
        if (x2 == null) {} // 1

        for (int i = 0; i < 2; i++)
        {
            x2 = z2;
            if (x2 == null) {} // 2
        }
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (11,13): warning CS8206: Result of the comparison is possibly always false.
    //         if (x1 == null) {} // 1
    Diagnostic(ErrorCode.WRN_NullCheckIsProbablyAlwaysFalse, "x1 == null").WithLocation(11, 13),
    // (23,13): warning CS8206: Result of the comparison is possibly always false.
    //         if (x2 == null) {} // 1
    Diagnostic(ErrorCode.WRN_NullCheckIsProbablyAlwaysFalse, "x2 == null").WithLocation(23, 13)
                );
        }

        [Fact]
        public void Var_01()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    CL1? Test1()
    {
        var x1 = (CL1)null;
        return x1;
    }

    CL1? Test2(CL1 x2)
    {
        var y2 = x2;
        y2 = null;
        return y2;
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void Array_01()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1? [] x1)
    {
        CL1? y1 = x1[0];
        CL1 z1 = x1[0];
    }

    void Test2(CL1 [] x2, CL1 y2, CL1? z2)
    {
        x2[0] = y2;
        x2[1] = z2;
    }

    void Test3(CL1 [] x3)
    {
        CL1? y3 = x3[0];
        CL1 z3 = x3[0];
    }

    void Test4(CL1? [] x4, CL1 y4, CL1? z4)
    {
        x4[0] = y4;
        x4[1] = z4;
    }

    void Test5(CL1 y5, CL1? z5)
    {
        var x5 = new CL1 [] { y5, z5 };
    }

    void Test6(CL1 y6, CL1? z6)
    {
        var x6 = new CL1 [,] { {y6}, {z6} };
    }

    void Test7(CL1 y7, CL1? z7)
    {
        var u7 = new CL1? [] { y7, z7 };
        var v7 = new CL1? [,] { {y7}, {z7} };
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (11,18): warning CS8201: Possible null reference assignment.
    //         CL1 z1 = x1[0];
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1[0]").WithLocation(11, 18),
    // (17,17): warning CS8201: Possible null reference assignment.
    //         x2[1] = z2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "z2").WithLocation(17, 17),
    // (34,35): warning CS8201: Possible null reference assignment.
    //         var x5 = new CL1 [] { y5, z5 };
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "z5").WithLocation(34, 35),
    // (39,39): warning CS8201: Possible null reference assignment.
    //         var x6 = new CL1 [,] { {y6}, {z6} };
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "z6").WithLocation(39, 39)
                );
        }

        [Fact]
        public void Array_02()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1 y1, CL1? z1)
    {
        CL1? [] u1 = new [] { y1, z1 };
        CL1? [,] v1 = new [,] { {y1}, {z1} };
    }

    void Test2(CL1 y2, CL1? z2)
    {
        var u2 = new [] { y2, z2 };
        var v2 = new [,] { {y2}, {z2} };

        u2[0] = z2;
        v2[0,0] = z2;
    }

    void Test3(CL1 y3, CL1? z3)
    {
        CL1? [] u3;
        CL1? [,] v3;

        u3 = new [] { y3, z3 };
        v3 = new [,] { {y3}, {z3} };
    }

    void Test4(CL1 y4, CL1? z4)
    {
        var u4 = new [] { y4 };
        var v4 = new [,] {{y4}};

        u4[0] = z4;
        v4[0,0] = z4;
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void Array_03()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void Test1()
    {
        var u1 = new [] { 1, 2 };
        u1 = null;
        var z1 = u1[0];
    }

    void Test2()
    {
        var u1 = new [] { 1, 2 };
        u1 = null;
        var z1 = u1?[u1[0]];
    }
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (12,18): warning CS8202: Possible dereference of a null reference.
    //         var z1 = u1[0];
    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u1").WithLocation(12, 18)
                );
        }

        [Fact(Skip = "TODO")]
        public void Array_04()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {
    }

    void Test1(CL1 y1, CL1? z1)
    {
        CL1 [] u1;
        CL1 [,] v1;

        u1 = new [] { y1, z1 };
        v1 = new [,] { {y1}, {z1} };
    }

    void Test3(CL1 y2, CL1? z2)
    {
        CL1 [] u2;
        CL1 [,] v2;

        var a2 = new [] { y2, z2 };
        var b2 = new [,] { {y2}, {z2} };

        u2 = a2;
        v2 = b2;
    }

    void Test8(CL1 y8, CL1? z8)
    {
        CL1 [] x8 = new [] { y8, z8 };
    }

    void Test9(CL1 y9, CL1? z9)
    {
        CL1 [,] x9 = new [,] { {y9}, {z9} };
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            // TODO: Should probably get warnings about CL1?[] assigned to a CL1[] variable.
            c.VerifyDiagnostics(
                );
        }

        [Fact]
        public void ObjectInitializer_01()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {}

    void Test1(CL1? x1, CL1? y1)
    {
        var z1 = new CL1() { F1 = x1, F2 = y1 };
    }

    void Test2(CL1? x2, CL1? y2)
    {
        var z2 = new CL1() { P1 = x2, P2 = y2 };
    }

    void Test3(CL1 x3, CL1 y3)
    {
        var z31 = new CL1() { F1 = x3, F2 = y3 };
        var z32 = new CL1() { P1 = x3, P2 = y3 };
    }
}

class CL1
{
    public CL1 F1;
    public CL1? F2;

    public CL1 P1 {get; set;}
    public CL1? P2 {get; set;}
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (9,35): warning CS8201: Possible null reference assignment.
    //         var z1 = new CL1() { F1 = x1, F2 = y1 };
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1").WithLocation(9, 35),
    // (14,35): warning CS8201: Possible null reference assignment.
    //         var z2 = new CL1() { P1 = x2, P2 = y2 };
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x2").WithLocation(14, 35)
                );
        }

        [Fact]
        public void Structs_01()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {}

    void Test1(CL1 x1)
    {
        S1 y1 = new S1();
        y1.F1 = x1;
        y1 = new S1();
        x1 = y1.F1;
    }

    void M1(ref S1 x) {}

    void Test2(CL1 x2)
    {
        S1 y2 = new S1();
        y2.F1 = x2;
        M1(ref y2);
        x2 = y2.F1;
    }

    void Test3(CL1 x3)
    {
        S1 y3 = new S1() { F1 = x3 };
        x3 = y3.F1;
    }

    void Test4(CL1 x4, CL1? z4)
    {
        var y4 = new S2() { F2 = new S1() { F1 = x4, F3 = z4 } };
        x4 = y4.F2.F1 ?? x4;
        x4 = y4.F2.F3;
    }

    void Test5(CL1 x5, CL1? z5)
    {
        var y5 = new S2() { F2 = new S1() { F1 = x5, F3 = z5 } };
        var u5 = y5.F2;
        x5 = u5.F1 ?? x5;
        x5 = u5.F3;
    }
}

class CL1
{
}

struct S1
{
    public CL1? F1;
    public CL1? F3;
}

struct S2
{
    public S1 F2;
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (12,14): warning CS8201: Possible null reference assignment.
    //         x1 = y1.F1;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y1.F1").WithLocation(12, 14),
    // (22,14): warning CS8201: Possible null reference assignment.
    //         x2 = y2.F1;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y2.F1").WithLocation(22, 14),
    // (34,14): warning CS8207: Expression is probably never null.
    //         x4 = y4.F2.F1 ?? x4;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "y4.F2.F1").WithLocation(34, 14),
    // (35,14): warning CS8201: Possible null reference assignment.
    //         x4 = y4.F2.F3;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y4.F2.F3").WithLocation(35, 14),
    // (42,14): warning CS8207: Expression is probably never null.
    //         x5 = u5.F1 ?? x5;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "u5.F1").WithLocation(42, 14),
    // (43,14): warning CS8201: Possible null reference assignment.
    //         x5 = u5.F3;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "u5.F3").WithLocation(43, 14)
                );
        }

        [Fact]
        public void Structs_02()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {}

    void Test1(CL1 x1)
    {
        S1 y1;
        y1.F1 = x1;
        S1 z1 = y1;
        x1 = z1.F3;
        x1 = z1.F3 ?? x1;
        z1.F3 = null;
    }

    struct Test2
    {
        S1 z2 {get;}

        public Test2(CL1 x2)
        {
            S1 y2;
            y2.F1 = x2;
            z2 = y2;
            x2 = z2.F3;
            x2 = z2.F3 ?? x2;
        }
    }

    void Test3(CL1 x3)
    {
        S1 y3;
        CL1? z3 = y3.F3;
        x3 = z3;
        x3 = z3 ?? x3;
    }

    void Test4(CL1 x4, CL1? z4)
    {
        S1 y4;
        z4 = y4.F3;
        x4 = z4;
        x4 = z4 ?? x4;
    }

    void Test5(CL1 x5)
    {
        S1 y5;
        var z5 = new { F3 = y5.F3 };
        x5 = z5.F3;
        x5 = z5.F3 ?? x5;
    }

    void Test6(CL1 x6, S1 z6)
    {
        S1 y6;
        y6.F1 = x6;
        z6 = y6;
        x6 = z6.F3;
        x6 = z6.F3 ?? x6;
    }

    void Test7(CL1 x7)
    {
        S1 y7;
        y7.F1 = x7;
        var z7 = new { F3 = y7 };
        x7 = z7.F3.F3;
        x7 = z7.F3.F3 ?? x7;
    }

    struct Test8
    {
        CL1? z8 {get;}

        public Test8(CL1 x8)
        {
            S1 y8;
            y8.F1 = x8;
            z8 = y8.F3;
            x8 = z8;
            x8 = z8 ?? x8;
        }
    }
}

class CL1
{
}

struct S1
{
    public CL1? F1;
    public CL1? F3;
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (11,17): error CS0165: Use of unassigned local variable 'y1'
    //         S1 z1 = y1;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "y1").WithArguments("y1").WithLocation(11, 17),
    // (34,19): error CS0170: Use of possibly unassigned field 'F3'
    //         CL1? z3 = y3.F3;
    Diagnostic(ErrorCode.ERR_UseDefViolationField, "y3.F3").WithArguments("F3").WithLocation(34, 19),
    // (42,14): error CS0170: Use of possibly unassigned field 'F3'
    //         z4 = y4.F3;
    Diagnostic(ErrorCode.ERR_UseDefViolationField, "y4.F3").WithArguments("F3").WithLocation(42, 14),
    // (25,18): error CS0165: Use of unassigned local variable 'y2'
    //             z2 = y2;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "y2").WithArguments("y2").WithLocation(25, 18),
    // (50,29): error CS0170: Use of possibly unassigned field 'F3'
    //         var z5 = new { F3 = y5.F3 };
    Diagnostic(ErrorCode.ERR_UseDefViolationField, "y5.F3").WithArguments("F3").WithLocation(50, 29),
    // (59,14): error CS0165: Use of unassigned local variable 'y6'
    //         z6 = y6;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "y6").WithArguments("y6").WithLocation(59, 14),
    // (68,29): error CS0165: Use of unassigned local variable 'y7'
    //         var z7 = new { F3 = y7 };
    Diagnostic(ErrorCode.ERR_UseDefViolation, "y7").WithArguments("y7").WithLocation(68, 29),
    // (81,18): error CS0170: Use of possibly unassigned field 'F3'
    //             z8 = y8.F3;
    Diagnostic(ErrorCode.ERR_UseDefViolationField, "y8.F3").WithArguments("F3").WithLocation(81, 18)
                );
        }

        [Fact]
        public void Structs_03()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {}

    void Test1(CL1 x1)
    {
        x1 = new S1().F1;
    }

    void Test2(CL1 x2)
    {
        x2 = new S1() {F1 = x2}.F1;
    }

    void Test3(CL1 x3)
    {
        x3 = new S1() {F1 = x3}.F1 ?? x3;
    }

    void Test4(CL1 x4)
    {
        x4 = new S2().F2;
    }

    void Test5(CL1 x5)
    {
        x5 = new S2().F2 ?? x5;
    }
}

class CL1
{
}

struct S1
{
    public CL1? F1;
}

struct S2
{
    public CL1 F2;

    S2(CL1 x) { F2 = x; }
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (9,14): warning CS8201: Possible null reference assignment.
    //         x1 = new S1().F1;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "new S1().F1").WithLocation(9, 14),
    // (19,14): warning CS8207: Expression is probably never null.
    //         x3 = new S1() {F1 = x3}.F1 ?? x3;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "new S1() {F1 = x3}.F1").WithLocation(19, 14),
    // (29,14): warning CS8207: Expression is probably never null.
    //         x5 = new S2().F2 ?? x5;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "new S2().F2").WithLocation(29, 14)
                );
        }

        [Fact]
        public void AnonymousTypes_01()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {}

    void Test1(CL1 x1, CL1? z1)
    {
        var y1 = new { p1 = x1, p2 = z1 };
        x1 = y1.p1 ?? x1;
        x1 = y1.p2;
    }

    void Test2(CL1 x2, CL1? z2)
    {
        var u2 = new { p1 = x2, p2 = z2 };
        var v2 = new { p1 = z2, p2 = x2 };
        u2 = v2;
        x2 = u2.p2 ?? x2;
        x2 = u2.p1;
        x2 = v2.p2 ?? x2;
        x2 = v2.p1;
    }

    void Test3(CL1 x3, CL1? z3)
    {
        var u3 = new { p1 = x3, p2 = z3 };
        var v3 = u3;
        x3 = v3.p1 ?? x3;
        x3 = v3.p2;
    }

    void Test4(CL1 x4, CL1? z4)
    {
        var u4 = new { p0 = new { p1 = x4, p2 = z4 } };
        var v4 = new { p0 = new { p1 = z4, p2 = x4 } };
        u4 = v4;
        x4 = u4.p0.p2 ?? x4;
        x4 = u4.p0.p1;
        x4 = v4.p0.p2 ?? x4;
        x4 = v4.p0.p1;
    }

    void Test5(CL1 x5, CL1? z5)
    {
        var u5 = new { p0 = new { p1 = x5, p2 = z5 } };
        var v5 = u5;
        x5 = v5.p0.p1 ?? x5;
        x5 = v5.p0.p2;
    }

    void Test6(CL1 x6, CL1? z6)
    {
        var u6 = new { p0 = new { p1 = x6, p2 = z6 } };
        var v6 = u6.p0;
        x6 = v6.p1 ?? x6;
        x6 = v6.p2;
    }

    void Test7(CL1 x7, CL1? z7)
    {
        var u7 = new { p0 = new S1() { p1 = x7, p2 = z7 } };
        var v7 = new { p0 = new S1() { p1 = z7, p2 = x7 } };
        u7 = v7;
        x7 = u7.p0.p2 ?? x7;
        x7 = u7.p0.p1;
        x7 = v7.p0.p2 ?? x7;
        x7 = v7.p0.p1;
    }

    void Test8(CL1 x8, CL1? z8)
    {
        var u8 = new { p0 = new S1() { p1 = x8, p2 = z8 } };
        var v8 = u8;
        x8 = v8.p0.p1 ?? x8;
        x8 = v8.p0.p2;
    }

    void Test9(CL1 x9, CL1? z9)
    {
        var u9 = new { p0 = new S1() { p1 = x9, p2 = z9 } };
        var v9 = u9.p0;
        x9 = v9.p1 ?? x9;
        x9 = v9.p2;
    }

    void M1<T>(ref T x) {}

    void Test10(CL1 x10)
    {
        var u10 = new { a0 = x10, a1 = new { p1 = x10 }, a2 = new S1() { p2 = x10 } };
        x10 = u10.a0; // 1
        x10 = u10.a1.p1; // 2
        x10 = u10.a2.p2; // 3 

        M1(ref u10);

        x10 = u10.a0; // 4
        x10 = u10.a1.p1; // 5
        x10 = u10.a2.p2; // 6 
    }
}

class CL1
{
}

struct S1
{
    public CL1? p1;
    public CL1? p2;
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (10,14): warning CS8207: Expression is probably never null.
    //         x1 = y1.p1 ?? x1;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "y1.p1").WithLocation(10, 14),
    // (11,14): warning CS8201: Possible null reference assignment.
    //         x1 = y1.p2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "y1.p2").WithLocation(11, 14),
    // (19,14): warning CS8207: Expression is probably never null.
    //         x2 = u2.p2 ?? x2;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "u2.p2").WithLocation(19, 14),
    // (20,14): warning CS8201: Possible null reference assignment.
    //         x2 = u2.p1;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "u2.p1").WithLocation(20, 14),
    // (21,14): warning CS8207: Expression is probably never null.
    //         x2 = v2.p2 ?? x2;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "v2.p2").WithLocation(21, 14),
    // (22,14): warning CS8201: Possible null reference assignment.
    //         x2 = v2.p1;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "v2.p1").WithLocation(22, 14),
    // (29,14): warning CS8207: Expression is probably never null.
    //         x3 = v3.p1 ?? x3;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "v3.p1").WithLocation(29, 14),
    // (30,14): warning CS8201: Possible null reference assignment.
    //         x3 = v3.p2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "v3.p2").WithLocation(30, 14),
    // (38,14): warning CS8207: Expression is probably never null.
    //         x4 = u4.p0.p2 ?? x4;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "u4.p0.p2").WithLocation(38, 14),
    // (39,14): warning CS8201: Possible null reference assignment.
    //         x4 = u4.p0.p1;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "u4.p0.p1").WithLocation(39, 14),
    // (40,14): warning CS8207: Expression is probably never null.
    //         x4 = v4.p0.p2 ?? x4;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "v4.p0.p2").WithLocation(40, 14),
    // (41,14): warning CS8201: Possible null reference assignment.
    //         x4 = v4.p0.p1;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "v4.p0.p1").WithLocation(41, 14),
    // (48,14): warning CS8207: Expression is probably never null.
    //         x5 = v5.p0.p1 ?? x5;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "v5.p0.p1").WithLocation(48, 14),
    // (49,14): warning CS8201: Possible null reference assignment.
    //         x5 = v5.p0.p2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "v5.p0.p2").WithLocation(49, 14),
    // (56,14): warning CS8207: Expression is probably never null.
    //         x6 = v6.p1 ?? x6;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "v6.p1").WithLocation(56, 14),
    // (57,14): warning CS8201: Possible null reference assignment.
    //         x6 = v6.p2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "v6.p2").WithLocation(57, 14),
    // (65,14): warning CS8207: Expression is probably never null.
    //         x7 = u7.p0.p2 ?? x7;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "u7.p0.p2").WithLocation(65, 14),
    // (66,14): warning CS8201: Possible null reference assignment.
    //         x7 = u7.p0.p1;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "u7.p0.p1").WithLocation(66, 14),
    // (67,14): warning CS8207: Expression is probably never null.
    //         x7 = v7.p0.p2 ?? x7;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "v7.p0.p2").WithLocation(67, 14),
    // (68,14): warning CS8201: Possible null reference assignment.
    //         x7 = v7.p0.p1;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "v7.p0.p1").WithLocation(68, 14),
    // (75,14): warning CS8207: Expression is probably never null.
    //         x8 = v8.p0.p1 ?? x8;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "v8.p0.p1").WithLocation(75, 14),
    // (76,14): warning CS8201: Possible null reference assignment.
    //         x8 = v8.p0.p2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "v8.p0.p2").WithLocation(76, 14),
    // (83,14): warning CS8207: Expression is probably never null.
    //         x9 = v9.p1 ?? x9;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "v9.p1").WithLocation(83, 14),
    // (84,14): warning CS8201: Possible null reference assignment.
    //         x9 = v9.p2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "v9.p2").WithLocation(84, 14),
    // (98,15): warning CS8201: Possible null reference assignment.
    //         x10 = u10.a0; // 4
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "u10.a0").WithLocation(98, 15),
    // (99,15): warning CS8202: Possible dereference of a null reference.
    //         x10 = u10.a1.p1; // 5
    Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "u10.a1").WithLocation(99, 15),
    // (99,15): warning CS8201: Possible null reference assignment.
    //         x10 = u10.a1.p1; // 5
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "u10.a1.p1").WithLocation(99, 15),
    // (100,15): warning CS8201: Possible null reference assignment.
    //         x10 = u10.a2.p2; // 6 
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "u10.a2.p2").WithLocation(100, 15)
                );
        }

        [Fact]
        public void AnonymousTypes_02()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {}

    void Test1(CL1? x1)
    {
        var y1 = new { p1 = x1 };
        y1.p1?.
               M1(y1.p1);
    }

    void Test2(CL1? x2)
    {
        var y2 = new { p1 = x2 };
        if (y2.p1 != null)
        {
            y2.p1.M1(y2.p1);
        }
    }

    void Test3(out CL1? x3, CL1 z3)
    {
        var y3 = new { p1 = x3 };
        x3 = y3.p1 ?? 
                      z3.M1(y3.p1);
    }
}

class CL1
{
    public CL1? M1(CL1 x) { return null; }
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (25,29): error CS0269: Use of unassigned out parameter 'x3'
    //         var y3 = new { p1 = x3 };
    Diagnostic(ErrorCode.ERR_UseDefViolationOut, "x3").WithArguments("x3").WithLocation(25, 29),
    // (27,29): warning CS8204: Possible null reference argument.
    //                       z3.M1(y3.p1);
    Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y3.p1").WithLocation(27, 29)
                );
        }

        [Fact]
        public void AnonymousTypes_03()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {}

    void Test2(CL1 x2)
    {
        x2 = new {F1 = x2}.F1;
    }

    void Test3(CL1 x3)
    {
        x3 = new {F1 = x3}.F1 ?? x3;
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (14,14): warning CS8207: Expression is probably never null.
    //         x3 = new {F1 = x3}.F1 ?? x3;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "new {F1 = x3}.F1").WithLocation(14, 14)
                );
        }

        [Fact]
        public void This()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {}

    void Test1()
    {
        this.Test2();
    }

    void Test2()
    {
        this?.Test1();
    }
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (14,9): warning CS8207: Expression is probably never null.
    //         this?.Test1();
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "this").WithLocation(14, 9)
                );
        }

        [Fact]
        public void ReadonlyAutoProperties_01()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C1
{
    static void Main()
    {
    }

    C1 P1 {get;}

    public C1(C1? x1)
    {
        P1 = x1;
    }
}

class C2
{
    C2? P2 {get;}

    public C2(C2 x2)
    {
        x2 = P2;
    }
}

class C3
{
    C3? P3 {get;}

    public C3(C3 x3, C3? y3)
    {
        P3 = y3;
        x3 = P3;
    }
}

class C4
{
    C4? P4 {get;}

    public C4(C4 x4)
    {
        P4 = x4;
        x4 = P4;
    }
}

class C5
{
    S1 P5 {get;}

    public C5(C0 x5)
    {
        P5 = new S1() { F1 = x5 };
        x5 = P5.F1;
    }
}

class C0
{}

struct S1
{
    public C0? F1;
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (12,14): warning CS8201: Possible null reference assignment.
    //         P1 = x1;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1").WithLocation(12, 14),
    // (33,14): warning CS8201: Possible null reference assignment.
    //         x3 = P3;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "P3").WithLocation(33, 14),
    // (22,14): warning CS8201: Possible null reference assignment.
    //         x2 = P2;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "P2").WithLocation(22, 14),
    // (44,14): warning CS8201: Possible null reference assignment.
    //         x4 = P4;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "P4").WithLocation(44, 14),
    // (55,14): warning CS8201: Possible null reference assignment.
    //         x5 = P5.F1;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "P5.F1").WithLocation(55, 14)
                );
        }

        [Fact]
        public void ReadonlyAutoProperties_02()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
struct C1
{
    static void Main()
    {
    }

    C0 P1 {get;}

    public C1(C0? x1)
    {
        P1 = x1;
    }
}

struct C2
{
    C0? P2 {get;}

    public C2(C0 x2)
    {
        x2 = P2;
        P2 = null;
    }
}

struct C3
{
    C0? P3 {get;}

    public C3(C0 x3, C0? y3)
    {
        P3 = y3;
        x3 = P3;
    }
}

struct C4
{
    C0? P4 {get;}

    public C4(C0 x4)
    {
        P4 = x4;
        x4 = P4;
    }
}

struct C5
{
    S1 P5 {get;}

    public C5(C0 x5)
    {
        P5 = new S1() { F1 = x5 };
        x5 = P5.F1 ?? x5;
    }
}

class C0
{}

struct S1
{
    public C0? F1;
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (34,14): warning CS8201: Possible null reference assignment.
    //         x3 = P3;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "P3").WithLocation(34, 14),
    // (12,14): warning CS8201: Possible null reference assignment.
    //         P1 = x1;
    Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "x1").WithLocation(12, 14),
    // (22,14): error CS8079: Use of possibly unassigned auto-implemented property 'P2'
    //         x2 = P2;
    Diagnostic(ErrorCode.ERR_UseDefViolationProperty, "P2").WithArguments("P2").WithLocation(22, 14),
    // (56,14): warning CS8207: Expression is probably never null.
    //         x5 = P5.F1 ?? x5;
    Diagnostic(ErrorCode.WRN_ExpressionIsProbablyNeverNull, "P5.F1").WithLocation(56, 14)
                );
        }

        [Fact]
        public void NotAssigned()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {}

    void Test1(object? x1)
    {
        CL1? y1;

        if (x1 == null)
        {
            y1 = null;
            return;
        }

        CL1 z1 = y1;
    }

    void Test2(object? x2, out CL1? y2)
    {
        if (x2 == null)
        {
            y2 = null;
            return;
        }

        CL1 z2 = y2;
        y2 = null;
    }
}

class CL1
{
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
    // (17,18): error CS0165: Use of unassigned local variable 'y1'
    //         CL1 z1 = y1;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "y1").WithArguments("y1").WithLocation(17, 18),
    // (28,18): error CS0269: Use of unassigned out parameter 'y2'
    //         CL1 z2 = y2;
    Diagnostic(ErrorCode.ERR_UseDefViolationOut, "y2").WithArguments("y2").WithLocation(28, 18)
                );
        }

        [Fact(Skip = "TODO")]
        public void Test2()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
using nullableString = System.String?;

class C
{
    static void Main()
    {
        nullableString? x = null;
    }
}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
                );
        }

        [Fact(Skip = "Yes")]
        public void DebugHelper()
        {
            CSharpCompilation c = CreateCompilationWithMscorlib(@"
class C
{
    static void Main()
    {}

    void Test33(object x33)
    {
        var y33 = new { p = (object)null };
        object o = y33.p;
    }
}

//class CL1
//{
//    public CL1()
//    {
//        F1 = this;
//    }

//    public CL1 F1;
//    public CL1? F2;

//    public CL1 P1 { get; set; }
//    public CL1? P2 { get; set; }

//    public CL1 M1() { return new CL1(); }
//    public CL1? M2() { return null; }
//}

//struct S1
//{
//    public CL1 F3;
//    public CL1? F4;
//}
", parseOptions: TestOptions.Regular.WithFeature("staticNullChecking", "true"));

            c.VerifyDiagnostics(
                );
        }

    }
}
