﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.Designers.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.CSharp.Designers
{
    /// <summary>
    ///     A tree modifier that turns "Properties" folder into a special folder.
    /// </summary>
    [Export(typeof(IProjectTreeModifier))]
    [AppliesTo(ProjectCapabilities.CSharp)]
    internal class PropertiesFolderProjectTreeModifier : AppDesignerFolderProjectTreeModifierBase
    {
        [ImportingConstructor]
        public PropertiesFolderProjectTreeModifier([Import(typeof(ProjectImageProviderAggregator))]IProjectImageProvider imageProvider)
            : base(imageProvider)
        {
        }
        
        public override bool IsExpandable
        {
            get { return true; }
        }

        protected override string GetAppDesignerFolderName()
        {
            string folderName = base.GetAppDesignerFolderName();
            if (!string.IsNullOrEmpty(folderName))
                return folderName;

            return "Properties";        // Not localized
        }
    }
}
