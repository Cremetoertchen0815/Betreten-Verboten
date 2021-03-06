﻿Imports System.Collections.Generic
Imports System.IO
Imports System.Net.Sockets
Imports System.Threading

Namespace Networking
    Public Class Client
        Public Property Connected As Boolean = False
        Public Property Hostname As String
        Public Property IsHost As Boolean
        Public Property LeaveFlag As Boolean = False
        Public Property AutomaticRefresh As Boolean = True

        Private stream As NetworkStream
        Private streamw As StreamWriter
        Private streamr As StreamReader
        Private client As TcpClient
        Private blastmode As Boolean
        Private listener As Thread
        Private data As New List(Of String)

        Public Sub Connect(hostname As String, nickname As String)

            Try
                My.Settings.IP = hostname
                My.Settings.Save()
                client = New TcpClient
                client.Connect(hostname, 187) ' hier die ip des servers eintragen. 

                If client.Connected Then
                    stream = client.GetStream
                    streamw = New StreamWriter(stream) With {.AutoFlush = True}
                    streamr = New StreamReader(stream)
                    If Not ReadString() = "Hello there!" Then Throw New NotImplementedException()
                    WriteString("Wassup?")
                    If Not ReadString() = "What's your name?" Then Throw New NotImplementedException()
                    WriteString(nickname)
                    If Not ReadString() = "Alrighty!" Then
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
                WriteErrorToFile(ex)
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
                    WriteErrorToFile(ex)
                    Return False
                End Try
            End Using
        End Function

        Public Sub Disconnect()
            If client.Connected Then
                WriteString("I'm outta here!")
                streamw.Close()
                streamr.Close()
                stream.Close()
                client.Close()
            End If

            Connected = False
            blastmode = False
        End Sub

        Private Function ReadString() As String
            Try
                Dim tmp As String = streamr.ReadLine
                Console.WriteLine("[Client/I]" & tmp)
                Return tmp
            Catch ex As Exception
                WriteErrorToFile(ex)
                Disconnect()
                Return ""
            End Try
        End Function

        Private Sub WriteString(str As String)
            Try
                Console.WriteLine("[Client/O]" & str)
                If str = "Ich putz hier mal durch." Then Console.WriteLine()
                streamw.WriteLine(str)
            Catch ex As Exception
                WriteErrorToFile(ex)
                Disconnect()
            End Try
        End Sub

        Friend Function ReadStream() As String()
            SyncLock data
                Dim dataS As String() = data.ToArray
                data.Clear()
                Return dataS
            End SyncLock
        End Function

        Friend Sub WriteStream(msg As String)
            If (msg(0) = "l"c And IsHost) Or (msg(0) = "e"c And Not IsHost) Then blastmode = False
            If Connected Then WriteString(msg)
        End Sub

        Public Function GetGamesList() As OnlineGameInstance()
            'Kein Zugriff auf diese Daten wenn in Blastmodus oder Verbindung getrennt
            If blastmode Or Not Connected Then Return {}

            Try
                Dim lst As New List(Of OnlineGameInstance)
                WriteString("list")
                Do
                    Dim firstline As String = ReadString()
                    If firstline <> "That's it!" Then
                        Dim gaem As New OnlineGameInstance With {.Key = CInt(firstline),
                                                                 .Name = ReadString(),
                                                                 .Players = CInt(ReadString()),
                                                                 .PlayerCount = CInt(ReadString())}
                        lst.Add(gaem)
                    Else
                        Exit Do
                    End If
                Loop
                Return lst.ToArray
            Catch ex As Exception
                WriteErrorToFile(ex)
                Disconnect()
                Return {}
            End Try
        End Function

        Public Function GetOnlineMemberCount() As Integer
            'Kein Zugriff auf diese Daten wenn in Blastmodus oder Verbindung getrennt
            If blastmode Or Not Connected Then Return 0

            Try
                WriteString("membercount")
                Return CInt(ReadString())
            Catch ex As Exception
                WriteErrorToFile(ex)
                Disconnect()
                Return 0
            End Try
        End Function

        Public Function JoinGame(id As Integer, ByRef index As Integer, ByRef Spielers As Player(), ByRef Rejoin As Boolean, ByRef map As GaemMap) As Boolean
            'Kein Zugriff auf diese Daten wenn in Blastmodus oder Verbindung getrennt
            If blastmode Or Not Connected Then Return False
            blastmode = True

            WriteString("join")
            WriteString(id)
            map = CInt(ReadString())
            ReDim Spielers(GetMapSize(map) - 1)
            index = CInt(ReadString())
            For i As Integer = 0 To GetMapSize(map) - 1
                Dim type As SpielerTyp = CInt(ReadString())
                Dim name As String = ReadString()
                Spielers(i) = New Player(If(type = SpielerTyp.None, type, SpielerTyp.Online)) With {.Name = If(i = index, My.Settings.Username, name)}
            Next

            Rejoin = ReadString() = "Rejoin"

            WriteString("Okidoki!")
            Dim tmp As String = ReadString()
            If tmp <> "LET'S HAVE A BLAST!" Then blastmode = False : Return False
            blastmode = True
            LeaveFlag = False
            data.Clear()
            listener = New Thread(AddressOf MainClientListenerSub)
            listener.Start()
            Return True
        End Function

        Public Function CreateGame(name As String, map As GaemMap, types As Player()) As Boolean
            'Kein Zugriff auf diese Daten wenn in Blastmodus oder Verbindung getrennt
            If blastmode Or Not Connected Then Return False

            WriteString("create")
            WriteString(name)
            WriteString(CInt(map).ToString)
            For i As Integer = 0 To GetMapSize(map) - 1
                WriteString(CInt(types(i).Typ).ToString)
                If types(i).Typ <> SpielerTyp.Online And types(i).Typ <> SpielerTyp.None Then WriteString(types(i).Name)
            Next
            WriteString("Okidoki!")
            Dim rdl As String = ReadString()
            If rdl <> "LET'S HAVE A BLAST!" Then Return False
            blastmode = True
            LeaveFlag = False
            data.Clear()
            listener = New Thread(AddressOf MainClientListenerSub)
            listener.Start()
            Return True
        End Function

        Private Sub MainClientListenerSub()
            Try
                While blastmode And Not LeaveFlag
                    Dim tmp As String = ReadString()
                    If tmp.StartsWith("Sorry m8!") Then Throw New Exception() Else data.Add(tmp)
                    If tmp.StartsWith("Understandable, have a nice day!") Then LeaveFlag = True : Exit While
                    End While
                    Catch ex As Exception
                    WriteErrorToFile(ex)
                Disconnect()
            End Try
            blastmode = False
            AutomaticRefresh = True
            WriteString("Ich putz hier mal durch.")
            WriteString("Damit keine ReadLine-Commands offen bleiben.")
        End Sub

    End Class

End Namespace