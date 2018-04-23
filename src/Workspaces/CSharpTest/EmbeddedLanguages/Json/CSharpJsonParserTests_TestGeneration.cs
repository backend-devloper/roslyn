﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Json;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.EmbeddedLanguages.Json
{
    public class Fixture : IDisposable
    {
        public void Dispose()
        {
#if false
            var other = new Dictionary<string, string>();

            var tree = SyntaxFactory.ParseSyntaxTree(
                File.ReadAllText(@"C:\GitHub\roslyn-internal\Open\src\Workspaces\CSharpTest\Json\CSharpJsonParserTests_BasicTests.cs"));

            var methodNames = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Select(m => m.Identifier.ValueText);
            var nameToIndex = new Dictionary<string, int>();

            var index = 0;
            foreach (var name in methodNames)
            {
                nameToIndex[name] = index;
                index++;
            }

#if true
            var tests =
                CSharpJsonParserTests.nameToTest.Where(kvp => !kvp.Key.StartsWith("NegativeTest") && !kvp.Key.StartsWith("Reference"))
                     .OrderBy(kvp => nameToIndex[kvp.Key])
                     .Select(kvp => kvp.Value);
#elif false
            var tests =
                CSharpRegexParserTests.nameToTest.Where(kvp => kvp.Key.StartsWith("NegativeTest"))
                     .OrderBy(kvp => kvp.Key, LogicalStringComparer.Instance)
                     .Select(kvp => kvp.Value);
#else
            var tests =
                CSharpRegexParserTests.nameToTest.Where(kvp => kvp.Key.StartsWith("Reference"))
                     .OrderBy(kvp => kvp.Key, LogicalStringComparer.Instance)
                     .Select(kvp => kvp.Value);
#endif
#endif
            var tests =
                CSharpJsonParserTests.nameToTest.Values;
            var val = string.Join("\r\n", tests);
        }
    }

    [CollectionDefinition(nameof(MyCollection))]
    public class MyCollection : ICollectionFixture<Fixture>
    {
    }

    [Collection(nameof(MyCollection))]
    public partial class CSharpJsonParserTests
    {
        private readonly Fixture _fixture;

        public CSharpJsonParserTests(Fixture fixture)
        {
            _fixture = fixture;
        }

        public static Dictionary<string, string> nameToTest = new Dictionary<string, string>();

        [Fact]
        private void GenerateTestSuiteTests()
        {
            Process(@"C:\GitHub\JSONTestSuite\test_parsing");
            Process(@"C:\GitHub\JSONTestSuite\test_transform");
        }

        private void Process(string path)
        {
            foreach (var entry in Directory.EnumerateFiles(path, "*.json"))
            {
                try
                {
                    var contents = "@\"" + File.ReadAllText(entry).Replace("\"", "\"\"") + "\"";
                    if (contents.Contains((char)0))
                    {
                        continue;
                    }

                    var fileName = Massage(new FileInfo(entry).Name);

                    var test = GenerateTests(contents, true, true, fileName);
                    nameToTest.Add(fileName, test);
                }
                catch (ArgumentException)
                {
                }
            }
        }

        private static readonly Regex _regex = new Regex("[^0-9a-zA-Z_]");
        private string Massage(string name)
        {
            return _regex.Replace(name, "_");
        }

        //private void Test(string stringText, string expected, bool runJsonNetCheck = true, bool runJsonNetSubTreeTests = true, [CallerMemberName]string name = "")
        //{
        //    var test = GenerateTests(stringText, runJsonNetCheck, runJsonNetSubTreeTests, name);
        //    nameToTest.Add(name, test);
        //}

        private string GenerateTests(string val, bool runJsonNetCheck, bool runJsonNetSubTreeTests, string testName)
        {
            var builder = new StringBuilder();
            builder.AppendLine("[Fact]");
            builder.AppendLine("public void " + testName + "()");
            builder.AppendLine("{");
            builder.Append(@"    Test(");

            var escaped = val.Replace("\"", "\"\"");
            var quoted = "" + '@' + '"' + escaped + '"';
            builder.Append(quoted);

            var token = GetStringToken(val);
            var allChars = _service.TryConvertToVirtualChars(token);
            var looseTree = JsonParser.TryParse(allChars, strict: false);
            if (looseTree == null)
            {
                throw new ArgumentException();
            }

            builder.Append(", " + '@' + '"');
            builder.Append(TreeToText(looseTree).Replace("\"", "\"\""));

            builder.AppendLine("" + '"' + ',');
            builder.Append("" + '@' + '"');
            builder.Append(DiagnosticsToText(looseTree.Diagnostics).Replace("\"", "\"\""));
            builder.AppendLine("" + '"' + ',');


            var strictTree = JsonParser.TryParse(allChars, strict: true);
            builder.Append("" + '@' + '"');
            builder.Append(DiagnosticsToText(strictTree.Diagnostics).Replace("\"", "\"\""));
            builder.Append('"');

            if (!runJsonNetCheck)
            {
                builder.Append(", runLooseTreeCheck: false");
            }

            if (!runJsonNetSubTreeTests)
            {
                builder.Append(", runLooseSubTreeCheck: false");
            }

            builder.AppendLine(");");
            builder.AppendLine("}");

            return builder.ToString();
        }
    }
}
