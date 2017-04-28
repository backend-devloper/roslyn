﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenAsyncMainTests : EmitMetadataTestBase
    {

        [Fact]
        public void MultipleMainsOneOfWhichHasBadTaskType_WithMainType()
        {
            var source = @"
using System.Threading.Tasks;

namespace System.Threading.Tasks {
    public class Task<T> {
        public void GetAwaiter() {}
    }
}

static class Program {
    static Task<int> Main() {
        return null;
    }
    static void Main(string[] args) { }
}";
            var sourceCompilation = CreateStandardCompilation(source, options: TestOptions.DebugExe.WithMainTypeName("Program"), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // (11,12): warning CS0436: The type 'Task<T>' in '' conflicts with the imported type 'Task<TResult>' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task<int>").WithArguments("", "System.Threading.Tasks.Task<T>", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task<TResult>").WithLocation(11, 12));
        }

        [Fact]
        public void MultipleMainsOneOfWhichHasBadTaskType()
        {
            var source = @"
using System.Threading.Tasks;

namespace System.Threading.Tasks {
    public class Task<T> {
        public void GetAwaiter() {}
    }
}

static class Program {
    static Task<int> Main() {
        return null;
    }
    static void Main(string[] args) { }
}";
            var sourceCompilation = CreateStandardCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // (11,12): warning CS0436: The type 'Task<T>' in '' conflicts with the imported type 'Task<TResult>' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task<int>").WithArguments("", "System.Threading.Tasks.Task<T>", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task<TResult>").WithLocation(11, 12),
                // (11,12): error CS1986: 'await' requires that the type Task<int> have a suitable GetAwaiter method
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "Task<int>").WithArguments("System.Threading.Tasks.Task<int>").WithLocation(11, 12),
                // (11,22): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(11, 22));
        }

        [Fact]
        public void GetResultReturnsSomethingElse()
        {
            var source = @"
using System.Threading.Tasks;
using System;

namespace System.Runtime.CompilerServices {
    public interface INotifyCompletion {
        void OnCompleted(Action action);
    }
}

namespace System.Threading.Tasks {
    public class Awaiter: System.Runtime.CompilerServices.INotifyCompletion {
        public double GetResult() { return 0.0; }
        public bool IsCompleted  => true;
        public void OnCompleted(Action action) {}
    }
    public class Task<T> {
        public Awaiter GetAwaiter() {
            return new Awaiter();
        }
    }
}

static class Program {
    static Task<int> Main() {
        return null;
    }
}";
            var sourceCompilation = CreateStandardCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // (25,12): warning CS0436: The type 'Task<T>' in '' conflicts with the imported type 'Task<TResult>' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task<int>").WithArguments("", "System.Threading.Tasks.Task<T>", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task<TResult>").WithLocation(25, 12),
                // (25,22): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(25, 22),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1)
);
        }

        [Fact]
        public void TaskOfTGetAwaiterReturnsVoid()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace System.Threading.Tasks {
    public class Task<T> {
        public void GetAwaiter() {}
    }
}

static class Program {
    static Task<int> Main() {
        return null;
    }
}";

            var sourceCompilation = CreateStandardCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyDiagnostics(
                // (12,12): warning CS0436: The type 'Task<T>' in '' conflicts with the imported type 'Task<TResult>' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task<int>").WithArguments("", "System.Threading.Tasks.Task<T>", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task<TResult>").WithLocation(12, 12),
                // (12,12): error CS1986: 'await' requires that the type Task<int> have a suitable GetAwaiter method
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "Task<int>").WithArguments("System.Threading.Tasks.Task<int>").WithLocation(12, 12),
                // (12,22): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(12, 22),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1),
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(2, 1));
        }
        [Fact]
        public void TaskGetAwaiterReturnsVoid()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace System.Threading.Tasks {
    public class Task {
        public void GetAwaiter() {}
    }
}

static class Program {
    static Task Main() {
        return null;
    }
}";
            var sourceCompilation = CreateStandardCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // (12,12): warning CS0436: The type 'Task' in '' conflicts with the imported type 'Task' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task").WithArguments("", "System.Threading.Tasks.Task", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task").WithLocation(12, 12),
                // (12,12): error CS1986: 'await' requires that the type Task have a suitable GetAwaiter method
                //     static Task Main() {
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "Task").WithArguments("System.Threading.Tasks.Task").WithLocation(12, 12),
                // (12,17): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static Task Main() {
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(12, 17),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void MissingMethodsOnTask()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace System.Threading.Tasks {
    public class Task {}
}

