﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.DynamicAnalysis.UnitTests
{
    public class DynamicCodeCoverageTests : CSharpTestBase
    {
        [Fact]
        public void GotoCoverage()
        {
            string source = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""foo"");
        goto bar;
        Console.Write(""you won't see me"");
        bar: Console.WriteLine(""bar"");
        Fred();
        return;
    }

    static void Fred()
    {
        return;
    }
}

namespace Microsoft.CodeAnalysis.Runtime
{
    public class Instrumentation
    {
        public static void CreatePayload(System.Type type, System.RuntimeMethodHandle methodToken, ref bool[] payload, int payloadLength)
        {
            if (System.Threading.Interlocked.CompareExchange(ref payload, new bool[payloadLength], null) == null)
            {
                Console.WriteLine(type.Name);
                Console.WriteLine(methodToken.Value);
                    foreach (bool b in payload)
                        Console.WriteLine(b);
            }
        }
    }
}
";
            string expectedOutput = @"Program
Main
False
False
False
False
False
False
foo
bar
";
            CompileAndVerify(source, emitOptions: EmitOptions.Default.WithEmitDynamicAnalysisData(true), expectedOutput: expectedOutput);
        }
    }
}
