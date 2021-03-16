﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.ReassignedVariable;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReassignedVariable
{
    public class CSharpReassignedVariableTests : AbstractReassignedVariableTests
    {
        protected override TestWorkspace CreateWorkspace(string markup)
            => TestWorkspace.CreateCSharp(markup);

        [Fact]
        public async Task TestNoParameterReassignment()
        {
            await TestAsync(
@"class C
{
    void M(int p)
    {
    }
}");
        }

        [Fact]
        public async Task TestParameterReassignment()
        {
            await TestAsync(
@"class C
{
    void M(int [|p|])
    {
        [|p|] = 1;
    }
}");
        }

        [Fact]
        public async Task TestParameterReassignmentWhenReadAfter()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(int [|p|])
    {
        [|p|] = 1;
        Console.WriteLine([|p|]);
    }
}");
        }

        [Fact]
        public async Task TestParameterReassignmentWhenReadBefore()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(int [|p|])
    {
        Console.WriteLine([|p|]);
        [|p|] = 1;
    }
}");
        }

        [Fact]
        public async Task TestParameterReassignmentWhenReadWithDefaultValue()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(int [|p|] = 1)
    {
        Console.WriteLine([|p|]);
        [|p|] = 1;
    }
}");
        }

        [Fact]
        public async Task TestParameterWithExprBodyWithReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(int [|p|]) => Console.WriteLine([|p|]++);
}");
        }

        [Fact]
        public async Task TestLocalFunctionWithExprBodyWithReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M()
    {
        void Local(int [|p|])
            => Console.WriteLine([|p|]++);
}");
        }

        [Fact]
        public async Task TestIndexerWithWriteInExprBody()
        {
            await TestAsync(
@"
using System;
class C
{
    int this[int [|p|]] => [|p|]++;
}");
        }

        [Fact]
        public async Task TestIndexerWithWriteInGetter1()
        {
            await TestAsync(
@"
using System;
class C
{
    int this[int [|p|]] { get => [|p|]++; }
}");
        }

        [Fact]
        public async Task TestIndexerWithWriteInGetter2()
        {
            await TestAsync(
@"
using System;
class C
{
    int this[int [|p|]] { get { [|p|]++; } }
}");
        }

        [Fact]
        public async Task TestIndexerWithWriteInSetter1()
        {
            await TestAsync(
@"
using System;
class C
{
    int this[int [|p|]] { set => [|p|]++; }
}");
        }

        [Fact]
        public async Task TestIndexerWithWriteInSetter2()
        {
            await TestAsync(
@"
using System;
class C
{
    int this[int [|p|]] { set { [|p|]++; } }
}");
        }

        [Fact]
        public async Task TestPropertyWithAssignmentToValue1()
        {
            await TestAsync(
@"
using System;
class C
{
    int Goo { set => [|value|] = [|value|] + 1; }
}");
        }

        [Fact]
        public async Task TestPropertyWithAssignmentToValue2()
        {
            await TestAsync(
@"
using System;
class C
{
    int Goo { set { [|value|] = [|value|] + 1; } }
}");
        }

        [Fact]
        public async Task TestLambdaParameterWithoutReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M()
    {
        Action<int> a = x => Console.WriteLine(x);
    }
}");
        }

        [Fact]
        public async Task TestLambdaParameterWithReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M()
    {
        Action<int> a = [|x|] => Console.WriteLine([|x|]++);
    }
}");
        }

        [Fact]
        public async Task TestLambdaParameterWithReassignment2()
        {
            await TestAsync(
@"
using System;
class C
{
    void M()
    {
        Action<int> a = (int [|x|]) => Console.WriteLine([|x|]++);
    }
}");
        }

        [Fact]
        public async Task TestLocalWithoutInitializerWithoutReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(bool b)
    {
        int p;
        if (b)
            p = 1;
        else
            p = 2;

        Console.WriteLine(p);
    }
}");
        }

        [Fact]
        public async Task TestLocalWithoutInitializerWithReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(bool b)
    {
        int [|p|];
        if (b)
            [|p|] = 1;
        else
            [|p|] = 2;

        [|p|] = 0;
        Console.WriteLine([|p|]);
    }
}");
        }

        [Fact]
        public async Task TestLocalDeclaredByPattern()
        {
            await TestAsync(
@"
using System;
class C
{
    void M()
    {
        if (0 is var [|p|]) [|p|] = 0;
        Console.WriteLine([|p|]);
    }
}");
        }

        [Fact]
        public async Task TestLocalDeclaredByOutVar()
        {
            await TestAsync(
@"
using System;
class C
{
    void M()
    {
        M2(out var [|p|]);
        [|p|] = 0;
        Console.WriteLine([|p|]);
    }

    void M2(out int p) => p = 0;
}");
        }

        [Fact]
        public async Task TestOutParameterCausingReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M()
    {
        int [|p|] = 0;
        M2(out [|p|]);
        Console.WriteLine([|p|]);
    }

    void M2(out int p) => p = 0;
}");
        }

        [Fact]
        public async Task TestOutParameterWithoutReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M()
    {
        int p;
        M2(out p);
        Console.WriteLine(p);
    }

    void M2(out int p) => p = 0;
}");
        }

        [Fact]
        public async Task AssignmentThroughOutParameter()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(out int [|p|])
    {
        [|p|] = 0;
        [|p|] = 1;
        Console.WriteLine([|p|]);
    }
}");
        }

        [Fact]
        public async Task TestOutParameterReassignmentOneWrites()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(out int p)
    {
        p = ref p;
        Console.WriteLine(p);
    }
}");
        }

        [Fact]
        public async Task AssignmentThroughRefParameter()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(ref int [|p|])
    {
        [|p|] = 0;
        [|p|] = 1;
        Console.WriteLine([|p|]);
    }
}");
        }

        [Fact]
        public async Task TestRefParameterReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(ref int [|p|])
    {
        [|p|] = ref [|p|];
        Console.WriteLine([|p|]);
    }
}");
        }

        [Fact]
        public async Task AssignmentThroughRefLocal()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(ref int [|p|])
    {
        ref var [|local|] = ref [|p|];
        [|local|] = 0;
        [|local|] = 1;
        Console.WriteLine([|local|]);
    }
}");
        }

        [Fact]
        public async Task TestRefLocalReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(ref int [|p|])
    {
        ref var [|local|] = ref [|p|];
        [|local|] = ref [|p|];
        Console.WriteLine([|local|]);
    }
}");
        }

        [Fact]
        public async Task AssignmentThroughPointerIsNotAssignmentOfTheVariableItself()
        {
            await TestAsync(
@"
using System;
class C
{
    unsafe void M(int* p)
    {
        *p = 4;
        Console.WriteLine((IntPtr)p);
    }
}");
        }

        [Fact]
        public async Task TestPointerVariableReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    unsafe void M(int* [|p|])
    {
        [|p|] = null;
        Console.WriteLine((IntPtr)[|p|]);
    }
}");
        }

        [Fact]
        public async Task TestRefParameterCausingPossibleReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M()
    {
        int [|p|] = 0;
        M2(ref [|p|]);
        Console.WriteLine([|p|]);
    }

    void M2(ref int p) { }
}");
        }

        [Fact]
        public async Task TestRefParameterWithoutReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M()
    {
        int p;
        M2(ref p);
        Console.WriteLine(p);
    }

    void M2(ref int p) { }
}");
        }

        [Fact]
        public async Task TestRefLocalCausingPossibleReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M()
    {
        int [|p|] = 0;
        ref int refP = ref [|p|];
        Console.WriteLine([|p|]);
    }
}");
        }

        [Fact]
        public async Task TestPointerCausingPossibleReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    unsafe void M()
    {
        int [|p|] = 0;
        int* pointer = &[|p|];
        Console.WriteLine([|p|]);
    }
}");
        }

        [Fact]
        public async Task TestRefExtensionMethodCausingPossibleReassignment()
        {
            await TestAsync(
@"
using System;
static class C
{
    void M()
    {
        int [|p|] = 0;
        [|p|].M2();
        Console.WriteLine([|p|]);
    }

    static void M2(this ref int p) { }
}");
        }

        [Fact]
        public async Task TestMutatingStructMethod()
        {
            await TestAsync(
@"
using System;
struct S
{
    int f;

    void M(S p)
    {
        p.MutatingMethod();
        Console.WriteLine(p);
    }

    void MutatingMethod() => this = default;
}");
        }

        [Fact]
        public async Task TestDeconstructionReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M()
    {
        var ([|x|], y) = Goo();
        [|x|] = 0;
        Console.WriteLine([|x|]);
    }

    (int x, int y) Goo() => default;
}");
        }

        [Fact]
        public async Task TestTopLevelNotReassigned()
        {
            await TestAsync(
@"
int p;
p = 0;
Console.WriteLine(p);
");
        }

        [Fact]
        public async Task TestTopLevelReassigned()
        {
            await TestAsync(
@"
int [|p|] = 1;
[|p|] = 0;
Console.WriteLine([|p|]);
");
        }

        [Fact]
        public async Task TestUsedInThisBase1()
        {
            await TestAsync(
@"
class C
{
    public C(int [|x|])
        : this([|x|]++, true)
    {
    }

    public C(int x, bool b)
    {
    }
}
");
        }

        [Fact]
        public async Task TestUsedInThisBase2()
        {
            await TestAsync(
@"
class C
{
    public C(string s)
        : this(int.TryParse(s, out var [|x|]) ? [|x|]++ : 0, true)
    {
    }

    public C(int x, bool b)
    {
    }
}
");
        }
    }
}
