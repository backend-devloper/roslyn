﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.DesignerAttribute
{
    interface IRemoteNewDesignerAttributeService
    {
        Task ScanForDesignerAttributesAsync(CancellationToken cancellation);
    }

    struct RemoteDesignerAttributeArgument
    {
        public DocumentId DocumentId;
        public string Argument;
    }

    interface INewDesignerAttributeServiceCallback
    {
        Task RegisterDesignerAttributesAsync(IList<RemoteDesignerAttributeArgument> arguments, CancellationToken cancellationToken);
    }
}
