﻿Imports System.Runtime.InteropServices
Imports Betreten_Verboten.Framework.Misc

Namespace Framework.Tweening.TransitionTypes

    <TestState(TestState.Finalized)>
    Public Class TransitionType_Parabole
        Implements ITransitionType

        Public Sub New(ByVal iTransitionTime As Integer)
            If iTransitionTime <= 0 Then
                Throw New Exception("Transition time must be greater than zero.")
            End If

            m_dTransitionTime = iTransitionTime
        End Sub

        Public Sub onTimer(ByVal iTime As Integer, <Out> ByRef dPercentage As Double, <Out> ByRef bCompleted As Boolean) Implements ITransitionType.onTimer
            Dim xpos = (iTime / m_dTransitionTime)
            dPercentage = -4 * (xpos - 0.5) ^ 2 + 1

            If xpos >= 1.0 Then
                dPercentage = 1.0
                bCompleted = True
            Else
                bCompleted = False
            End If
        End Sub

        Private m_dTransitionTime As Double = 0.0
    End Class
End Namespace