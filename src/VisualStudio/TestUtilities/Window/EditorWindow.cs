﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities.Remoting;

namespace Roslyn.VisualStudio.Test.Utilities
{
    /// <summary>Provides a means of interacting with the active editor window in the Visual Studio host.</summary>
    public class EditorWindow
    {
        private readonly VisualStudioInstance _visualStudio;

        internal EditorWindow(VisualStudioInstance visualStudio)
        {
            _visualStudio = visualStudio;
        }

        public string Text
        {
            get
            {
                return _visualStudio.ExecuteOnHostProcess<string>(typeof(RemotingHelper), nameof(RemotingHelper.GetActiveTextViewContents), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            }

            set
            {
                _visualStudio.ExecuteOnHostProcess<string>(typeof(RemotingHelper), nameof(RemotingHelper.SetActiveTextViewContents), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, value);
            }
        }
    }
}