static class Program {
    static Task Main() {
        return null;
    }
}";
            var sourceCompilation = CreateStandardCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // (10,12): warning CS0436: The type 'Task' in '' conflicts with the imported type 'Task' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task").WithArguments("", "System.Threading.Tasks.Task", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task").WithLocation(10, 12),
                // (10,12): error CS1061: 'Task' does not contain a definition for 'GetAwaiter' and no extension method 'GetAwaiter' accepting a first argument of type 'Task' could be found (are you missing a using directive or an assembly reference?)
                //     static Task Main() {
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Task").WithArguments("System.Threading.Tasks.Task", "GetAwaiter").WithLocation(10, 12),
                // (10,17): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static Task Main() {
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(10, 17),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void EmitTaskOfIntReturningMainWithoutInt()
        {
            var corAssembly = @"
namespace System {
    public class Object {}
}";
            var corCompilation = CreateCompilation(corAssembly, options: TestOptions.DebugDll);
            corCompilation.VerifyDiagnostics();

            var taskAssembly = @"
namespace System.Threading.Tasks {
    public class Task<T>{}
}";
            var taskCompilation = CreateCompilationWithMscorlib45(taskAssembly, options: TestOptions.DebugDll);
            taskCompilation.VerifyDiagnostics();

            var source = @"
using System;
using System.Threading.Tasks;

static class Program {
    static Task<int> Main() {
        return null;
    }
}";
            var sourceCompilation = CreateCompilation(source, new[] { corCompilation.ToMetadataReference(), taskCompilation.ToMetadataReference() }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (6,17): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "int").WithArguments("System.Int32").WithLocation(6, 17),
                // (6,12): error CS1061: 'Task<int>' does not contain a definition for 'GetAwaiter' and no extension method 'GetAwaiter' accepting a first argument of type 'Task<int>' could be found (are you missing a using directive or an assembly reference?)
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Task<int>").WithArguments("System.Threading.Tasks.Task<int>", "GetAwaiter").WithLocation(6, 12),
                // (6,22): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(6, 22),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void EmitTaskReturningMainWithoutVoid()
        {
            var corAssembly = @"
namespace System {
    public class Object {}
}";
            var corCompilation = CreateCompilation(corAssembly, options: TestOptions.DebugDll);
            corCompilation.VerifyDiagnostics();

            var taskAssembly = @"
namespace System.Threading.Tasks {
    public class Task{}
}";
            var taskCompilation = CreateCompilationWithMscorlib45(taskAssembly, options: TestOptions.DebugDll);
            taskCompilation.VerifyDiagnostics();

            var source = @"
using System;
using System.Threading.Tasks;

static class Program {
    static Task Main() {
        return null;
    }
}";
            var sourceCompilation = CreateCompilation(source, new[] { corCompilation.ToMetadataReference(), taskCompilation.ToMetadataReference() }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (6,12): error CS1061: 'Task' does not contain a definition for 'GetAwaiter' and no extension method 'GetAwaiter' accepting a first argument of type 'Task' could be found (are you missing a using directive or an assembly reference?)
                //     static Task Main() {
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Task").WithArguments("System.Threading.Tasks.Task", "GetAwaiter").WithLocation(6, 12),
                // (6,17): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static Task Main() {
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(6, 17),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void AsyncEmitMainOfIntTest()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task<int> Main() {
        Console.Write(""hello "");
        await Task.Factory.StartNew(() => 5);
        Console.Write(""async main"");
        return 10;
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = CompileAndVerify(c, expectedOutput: "hello async main", expectedReturnCode: 10);

        }

        [Fact]
        public void AsyncEmitMainTest()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task Main() {
        Console.Write(""hello "");
        await Task.Factory.StartNew(() => 5);
        Console.Write(""async main"");
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = CompileAndVerify(c, expectedOutput: "hello async main", expectedReturnCode: 0);
        }

        [Fact]
        public void AsyncMainTestCodegenWithErrors()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task<int> Main() {
        Console.WriteLine(""hello"");
        await Task.Factory.StartNew(() => 5);
        Console.WriteLine(""async main"");
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            c.VerifyEmitDiagnostics(
                // (6,28): error CS0161: 'Program.Main()': not all code paths return a value
                //     static async Task<int> Main() {
                Diagnostic(ErrorCode.ERR_ReturnExpected, "Main").WithArguments("Program.Main()").WithLocation(6, 28));
        }


        [Fact]
        public void AsyncEmitMainOfIntTest_StringArgs()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task<int> Main(string[] args) {
        Console.Write(""hello "");
        await Task.Factory.StartNew(() => 5);
        Console.Write(""async main"");
        return 10;
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = CompileAndVerify(c, expectedOutput: "hello async main", expectedReturnCode: 10);
        }

        [Fact]
        public void AsyncEmitMainOfIntTest_ParamsStringArgs()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task<int> Main(params string[] args) {
        Console.Write(""hello "");
        await Task.Factory.StartNew(() => 5);
        Console.Write(""async main"");
        return 10;
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = CompileAndVerify(c, expectedOutput: "hello async main", expectedReturnCode: 10);
        }

        [Fact]
        public void AsyncEmitMainTest_StringArgs()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task Main(string[] args) {
        Console.Write(""hello "");
        await Task.Factory.StartNew(() => 5);
        Console.Write(""async main"");
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = CompileAndVerify(c, expectedOutput: "hello async main", expectedReturnCode: 0);
        }

        [Fact]
        public void AsyncEmitMainTestCodegenWithErrors_StringArgs()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task<int> Main(string[] args) {
        Console.WriteLine(""hello"");
        await Task.Factory.StartNew(() => 5);
        Console.WriteLine(""async main"");
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            c.VerifyEmitDiagnostics(
                // (6,28): error CS0161: 'Program.Main()': not all code paths return a value
                //     static async Task<int> Main() {
                Diagnostic(ErrorCode.ERR_ReturnExpected, "Main").WithArguments("Program.Main(string[])").WithLocation(6, 28));
        }

        [Fact]
        public void AsyncEmitMainOfIntTest_StringArgs_WithArgs()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task<int> Main(string[] args) {
        Console.Write(""hello "");
        await Task.Factory.StartNew(() => 5);
        Console.Write(args[0]);
        return 10;
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = CompileAndVerify(c, expectedOutput: "hello async main", expectedReturnCode: 10, args: new string[] { "async main" });
        }

        [Fact]
        public void AsyncEmitMainTest_StringArgs_WithArgs()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task Main(string[] args) {
        Console.Write(""hello "");
        await Task.Factory.StartNew(() => 5);
        Console.Write(args[0]);
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = CompileAndVerify(c, expectedOutput: "hello async main", expectedReturnCode: 0, args: new string[] { "async main" });
        }

        [Fact]
        public void MainCanBeAsyncWithArgs()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static Task Main(string[] args)
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics();
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.NotNull(entry);
            Assert.Equal("System.Threading.Tasks.Task A.Main(System.String[] args)", entry.ToTestDisplayString());

            CompileAndVerify(compilation, expectedReturnCode: 0);
        }

        [Fact]
        public void MainCanReturnTaskWithArgs_NoAsync()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static Task Main(string[] args)
    {
        return Task.Factory.StartNew(() => { });
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics();
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.NotNull(entry);
            Assert.Equal("System.Threading.Tasks.Task A.Main(System.String[] args)", entry.ToTestDisplayString());

            CompileAndVerify(compilation, expectedReturnCode: 0);
        }


        [Fact]
        public void MainCantBeAsyncWithRefTask()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static ref Task Main(string[] args)
    {
        throw new System.Exception();
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics(
                // (6,21): warning CS0028: 'A.Main(string[])' has the wrong signature to be an entry point
                //     static ref Task Main(string[] args)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main(string[])").WithLocation(6, 21),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void MainCantBeAsyncWithArgs_CSharp7()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static Task Main(string[] args)
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation.VerifyDiagnostics(
                // (6,5): error CS8107: Feature 'async main' is not available in C# 7. Please use language version 7.1 or greater.
                //     async static Task Main(string[] args)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, @"async static Task Main(string[] args)
    {
        await Task.Factory.StartNew(() => { });
    }").WithArguments("async main", "7.1").WithLocation(6, 5));
        }

        [Fact]
        public void MainCanReturnTask()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static Task Main()
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics();
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.NotNull(entry);
            Assert.Equal("System.Threading.Tasks.Task A.Main()", entry.ToTestDisplayString());
        }
        [Fact]
        public void MainCanReturnTask_NoAsync()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static Task Main()
    {
        return Task.Factory.StartNew(() => { });
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics();
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.NotNull(entry);
            Assert.Equal("System.Threading.Tasks.Task A.Main()", entry.ToTestDisplayString());
        }

        [Fact]
        public void MainCantBeAsyncVoid()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static void Main()
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics(
                // (6,23): warning CS0028: 'A.Main()' has the wrong signature to be an entry point
                //     async static void Main()
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main()").WithLocation(6, 23),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.Null(entry);
        }

        [Fact]
        public void MainCantBeAsyncVoid_CSharp7()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static void Main()
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation.VerifyDiagnostics(
            // (6,5): error CS8107: Feature 'async main' is not available in C# 7. Please use language version 7.1 or greater.
            //     async static void Main()
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, @"async static void Main()
    {
        await Task.Factory.StartNew(() => { });
    }").WithArguments("async main", "7.1").WithLocation(6, 5));
        }

        [Fact]
        public void MainCantBeAsyncInt()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static int Main()
    {
        return await Task.Factory.StartNew(() => 5);
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics(
                // (6,22): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async static int Main()
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "Main").WithLocation(6, 22),
                // (6,23): warning CS0028: 'A.Main()' has the wrong signature to be an entry point
                //     async static void Main()
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main()").WithLocation(6, 22),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.Null(entry);
        }

        [Fact]
        public void MainCantBeAsyncInt_CSharp7()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static int Main()
    {
        return await Task.Factory.StartNew(() => 5);
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation.VerifyDiagnostics(
                // (6,22): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async static int Main()
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "Main"),
                // (6,5): error CS8107: Feature 'async main' is not available in C# 7. Please use language version 7.1 or greater.
                //     async static int Main()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, @"async static int Main()
    {
        return await Task.Factory.StartNew(() => 5);
    }").WithArguments("async main", "7.1").WithLocation(6, 5)
);
        }

        [Fact]
        public void MainCanReturnTaskAndGenericOnInt_WithArgs()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static Task<int> Main(string[] args)
    {
        return await Task.Factory.StartNew(() => 5);
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics();
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.NotNull(entry);
            Assert.Equal("System.Threading.Tasks.Task<System.Int32> A.Main(System.String[] args)", entry.ToTestDisplayString());
        }

        [Fact]
        public void MainCanReturnTaskAndGenericOnInt_WithArgs_NoAsync()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static Task<int> Main(string[] args)
    {
        return Task.Factory.StartNew(() => 5);
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics();
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.NotNull(entry);
            Assert.Equal("System.Threading.Tasks.Task<System.Int32> A.Main(System.String[] args)", entry.ToTestDisplayString());
        }

        [Fact]
        public void MainCantBeAsyncAndGenericOnInt_WithArgs_Csharp7()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static Task<int> Main(string[] args)
    {
        return await Task.Factory.StartNew(() => 5);
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation.VerifyDiagnostics(
            // (6,5): error CS8107: Feature 'async main' is not available in C# 7. Please use language version 7.1 or greater.
            //     async static Task<int> Main(string[] args)
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, @"async static Task<int> Main(string[] args)
    {
        return await Task.Factory.StartNew(() => 5);
    }").WithArguments("async main", "7.1").WithLocation(6, 5)
);
        }

        [Fact]
        public void MainCanReturnTaskAndGenericOnInt()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static Task<int> Main()
    {
        return await Task.Factory.StartNew(() => 5);
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics();
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.NotNull(entry);
            Assert.Equal("System.Threading.Tasks.Task<System.Int32> A.Main()", entry.ToTestDisplayString());
        }
        [Fact]
        public void MainCanReturnTaskAndGenericOnInt_NoAsync()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static Task<int> Main()
    {
        return Task.Factory.StartNew(() => 5);
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics();
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.NotNull(entry);
            Assert.Equal("System.Threading.Tasks.Task<System.Int32> A.Main()", entry.ToTestDisplayString());
        }

        [Fact]
        public void MainCantBeAsyncAndGenericOnInt_CSharp7()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static Task<int> Main()
    {
        return await Task.Factory.StartNew(() => 5);
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation.VerifyDiagnostics(
                // (6,5): error CS8107: Feature 'async main' is not available in C# 7. Please use language version 7.1 or greater.
                //     async static Task<int> Main()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, @"async static Task<int> Main()
    {
        return await Task.Factory.StartNew(() => 5);
    }").WithArguments("async main", "7.1").WithLocation(6, 5)
                );
        }

        [Fact]
        public void MainCantBeAsyncAndGenericOverFloats()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static Task<float> Main()
    {
        await Task.Factory.StartNew(() => { });
        return 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics(
                // (6,30): warning CS0028: 'A.Main()' has the wrong signature to be an entry point
                //     async static Task<float> Main()
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main()").WithLocation(6, 30),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void MainCantBeAsync_AndGeneric()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static void Main<T>()
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1)).VerifyDiagnostics(
                // (6,23): warning CS0028: 'A.Main<T>()' has the wrong signature to be an entry point
                //     async static void Main<T>()
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main<T>()").WithLocation(6, 23),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void MainCantBeAsync_AndBadSig()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static void Main(bool truth)
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (6,23): warning CS0028: 'A.Main(bool)' has the wrong signature to be an entry point
                //     async static void Main(bool truth)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main(bool)").WithLocation(6, 23),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void MainCantBeAsync_AndGeneric_AndBadSig()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static void Main<T>(bool truth)
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (6,23): warning CS0028: 'A.Main<T>(bool)' has the wrong signature to be an entry point
                //     async static void Main<T>(bool truth)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main<T>(bool)").WithLocation(6, 23),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }
    }
}
