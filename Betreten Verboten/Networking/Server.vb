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
                                Dim playercount As Integer = element.Value.GetPlayerCount
                                If playercount < 4 Then
                                    con.streamw.WriteLine(element.Key.ToString)
                                    con.streamw.WriteLine(element.Value.Name.ToString)
                                    con.streamw.WriteLine(playercount.ToString)
                                End If
                            Next
                            con.streamw.WriteLine("EOF")
                        Case "join"
                            Try
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
                                con.streamw.WriteLine("Okidoki!")
                            Catch ex As Exception

                            End Try
                    End Select
                Loop
                con.streamw.WriteLine("Bye!")
            Catch ' die aktuelle überwachte verbindung hat sich wohl verabschiedet.
            End Try
            list.Remove(con)
        End Sub

        Private Function AlreadyContainsNickname(nick As String) As Boolean
            For Each element In list
                If element.nick = nick Then Return True
            Next
            Return False
        End Function
    End Module
End Namespace
