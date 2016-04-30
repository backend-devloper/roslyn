﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    using Roslyn.Reflection;
    using Roslyn.Reflection.Metadata;
    using Roslyn.Reflection.Metadata.Ecma335;

    internal class DynamicAnalysisDataWriter
    {
        private struct DocumentRow
        {
            public BlobHandle Name;
            public GuidHandle HashAlgorithm; 
            public BlobHandle Hash;
        }

        private struct MethodRow
        {
            public BlobHandle Spans;
        }

        private readonly List<DocumentRow> _documentTable = new List<DocumentRow>();
        private readonly List<MethodRow> _methodTable = new List<MethodRow>();

        private readonly Dictionary<DebugSourceDocument, int> _documentIndex = new Dictionary<DebugSourceDocument, int>();
        private readonly MetadataBuilder _dynamicAnalysisDataHeaps = new MetadataBuilder();

        internal void SerializeMethodDynamicAnalysisData(IMethodBody bodyOpt)
        {
            var data = bodyOpt?.DynamicAnalysisData;

            if (data == null)
            {
                _methodTable.Add(default(MethodRow));
                return;
            }

            BlobHandle spanBlob = SerializeSpans(data.Spans, _documentIndex);
            _methodTable.Add(new MethodRow { Spans = spanBlob });
        }

        #region Spans

        private BlobHandle SerializeSpans(
            ImmutableArray<SourceSpan> spans,
            Dictionary<DebugSourceDocument, int> documentIndex)
        {
            if (spans.Length == 0)
            {
                return default(BlobHandle);
            }

            var writer = new BlobBuilder();

            int previousStartLine = -1;
            int previousStartColumn = -1;
            DebugSourceDocument previousDocument = spans[0].Document;

            // header:
            writer.WriteCompressedInteger(GetOrAddDocument(previousDocument, documentIndex));

            for (int i = 0; i < spans.Length; i++)
            {
                var currentDocument = spans[i].Document;
                if (previousDocument != currentDocument)
                {
                    writer.WriteInt16(0);
                    writer.WriteCompressedInteger(GetOrAddDocument(currentDocument, documentIndex));
                    previousDocument = currentDocument;
                }

                // Delta Lines & Columns:
                SerializeDeltaLinesAndColumns(writer, spans[i]);

                // delta Start Lines & Columns:
                if (previousStartLine < 0)
                {
                    Debug.Assert(previousStartColumn < 0);
                    writer.WriteCompressedInteger(spans[i].StartLine);
                    writer.WriteCompressedInteger(spans[i].StartColumn);
                }
                else
                {
                    writer.WriteCompressedSignedInteger(spans[i].StartLine - previousStartLine);
                    writer.WriteCompressedSignedInteger(spans[i].StartColumn - previousStartColumn);
                }

                previousStartLine = spans[i].StartLine;
                previousStartColumn = spans[i].StartColumn;
            }

            return _dynamicAnalysisDataHeaps.GetOrAddBlob(writer);
        }

        private void SerializeDeltaLinesAndColumns(BlobBuilder writer, SourceSpan span)
        {
            int deltaLines = span.EndLine - span.StartLine;
            int deltaColumns = span.EndColumn - span.StartColumn;

            // spans can't have zero width
            Debug.Assert(deltaLines != 0 || deltaColumns != 0);

            writer.WriteCompressedInteger(deltaLines);

            if (deltaLines == 0)
            {
                writer.WriteCompressedInteger(deltaColumns);
            }
            else
            {
                writer.WriteCompressedSignedInteger(deltaColumns);
            }
        }

        #endregion

        #region Documents

        private int GetOrAddDocument(DebugSourceDocument document, Dictionary<DebugSourceDocument, int> index)
        {
            int documentRowId;
            if (!index.TryGetValue(document, out documentRowId))
            {
                documentRowId = _documentTable.Count + 1;
                index.Add(document, documentRowId);

                var checksumAndAlgorithm = document.ChecksumAndAlgorithm;
                _documentTable.Add(new DocumentRow
                {
                    Name = SerializeDocumentName(document.Location),
                    HashAlgorithm = (checksumAndAlgorithm.Item1.IsDefault ? default(GuidHandle) : _dynamicAnalysisDataHeaps.GetOrAddGuid(checksumAndAlgorithm.Item2)),
                    Hash = (checksumAndAlgorithm.Item1.IsDefault) ? default(BlobHandle) : _dynamicAnalysisDataHeaps.GetOrAddBlob(checksumAndAlgorithm.Item1)
                });
            }

            return documentRowId;
        }

        private static readonly char[] s_separator1 = { '/' };
        private static readonly char[] s_separator2 = { '\\' };

        private BlobHandle SerializeDocumentName(string name)
        {
            Debug.Assert(name != null);

            var writer = new BlobBuilder();

            int c1 = Count(name, s_separator1[0]);
            int c2 = Count(name, s_separator2[0]);
            char[] separator = (c1 >= c2) ? s_separator1 : s_separator2;

            writer.WriteByte((byte)separator[0]);

            // TODO: avoid allocations
            foreach (var part in name.Split(separator))
            {
                BlobHandle partIndex = _dynamicAnalysisDataHeaps.GetOrAddBlob(ImmutableArray.Create(MetadataWriter.s_utf8Encoding.GetBytes(part)));
                writer.WriteCompressedInteger(_dynamicAnalysisDataHeaps.GetHeapOffset(partIndex));
            }

            return _dynamicAnalysisDataHeaps.GetOrAddBlob(writer);
        }

        private static int Count(string str, char c)
        {
            int count = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == c)
                {
                    count++;
                }
            }

            return count;
        }

        #endregion

        #region Table Serialization

        private sealed class Sizes
        {
            public readonly ImmutableArray<int> HeapSizes;
            public readonly int BlobIndexSize;
            public readonly int GuidIndexSize;

            public Sizes(ImmutableArray<int> heapSizes)
            {
                HeapSizes = heapSizes;
                BlobIndexSize = (heapSizes[(int)HeapIndex.Blob] <= ushort.MaxValue) ? 2 : 4;
                GuidIndexSize = (heapSizes[(int)HeapIndex.Guid] <= ushort.MaxValue) ? 2 : 4;
            }
        }

        internal void SerializeMetadataTables(BlobBuilder writer)
        {
            _dynamicAnalysisDataHeaps.CompleteHeaps();

            var sizes = new Sizes(_dynamicAnalysisDataHeaps.GetHeapSizes());

            SerializeHeader(writer, sizes);

            // tables:
            SerializeDocumentTable(writer, sizes);
            SerializeMethodTable(writer, sizes);

            // heaps:
            _dynamicAnalysisDataHeaps.WriteHeapsTo(writer);
        }

        private void SerializeHeader(BlobBuilder writer, Sizes sizes)
        {
            // signature:
            writer.WriteByte((byte)'D');
            writer.WriteByte((byte)'A');
            writer.WriteByte((byte)'M');
            writer.WriteByte((byte)'D');

            // version: 0.1
            writer.WriteByte(0);
            writer.WriteByte(1);

            // table sizes:
            writer.WriteInt32(_documentTable.Count);
            writer.WriteInt32(_methodTable.Count);

            // blob heap sizes:
            writer.WriteInt32(GetAlignedHeapSize(sizes.HeapSizes[(int)HeapIndex.String]));
            writer.WriteInt32(GetAlignedHeapSize(sizes.HeapSizes[(int)HeapIndex.UserString]));
            writer.WriteInt32(GetAlignedHeapSize(sizes.HeapSizes[(int)HeapIndex.Guid]));
            writer.WriteInt32(GetAlignedHeapSize(sizes.HeapSizes[(int)HeapIndex.Blob]));
        }

        private static int GetAlignedHeapSize(int unalignedSize)
        {
            return BitArithmeticUtilities.Align(unalignedSize, 4);
        }

        private void SerializeDocumentTable(BlobBuilder writer, Sizes sizes)
        {
            foreach (var row in _documentTable)
            {
                writer.WriteReference((uint)_dynamicAnalysisDataHeaps.GetHeapOffset(row.Name), sizes.BlobIndexSize);
                writer.WriteReference((uint)_dynamicAnalysisDataHeaps.GetHeapOffset(row.HashAlgorithm), sizes.GuidIndexSize);
                writer.WriteReference((uint)_dynamicAnalysisDataHeaps.GetHeapOffset(row.Hash), sizes.BlobIndexSize);
            }
        }

        private void SerializeMethodTable(BlobBuilder writer, Sizes sizes)
        {
            foreach (var row in _methodTable)
            {
                writer.WriteReference((uint)_dynamicAnalysisDataHeaps.GetHeapOffset(row.Spans), sizes.BlobIndexSize);
            }
        }

        #endregion
    }
}
