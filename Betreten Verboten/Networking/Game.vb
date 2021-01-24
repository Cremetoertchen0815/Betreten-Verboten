Namespace Networking
    Public Class Game
        Public Property Name As String
        Public Property Players As Player()

        Public Function GetPlayerCount() As Integer
            Dim cnt As Integer = 0
            For Each element In Players
                If Players IsNot Nothing Then cnt += 1
            Next
            Return cnt
        End Function
    End Class
End Namespace