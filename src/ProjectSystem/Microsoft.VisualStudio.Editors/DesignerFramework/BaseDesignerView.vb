Imports System
Imports System.Collections
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports System.Windows.Forms.Design

Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.DesignerFramework
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VSDesigner.VSDesignerPackage

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

    Friend MustInherit Class BaseDesignerView
        Inherits System.Windows.Forms.UserControl

        'True if the designer's view was forcibly closed (by SCC) during an apply.  In this case, we want to
        '  delay disposing our controls, and exit the apply and events as soon as possible to avoid possible
        '  problems since the project may have been closed down from under us.
        Private m_ProjectReloadedDuringCheckout As Boolean

        'When positive, the designer is in a project checkout section, which means that the project
        '  file might get checked out, which means that it is possible the checkout will cause a reload
        '  of the project.  
        Private m_CheckoutSectionCount As Integer




#Region "Rude checkout support"

        ''' <summary>
        '''Before any code which may check out the project file, a designer must call this function.  This
        '''  alerts the page to the fact that we might get an unexpected Dispose() during this period, and if so,
        '''  to interpret it as meaning that the project file was checked out and updated, causing a project
        '''  reload.
        ''' </summary>
        ''' <remarks></remarks>
        Protected Friend Sub EnterProjectCheckoutSection()
            Debug.Assert(m_CheckoutSectionCount >= 0, "Bad m_CheckoutCriticalSectionCount count")
            m_CheckoutSectionCount = m_CheckoutSectionCount + 1
        End Sub


        ''' <summary>
        '''After any code which may check out the project file, a designer must call this function.  This
        '''  alerts the page to the fact that the code which might cause a project checkout is finished running.
        '''  If a Dispose occurred during the interval between the EnterProjectCheckoutSection and 
        '''  LeaveProjectCheckoutSection calls, the disposal of the controls on the designer view 
        '''  will be delayed by via a PostMessage() call to allow the designer view to more easily recover from 
        '''  this situation.  The flag ReloadedDuringCheckout will be set to true.  After the project file checkout 
        '''  is successful, callers should check this flag and exit as soon as possible if it is true.  If it's true, 
        '''  the project file probably has been zombied, and the latest changes to the designer made by the user will 
        '''  be lost, so there will be no need to attempt to persist any changes.
        ''' 
        ''' Expected coding pattern:
        ''' 
        '''  EnterProjectCheckoutSection()
        '''  Try
        '''    ...
        '''    CallMethodWhichMayCauseProjectFileCheckout
        '''    If ReloadedDuringCheckout Then
        '''      Return
        '''    End If
        '''    ...
        '''  Finally
        '''    LeaveProjectCheckoutSection()
        '''  End Try
        ''' </summary>
        ''' <remarks></remarks>
        Protected Friend Sub LeaveProjectCheckoutSection()
            m_CheckoutSectionCount = m_CheckoutSectionCount - 1
            Debug.Assert(m_CheckoutSectionCount >= 0, "Mismatched EnterProjectCheckoutSection/LeaveProjectCheckoutSection calls")
            If m_CheckoutSectionCount = 0 AndAlso m_ProjectReloadedDuringCheckout Then
                Try
                    Trace.WriteLine("**** Dispose happened during a checkout.  Queueing a delayed Dispose() call for the designer view.")
                    If Not IsHandleCreated Then
                        ' We need a window handle in order to do a begin invoke...
                        CreateHandle()
                    End If
                    Debug.Assert(IsHandleCreated AndAlso Not Handle.Equals(IntPtr.Zero), "We should have a handle still.  Without it, BeginInvoke will fail.")
                    BeginInvoke(New MethodInvoker(AddressOf DelayedMyBaseDispose))
                Catch ex As Exception
                    ' At this point, all we can do is to avoid crashing the shell. 
                    Debug.Fail(String.Format("Failed to queue a delayed Dispose for the designer view: {0}", ex))
                End Try
            End If
        End Sub


        ''' <summary>
        ''' If true, the project has been reloaded between a call to EnterProjectCheckoutSection and 
        '''   LeaveProjectCheckoutSection.  See EnterProjectCheckoutSection() for more information.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Friend ReadOnly Property ProjectReloadedDuringCheckout() As Boolean
            Get
                Return m_ProjectReloadedDuringCheckout
            End Get
        End Property


        ''' <summary>
        ''' If true, a call to EnterProjectCheckoutSection has been made, and the matching LeaveProjectCheckoutSection
        '''   call has not yet been made.
        ''' </summary>
        ''' <remarks></remarks>
        Protected ReadOnly Property IsInProjectCheckoutSection() As Boolean
            Get
                Return m_CheckoutSectionCount > 0
            End Get
        End Property


        ''' <summary>
        ''' Dispose
        ''' </summary>
        ''' <param name="disposing"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If IsInProjectCheckoutSection Then
                    'It is possible for a source code checkout operation to cause a project reload (and thus the closing/disposal of 
                    '  the designer and its view).  If we are in the middle of handling a WinForms events, it is difficult for WinForms 
                    '  to gracefully recover from the disposal of the controls, so we delay the main Dispose() until after the section
                    '  that caused the checkout is done.
                    'We do go ahead and get rid of COM references and do general clean-up, though.  This includes removing our 
                    '  listening to events from the environment, etc.
                    'We do *not* call in to the base's Dispose(), because that will get rid of the controls, and that's what we're
                    '  trying to avoid right now.

                    Trace.WriteLine("***** BaseDesignerView.Dispose(): Being forcibly disposed during an checkout.  Disposal of controls will be delayed until after the current callstack is finished.")
                    m_ProjectReloadedDuringCheckout = True
                    Return
                End If
            End If

            MyBase.Dispose(disposing)
        End Sub


        ''' <summary>
        ''' Called in a delayed fashion (via PostMessage) after a LeaveProjectCheckoutSection call if the
        '''   project was forcibly reloaded during the project checkout section.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub DelayedMyBaseDispose()
            'Set this flag back to false so that subclasses which override Dispose() know when it's
            '  safe to Dispose of their controls.
            m_ProjectReloadedDuringCheckout = False
            MyBase.Dispose(True)
        End Sub

#End Region

    End Class

End Namespace
