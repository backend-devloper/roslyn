﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 9.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.ConvertIfToSwitch
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertIfToSwitch
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicConvertIfToSwitchCodeRefactoringProvider)), [Shared]>
    Friend NotInheritable Class VisualBasicConvertIfToSwitchCodeRefactoringProvider
        Inherits AbstractConvertIfToSwitchCodeRefactoringProvider

        Private Shared ReadOnly s_operatorMap As Dictionary(Of BinaryOperatorKind, (CaseClauseKind As SyntaxKind, OperatorTokenKind As SyntaxKind)) =
            New Dictionary(Of BinaryOperatorKind, (SyntaxKind, SyntaxKind))() From
            {
                {BinaryOperatorKind.NotEquals, (SyntaxKind.CaseNotEqualsClause, SyntaxKind.LessThanGreaterThanToken)},
                {BinaryOperatorKind.LessThan, (SyntaxKind.CaseLessThanClause, SyntaxKind.LessThanToken)},
                {BinaryOperatorKind.GreaterThan, (SyntaxKind.CaseGreaterThanClause, SyntaxKind.GreaterThanToken)},
                {BinaryOperatorKind.LessThanOrEqual, (SyntaxKind.CaseLessThanOrEqualClause, SyntaxKind.LessThanEqualsToken)},
                {BinaryOperatorKind.GreaterThanOrEqual, (SyntaxKind.CaseGreaterThanOrEqualClause, SyntaxKind.GreaterThanEqualsToken)}
            }

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides Function CreateAnalyzer(syntaxFacts As ISyntaxFactsService) As IAnalyzer
            Return New VisualBasicAnalyzer(syntaxFacts)
        End Function

        Private NotInheritable Class VisualBasicAnalyzer
            Inherits Analyzer(Of ExecutableStatementSyntax)

            Public Sub New(syntaxFacts As ISyntaxFactsService)
                MyBase.New(syntaxFacts)
            End Sub

            Public Overrides Function CreateSwitchExpressionStatement(target As SyntaxNode, sections As ImmutableArray(Of SwitchSection)) As SyntaxNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public Overrides Function CreateSwitchStatement(ifStatement As SyntaxNode, expression As SyntaxNode, sectionList As IEnumerable(Of SyntaxNode)) As SyntaxNode
                Return VisualBasicSyntaxGenerator.Instance.SwitchStatement(expression, sectionList)
            End Function

            Public Overrides Function AsSwitchSectionStatements(operation As IOperation) As IEnumerable(Of SyntaxNode)
                Return GetStatements(operation.Syntax)
            End Function

            Private Shared Function GetStatements(node As SyntaxNode) As SyntaxList(Of StatementSyntax)
                Return node.TypeSwitch(
                    Function(p As MultiLineIfBlockSyntax) p.Statements,
                    Function(p As SingleLineIfStatementSyntax) p.Statements,
                    Function(p As SingleLineElseClauseSyntax) p.Statements,
                    Function(p As ElseIfBlockSyntax) p.Statements,
                    Function(p As ElseBlockSyntax) p.Statements,
                    Function(p As StatementSyntax) SyntaxFactory.SingletonList(p),
                    Function(p) As SyntaxList(Of StatementSyntax)
                        Throw ExceptionUtilities.UnexpectedValue(node.Kind())
                    End Function)
            End Function

            Public Overrides Function AsSwitchLabelSyntax(label As SwitchLabel) As SyntaxNode
                Debug.Assert(label.Guards.IsDefaultOrEmpty)
                Return AsCaseClauseSyntax(label.Pattern).WithAppendedTrailingTrivia(SyntaxFactory.ElasticMarker)
            End Function

            Private Shared Function AsCaseClauseSyntax(pattern As Pattern) As CaseClauseSyntax
                Return pattern.TypeSwitch(
                    Function(p As ConstantPattern) SyntaxFactory.SimpleCaseClause(DirectCast(p.ExpressionSyntax, ExpressionSyntax)),
                    Function(p As RangePattern) SyntaxFactory.RangeCaseClause(DirectCast(p.LowerBound, ExpressionSyntax),
                                                                              DirectCast(p.HigherBound, ExpressionSyntax)),
                    Function(p As RelationalPattern)
                        Dim relationalOperator = s_operatorMap(p.OperatorKind)
                        Return SyntaxFactory.RelationalCaseClause(
                            relationalOperator.CaseClauseKind,
                            SyntaxFactory.Token(SyntaxKind.IsKeyword),
                            SyntaxFactory.Token(relationalOperator.OperatorTokenKind),
                            DirectCast(p.Value, ExpressionSyntax))
                    End Function,
                    Function(p) As CaseClauseSyntax
                        Throw ExceptionUtilities.UnexpectedValue(p.GetType())
                    End Function)
            End Function

            Public Overrides ReadOnly Property Title As String
                Get
                    Return VBFeaturesResources.Convert_to_Select_Case
                End Get
            End Property

            Public Overrides Function HasUnreachableEndPoint(operation As IOperation) As Boolean
                Dim statements = GetStatements(operation.Syntax)
                Return Not (statements.Count = 0 OrElse operation.SemanticModel.AnalyzeControlFlow(statements.First(), statements.Last()).EndPointIsReachable)
            End Function

            Public Overrides Function CanConvert(operation As IConditionalOperation) As Boolean
                Select Case operation.Syntax.Kind
                    Case SyntaxKind.MultiLineIfBlock,
                         SyntaxKind.SingleLineIfStatement,
                         SyntaxKind.ElseIfBlock
                        Return True
                    Case Else
                        Return False
                End Select
            End Function

            Public Overrides ReadOnly Property SupportsCaseGuard As Boolean = False
            Public Overrides ReadOnly Property SupportsRangePattern As Boolean = True
            Public Overrides ReadOnly Property SupportsTypePattern As Boolean = False
            Public Overrides ReadOnly Property SupportsSourcePattern As Boolean = False
            Public Overrides ReadOnly Property SupportsRelationalPattern As Boolean = True
            Public Overrides ReadOnly Property SupportsSwitchExpression As Boolean = False
        End Class
    End Class
End Namespace

