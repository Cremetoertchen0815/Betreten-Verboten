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
                        Dim t As New Threading.Thread(AddressOf ListenToConnection)
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
                con.streamw.WriteLine("Hello there!")
                If Not ReadString(con) = "Wassup?" Then Exit Try
                con.streamw.WriteLine("What's your name?")
                Dim tmpusr As String = ReadString(con)
                If AlreadyContainsNickname(tmpusr) And False Then con.streamw.WriteLine("Sorry m8! USername already taken") : Exit Try
                con.nick = tmpusr
                con.streamw.WriteLine("Alrighty!")

                Do
                    Dim str As String = ReadString(con)
                    Select Case str
                        Case "list"
                            For Each element In games
                                If element.Value.GetPlayerCount < 4 Then
                                    con.streamw.WriteLine(element.Key.ToString)
                                    con.streamw.WriteLine(element.Value.Name.ToString)
                                    con.streamw.WriteLine(element.Value.GetPlayerCount.ToString)
                                End If
                            Next
                            con.streamw.WriteLine("That's it!")
                        Case "join"
                            Try
                                Dim id As Integer = CInt(ReadString(con))
                                Dim gaem As Game = games(id)
                                If gaem.GetPlayerCount >= 4 Then Throw New NotImplementedException
                                Dim index As Integer = -1
                                For i As Integer = 0 To 3
                                    If gaem.Players(i) Is Nothing Then index = i : Exit For
                                Next
                                If index = -1 Then Throw New NotImplementedException
                                gaem.Players(index) = New Player(SpielerTyp.Online) With {.Bereit = False, .Connection = con, .Name = con.nick}
                                con.streamw.WriteLine(index)
                                For i As Integer = 0 To 3
                                    con.streamw.WriteLine(gaem.Players(i).Name)
                                Next
                                If ReadString(con) <> "Okidoki!" Then Throw New NotImplementedException
                                con.streamw.WriteLine("LET'S HAVE A BLAST!")
                                EnterJoinMode(con, gaem, index)
                            Catch ex As Exception
                                con.streamw.WriteLine("Sorry m8!")
                            End Try
                        Case "create"
                            Try
                                Dim gamename As String = ReadString(con)
                                Dim nugaem As New Game With {.HostConnection = con, .Name = gamename}
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
                                    End Select
                                Next
                                If ReadString(con) <> "Okidoki!" Then Throw New NotImplementedException
                                games.Add(RNG.Next, nugaem)
                                con.streamw.WriteLine("LET'S HAVE A BLAST!")
                                EnterCreateMode(con, nugaem)
                            Catch ex As Exception
                                con.streamw.WriteLine("Sorry m8!")
                            End Try
                        Case "membercount"
                            con.streamw.WriteLine(list.Count)
                        Case Else
                            Console.WriteLine("sos")
                    End Select
                Loop

                If con.stream.CanWrite Then
                    con.streamw.WriteLine("Bye!")
                    con.stream.Close()
                End If
                Catch ' die aktuelle überwachte verbindung hat sich wohl verabschiedet.
            End Try

            list.Remove(con)

        End Sub

        Private Function ReadString(con As Connection) As String
            Dim tmp As String = con.streamr.ReadLine
            Console.WriteLine(tmp)
            If tmp = "I'm outta here!" Then Throw New SocketException("Client disconnected!")
            Return tmp
        End Function

        Private Sub EnterJoinMode(con As Connection, gaem As Game, index As Integer)
            'Warte auf alle Spieler
            Do
                gaem.HostConnection.streamw.WriteLine(index.ToString & con.streamr.ReadLine)
            Loop
        End Sub

        Private Sub EnterCreateMode(con As Connection, gaem As Game)
            'Warte auf alle Spieler
            Do
                Dim nl As String = con.streamr.ReadLine
                For i As Integer = 1 To 3
                    With gaem.Players(i)
                        If .Typ = SpielerTyp.Online AndAlso .Connection IsNot Nothing Then .Connection.streamw.WriteLine(nl)
                    End With
                Next
            Loop
        End Sub

        Private Function AlreadyContainsNickname(nick As String) As Boolean
            For Each element In list
                If element.nick = nick Then Return True
            Next
            Return False
        End Function
    End Module
End Namespace
