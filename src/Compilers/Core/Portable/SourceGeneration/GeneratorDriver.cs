﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

#nullable enable
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Responsible for orchestrating a source generation pass
    /// </summary>
    /// <remarks>
    /// GeneratorDriver is an immutable class that can be manipulated by returning a mutated copy of itself.
    /// In the compiler we only ever create a single instance and ignore the mutated copy. The IDE may perform 
    /// multiple edits, or generation passes of the same driver, re-using the state as needed.
    /// 
    /// A generator driver works like a small state machine:
    ///   - It starts off with no generated sources
    ///   - A full generation pass will run every generator and produce all possible generated source
    ///   - At any time an 'edit' maybe supplied, which represents potential future work
    ///   - TryApplyChanges can be called, which will iterate through the pending edits and try and attempt to 
    ///     bring the state back to what it would be if a full generation occurred by running partial generation
    ///     on generators that support it
    ///   - At any time a full generation pass can be re-run, resetting the pending edits
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "In Progress")]
    public abstract class GeneratorDriver
    {
        internal readonly GeneratorDriverState _state;

        internal GeneratorDriver(GeneratorDriverState state)
        {
            _state = state;
        }

        internal GeneratorDriver(Compilation compilation, ParseOptions parseOptions, ImmutableArray<ISourceGenerator> generators, ImmutableArray<AdditionalText> additionalTexts)
        {
            _state = new GeneratorDriverState(compilation, parseOptions, generators, additionalTexts, ImmutableDictionary<ISourceGenerator, GeneratorState>.Empty, ImmutableArray<PendingEdit>.Empty, finalCompilation: null, editsFailed: true);
        }

        public GeneratorDriver RunFullGeneration(Compilation compilation, out Compilation outputCompilation, CancellationToken cancellationToken = default)
        {
            // with no generators, there is no work to do
            if (_state.Generators.Length == 0)
            {
                outputCompilation = compilation;
                return this;
            }

            // PERF: if the input compilation is the same as the last compilation we saw, and we have a final compilation
            //       we know nothing can have changed and can just short circuit, returning the already processed final compilation 
            if (compilation == _state.Compilation && _state.FinalCompilation is object)
            {
                outputCompilation = _state.FinalCompilation;
                return this;
            }

            // run the actual generation
            var state = StateWithPendingEditsApplied(_state);
            var stateBuilder = PooledDictionary<ISourceGenerator, GeneratorState>.GetInstance();

            //PROTOTYPE: should be possible to parallelize this
            foreach (var generator in state.Generators)
            {
                try
                {
                    // initialize the generator if needed
                    GeneratorState generatorState;
                    if (!state.GeneratorStates.TryGetValue(generator, out generatorState))
                    {
                        generatorState = InitializeGenerator(generator, cancellationToken);
                    }

                    // we create a new context for each run of the generator. We'll never re-use existing state, only replace anything we have
                    var context = new SourceGeneratorContext(state.Compilation, new AnalyzerOptions(state.AdditionalTexts.NullToEmpty(), CompilerAnalyzerConfigOptionsProvider.Empty));
                    generator.Execute(context);
                    stateBuilder.Add(generator, generatorState.WithSources(context.AdditionalSources.ToImmutableAndFree()));
                }
                catch
                {
                    //PROTOTYPE: we should issue a diagnostic that the generator failed
                }
            }
            state = state.With(generatorStates: stateBuilder.ToImmutableDictionaryAndFree());

            // build the final state, and return 
            return BuildFinalCompilation(compilation, out outputCompilation, state, cancellationToken);
        }

        public GeneratorDriver TryApplyEdits(Compilation compilation, out Compilation outputCompilation, out bool success, CancellationToken cancellationToken = default)
        {
            // if we can't apply any partial edits, just instantly return
            if (_state.EditsFailed || _state.Edits.Length == 0)
            {
                outputCompilation = compilation;
                success = !_state.EditsFailed;
                return this;
            }

            // Apply any pending edits
            var state = _state;
            foreach (var edit in _state.Edits)
            {
                // PROTOTYPE: we'll need to pass in the various compilation states too
                state = ApplyPartialEdit(state, edit);
                if (state.EditsFailed)
                {
                    outputCompilation = compilation;
                    success = false;
                    return this;
                }
            }

            success = true;
            return BuildFinalCompilation(compilation, out outputCompilation, state, cancellationToken);
        }

        public GeneratorDriver AddGenerators(ImmutableArray<ISourceGenerator> generators)
        {
            // set editsFailed true, as we won't be able to apply edits with a new generator
            var newState = _state.With(generators: _state.Generators.AddRange(generators), editsFailed: true);
            return FromState(newState);
        }

        public GeneratorDriver RemoveGenerators(ImmutableArray<ISourceGenerator> generators)
        {
            var newState = _state.With(generators: _state.Generators.RemoveRange(generators), generatorStates: _state.GeneratorStates.RemoveRange(generators));
            return FromState(newState);
        }

        public GeneratorDriver AddAdditionalTexts(ImmutableArray<AdditionalText> additionalTexts)
        {
            var newState = _state.With(additionalTexts: _state.AdditionalTexts.AddRange(additionalTexts));
            return FromState(newState);
        }

        //PROTOTYPE: remove arbitrary edit adding and replace with dedicated edit types
        public GeneratorDriver WithPendingEdits(ImmutableArray<PendingEdit> edits)
        {
            var newState = _state.With(edits: _state.Edits.AddRange(edits));
            return FromState(newState);
        }

        private static GeneratorDriverState ApplyPartialEdit(GeneratorDriverState state, PendingEdit edit, CancellationToken cancellationToken = default)
        {
            var initialState = state;

            // see if any generators accept this particular edit
            var stateBuilder = PooledDictionary<ISourceGenerator, GeneratorState>.GetInstance();
            foreach (var (generator, generatorState) in state.GeneratorStates)
            {
                if (edit.AcceptedBy(generatorState.Info))
                {
                    // attempt to apply the edit
                    var context = new EditContext(generatorState.Sources, cancellationToken);
                    var succeeded = edit.TryApply(generatorState.Info, context);
                    if (!succeeded)
                    {
                        // we couldn't successfully apply this edit
                        // return the original state noting we failed
                        return initialState.With(editsFailed: true);
                    }

                    // update the state with the new edits
                    state = state.With(generatorStates: state.GeneratorStates.SetItem(generator, generatorState.WithSources(context.AdditionalSources.ToImmutableAndFree())));
                }
            }

            return state;
        }

        private static GeneratorDriverState StateWithPendingEditsApplied(GeneratorDriverState state)
        {
            if (state.Edits.Length == 0)
            {
                return state;
            }

            foreach (var edit in state.Edits)
            {
                state = edit.Commit(state);
            }
            return state.With(edits: ImmutableArray<PendingEdit>.Empty, editsFailed: false);
        }

        private static GeneratorState InitializeGenerator(ISourceGenerator generator, CancellationToken cancellationToken)
        {
            GeneratorInfo info = default;
            try
            {
                InitializationContext context = new InitializationContext(cancellationToken);
                generator.Initialize(context);
                info = context.InfoBuilder.ToImmutable();
            }
            catch
            {
                // PROTOTYPE: we should issue a diagnostic when generator intialization fails
            }
            return new GeneratorState(info);
        }

        private GeneratorDriver BuildFinalCompilation(Compilation compilation, out Compilation outputCompilation, GeneratorDriverState state, CancellationToken cancellationToken)
        {
            var finalCompilation = compilation;
            foreach (var (_, generatorState) in state.GeneratorStates)
            {
                foreach (var sourceText in generatorState.Sources)
                {
                    try
                    {
                        //PROTOTYPE: should be possible to parallelize the parsing
                        var tree = ParseGeneratedSourceText(sourceText, cancellationToken);
                        finalCompilation = finalCompilation.AddSyntaxTrees(tree);
                    }
                    catch
                    {
                        //PROTOTYPE: should issue a diagnostic that the generator produced unparseable source
                    }
                }
            }
            outputCompilation = finalCompilation;
            state = state.With(compilation: compilation,
                               finalCompilation: finalCompilation,
                               edits: ImmutableArray<PendingEdit>.Empty,
                               editsFailed: false);
            return FromState(state);
        }

        internal abstract GeneratorDriver FromState(GeneratorDriverState state);

        internal abstract SyntaxTree ParseGeneratedSourceText(GeneratedSourceText input, CancellationToken cancellationToken);
    }
}
