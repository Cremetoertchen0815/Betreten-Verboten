Imports System.Collections.Generic
Imports System.IO
Imports System.Net.Sockets
Imports System.Threading

Namespace Networking
    Public Class Client
        Public Property Connected As Boolean = False
        Public Property Hostname As String

        Public Property AutomaticRefresh As Boolean
            Get
                Return _AutomaticRefresh
            End Get
            Set(value As Boolean)
                _AutomaticRefresh = value
            End Set
        End Property

        Private stream As NetworkStream
        Private streamw As StreamWriter
        Private streamr As StreamReader
        Private client As TcpClient
        Private blastmode As Boolean
        Private listener As Thread
        Private data As New List(Of String)
        Private _AutomaticRefresh As Boolean = True
        Public Sub Connect(hostname As String, nickname As String)

            Try
                client = New TcpClient
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

        'Gibt an, ob ein Server bereits läuft
        Friend Function TryConnect() As Boolean
            Using cl As New TcpClient
                Try
                    cl.Connect("127.0.0.1", 187)
                    Return True
                Catch ex As Exception
                    Return False
                End Try
            End Using
        End Function

        Public Sub Disconnect()
            If client.Connected Then
                streamw.WriteLine("I'm outta here!")
                streamw.Close()
                streamr.Close()
                stream.Close()
                client.Close()
            End If

            Connected = False
        End Sub

        Friend Function ReadStream() As String()
            SyncLock data
                Dim dataS As String() = data.ToArray
                data.Clear()
                Return dataS
            End SyncLock
        End Function

        Friend Sub WriteStream(msg As String)
            streamw.WriteLine(msg)
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

        Public Function JoinGame(id As Integer, ByRef index As Integer, ByRef names As String()) As Boolean
            'Kein Zugriff auf diese Daten wenn in Blastmodus oder Verbindung getrennt
            If blastmode Or Not Connected Then Return False

            streamw.WriteLine("join")
            streamw.WriteLine(id)
            index = CInt(streamr.ReadLine())
            For i As Integer = 0 To 3
                names(i) = streamr.ReadLine
            Next
            streamw.WriteLine("Okidoki!")
            If streamr.ReadLine <> "LET'S HAVE A BLAST!" Then Return False
            blastmode = True
            data.Clear()
            listener = New Thread(AddressOf MainClientListenerSub)
            listener.Start()
            Return True
        End Function

        Public Function CreateGame(name As String, types As Player()) As Boolean
            'Kein Zugriff auf diese Daten wenn in Blastmodus oder Verbindung getrennt
            If blastmode Or Not Connected Then Return False

            streamw.WriteLine("create")
            streamw.WriteLine(name)
            For i As Integer = 0 To 3
                streamw.WriteLine(CInt(types(i).Typ).ToString)
                If types(i).Typ <> SpielerTyp.Online Then streamw.WriteLine(types(i).Name)
            Next
            streamw.WriteLine("Okidoki!")
            Dim rdl As String = streamr.ReadLine
            If rdl <> "LET'S HAVE A BLAST!" Then
                Return False
            End If
            blastmode = True
            data.Clear()
            listener = New Thread(AddressOf MainClientListenerSub)
            listener.Start()
            Return True
        End Function

        Private Sub MainClientListenerSub()
            Try
                While blastmode
                    Dim tmp As String = streamr.ReadLine
                    data.Add(tmp)
                End While
            Catch
                Disconnect()
            End Try
        End Sub

    End Class

End Namespace