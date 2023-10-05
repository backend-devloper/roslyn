﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Nerdbank.Streams;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteHostAssetSerialization
    {
        public static async ValueTask WriteDataAsync(
            Stream stream,
            SolutionAsset? singleAsset,
            IReadOnlyDictionary<Checksum, SolutionAsset>? assetMap,
            ISerializerService serializer,
            SolutionReplicationContext context,
            Checksum solutionChecksum,
            ImmutableArray<Checksum> checksums,
            CancellationToken cancellationToken)
        {
            using var writer = new ObjectWriter(stream, leaveOpen: true, cancellationToken);

            // This information is not actually needed on the receiving end.  However, we still send it so that the
            // receiver can assert that both sides are talking about the same solution snapshot and no weird invariant
            // breaks have occurred.
            solutionChecksum.WriteTo(writer);

            // special case
            if (checksums.Length == 0)
                return;

            if (singleAsset != null)
            {
                WriteAsset(writer, serializer, context, singleAsset, cancellationToken);
                return;
            }

            Debug.Assert(assetMap != null);

            var count = 0;
            foreach (var checksum in checksums)
            {
                var asset = assetMap[checksum];

                WriteAsset(writer, serializer, context, asset, cancellationToken);

                // Flush every so often.  We don't want to flush on each write as that can be expensive.  But we also
                // want to push reasonable chunks of data across the pipe so the host can start reading them.
                // Note: our caller will flush teh remaining data at the end as well.
                if (count % 512 == 0)
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                count++;
            }

            return;

            static void WriteAsset(ObjectWriter writer, ISerializerService serializer, SolutionReplicationContext context, SolutionAsset asset, CancellationToken cancellationToken)
            {
                Debug.Assert(asset.Kind != WellKnownSynchronizationKind.Null, "We should not be sending null assets");
                writer.WriteInt32((int)asset.Kind);

                // null is already indicated by checksum and kind above:
                if (asset.Value is not null)
                    serializer.Serialize(asset.Value, writer, context, cancellationToken);
            }
        }

        public static async ValueTask<ImmutableArray<object>> ReadDataAsync(
            PipeReader pipeReader, Checksum solutionChecksum, ImmutableArray<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            using var stream = await pipeReader.AsPrebufferedStreamAsync(cancellationToken).ConfigureAwait(false);
            return ReadData(stream, solutionChecksum, checksums, serializerService, cancellationToken);
        }

        public static ImmutableArray<object> ReadData(Stream stream, Checksum solutionChecksum, ImmutableArray<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            Debug.Assert(!checksums.Contains(Checksum.Null));

            using var _ = ArrayBuilder<object>.GetInstance(checksums.Length, out var results);

            using var reader = ObjectReader.GetReader(stream, leaveOpen: true, cancellationToken);

            // Ensure that no invariants were broken and that both sides of the communication channel are talking about
            // the same pinned solution.
            var responseSolutionChecksum = Checksum.ReadFrom(reader);
            Contract.ThrowIfFalse(solutionChecksum == responseSolutionChecksum);

            for (int i = 0, n = checksums.Length; i < n; i++)
            {
                var kind = (WellKnownSynchronizationKind)reader.ReadInt32();

                // in service hub, cancellation means simply closed stream
                var result = serializerService.Deserialize<object>(kind, reader, cancellationToken);

                Debug.Assert(result != null, "We should not be requesting null assets");

                results.Add(result);
            }

            return results.ToImmutableAndClear();
        }
    }
}
