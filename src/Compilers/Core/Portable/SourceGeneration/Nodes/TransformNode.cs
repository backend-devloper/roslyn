﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal sealed class TransformNode<TInput, TOutput> : IIncrementalGeneratorNode<TOutput>
    {
        private readonly Func<TInput, ImmutableArray<TOutput>> _func;
        private readonly IIncrementalGeneratorNode<TInput> _sourceNode;

        public TransformNode(IIncrementalGeneratorNode<TInput> sourceNode, Func<TInput, TOutput> userFunc)
            : this(sourceNode, userFunc: i => ImmutableArray.Create(userFunc(i)))
        {
        }

        public TransformNode(IIncrementalGeneratorNode<TInput> sourceNode, Func<TInput, ImmutableArray<TOutput>> userFunc)
        {
            _sourceNode = sourceNode;
            _func = userFunc;
        }

        // PROTOTYPE(source-generators):
        public IIncrementalGeneratorNode<TOutput> WithComparer(IEqualityComparer<TOutput> comparer) => this;

        public NodeStateTable<TOutput> UpdateStateTable(DriverStateTable.Builder builder, NodeStateTable<TOutput> previousTable, CancellationToken cancellationToken)
        {
            // PROTOTYPE(source-generators):caching, faulted etc.

            // Semantics of a transform:
            // Element-wise comparison of upstream table
            // - Cached or Removed: no transform, just use previous values
            // - Added: perform transform and add
            // - Modified: perform transform and do element wise comparison with previous results

            // grab the source inputs
            var sourceTable = builder.GetLatestStateTableForNode(_sourceNode);
            var newTable = new NodeStateTable<TOutput>.Builder();

            foreach (var entry in sourceTable)
            {
                if (entry.state == EntryState.Cached || entry.state == EntryState.Removed)
                {
                    newTable.AddEntriesFromPreviousTable(previousTable, entry.state);
                }
                else
                {
                    // generate the new entries
                    var newOutputs = _func(entry.item);

                    if (entry.state == EntryState.Added)
                    {
                        newTable.AddEntries(newOutputs, EntryState.Added);
                    }
                    else
                    {
                        newTable.ModifyEntriesFromPreviousTable(previousTable, newOutputs);
                    }
                }
            }
            return newTable.ToImmutableAndFree();
        }
    }
}
