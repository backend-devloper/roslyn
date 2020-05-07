﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
{
    /// <summary>
    /// The base class of any vertex in the graph.
    /// </summary>
    internal abstract class Vertex : Element
    {
        protected Vertex(string label)
            : base(type: "vertex", label)
        {
        }

        public override string ToString()
        {
            return $"{Label} vertex with ID {Id}";
        }
    }
}
