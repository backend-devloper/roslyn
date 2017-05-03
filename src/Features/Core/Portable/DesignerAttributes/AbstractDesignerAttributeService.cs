﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.DesignerAttributes
{
    internal abstract class AbstractDesignerAttributeService : IDesignerAttributeService
    {
        protected abstract bool ProcessOnlyFirstTypeDefined();
        protected abstract IEnumerable<SyntaxNode> GetAllTopLevelTypeDefined(SyntaxNode root);
        protected abstract bool HasAttributesOrBaseTypeOrIsPartial(SyntaxNode typeNode);

        public async Task<DesignerAttributeResult> ScanDesignerAttributesAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

            // Delay getting any of these until we need them, but hold on to them once we have them.
            Compilation compilation = null;
            INamedTypeSymbol designerAttribute = null;
            SemanticModel model = null;

            string designerAttributeArgument = null;
            var documentHasError = false;

            // get type defined in current tree
            foreach (var typeNode in GetAllTopLevelTypeDefined(root))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (HasAttributesOrBaseTypeOrIsPartial(typeNode))
                {
                    if (designerAttribute == null)
                    {
                        compilation = compilation ?? await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                        designerAttribute = compilation.DesignerCategoryAttributeType();
                        if (designerAttribute == null)
                        {
                            // The DesignerCategoryAttribute doesn't exist. either not applicable or
                            // no idea on design attribute status, just leave things as it is.
                            return new DesignerAttributeResult(document.FilePath, designerAttributeArgument, documentHasError, notApplicable: true);
                        }
                    }

                    if (model == null)
                    {
                        model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                    }

                    var definedType = model.GetDeclaredSymbol(typeNode, cancellationToken) as INamedTypeSymbol;
                    if (definedType == null)
                    {
                        continue;
                    }

                    // walk up type chain
                    foreach (var type in definedType.GetBaseTypesAndThis())
                    {
                        if (type.IsErrorType())
                        {
                            documentHasError = true;
                            continue;
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        // if it has designer attribute, set it
                        var attribute = type.GetAttributes().Where(d => designerAttribute.Equals(d.AttributeClass)).FirstOrDefault();
                        if (attribute != null && attribute.ConstructorArguments.Length == 1)
                        {
                            designerAttributeArgument = GetArgumentString(attribute.ConstructorArguments[0]);
                            return new DesignerAttributeResult(document.FilePath, designerAttributeArgument, documentHasError, notApplicable: false);
                        }
                    }
                }

                // check only first type
                if (ProcessOnlyFirstTypeDefined())
                {
                    break;
                }
            }

            return new DesignerAttributeResult(document.FilePath, designerAttributeArgument, documentHasError, notApplicable: false);
        }

        private static string GetArgumentString(TypedConstant argument)
        {
            if (argument.Type == null ||
                argument.Type.SpecialType != SpecialType.System_String ||
                argument.IsNull)
            {
                return null;
            }

            return ((string)argument.Value).Trim();
        }
    }
}