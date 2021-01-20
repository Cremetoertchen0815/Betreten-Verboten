Namespace Framework.Scripting
    Public Structure TransitionArgs
        Public TriggerTime As Integer
        Public Path As String
        Public Mode As String
        Public AimValue As Object
        Public Duration As Integer
        Public [Object] As Object

        Sub New(TriggerTime As Integer, Path As String, [Object] As Object, Mode As String, Duration As Integer, AimValue As Object)
            Me.TriggerTime = TriggerTime
            Me.Path = Path
            Me.[Object] = [Object]
            Me.Mode = Mode
            Me.AimValue = AimValue
            Me.Duration = Duration
        End Sub
    End Structure
End Namespace