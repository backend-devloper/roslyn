﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Outlining
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    ''' <summary>
    ''' Identifiers coming from IL can be just about any valid string and since VB doesn't have a way to escape all possible
    ''' IL identifiers, we have to account for the possibility that an item's metadata name could lead to unparseable code.
    ''' </summary>
    Public Class InvalidIdentifierTests
        Inherits AbstractSyntaxOutlinerTests

        Private Async Function Test(fileContents As String, ParamArray ByVal expectedSpans As OutliningSpan()) As Tasks.Task
            Using workspace = TestWorkspaceFactory.CreateWorkspaceFromFiles(WorkspaceKind.MetadataAsSource, LanguageNames.VisualBasic, Nothing, Nothing, fileContents)
                Dim hostDocument = workspace.Documents.Single()
                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim outliningService = document.Project.LanguageServices.GetService(Of IOutliningService)()
                Dim actualOutliningSpans = (Await outliningService.GetOutliningSpansAsync(document, CancellationToken.None)) _
                    .WhereNotNull().ToArray()

                Assert.Equal(expectedSpans.Length, actualOutliningSpans.Length)
                For i As Integer = 0 To expectedSpans.Length - 1
                    AssertRegion(expectedSpans(i), actualOutliningSpans(i))
                Next
            End Using
        End Function

        <WorkItem(1174405)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function PrependDollarSign() As Tasks.Task
            Dim code = "
Class C
    Public Sub $Invoke()
End Class
"
            Await TestAsync(code)
        End Function

        <WorkItem(1174405)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function SymbolsAndPunctuation() As Tasks.Task
            Dim code = "
Class C
    Public Sub !#$%^&*(()_-+=|\}]{[""':;?/>.<,~`()
End Class
"
            Await TestAsync(code)
        End Function

        <WorkItem(1174405)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function IdentifierThatLooksLikeCode() As Tasks.Task
            Dim code = "
Class C
    Public Sub : End Sub : End Class "" now the document is a string until the next quote ()
End Class
"
            Await TestAsync(code)
        End Function
    End Class
End Namespace