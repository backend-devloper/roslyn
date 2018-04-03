﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundDagTemp
    {
        /// <summary>
        /// Does this dag temp represent the original input of the pattern-matching operation?
        /// </summary>
        public bool IsOriginalInput => this.Source == null;

        public override bool Equals(object obj) => obj is BoundDagTemp other && this.Equals(other);
        public bool Equals(BoundDagTemp other)
        {
            return other != (object)null && this.Type == other.Type && object.Equals(this.Source, other.Source) && this.Index == other.Index;
        }
        public override int GetHashCode()
        {
            return Hash.Combine(this.Type.GetHashCode(), Hash.Combine(this.Source?.GetHashCode() ?? 0, this.Index));
        }
        public static bool operator ==(BoundDagTemp left, BoundDagTemp right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(BoundDagTemp left, BoundDagTemp right)
        {
            return !left.Equals(right);
        }
    }
}
