﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class Instrumentation
    {
        internal static void GenerateInstrumentationTables(MethodSymbol method, BoundBlock methodBody)
        {
            if (methodBody != null)
            {
                OperationWalker collector = new InstrumentationCollectionWalker();
                collector.Visit(methodBody);
            }
        }
    }

    internal sealed class InstrumentationCollectionWalker : OperationWalker
    {
        public override void Visit(IOperation operation)
        {
            if (operation == null)
            {
                return;
            }
            
            if (operation.IsStatement())
            {

            }

            base.Visit(operation);
        }
    }
}
