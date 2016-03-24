﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.DynamicAnalysis.UnitTests
{
    public class DynamicInstrumentationTests : CSharpTestBase
    {
        const string InstrumentationHelperSource = @"namespace Microsoft.CodeAnalysis.Runtime
{
    public static class Instrumentation
    {
        private static bool[][] _payloads;
        private static System.Guid _mvid;

        public static void CreatePayload(System.Guid mvid, int methodToken, ref bool[] payload, int payloadLength)
        {
            if (_mvid != mvid)
            {
                _payloads = new bool[100][];
                _mvid = mvid;
            }

            if (System.Threading.Interlocked.CompareExchange(ref payload, new bool[payloadLength], null) == null)
            {
                int methodIndex = methodToken & 0xffffff;
                _payloads[methodIndex] = payload;
            }
        }

        public static void FlushPayload()
        {
            Console.WriteLine(""Flushing"");
            if (_payloads == null)
            {
                return;
            }
            for (int i = 0; i < _payloads.Length; i++)
            {
                bool[] payload = _payloads[i];
                if (payload != null)
                {
                    Console.WriteLine(i);
                    for (int j = 0; j < payload.Length; j++)
                    {
                        Console.WriteLine(payload[j]);
                        payload[j] = false;
                    }
                }
            }
        }
    }
}
";

        [Fact]
        public void GotoCoverage()
        {
            string source = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        TestMain();
    }

    static void TestMain()
    {
        Console.WriteLine(""foo"");
        goto bar;
        Console.Write(""you won't see me"");
        bar: Console.WriteLine(""bar"");
        Fred();
        return;
    }

    static void Wilma()
    {
        Betty(true);
        Barney(true);
        Barney(false);
        Betty(true);
    }

    static int Barney(bool b)
    {
        if (b)
            return 10;
        if (b)
            return 100;
        return 20;
    }

    static int Betty(bool b)
    {
        if (b)
            return 30;
        if (b)
            return 100;
        return 40;
    }

    static void Fred()
    {
        Wilma();
    }
}
";
            string expectedOutput = @"foo
bar
Flushing
1
True
2
True
True
False
True
True
True
3
True
True
True
True
4
True
True
False
True
True
5
True
True
False
False
False
6
True
";

            string expectedBarneyIL = @"{
  // Code size       83 (0x53)
  .maxstack  4
  IL_0000:  ldsfld     ""bool[] Program.<Barney>3ipayload__Field""
  IL_0005:  brtrue.s   IL_001c
  IL_0007:  ldsfld     ""System.Guid <PrivateImplementationDetails>.MVID""
  IL_000c:  ldtoken    ""int Program.Barney(bool)""
  IL_0011:  ldsflda    ""bool[] Program.<Barney>3ipayload__Field""
  IL_0016:  ldc.i4.5
  IL_0017:  call       ""void Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, int, ref bool[], int)""
  IL_001c:  ldsfld     ""bool[] Program.<Barney>3ipayload__Field""
  IL_0021:  ldc.i4.1
  IL_0022:  ldc.i4.1
  IL_0023:  stelem.i1
  IL_0024:  ldarg.0
  IL_0025:  brfalse.s  IL_0032
  IL_0027:  ldsfld     ""bool[] Program.<Barney>3ipayload__Field""
  IL_002c:  ldc.i4.0
  IL_002d:  ldc.i4.1
  IL_002e:  stelem.i1
  IL_002f:  ldc.i4.s   10
  IL_0031:  ret
  IL_0032:  ldsfld     ""bool[] Program.<Barney>3ipayload__Field""
  IL_0037:  ldc.i4.3
  IL_0038:  ldc.i4.1
  IL_0039:  stelem.i1
  IL_003a:  ldarg.0
  IL_003b:  brfalse.s  IL_0048
  IL_003d:  ldsfld     ""bool[] Program.<Barney>3ipayload__Field""
  IL_0042:  ldc.i4.2
  IL_0043:  ldc.i4.1
  IL_0044:  stelem.i1
  IL_0045:  ldc.i4.s   100
  IL_0047:  ret
  IL_0048:  ldsfld     ""bool[] Program.<Barney>3ipayload__Field""
  IL_004d:  ldc.i4.4
  IL_004e:  ldc.i4.1
  IL_004f:  stelem.i1
  IL_0050:  ldc.i4.s   20
  IL_0052:  ret
}
";
            CompilationVerifier verifier = CompileAndVerify(source + InstrumentationHelperSource, emitOptions: EmitOptions.Default.WithEmitDynamicAnalysisData(true), expectedOutput: expectedOutput);
            verifier.VerifyIL("Program.Barney", expectedBarneyIL);
        }

        [Fact]
        public void ManyStatementsCoverage()
        {
            string source = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        TestMain();
    }

    static void TestMain()
    {
        VariousStatements(2);
    }

    static void VariousStatements(int z)
    {
        int x = z + 10;
        switch (z)
        {
            case 1:
                break;
            case 2:
                break;
            case 3:
                break;
            default:
                break;
        }

        if (x > 10)
        {
            x++;
        }
        else
        {
            x--;
        }

        for (int y = 0; y < 50; y++)
        {
            if (y < 30)
            {
                x++;
                continue;
            }
            else
                break;
        }

        int[] a = new int[] { 1, 2, 3, 4 };
        foreach (int i in a)
        {
            x++;
        }

        while (x < 100)
        {
            x++;
        }

        try
        {
            x++;
            if (x > 10)
            {
                throw new System.Exception();
            }
            x++;
        }
        catch (System.Exception e)
        {
            x++;
        }
        finally
        {
            x++;
        }

        lock (new object())
        {
            ;
        }

        Console.WriteLine(x);

        try
        {
            using ((System.IDisposable)new object())
            {
                ;
            }
        }
        catch (System.Exception e)
        {
        }

        return;
    }
}
";
            string expectedOutput = @"103
