Imports System.Collections.Generic
Imports System.IO
Imports System.Net.Sockets
Namespace Networking
    Public Class Client
        Public Property Connected As Boolean = False
        Public Property Hostname As String

        Private stream As NetworkStream
        Private streamw As StreamWriter
        Private streamr As StreamReader
        Private client As New TcpClient
        Private blastmode As Boolean
        Public Sub Connect(hostname As String, nickname As String)

            Try
                client.Connect(hostname, 187) ' hier die ip des servers eintragen. 

                If client.Connected Then
                    stream = client.GetStream
                    streamw = New StreamWriter(stream) With {.AutoFlush = True}
                    streamr = New StreamReader(stream)
                    If Not streamr.ReadLine() = "Hello there!" Then Throw New NotImplementedException()
                    streamw.WriteLine("Wassup?")
                    If Not streamr.ReadLine() = "What's your name?" Then Throw New NotImplementedException()
                    streamw.WriteLine(nickname)
                    If Not streamr.ReadLine() = "Alrighty!" Then
                        Microsoft.VisualBasic.MsgBox("Username already taken on this server! Please change username!")
                        Exit Sub
                    End If
                    Connected = True
                    blastmode = False
                    Me.Hostname = hostname
                Else
                    Throw New NotImplementedException()
                End If
            Catch ex As Exception
                Microsoft.VisualBasic.MsgBox("Verbindung zum Server nicht möglich!")
            End Try
        End Sub

        Public Sub Disconnect()
            If client.Connected Then
                stream.Close()
                client.Close()
            End If

            Connected = False
        End Sub

        Public Function GetGamesList() As OnlineGameInstance()
            'Kein Zugriff auf diese Daten wenn in Blastmodus oder Verbindung getrennt
            If blastmode Or Not Connected Then Return {}

            Try
                Dim lst As New List(Of OnlineGameInstance)
                streamr.DiscardBufferedData()
                streamw.WriteLine("list")
                Do
                    Dim firstline As String = streamr.ReadLine()
                    If firstline <> "That's it!" Then
                        Dim gaem As New OnlineGameInstance With {.Key = CInt(firstline),
                                                                 .Name = streamr.ReadLine(),
                                                                 .Players = CInt(streamr.ReadLine())}
                        lst.Add(gaem)
                    Else
                        Exit Do
                    End If
                Loop
                Return lst.ToArray
            Catch ex As Exception
                Disconnect()
                Return {}
            End Try
        End Function

        Public Function GetOnlineMemberCount() As Integer
            'Kein Zugriff auf diese Daten wenn in Blastmodus oder Verbindung getrennt
            If blastmode Or Not Connected Then Return 0

            Try
                streamw.WriteLine("membercount")
                Return CInt(streamr.ReadLine)
            Catch ex As Exception
                Disconnect()
                Return 0
            End Try
        End Function
    End Class

End Namespace