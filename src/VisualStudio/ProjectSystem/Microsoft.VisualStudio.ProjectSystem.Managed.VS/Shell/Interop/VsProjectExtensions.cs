﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.Shell.Interop
{
    /// <summary>
    ///     Provides extension methods for <see cref="IVsProject"/> instances.
    /// </summary>
    internal static class VsProjectExtensions
    {
        /// <summary>
        ///     Returns the <see cref="HierarchyId"/> of the given document moniker, or 
        ///     <see cref="HierarchyId.Nil"/> if the document moniker is not part of the project.
        /// </summary>
        public static HierarchyId GetHierarchyId(this IVsProject project, string documentMoniker)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNullOrEmpty(documentMoniker, nameof(documentMoniker));

            var priority = new VSDOCUMENTPRIORITY[1];
            int isFound;
            uint itemId;
            HResult result = project.IsDocumentInProject(documentMoniker, out isFound, priority, out itemId);
            if (result.Failed)
                throw result.Exception;

            // We only return items that are actually part of the project. CPS returns non-member from this API.
            if (isFound == 0 || priority[0] != VSDOCUMENTPRIORITY.DP_Standard && priority[0] != VSDOCUMENTPRIORITY.DP_Intrinsic)
                return HierarchyId.Nil;

            HierarchyId id = itemId;

            Assumes.False(id.IsNilOrEmpty);

            return id;
        }
    }
}
