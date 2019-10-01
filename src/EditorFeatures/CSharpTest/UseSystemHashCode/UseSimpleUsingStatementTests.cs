﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UseSystemHashCode;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseSimpleUsingStatement
{
    public partial class UseSystemHashCodeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new UseSystemHashCodeDiagnosticAnalyzer(), new UseSystemHashCodeCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestDerivedClassWithFieldWithBase()
        {
            await TestInRegularAndScriptAsync(
@"namespace System { public struct HashCode { } }

class B
{
    public override int GetHashCode() => 0;
}

class C : B
{
    int j;

    public override int $$GetHashCode()
    {
        var hashCode = 339610899;
        hashCode = hashCode * -1521134295 + base.GetHashCode();
        hashCode = hashCode * -1521134295 + j.GetHashCode();
        return hashCode;
    }
}",
@"namespace System { public struct HashCode { } }

class B
{
    public override int GetHashCode() => 0;
}

class C : B
{
    int j;

    public override int GetHashCode()
    {
        return System.HashCode.Combine(base.GetHashCode(), j);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestDerivedClassWithFieldWithNoBase()
        {
            await TestInRegularAndScriptAsync(
@"namespace System { public struct HashCode { } }

class B
{
    public override int GetHashCode() => 0;
}

class C : B
{
    int j;

    public override int $$GetHashCode()
    {
        var hashCode = 339610899;
        hashCode = hashCode * -1521134295 + j.GetHashCode();
        return hashCode;
    }
}",
@"namespace System { public struct HashCode { } }

class B
{
    public override int GetHashCode() => 0;
}

class C : B
{
    int j;

    public override int GetHashCode()
    {
        return System.HashCode.Combine(j);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestDerivedClassWithNoFieldWithBase()
        {
            await TestInRegularAndScriptAsync(
@"namespace System { public struct HashCode { } }

class B
{
    public override int GetHashCode() => 0;
}

class C : B
{
    int j;

    public override int $$GetHashCode()
    {
        var hashCode = 339610899;
        hashCode = hashCode * -1521134295 + base.GetHashCode();
        return hashCode;
    }
}",
@"namespace System { public struct HashCode { } }

class B
{
    public override int GetHashCode() => 0;
}

class C : B
{
    int j;

    public override int GetHashCode()
    {
        return System.HashCode.Combine(base.GetHashCode());
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestFieldAndProp()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

class C 
{
    int i;

    string S { get; }

    public override int $$GetHashCode()
    {
        var hashCode = -538000506;
        hashCode = hashCode * -1521134295 + i.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
        return hashCode;
    }
}",
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

class C 
{
    int i;

    string S { get; }

    public override int $$GetHashCode()
    {
        return System.HashCode.Combine(i, S);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestUnchecked()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

class C 
{
    int i;

    string S { get; }

    public override int $$GetHashCode()
    {
        unchecked
        {
            var hashCode = -538000506;
            hashCode = hashCode * -1521134295 + i.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
            return hashCode;
        }
    }
}",
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

class C 
{
    int i;

    string S { get; }

    public override int $$GetHashCode()
    {
        return System.HashCode.Combine(i, S);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestNotOnNonGetHashCode()
        {
            await TestMissingAsync(
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

class C 
{
    int i;

    string S { get; }

    public override int $$GetHashCode1()
    {
        var hashCode = -538000506;
        hashCode = hashCode * -1521134295 + i.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
        return hashCode;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestNotWithoutReturn()
        {
            await TestMissingAsync(
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

class C 
{
    int i;

    string S { get; }

    public override int $$GetHashCode()
    {
        var hashCode = -538000506;
        hashCode = hashCode * -1521134295 + i.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestNotWithoutLocal()
        {
            await TestMissingAsync(
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

class C 
{
    int i;

    string S { get; }

    public override int $$GetHashCode()
    {
        hashCode = hashCode * -1521134295 + i.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
        return hashCode;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestNotWithMultipleLocals()
        {
            await TestMissingAsync(
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

class C 
{
    int i;

    string S { get; }

    public override int $$GetHashCode()
    {
        var hashCode = -538000506, x;
        hashCode = hashCode * -1521134295 + i.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
        return hashCode;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestNotWithoutInitializer()
        {
            await TestMissingAsync(
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

class C 
{
    int i;

    string S { get; }

    public override int $$GetHashCode()
    {
        var hashCode;
        hashCode = hashCode * -1521134295 + i.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
        return hashCode;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestNotReturningAccumulator()
        {
            await TestMissingAsync(
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

class C 
{
    int i;

    string S { get; }

    public override int $$GetHashCode()
    {
        var hashCode = -538000506;
        hashCode = hashCode * -1521134295 + i.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
        return 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestAcumulatorInitializedToField()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

class C 
{
    int i;

    string S { get; }

    public override int $$GetHashCode()
    {
        var hashCode = i;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
        return hashCode;
    }
}",
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

class C 
{
    int i;

    string S { get; }

    public override int GetHashCode()
    {
        return System.HashCode.Combine(i, S);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseSimpleUsingStatement)]
        public async Task TestAcumulatorInitializedToHashedField()
        {
            await TestInRegularAndScriptAsync(
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

class C 
{
    int i;

    string S { get; }

    public override int $$GetHashCode()
    {
        var hashCode = i.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
        return hashCode;
    }
}",
@"using System.Collections.Generic;
namespace System { public struct HashCode { } }

class C 
{
    int i;

    string S { get; }

    public override int GetHashCode()
    {
        return System.HashCode.Combine(i, S);
    }
}");
        }
    }
}
