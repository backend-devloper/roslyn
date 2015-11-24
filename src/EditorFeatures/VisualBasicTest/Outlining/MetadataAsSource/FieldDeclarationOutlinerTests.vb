' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports MaSOutliners = Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining.MetadataAsSource

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    Public Class FieldDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of FieldDeclarationSyntax)

        Protected Overrides ReadOnly Property WorkspaceKind As String
            Get
                Return CodeAnalysis.WorkspaceKind.MetadataAsSource
            End Get
        End Property

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New MaSOutliners.FieldDeclarationOutliner()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub NoCommentsOrAttributes()
            Dim code = "
Class C
    Dim $$x As Integer
End Class
"

            NoRegions(code)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithAttributes()
            Dim code = "
Class C
    {|hint:{|collapse:<Foo>
    |}Dim $$x As Integer|}
End Class
"

            Regions(code,
                Region("collapse", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub WithCommentsAndAttributes()
            Dim code = "
Class C
    {|hint:{|collapse:' Summary:
    '     This is a summary.
    <Foo>
    |}Dim $$x As Integer|}
End Class
"

            Regions(code,
                Region("collapse", "hint", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Sub
    End Class
End Namespace