Flushing
1
True
2
True
3
True
False
True
False
False
True
True
False
True
True
True
True
True
True
True
True
True
True
True
True
True
True
True
False
True
True
True
True
True
False
True
True
";

            CompileAndVerify(source + InstrumentationHelperSource, emitOptions: EmitOptions.Default.WithEmitDynamicAnalysisData(true), expectedOutput: expectedOutput);
        }

        [Fact]
        public void LambdaCoverage()
        {
            string source = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        TestMain();
    }

    static void TestMain()
    {
        int y = 5;
        Func<int, int> tester = (x) =>
        {
            while (x > 10)
            {
                return y;
            }

            return x;
        };

        y = 75;
        if (tester(20) > 50)
            Console.WriteLine(""OK"");
        else
            Console.WriteLine(""Bad"");        
    }
}
";
            string expectedOutput = @"OK
Flushing
1
True
2
True
True
True
False
True
True
True
False
True
";

            CompileAndVerify(source + InstrumentationHelperSource, emitOptions: EmitOptions.Default.WithEmitDynamicAnalysisData(true), expectedOutput: expectedOutput);
        }

        [Fact]
        public void AsyncCoverage()
        {
            string source = @"
using System;
using System.Threading.Tasks;

public class Program
{
    public static void Main(string[] args)
    {
        TestMain();
    }

    static void TestMain()
    {
        Console.WriteLine(Outer(""Goo"").Result);
    }

    async static Task<string> Outer(string s)
    {
        string s1 = await First(s);
        string s2 = await Second(s);

        return s1 + s2;
    }

    async static Task<string> First(string s)
    {
        string result = await Second(s) + ""Glue"";
        if (result.Length > 2)
            return result;
        else
            return ""Too short"";
    }

    async static Task<string> Second(string s)
    {
        string doubled;
        if (s.Length > 2)
            doubled = s + s;
        else
            doubled = ""HuhHuh"";
        return await Task.Factory.StartNew(() => doubled);
    }
}
";
            string expectedOutput = @"GooGooGlueGooGoo
Flushing
1
True
2
True
3
True
True
True
4
True
True
False
True
5
True
True
False
True
True
";

            CompileAndVerify(source + InstrumentationHelperSource, emitOptions: EmitOptions.Default.WithEmitDynamicAnalysisData(true), expectedOutput: expectedOutput);
        }

        private CompilationVerifier CompileAndVerify(string source, EmitOptions emitOptions, string expectedOutput = null)
        {
            return base.CompileAndVerify(source, expectedOutput: expectedOutput, additionalRefs: s_asyncRefs, emitOptions: emitOptions);
        }

        private static readonly MetadataReference[] s_asyncRefs = new[] { MscorlibRef_v4_0_30316_17626, SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929 };
    }
}
