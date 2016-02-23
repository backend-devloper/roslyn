﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.UseImplicitTyping;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.UseImplicitTyping;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseExplicitTyping
{
    public class UseExplicitTypingTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpUseExplicitTypingDiagnosticAnalyzer(), new UseExplicitTypingCodeFixProvider());
        }

        #region Error Cases

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task NotOnFieldDeclaration()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    [|var|] _myfield = 5;
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task NotOnFieldLikeEvents()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    public event [|var|] _myevent;
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task NotOnAnonymousMethodExpression()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|var|] comparer = delegate(string value) {
            return value != ""0"";
        };
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task NotOnLambdaExpression()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|var|] x = y => y * y;
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task NotOnDeclarationWithMultipleDeclarators()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|var|] x = 5, y = x;
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task NotOnDeclarationWithoutInitializer()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|var|] x;
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task NotDuringConflicts()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
         [|var|] p = new var();
    }

    class var
    {

    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task NotIfAlreadyExplicitlyTyped()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
         [|Program|] p = new Program();
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task NotOnRHS()
        {
            await TestMissingAsync(
@"using System;
class C
{
    void M()
    {
        var c = new [|var|]();
    }
}

class var
{

}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task NotOnErrorSymbol()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|var|] x = new Foo();
    }
}");
        }

        #endregion

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task NotOnDynamic()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|dynamic|] x = 1;
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task NotOnAnonymousType()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|var|] x = new { Amount = 108, Message = ""Hello"" };
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task NotOnArrayOfAnonymousType()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
        [|var|] x = new[] { new { name = ""apple"", diam = 4 }, new { name = ""grape"", diam = 1 }};
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task NotOnEnumerableOfAnonymousTypeFromAQueryExpression()
        {
            await TestMissingAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
class Program
{
    void Method()
    {
        var products = new List<Product>();
        [|var|] productQuery =
            from prod in products
            select new { prod.Color, prod.Price };
    }
}
class Product
{
    public ConsoleColor Color { get; set; }
    public int Price { get; set; }
}
");
        }

        [WpfFact(Skip = "TODO"), Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task NotOnImplicitConversion()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
    }
}");
        }

        // TODO: should we or should we not? also, check boxing cases.
        [WpfFact(Skip = "TODO"), Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task NotOnExplicitConversion()
        {
            await TestMissingAsync(
@"using System;
class Program
{
    void Method()
    {
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task SuggestExplicitTypeOnLocalWithIntrinsicTypeString()
        {
            await TestAsync(
@"using System;
class C
{
    static void M()
    {
        [|var|] s = ""hello"";
    }
}",
@"using System;
class C
{
    static void M()
    {
        string s = ""hello"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task SuggestExplicitTypeOnIntrinsicType()
        {
            await TestAsync(
@"using System;
class C
{
    static void M()
    {
        [|var|] s = 5;
    }
}",
@"using System;
class C
{
    static void M()
    {
        int s = 5;
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task SuggestExplicitTypeOnFrameworkType()
        {
            await TestAsync(
@"using System.Collections.Generic;
class C
{
    static void M()
    {
        [|var|] c = new List<int>();
    }
}",
@"using System.Collections.Generic;
class C
{
    static void M()
    {
        List<int> c = new List<int>();
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task SuggestExplicitTypeOnUserDefinedType()
        {
            await TestAsync(
@"using System;
class C
{
    void M()
    {
        [|var|] c = new C();
    }
}",
@"using System;
class C
{
    void M()
    {
        C c = new C();
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task SuggestExplicitTypeOnGenericType()
        {
            await TestAsync(
@"using System;
class C<T>
{
    static void M()
    {
        [|var|] c = new C<int>();
    }
}",
@"using System;
class C<T>
{
    static void M()
    {
        C<int> c = new C<int>();
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task SuggestExplicitTypeOnSingleDimensionalArrayTypeWithNewOperator()
        {
            await TestAsync(
@"using System;
class C
{
    static void M()
    {
        [|var|] n1 = new int[4] {2, 4, 6, 8};
    }
}",
@"using System;
class C
{
    static void M()
    {
        int[] n1 = new int[4] {2, 4, 6, 8};
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task SuggestExplicitTypeOnSingleDimensionalArrayTypeWithNewOperator2()
        {
            await TestAsync(
@"using System;
class C
{
    static void M()
    {
        [|var|] n1 = new[] {2, 4, 6, 8};
    }
}",
@"using System;
class C
{
    static void M()
    {
        int[] n1 = new[] {2, 4, 6, 8};
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task SuggestExplicitTypeOnSingleDimensionalJaggedArrayType()
        {
            await TestAsync(
@"using System;
class C
{
    static void M()
    {
        [|var|] cs = new[]
        {
            new[]{1,2,3,4},
            new[]{5,6,7,8}
        };
    }
}",
@"using System;
class C
{
    static void M()
    {
        int[][] cs = new[]
        {
            new[]{1,2,3,4},
            new[]{5,6,7,8}
        };
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task SuggestExplicitTypeOnDeclarationWithObjectInitializer()
        {
            await TestAsync(
@"using System;
class C
{
    static void M()
    {
        [|var|] cc = new Customer { City = ""Madras"" };
    }
    private class Customer
    {
        public string City { get; set; }
    }
}",
@"using System;
class C
{
    static void M()
    {
        Customer cc = new Customer { City = ""Madras"" };
    }
    private class Customer
    {
        public string City { get; set; }
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task SuggestExplicitTypeOnDeclarationWithCollectionInitializer()
        {
            await TestAsync(
@"using System;
using System.Collections.Generic;
class C
{
    static void M()
    {
        [|var|] digits = new List<int> { 1, 2, 3 };
    }
}",
@"using System;
using System.Collections.Generic;
class C
{
    static void M()
    {
        List<int> digits = new List<int> { 1, 2, 3 };
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task SuggestExplicitTypeOnDeclarationWithCollectionAndObjectInitializers()
        {
            await TestAsync(
@"using System;
using System.Collections.Generic;
class C
{
    static void M()
    {
        [|var|] cs = new List<Customer>
        {
            new Customer { City = ""Madras"" }
        };
    }
    private class Customer
    {
        public string City { get; set; }
    }
}",
@"using System;
using System.Collections.Generic;
class C
{
    static void M()
    {
        List<Customer> cs = new List<Customer>
        {
            new Customer { City = ""Madras"" }
        };
    }
    private class Customer
    {
        public string City { get; set; }
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task SuggestExplicitTypeOnForStatement()
        {
            await TestAsync(
@"using System;
class C
{
    static void M()
    {
        for ([|var|] i = 0; i < 5; i++)
        {

        }
    }
}",
@"using System;
class C
{
    static void M()
    {
        for (int i = 0; i < 5; i++)
        {

        }
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task SuggestExplicitTypeOnForeachStatement()
        {
            await TestAsync(
@"using System;
using System.Collections.Generic;
class C
{
    static void M()
    {
        var l = new List<int> { 1, 3, 5 };
        foreach ([|var|] item in l)
        {

        }
    }
}",
@"using System;
using System.Collections.Generic;
class C
{
    static void M()
    {
        var l = new List<int> { 1, 3, 5 };
        foreach (int item in l)
        {

        }
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task SuggestExplicitTypeOnQueryExpression()
        {
            await TestAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    static void M()
    {
        var customers = new List<Customer>();
        [|var|] expr =
            from c in customers
            where c.City == ""London""
            select c;
        }

        private class Customer
        {
            public string City { get; set; }
        }
    }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    static void M()
    {
        var customers = new List<Customer>();
        IEnumerable<Customer> expr =
            from c in customers
            where c.City == ""London""
            select c;
        }

        private class Customer
        {
            public string City { get; set; }
        }
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task SuggestExplicitTypeInUsingStatement()
        {
            await TestAsync(
@"using System;
class C
{
    static void M()
    {
        using ([|var|] r = new Res())
        {

        }
    }
    private class Res : IDisposable
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}",
@"using System;
class C
{
    static void M()
    {
        using (Res r = new Res())
        {

        }
    }
    private class Res : IDisposable
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitTyping)]
        public async Task SuggestExplicitTypeOnInterpolatedString()
        {
            await TestAsync(
@"using System;
class Program
{
    void Method()
    {
        [|var|] s = $""Hello, {name}""
    }
}",
@"using System;
class Program
{
    void Method()
    {
        string s = $""Hello, {name}""
    }
}");
        }

        // TODO: Tests for ConditionalAccessExpression.
        // TODO: Tests with various options - where apparent, primitive types etc.
    }
}