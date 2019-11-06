﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal interface IExtractMethodService : ILanguageService
    {
        Task<ExtractMethodResult> ExtractMethodAsync(Document document, TextSpan textSpan, bool extractLocalFunction = false, bool preferStatic = true, OptionSet options = null, CancellationToken cancellationToken = default);
    }
}
