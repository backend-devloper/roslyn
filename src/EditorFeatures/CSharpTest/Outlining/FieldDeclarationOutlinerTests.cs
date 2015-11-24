// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class FieldDeclarationOutlinerTests : AbstractOutlinerTests<FieldDeclarationSyntax>
    {
        internal override AbstractSyntaxNodeOutliner<FieldDeclarationSyntax> CreateOutliner()
        {
            return new FieldDeclarationOutliner();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestFieldWithComments()
        {
            const string code = @"
class C
{
    {|span:// Foo
    // Bar|}
    $$int F;
}";

            Regions(code,
                Region("span", "// Foo ...", autoCollapse: true));
        }
    }
}
