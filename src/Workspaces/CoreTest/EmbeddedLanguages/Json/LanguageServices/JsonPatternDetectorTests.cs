﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Json;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Json.LanguageServices;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.EmbeddedLanguages.Json.LanguageServices
{
    public class JsonPatternDetectorTests
    {
        private void Match(string value, JsonOptions? expectedOptions = null)
        {
            Assert.True(JsonPatternDetector.TestAccessor.TryMatch(value, out var actualOptions));

            if (expectedOptions != null)
            {
                Assert.Equal(expectedOptions.Value, actualOptions);
            }
        }

        private void NoMatch(string value)
        {
            Assert.False(JsonPatternDetector.TestAccessor.TryMatch(value, out _));
        }

        [Fact]
        public void TestSimpleForm()
        {
            Match("lang=json");
        }

        [Fact]
        public void TestIncompleteForm1()
        {
            NoMatch("lan=json");
        }

        [Fact]
        public void TestIncompleteForm2()
        {
            NoMatch("lang=rege");
        }

        [Fact]
        public void TestMissingEquals()
        {
            NoMatch("lang json");
        }

        [Fact]
        public void TestLanguageForm()
        {
            Match("language=json");
        }

        [Fact]
        public void TestLanguageNotFullySpelled()
        {
            NoMatch("languag=json");
        }

        [Fact]
        public void TestSpacesAroundEquals()
        {
            Match("lang = json");
        }

        [Fact]
        public void TestSpacesAroundPieces()
        {
            Match(" lang=json ");
        }

        [Fact]
        public void TestSpacesAroundPiecesAndEquals()
        {
            Match(" lang = json ");
        }

        [Fact]
        public void TestSpaceBetweenRegexAndNextWord()
        {
            Match("lang=json here");
        }

        [Fact]
        public void TestPeriodAtEnd()
        {
            Match("lang=json.");
        }

        [Fact]
        public void TestNotWithWordCharAtEnd()
        {
            NoMatch("lang=jsonc");
        }

        [Fact]
        public void TestWithNoNWordBeforeStart1()
        {
            Match(":lang=json");
        }

        [Fact]
        public void TestWithNoNWordBeforeStart2()
        {
            Match(": lang=json");
        }

        [Fact]
        public void TestNotWithWordCharAtStart()
        {
            NoMatch("clang=json");
        }

        [Fact]
        public void TestOption()
        {
            Match("lang=json,strict", JsonOptions.Strict);
        }

        [Fact]
        public void TestOptionWithSpaces()
        {
            Match("lang=json , strict", JsonOptions.Strict);
        }

        [Fact]
        public void TestOptionFollowedByPeriod()
        {
            Match("lang=json,strict. Explanation", JsonOptions.Strict);
        }

        [Fact]
        public void TestMultiOptionFollowedByPeriod()
        {
            Match("lang=json,strict,Strict. Explanation", JsonOptions.Strict);
        }

        [Fact]
        public void TestMultiOptionFollowedByPeriod_CaseInsensitive()
        {
            Match("Language=Json,Strict. Explanation", JsonOptions.Strict);
        }

        [Fact]
        public void TestInvalidOption1()
        {
            NoMatch("lang=json,ignore");
        }

        [Fact]
        public void TestInvalidOption2()
        {
            NoMatch("lang=json,strict,ignore");
        }
    }
}
