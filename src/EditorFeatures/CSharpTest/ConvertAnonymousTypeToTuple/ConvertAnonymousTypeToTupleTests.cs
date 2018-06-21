﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertAnonymousTypeToTuple;
using Microsoft.CodeAnalysis.CSharp.ConvertForEachToFor;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertAnonymousTypeToTuple
{
    public partial class ConvertAnonymousTypeToTupleTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpConvertAnonymousTypeToTupleDiagnosticAnalyzer(), new CSharpConvertAnonymousTypeToTupleCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)]
        public async Task ConvertSingleAnonymousType()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||]new { a = 1, b = 2 };
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
    }
}
";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)]
        public async Task NotOnEmptyAnonymousType()
        {
            await TestMissingInRegularAndScriptAsync(@"
class Test
{
    void Method()
    {
        var t1 = [||]new { };
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)]
        public async Task NotOnSingleFieldAnonymousType()
        {
            await TestMissingInRegularAndScriptAsync(@"
class Test
{
    void Method()
    {
        var t1 = [||]new { a = 1 };
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)]
        public async Task ConvertSingleAnonymousTypeWithInferredName()
        {
            var text = @"
class Test
{
    void Method(int b)
    {
        var t1 = [||]new { a = 1, b };
    }
}
";
            var expected = @"
class Test
{
    void Method(int b)
    {
        var t1 = (a: 1, b);
    }
}
";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)]
        public async Task ConvertMultipleInstancesInSameMethod()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||]new { a = 1, b = 2 };
        var t2 = new { a = 3, b = 4 };
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
        var t2 = (a: 3, b: 4);
    }
}
";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)]
        public async Task ConvertMultipleInstancesAcrossMesthod()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = [||]new { a = 1, b = 2 };
        var t2 = new { a = 3, b = 4 };
    }

    void Method2()
    {
        var t1 = new { a = 1, b = 2 };
        var t2 = new { a = 3, b = 4 };
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
        var t2 = (a: 3, b: 4);
    }

    void Method2()
    {
        var t1 = new { a = 1, b = 2 };
        var t2 = new { a = 3, b = 4 };
    }
}
";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)]
        public async Task OnlyConvertMatchingTypesInSameMethod()
        {
            var text = @"
class Test
{
    void Method(int b)
    {
        var t1 = [||]new { a = 1, b = 2 };
        var t2 = new { a = 3, b };
        var t3 = new { a = 4 };
        var t4 = new { b = 5, a = 6 };
    }
}
";
            var expected = @"
class Test
{
    void Method(int b)
    {
        var t1 = (a: 1, b: 2);
        var t2 = (a: 3, b);
        var t3 = new { a = 4 };
        var t4 = new { b = 5, a = 6 };
    }
}
";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)]
        public async Task TestFixAllInSingleMethod()
        {
            var text = @"
class Test
{
    void Method(int b)
    {
        var t1 = {|FixAllInDocument:|}new { a = 1, b = 2 };
        var t2 = new { a = 3, b };
        var t3 = new { a = 4 };
        var t4 = new { b = 5, a = 6 };
    }
}
";
            var expected = @"
class Test
{
    void Method(int b)
    {
        var t1 = (a: 1, b: 2);
        var t2 = (a: 3, b);
        var t3 = new { a = 4 };
        var t4 = (b: 5, a: 6);
    }
}
";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)]
        public async Task TestFixAllAcrossMehtods()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = {|FixAllInDocument:|}new { a = 1, b = 2 };
        var t2 = new { a = 3, b = 4 };
    }

    void Method2()
    {
        var t1 = new { a = 1, b = 2 };
        var t2 = new { a = 3, b = 4 };
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = (a: 1, b: 2);
        var t2 = (a: 3, b: 4);
    }

    void Method2()
    {
        var t1 = (a: 1, b: 2);
        var t2 = (a: 3, b: 4);
    }
}
";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)]
        public async Task TestTrivia()
        {
            var text = @"
class Test
{
    void Method()
    {
        var t1 = /*1*/ [||]new /*2*/ { /*3*/ a /*4*/ = /*5*/ 1 /*7*/ , /*8*/ b /*9*/ = /*10*/ 2 /*11*/ } /*12*/ ;
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var t1 = /*1*/ ( /*3*/ a /*4*/ : /*5*/ 1 /*7*/ , /*8*/ b /*9*/ : /*10*/ 2 /*11*/ ) /*12*/ ;
    }
}
";
            await TestInRegularAndScriptAsync(text, expected);
        }
    }
}
