Imports System.Collections
Imports System.Collections.Generic
Imports System.Reflection
Imports Betreten_Verboten.Framework.Tweening

Namespace Framework.Scripting
    Public Class CommandParser
        Public Shared Sub SetCommand(path As String, obj As Object, value As Object)
            Try
                Dim paths As String() = path.Split("."c)
                Dim subob As Object = obj
                Dim tp As Type = subob.GetType
                Dim prop As PropertyInfo = tp.GetProperty(paths(0))
                For i As Integer = 1 To paths.Length - 1
                    subob = prop.GetValue(subob)
                    tp = subob.GetType
                    prop = tp.GetProperty(paths(i))
                Next
                prop.SetValue(subob, value)
            Catch
            End Try
        End Sub
        Public Shared Function GetCommand(path As String, obj As Object) As Object
            Try

                Dim subob As Object = Nothing
                Dim prop As PropertyInfo = Nothing
                ParsePath(path, obj, prop, subob)
                Return prop.GetValue(subob)
            Catch
                Return Nothing
            End Try
        End Function

        Private Shared Sub ParsePath(path As String, obj As Object, ByRef prop As PropertyInfo, ByRef subob As Object)
            Dim paths As String() = path.Split("."c)
            subob = obj
            Dim tp As Type = subob.GetType
            Dim en As Integer
            prop = tp.GetProperty(PathToEnumerable(paths(0), en))
            For i As Integer = 1 To paths.Length - 1
                If prop.PropertyType.GetInterface(NameOf(IList)) IsNot Nothing Then
                    Dim enn As IList = DirectCast(prop.GetValue(subob), IList)

                    subob = enn(en)
                    tp = subob.GetType
                    prop = tp.GetProperty(PathToEnumerable(paths(i), en))
                Else
                    subob = prop.GetValue(subob)
                    tp = subob.GetType
                    prop = tp.GetProperty(PathToEnumerable(paths(i), en))
                End If
            Next
        End Sub

        Private Shared Function PathToEnumerable(path As String, ByRef en As Integer) As String
            If path.Contains("("c) And path.Contains(")"c) Then
                Dim a As Integer = path.IndexOf("("c) + 1
                Dim b As Integer = path.IndexOf(")"c)
                en = CInt(path.Substring(a, b - a))
                Return path.Substring(0, a - 1)
            Else
                Return path
            End If
        End Function

        Public Shared Function GetPropertyTransition(path As String, obj As Object, TransType As String, time As Integer, aim As Object) As PropertyTransition
            'Try
            Dim tt As ITransitionType
            Select Case TransType
                Case "Acceleration"
                    tt = New TransitionTypes.TransitionType_Acceleration(time)
                Case "Bounce"
                    tt = New TransitionTypes.TransitionType_Bounce(time)
                Case "CriticalDamping"
                    tt = New TransitionTypes.TransitionType_CriticalDamping(time)
                Case "Deceleration"
                    tt = New TransitionTypes.TransitionType_Deceleration(time)
                Case "EaseInEaseOut"
                    tt = New TransitionTypes.TransitionType_EaseInEaseOut(time)
                Case "Linear"
                    tt = New TransitionTypes.TransitionType_Linear(time)
                Case "ThrowAndCatch"
                    tt = New TransitionTypes.TransitionType_ThrowAndCatch(time)
                Case Else
                    Return Nothing
            End Select

            Dim subob As Object = Nothing
            Dim prop As PropertyInfo = Nothing
            ParsePath(path, obj, prop, subob)
            Return New PropertyTransition(tt, subob, prop, aim, Nothing)
            'Catch ex As Exception
            '    Return Nothing
            'End Try
        End Function
    End Class

End Namespace