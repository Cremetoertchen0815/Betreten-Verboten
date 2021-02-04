Namespace Networking
    Public Class Game
        Public Property Key As Integer
        Public Property Name As String
        Public Property Players As Player() = {Nothing, Nothing, Nothing, Nothing}
        Public Property Ended As Boolean = False
        Public Property HostConnection As Connection
        Public Function GetPlayerCount() As Integer
            Dim cnt As Integer = 0
            For Each element In Players
                If element IsNot Nothing AndAlso element.Bereit Then cnt += 1
            Next
            Return cnt
        End Function
        Public Function GetOnlinePlayerCount() As Integer
            Dim cnt As Integer = 0
            For Each element In Players
                If element IsNot Nothing Then cnt += 1
            Next
            Return cnt
        End Function
    End Class
End Namespace