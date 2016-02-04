﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    partial class MethodDebugInfo<TTypeSymbol, TLocalSymbol>
    {
        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        public static MethodDebugInfo<TTypeSymbol, TLocalSymbol> ReadFromPortable(MetadataReader reader, int methodToken)
        {
            string defaultNamespace;
            ImmutableArray<HoistedLocalScopeRecord> hoistedLocalScopes;
            ImmutableDictionary<int, ImmutableArray<bool>> dynamicLocals;
            ImmutableArray<ImmutableArray<ImportRecord>> importGroups;
            ImmutableArray<ExternAliasRecord> externAliases;

            var methodHandle = (MethodDefinitionHandle)MetadataTokens.EntityHandle(methodToken);

            ReadImportScopes(reader, methodHandle, out importGroups, out externAliases);
            ReadMethodCustomDebugInformation(reader, methodHandle, out hoistedLocalScopes, out dynamicLocals, out defaultNamespace);

            return new MethodDebugInfo<TTypeSymbol, TLocalSymbol>(
                hoistedLocalScopes,
                importGroups, 
                externAliases,
                dynamicLocals, 
                defaultNamespace,
                // TODO
                ImmutableArray<string>.Empty,
                ImmutableArray<TLocalSymbol>.Empty,
                ILSpan.MaxValue);
        }

        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        private static void ReadImportScopes(
            MetadataReader reader, 
            MethodDefinitionHandle methodHandle, 
            out ImmutableArray<ImmutableArray<ImportRecord>> importGroups, 
            out ImmutableArray<ExternAliasRecord> externAliases)
        {
            // TODO:
            importGroups = ImmutableArray<ImmutableArray<ImportRecord>>.Empty;
            externAliases = ImmutableArray<ExternAliasRecord>.Empty;
        }

        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        private static void ReadMethodCustomDebugInformation(
            MetadataReader reader,
            MethodDefinitionHandle methodHandle,
            out ImmutableArray<HoistedLocalScopeRecord> hoistedLocalScopes,
            out ImmutableDictionary<int, ImmutableArray<bool>> dynamicLocals,
            out string defaultNamespace)
        {
            hoistedLocalScopes = ImmutableArray<HoistedLocalScopeRecord>.Empty;
            defaultNamespace = "";
            dynamicLocals = ImmutableDictionary<int, ImmutableArray<bool>>.Empty;
                
            foreach (var infoHandle in reader.GetCustomDebugInformation(methodHandle))
            {
                var info = reader.GetCustomDebugInformation(infoHandle);
                var id = reader.GetGuid(info.Kind);
                if (id == PortableCustomDebugInfoKinds.StateMachineHoistedLocalScopes)
                {
                    // only single CDI is allowed on a method:
                    if (!hoistedLocalScopes.IsEmpty)
                    {
                        throw new BadImageFormatException();
                    }

                    hoistedLocalScopes = DecodeHoistedLocalScopes(reader.GetBlobReader(info.Value));
                }
                else if (id == PortableCustomDebugInfoKinds.DynamicLocalVariables)
                {
                    // TODO
                }
                else if (id == PortableCustomDebugInfoKinds.DefaultNamespace)
                {
                    var valueReader = reader.GetBlobReader(info.Value);
                    defaultNamespace = valueReader.ReadUTF8(valueReader.Length);
                }
            }
        }

        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        private static ImmutableArray<HoistedLocalScopeRecord> DecodeHoistedLocalScopes(BlobReader reader)
        {
            var result = ArrayBuilder<HoistedLocalScopeRecord>.GetInstance();

            do
            {
                int startOffset = reader.ReadInt32();
                int length = reader.ReadInt32();

                result.Add(new HoistedLocalScopeRecord(startOffset, length));
            }
            while (reader.RemainingBytes > 0);

            return result.ToImmutableAndFree();
        }
    }
}
