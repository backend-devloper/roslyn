﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis
{
    public class AnalyzerConfigCodingConventionsContext : ICodingConventionContext, ICodingConventionsSnapshot
    {
        private readonly AnalyzerConfigOptions _analyzerConfigOptions;

        public AnalyzerConfigCodingConventionsContext(AnalyzerConfigOptions analyzerConfigOptions)
        {
            _analyzerConfigOptions = analyzerConfigOptions;
        }

        public ICodingConventionsSnapshot CurrentConventions => this;

        IUniversalCodingConventions ICodingConventionsSnapshot.UniversalConventions => throw new NotSupportedException();
        IReadOnlyDictionary<string, object> ICodingConventionsSnapshot.AllRawConventions => throw new NotSupportedException();
        int ICodingConventionsSnapshot.Version => 0;

        event CodingConventionsChangedAsyncEventHandler ICodingConventionContext.CodingConventionsChangedAsync
        {
            add { }
            remove { }
        }

        public void Dispose()
        {
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task WriteConventionValueAsync(string conventionName, string conventionValue, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            throw new NotSupportedException();
        }

        bool ICodingConventionsSnapshot.TryGetConventionValue<T>(string conventionName, [MaybeNullWhen(returnValue: false)] out T conventionValue)
        {
            if (typeof(T) != typeof(string))
            {
                conventionValue = default!;
                return false;
            }

            if (_analyzerConfigOptions.TryGetValue(conventionName, out var value))
            {
                conventionValue = (T)(object)value;
            }
            else
            {
                conventionValue = default!;
            }

            return conventionValue is object;
        }
    }
}
