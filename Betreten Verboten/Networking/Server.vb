Imports System.Collections.Generic
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Threading

Namespace Networking
    Module Server
        Public Property ServerActive As Boolean

        Private MainThread As Thread
        Private server As TcpListener
        Private client As New TcpClient
        Private endpoint As IPEndPoint = New IPEndPoint(IPAddress.Any, 187)
        Private list As New List(Of Connection)
        Private games As New Dictionary(Of Integer, Game)
        Private RNG As New Random

        Public Sub StartServer()
            MainThread = New Thread(AddressOf ServerMainSub)
            MainThread.Start()
            ServerActive = True
        End Sub

        Private Sub SendToAllClients(ByVal s As String)
            For Each c As Connection In list ' an alle clients weitersenden.
                Try
                    c.streamw.WriteLine(s)
                Catch
                End Try
            Next
        End Sub

        Private Sub ServerMainSub()
            'Starte Server
            Try
                server = New TcpListener(endpoint)
                server.Start()


                Do ' wir warten auf eine neue verbindung...
                    Try
                        client = server.AcceptTcpClient
                        Dim c As New Connection ' und erstellen für die neue verbindung eine neue connection...
                        c.stream = client.GetStream
                        c.streamr = New StreamReader(c.stream)
                        c.streamw = New StreamWriter(c.stream) With {.AutoFlush = True}
                        list.Add(c) ' und fügen sie der liste der clients hinzu.
                        ' falls alle anderen das auch lesen sollen können, an alle clients weiterleiten. siehe SendToAllClients
                        Dim t As New Thread(AddressOf ListenToConnection)
                        t.Start(c)
                    Catch
                    End Try
                Loop

            Catch ex As Exception
                Microsoft.VisualBasic.MsgBox("Other server already active!")
            End Try

        End Sub

        Friend Sub StopServer()
            Try
                If ServerActive Then
                    ServerActive = False
                    server.Stop()
                End If
            Catch
            End Try
        End Sub


        Private Sub ListenToConnection(ByVal con As Connection)
            Try
                WriteString(con, "Hello there!")
                If Not ReadString(con) = "Wassup?" Then Exit Try
                WriteString(con, "What's your name?")
                Dim tmpusr As String = ReadString(con)
                If AlreadyContainsNickname(tmpusr) Then WriteString(con, "Sorry m8! Username already taken") : Exit Try
                con.nick = tmpusr
                WriteString(con, "Alrighty!")

                Do
                    Dim str As String = ReadString(con)
                    Select Case str
                        Case "list"
                            For Each element In games
                                If element.Value.GetPlayerCount < 4 Then
                                    WriteString(con, element.Key.ToString)
                                    WriteString(con, element.Value.Name.ToString)
                                    WriteString(con, element.Value.GetPlayerCount.ToString)
                                End If
                            Next
                            WriteString(con, "That's it!")
                        Case "join"
                            Try
                                Dim id As Integer = CInt(ReadString(con))
                                Dim gaem As Game = games(id)
                                Dim index As Integer = -1
                                If gaem.GetOnlinePlayerCount >= 4 Then
                                    If IsRejoining(gaem, con.nick, index) Then
                                        'Is Rejoining
                                        If index = -1 Then Throw New NotImplementedException
                                        WriteString(con, index)
                                        For i As Integer = 0 To 3
                                            WriteString(con, gaem.Players(i).Name)
                                        Next
                                        gaem.Players(index).Connection = con
                                        WriteString(con, "Rejoin")
                                    Else
                                        Throw New NotImplementedException
                                    End If
                                Else
                                    'Is joining from scratch
                                    For i As Integer = 0 To 3
                                        If gaem.Players(i) Is Nothing Then index = i : Exit For
                                    Next
                                    If index = -1 Then Throw New NotImplementedException
                                    gaem.Players(index) = New Player(SpielerTyp.Online) With {.Bereit = False, .Connection = con, .Name = con.nick}
                                    WriteString(con, index)
                                    For i As Integer = 0 To 3
                                        If gaem.Players(i) IsNot Nothing Then WriteString(con, gaem.Players(i).Name) Else WriteString(con, "")
                                    Next
                                    WriteString(con, "Nujoin")
                                End If

                                'Check if rejoining
                                If ReadString(con) <> "Okidoki!" Then Throw New NotImplementedException
                                WriteString(con, "LET'S HAVE A BLAST!")
                                gaem.Players(index).Bereit = True
                                EnterJoinMode(con, gaem, index)
                            Catch ex As Exception
                                WriteString(con, "Sorry m8!")
                            End Try
                        Case "create"
                            Try
                                Dim gamename As String = ReadString(con)
                                Dim key As Integer = RNG.Next
                                Dim nugaem As New Game With {.HostConnection = con, .Name = gamename, .Key = key}
                                Dim types As SpielerTyp() = {SpielerTyp.Online, SpielerTyp.Online, SpielerTyp.Online, SpielerTyp.Online}
                                For i As Integer = 0 To 3
                                    types(i) = CInt(ReadString(con))
                                    Select Case types(i)
                                        Case SpielerTyp.Local
                                            Dim name As String = ReadString(con)
                                            nugaem.Players(i) = New Player(types(i)) With {.Name = name, .Bereit = True, .Connection = con}
                                        Case SpielerTyp.CPU
                                            Dim name As String = ReadString(con)
                                            nugaem.Players(i) = New Player(types(i)) With {.Name = name, .Bereit = True}
                                        Case SpielerTyp.None
                                            nugaem.Players(i) = New Player(types(i)) With {.Bereit = True}
                                    End Select
                                Next
                                If ReadString(con) <> "Okidoki!" Then Throw New NotImplementedException
                                games.Add(key, nugaem)
                                WriteString(con, "LET'S HAVE A BLAST!")
                                EnterCreateMode(con, nugaem)
                            Catch ex As Exception
                                WriteString(con, "Sorry m8!")
                            End Try
                        Case "membercount"
                            WriteString(con, list.Count)
                        Case Else
                            Console.WriteLine("sos")
                    End Select
                Loop

                If con.stream.CanWrite Then
                    WriteString(con, "Bye!")
                    con.stream.Close()
                End If
            Catch ' die aktuelle überwachte verbindung hat sich wohl verabschiedet.
            End Try

            list.Remove(con)

        End Sub

        Private Function IsRejoining(gaem As Game, nick As String, ByRef index As Integer) As Boolean
            If gaem Is Nothing Then Return False
            For i As Integer = 0 To gaem.Players.Length - 1
                If gaem.Players(i).Name = nick Then index = i : Return True
            Next
            Return False
        End Function


        Private Function ReadString(con As Connection) As String
            Dim tmp As String = con.streamr.ReadLine
            Console.WriteLine("[I]" & tmp)
            If tmp = "I'm outta here!" Then Throw New Exception("Client disconnected!")
            Return tmp
        End Function

        Private Sub WriteString(con As Connection, str As String)
            Console.WriteLine("[O]" & str)
            con.streamw.WriteLine(str)
        End Sub

        Private Sub EnterJoinMode(con As Connection, gaem As Game, index As Integer)
            Try
                Dim break As Boolean = False
                Do Until gaem.Ended Or break
                    Dim txt As String = ReadString(con)
                    If gaem.HostConnection IsNot Nothing Then WriteString(gaem.HostConnection, index.ToString & txt)
                    If txt = "e" Then break = True
                Loop
            Catch ex As Exception
                gaem.Players(index).Bereit = False
                gaem.Players(index).Connection = Nothing
                If gaem.HostConnection IsNot Nothing Then WriteString(gaem.HostConnection, index.ToString & "e") 'If connection was interrupted, send to host that connection was interrupted and halt game
            End Try
        End Sub

        Private Sub EnterCreateMode(con As Connection, gaem As Game)
            Try
                Do Until gaem.Ended
                    Dim nl As String = ReadString(con)
                    Select Case nl(0)
                        Case "b"c
                            'If host sends that the game shall begin, unlist round
                            If games.ContainsKey(gaem.Key) Then games.Remove(gaem.Key)
                        Case "l"c, "I"c
                            'If host lost connection, end game for everyone
                            SendToAllGameClients(gaem)
                            gaem.HostConnection = Nothing
                            Exit Try
                        Case "e"c
                            'If remote client lost connection, send to all other remotes and add relist game
                            Dim who As Integer = CInt(nl(1).ToString)
                            gaem.Players(who).Bereit = False
                            If Not games.ContainsKey(gaem.Key) Then games.Add(gaem.Key, gaem)

                            For i As Integer = 1 To 3
                                If gaem.Players(i) IsNot Nothing AndAlso gaem.Players(i).Typ = SpielerTyp.Online AndAlso gaem.Players(i).Connection IsNot Nothing Then WriteString(gaem.Players(i).Connection, nl)
                            Next
                        Case Else
                            For i As Integer = 1 To 3
                                If gaem.Players(i) IsNot Nothing AndAlso gaem.Players(i).Typ = SpielerTyp.Online AndAlso gaem.Players(i).Connection IsNot Nothing Then WriteString(gaem.Players(i).Connection, nl)
                            Next
                    End Select
                Loop
            Catch ex As Exception
                SendToAllGameClients(gaem)
            End Try
            If games.ContainsKey(gaem.Key) Then games.Remove(gaem.Key)
        End Sub

        Private Sub SendToAllGameClients(gaem As Game)
            If Not gaem.Ended Then
                gaem.Ended = True
                Dim takenconnections As New List(Of Connection)
                For i As Integer = 0 To 3
                    Try
                        If gaem IsNot Nothing AndAlso gaem.Players(i) IsNot Nothing AndAlso gaem.Players(i).Connection IsNot Nothing AndAlso Not takenconnections.Contains(gaem.Players(i).Connection) Then
                            WriteString(gaem.Players(i).Connection, "Understandable, have a nice day!")
                            takenconnections.Add(gaem.Players(i).Connection)
                        End If
                    Catch
                    End Try
                Next
            End If
        End Sub

        Private Function AlreadyContainsNickname(nick As String) As Boolean
            For Each element In list
                If element.nick = nick Then Return True
            Next
            Return False
        End Function
    End Module
End Namespace
