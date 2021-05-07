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
        private readonly IEqualityComparer<TOutput> _comparer;
        private readonly IIncrementalGeneratorNode<TInput> _sourceNode;

        public TransformNode(IIncrementalGeneratorNode<TInput> sourceNode, Func<TInput, TOutput> userFunc, IEqualityComparer<TOutput>? comparer = null)
            : this(sourceNode, userFunc: i => ImmutableArray.Create(userFunc(i)), comparer)
        {
        }

        public TransformNode(IIncrementalGeneratorNode<TInput> sourceNode, Func<TInput, ImmutableArray<TOutput>> userFunc, IEqualityComparer<TOutput>? comparer = null)
        {
            _sourceNode = sourceNode;
            _func = userFunc;
            _comparer = comparer ?? EqualityComparer<TOutput>.Default;
        }

        public IIncrementalGeneratorNode<TOutput> WithComparer(IEqualityComparer<TOutput> comparer) => new TransformNode<TInput, TOutput>(_sourceNode, _func, comparer);

        public NodeStateTable<TOutput> UpdateStateTable(DriverStateTable.Builder builder, NodeStateTable<TOutput> previousTable, CancellationToken cancellationToken)
        {
            // grab the source inputs
            var sourceTable = builder.GetLatestStateTableForNode(_sourceNode);
            if (sourceTable.IsCompacted)
            {
                return previousTable;
            }
            if (sourceTable.IsFaulted)
            {
                return NodeStateTable<TOutput>.FromFaultedTable(sourceTable);
            }

            // Semantics of a transform:
            // Element-wise comparison of upstream table
            // - Cached or Removed: no transform, just use previous values
            // - Added: perform transform and add
            // - Modified: perform transform and do element wise comparison with previous results

            var newTable = new NodeStateTable<TOutput>.Builder();

            foreach (var entry in sourceTable)
            {
                // PROTOTYPE(source-generators): this is a bit weird that we ask the state table before deciding what to apply
                // we should convert the Add... methods to a set of TryAdd... that the caller first says 'try getting this from cache'
                // if that fails, try modifying, then finally, just add them.
                if ((entry.state == EntryState.Cached || entry.state == EntryState.Removed) && !previousTable.IsEmpty)
                {
                    newTable.AddEntriesFromPreviousTable(previousTable, entry.state);
                }
                else
                {
                    // generate the new entries
                    var newOutputs = _func(entry.item);

                    if (entry.state == EntryState.Modified && !previousTable.IsEmpty)
                    {
                        newTable.ModifyEntriesFromPreviousTable(previousTable, newOutputs, _comparer);
                    }
                    else
                    {
                        newTable.AddEntries(newOutputs, EntryState.Added);
                    }
                }
            }
            return newTable.ToImmutableAndFree();
        }
    }
}
