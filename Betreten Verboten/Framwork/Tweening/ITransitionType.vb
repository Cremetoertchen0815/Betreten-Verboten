Imports System.Runtime.InteropServices
Imports Betreten_Verboten.Framework.Misc

Namespace Framework.Tweening

    <TestState(TestState.Finalized)>
    Public Interface ITransitionType
        Sub onTimer(ByVal iTime As Integer, <Out> ByRef dPercentage As Double, <Out> ByRef bCompleted As Boolean)
    End Interface
End Namespace
