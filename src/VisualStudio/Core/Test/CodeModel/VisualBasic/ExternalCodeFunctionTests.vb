﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp.VisualBasic
    Public Class ExternalCodeFunctionTests
        Inherits AbstractCodeFunctionTests

#Region "FullName tests"

        ' Note: This unit test has diverged and is not asynchronous in stabilization. If merged into master,
        ' take the master version and remove this comment.
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestFullName1()
            Dim code =
<Code>
Class C
    Sub $$Foo(string s)
    End Sub
End Class
</Code>

            TestFullName(code, "C.Foo")
        End Sub

#End Region

#Region "Name tests"

        ' Note: This unit test has diverged and is not asynchronous in stabilization. If merged into master,
        ' take the master version and remove this comment.
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName1()
            Dim code =
<Code>
Class C
    Sub $$Foo(string s)
    End Sub
End Class
</Code>

            TestName(code, "Foo")
        End Sub

#End Region

        Protected Overrides ReadOnly Property LanguageName As String = LanguageNames.VisualBasic
        Protected Overrides ReadOnly Property TargetExternalCodeElements As Boolean = True

    End Class
End Namespace