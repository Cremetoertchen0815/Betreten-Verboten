Imports System.IO
Imports System.Net.Sockets

Namespace Networking
    Public Class Connection
        Public Property stream As NetworkStream
        Public Property streamw As StreamWriter
        Public Property streamr As StreamReader
        Public Property nick As String ' natürlich optional, aber für die identifikation des clients empfehlenswert.
        Public Property BlastReady As Boolean = False
    End Class
End Namespace
