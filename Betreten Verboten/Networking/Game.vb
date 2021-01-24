Namespace Networking
    Public Class Game
        Public Property Name As String
        Public Property Players As Player() = {Nothing, Nothing, Nothing, Nothing}
        'Public Property LocalUserPlayerID As Integer
        Public Property HostConnection As Connection
        Public Function GetPlayerCount() As Integer
            Dim cnt As Integer = 0
            For Each element In Players
                If Players IsNot Nothing Then cnt += 1
            Next
            Return cnt
        End Function
    End Class
End Namespace