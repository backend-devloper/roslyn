﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpressions;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.SimplifyLinqExpressions
{
    public partial class SimplifyLinqExpressionsTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpSimplifyLinqExpressionsDiagnosticAnalyzer(), new CSharpSimplifyLinqExpressionsCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestBasicCase1()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = [||]Data().Where(x => x==1).Single();
    }
}";
            var fixedSource = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = Data().Single(x => x==1);
    }
}";
            await TestInRegularAndScriptAsync(source, fixedSource);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestBasicCase2()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    private static IEnumerable<int> test1 = from value in Enumerable.Range(0, 10)
            select value;

        private var test2 = [||]test1.Where(x => x==1).First();
}";
            var fixedSource = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    private static IEnumerable<int> test1 = from value in Enumerable.Range(0, 10)
            select value;

        private var test2 = test1.First(x => x==1);
}";
            await TestInRegularAndScriptAsync(source, fixedSource);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestBasicCase3()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static IEnumerable<string> _test1 = new List<string> { 'hello', 'world', '!' };

        var _test2 = [||]_test1.Where(x => x == '!').Any();
}";
            var fixedSource = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static IEnumerable<string> _test1 = new List<string> { 'hello', 'world', '!' };

        var _test2 = _test1.Any(x => x == '!');
}";
            await TestInRegularAndScriptAsync(source, fixedSource);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestUserDefinedWhere()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
namespace demo
{
    class Test
    {
        public class TestClass4
        {
            private string test;
            public TestClass4() => test = 'hello';

            public TestClass4 Where(Func<string, bool> input)
            {
                return this;
            }

            public string Single()
            {
                return test;
            }
        }
        static void Main()
        {
            TestClass4 Test1 = new TestClass4();
            TestClass4 test = [||]Test1.Where(y => true);
        }
    }
}";
            var fixedSource = @"
using System;
using System.Linq;
using System.Collections.Generic;
namespace demo
{
    class Test
    {
        public class TestClass4
        {
            private string test;
            public TestClass4() => test = 'hello';

            public TestClass4 Where(Func<string, bool> input)
            {
                return this;
            }

            public string Single()
            {
                return test;
            }
        }
        static void Main()
        {
            TestClass4 Test1 = new TestClass4();
            TestClass4 test = Test1.Where(y => true);
        }
    }
}";
            await TestInRegularAndScriptAsync(source, fixedSource);
        }
    }
}
