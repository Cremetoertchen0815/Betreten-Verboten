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


        Public Sub StopServer()
            ServerActive = False
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
            server = New TcpListener(endpoint)
            server.Start()

            While ServerActive ' wir warten auf eine neue verbindung...
                client = server.AcceptTcpClient
                Dim c As New Connection ' und erstellen für die neue verbindung eine neue connection...
                c.stream = client.GetStream
                c.streamr = New StreamReader(c.stream)
                c.streamw = New StreamWriter(c.stream) With {.AutoFlush = True}
                list.Add(c) ' und fügen sie der liste der clients hinzu.
                ' falls alle anderen das auch lesen sollen können, an alle clients weiterleiten. siehe SendToAllClients
                Dim t As New Threading.Thread(AddressOf ListenToConnection)
                t.Start(c)
            End While
        End Sub


        Private Sub ListenToConnection(ByVal con As Connection)
            Try
                con.streamw.WriteLine("Hello there!")
                If Not con.streamr.ReadLine() = "Wassup?" Then Exit Try
                con.streamw.WriteLine("What's your name?")
                Dim tmpusr As String = con.streamr.ReadLine()
                If AlreadyContainsNickname(tmpusr) Then Exit Try
                con.nick = tmpusr
                con.streamw.WriteLine("Alrighty!")
                Do While ServerActive
                    Select Case con.streamr.ReadLine()
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
                                ''sd
                                Dim id As Integer = CInt(con.streamr.ReadLine)
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
                                If con.streamr.ReadLine() <> "Okidoki!" Then Throw New NotImplementedException
                                con.streamw.WriteLine("LET'S HAVE A BLAST!")
                                EnterGameBlastMode(con, gaem)
                            Catch ex As Exception
                                con.streamw.WriteLine("Sorry m8!")
                            End Try
                        Case "create"
                            Try
                                Dim gamename As String = con.streamr.ReadLine
                                Dim nugaem As New Game With {.HostConnection = con, .Name = gamename}
                                Dim types As SpielerTyp() = {SpielerTyp.Online, SpielerTyp.Online, SpielerTyp.Online, SpielerTyp.Online}
                                For i As Integer = 0 To 3
                                    types(i) = CInt(con.streamr.ReadLine())
                                    Select Case types(i)
                                        Case SpielerTyp.Local
                                            Dim name As String = con.streamr.ReadLine
                                            nugaem.Players(i) = New Player(types(i)) With {.Name = name, .Bereit = False, .Connection = con}
                                        Case SpielerTyp.CPU
                                            Dim name As String = con.streamr.ReadLine
                                            nugaem.Players(i) = New Player(types(i)) With {.Name = name, .Bereit = False}
                                    End Select
                                Next
                                If con.streamr.ReadLine() <> "Okidoki!" Then Throw New NotImplementedException
                                games.Add(RNG.Next, nugaem)
                                con.streamw.WriteLine("LET'S HAVE A BLAST!")
                                EnterGameBlastMode(con, nugaem)
                            Catch ex As Exception
                                con.streamw.WriteLine("Sorry m8!")
                            End Try
                        Case "membercount"
                            con.streamw.WriteLine(list.Count)
                    End Select
                Loop
            Catch ' die aktuelle überwachte verbindung hat sich wohl verabschiedet.
            End Try

            If con.stream.CanWrite Then
                con.streamw.WriteLine("Bye!")
                con.stream.Close()
            End If
            list.Remove(con)
        End Sub

        Private Sub EnterGameBlastMode(con As Connection, gaem As Game)
            'Warte auf alle Spieler
            Do
                For i As Integer = 0 To 3

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
