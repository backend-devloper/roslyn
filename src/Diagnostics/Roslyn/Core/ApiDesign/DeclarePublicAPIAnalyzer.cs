﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Diagnostics.Analyzers.ApiDesign
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed partial class DeclarePublicAPIAnalyzer : DiagnosticAnalyzer
    {
        internal const string ShippedFileName = "PublicAPI.Shipped.txt";
        internal const string UnshippedFileName = "PublicAPI.Unshipped.txt";
        internal const string PublicApiNamePropertyBagKey = "PublicAPIName";
        internal const string MinimalNamePropertyBagKey = "MinimalName";
        internal const string RemovedApiPrefix = "*REMOVED*";
        internal const string InvalidReasonShippedCantHaveRemoved = "The shipped API file can't have removed members";

        internal static readonly DiagnosticDescriptor DeclareNewApiRule = new DiagnosticDescriptor(
            id: RoslynDiagnosticIds.DeclarePublicApiRuleId,
            title: RoslynDiagnosticsResources.DeclarePublicApiTitle,
            messageFormat: RoslynDiagnosticsResources.DeclarePublicApiMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: RoslynDiagnosticsResources.DeclarePublicApiDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor RemoveDeletedApiRule = new DiagnosticDescriptor(
            id: RoslynDiagnosticIds.RemoveDeletedApiRuleId,
            title: RoslynDiagnosticsResources.RemoveDeletedApiTitle,
            messageFormat: RoslynDiagnosticsResources.RemoveDeletedApiMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: RoslynDiagnosticsResources.RemoveDeletedApiDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor ExposedNoninstantiableType = new DiagnosticDescriptor(
            id: RoslynDiagnosticIds.ExposedNoninstantiableTypeRuleId,
            title: RoslynDiagnosticsResources.ExposedNoninstantiableTypeTitle,
            messageFormat: RoslynDiagnosticsResources.ExposedNoninstantiableTypeMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: RoslynDiagnosticsResources.ExposedNoninstantiableTypeDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor PublicApiFilesInvalid = new DiagnosticDescriptor(
            id: RoslynDiagnosticIds.PublicApiFilesInvalid,
            title: RoslynDiagnosticsResources.PublicApiFilesInvalid,
            messageFormat: RoslynDiagnosticsResources.PublicApiFilesInvalid,
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: RoslynDiagnosticsResources.PublicApiFilesInvalid,
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly SymbolDisplayFormat ShortSymbolNameFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.None,
                parameterOptions:
                    SymbolDisplayParameterOptions.None,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.None);

        private static readonly SymbolDisplayFormat s_publicApiFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeConstantValue,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DeclareNewApiRule, RemoveDeletedApiRule, ExposedNoninstantiableType, PublicApiFilesInvalid);

        private readonly ImmutableArray<AdditionalText> _extraAdditionalFiles;

        /// <summary>
        /// This API is used for testing to allow arguments to be passed to the analyzer.
        /// </summary>
        public DeclarePublicAPIAnalyzer(ImmutableArray<AdditionalText> extraAdditionalFiles)
        {
            _extraAdditionalFiles = extraAdditionalFiles;
        }

        public DeclarePublicAPIAnalyzer()
        {

        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext compilationContext)
        {
            var additionalFiles = compilationContext.Options.AdditionalFiles;
            if (!_extraAdditionalFiles.IsDefaultOrEmpty)
            {
                additionalFiles = additionalFiles.AddRange(_extraAdditionalFiles);
            }

            ApiData shippedData;
            ApiData unshippedData;
            if (!TryGetApiData(additionalFiles, compilationContext.CancellationToken, out shippedData, out unshippedData))
            {
                return;
            }

            List<Diagnostic> errors;
            if (!ValidateApiFiles(shippedData, unshippedData, out errors))
            {
                compilationContext.RegisterCompilationEndAction(context =>
                {
                    foreach (var cur in errors)
                    {
                        context.ReportDiagnostic(cur);
                    }
                });

                return;
            }

            var impl = new Impl(shippedData, unshippedData);
            compilationContext.RegisterSymbolAction(
                impl.OnSymbolAction,
                SymbolKind.NamedType,
                SymbolKind.Event,
                SymbolKind.Field,
                SymbolKind.Method);
            compilationContext.RegisterCompilationEndAction(impl.OnCompilationEnd);
        }

        internal static string GetPublicApiName(ISymbol symbol)
        {
            var publicApiName = symbol.ToDisplayString(s_publicApiFormat);

            ITypeSymbol memberType = null;
            if (symbol is IMethodSymbol)
            {
                memberType = ((IMethodSymbol)symbol).ReturnType;
            }
            else if (symbol is IPropertySymbol)
            {
                memberType = ((IPropertySymbol)symbol).Type;
            }
            else if (symbol is IEventSymbol)
            {
                memberType = ((IEventSymbol)symbol).Type;
            }
            else if (symbol is IFieldSymbol)
            {
                memberType = ((IFieldSymbol)symbol).Type;
            }

            if (memberType != null)
            {
                publicApiName = publicApiName + " -> " + memberType.ToDisplayString(s_publicApiFormat);
            }

            return publicApiName;
        }

        private static ApiData ReadApiData(string path, SourceText sourceText)
        {
            var apiBuilder = ImmutableArray.CreateBuilder<ApiLine>();
            var removedBuilder = ImmutableArray.CreateBuilder<ApiLine>();

            foreach (var line in sourceText.Lines)
            {
                var text = line.ToString();
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var apiLine = new ApiLine(text, line.Span, sourceText, path);
                if (text.StartsWith(RemovedApiPrefix, StringComparison.Ordinal))
                {
                    removedBuilder.Add(apiLine);
                }
                else
                {
                    apiBuilder.Add(apiLine);
                }
            }

            return new ApiData(apiBuilder.ToImmutable(), removedBuilder.ToImmutable());
        }

        private static bool TryGetApiData(ImmutableArray<AdditionalText> additionalTexts, CancellationToken cancellationToken, out ApiData shippedData, out ApiData unshippedData)
        {
            AdditionalText shippedText;
            AdditionalText unshippedText;
            if (!TryGetApiText(additionalTexts, cancellationToken, out shippedText, out unshippedText))
            {
                shippedData = default(ApiData);
                unshippedData = default(ApiData);
                return false;
            }

            shippedData = ReadApiData(ShippedFileName, shippedText.GetText(cancellationToken));
            unshippedData = ReadApiData(UnshippedFileName, unshippedText.GetText(cancellationToken));
            return true;
        }

        private static bool TryGetApiText(ImmutableArray<AdditionalText> additionalTexts, CancellationToken cancellationToken, out AdditionalText shippedText, out AdditionalText unshippedText)
        {
            shippedText = null;
            unshippedText = null;

            var comparer = StringComparer.Ordinal;
            foreach (var text in additionalTexts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileName = Path.GetFileName(text.Path);
                if (comparer.Equals(fileName, ShippedFileName))
                {
                    shippedText = text;
                    continue;
                }

                if (comparer.Equals(fileName, UnshippedFileName))
                {
                    unshippedText = text;
                    continue;
                }
            }

            return shippedText != null && unshippedText != null;
        }

        private bool ValidateApiFiles(ApiData shippedData, ApiData unshippedData, out List<Diagnostic> list)
        {
            list = new List<Diagnostic>();
            if (shippedData.RemovedApiList.Length > 0)
            {
                list.Add(Diagnostic.Create(PublicApiFilesInvalid, Location.None, InvalidReasonShippedCantHaveRemoved));
            }

            return list.Count == 0;
        }
    }
}
