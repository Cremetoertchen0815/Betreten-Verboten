

Imports Microsoft.Xna.Framework

Namespace Framework.Tweening

    <TestState(TestState.Finalized)>
    Public Class TimerTransition
        Implements ITransition

        Sub New(time As Integer, FinishAction As FinishedDelegate, Optional stubborn As Boolean = False)
            Me.Method = New TransitionTypes.TransitionType_Linear(time)
            Me.FinishAction = FinishAction
            Me.State = TransitionState.Idle
            Me.Stubborn = stubborn
        End Sub

        Sub New()
            Me.State = TransitionState.Idle
        End Sub

        Sub Update(gameTime As GameTime) Implements ITransition.Update
            If Enabled And State = TransitionState.InProgress Then
                Timer += gameTime.ElapsedGameTime.TotalMilliseconds

                Dim completed As Boolean
                Method.onTimer(Timer, Percentage, completed)

                If completed Then
                    State = TransitionState.Done
                    TriggerAction()
                End If
            End If
        End Sub

        Public Sub Prepare() Implements ITransition.Prepare

        End Sub

        Private Sub TriggerAction()
            If FinishAction IsNot Nothing Then FinishAction.Invoke(Me)
            RaiseEvent TransitionCompletedEvent(Me, New EventArgs)
        End Sub

        Public Percentage As Double
        Public Property Method As ITransitionType Implements ITransition.Method
        Public Property FinishAction As FinishedDelegate 'A delegate to be executed when the transition is complete/the transition loops
        Public Property Enabled As Boolean = True
        Public Property Stubborn As Boolean = False Implements ITransition.Stubborn 'Indicates that a transition can't be removed by a non-stubborn clear command
        Public Property State As TransitionState Implements ITransition.State
        Public ReadOnly Property ElapsedTime As Integer
            Get
                Return Timer
            End Get
        End Property

        Private Timer As Integer 'Keeps track of the elapsed time

        Public Event TransitionCompletedEvent(sender As Object, e As EventArgs) 'An event to be executed when the transition is complete/the transition loops
        Public Delegate Sub FinishedDelegate(sender As TimerTransition) 'A delegate to be executed when the transition is complete/the transition loops

    End Class

End Namespace
