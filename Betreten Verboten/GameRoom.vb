Imports System
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

''' <summary>
''' Enthällt den eigentlichen Code für das Basis-Spiel
''' </summary>
Public Class GameRoom

    'Spiele-Flags und Variables
    Private Spielers As Player() = {Nothing, Nothing, Nothing, Nothing} 'Enthält sämtliche Spieler, die an dieser Runde teilnehmen
    Private SpielerIndex As Integer 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
    Private Status As SpielStatus
    Private WürfelWert As Integer
    Private WürfelTimer As Double

    'Assets
    Private WürfelAugen As Texture2D
    Private WürfelRahmen As Texture2D
    Private RNG As Random 'Zufallsgenerator

    Friend Sub Init()
        'Bereite Flags und Variablen vor
        Status = SpielStatus.WarteAufOnlineSpieler
        WürfelTimer = 0
        'DEBUG: Setze sinnvolle Werte in Variablen ein, da das Menu noch nicht funktioniert.
        Spielers = {New Player, New Player, New Player, New Player}
    End Sub

    Friend Sub LoadContent()
        'Lade Assets
        WürfelAugen = Content.Load(Of Texture2D)("würfel_augen")
        WürfelRahmen = Content.Load(Of Texture2D)("würfel_rahmen")
        RNG = New Random()
    End Sub

    Friend Sub Draw(ByVal gameTime As GameTime)
        SpriteBatch.Begin()
        SpriteBatch.Draw(WürfelAugen, New Rectangle(100, 100, 250, 250), GetWürfelSourceRectangle(WürfelWert), Color.White)
        SpriteBatch.Draw(WürfelRahmen, New Rectangle(100, 100, 250, 250), Color.White)
        SpriteBatch.End()
    End Sub

    Friend Sub Update(ByVal gameTime As GameTime)
        Dim mstate As MouseState = Mouse.GetState()
        Dim kstate As KeyboardState = Keyboard.GetState()

        Select Case Status
            Case SpielStatus.Würfel
                'Prüft und speichert, ob der Würfel-Knopf gedrückt wurde
                Dim WürfelBtnGedrückt As Boolean = New Rectangle(0, 0, 100, 100).Contains(mstate.Position) And mstate.LeftButton = ButtonState.Pressed

                'Solange Knopf gedrückt, generiere zufällige Zahl in einem Intervall von 50ms
                If WürfelBtnGedrückt Then
                    WürfelTimer += gameTime.ElapsedGameTime.TotalMilliseconds

                    If WürfelTimer > 50 Then
                        WürfelTimer = 0
                        WürfelWert = RNG.Next(1, 7)
                    End If
                ElseIf Not WürfelBtnGedrückt And WürfelWert > 0 Then 'Wenn Knopf losgelassen wurde, fahre fort mit der Figurwahl
                    Status = SpielStatus.WähleFigur
                End If
            Case SpielStatus.WähleFigur

            Case SpielStatus.FahreFelder

            Case SpielStatus.WarteAufOnlineSpieler
                'Prüfe einer die vier Spieler nicht anwesend sind, kehre zurück
                For Each sp In Spielers
                    If sp Is Nothing Then Return
                Next

                'Falls vollzählig, starte Spiel
                Status = SpielStatus.Würfel
                SpielerIndex = 0
                WürfelWert = 0
        End Select
    End Sub

#Region "Hilfsfunktionen"
    Private Function GetWürfelSourceRectangle(augenzahl As Integer) As Rectangle
        Select Case augenzahl
            Case 1
                Return New Rectangle(0, 0, 260, 260)
            Case 2
                Return New Rectangle(260, 0, 260, 260)
            Case 3
                Return New Rectangle(520, 0, 260, 260)
            Case 4
                Return New Rectangle(0, 260, 260, 260)
            Case 5
                Return New Rectangle(260, 260, 260, 260)
            Case 6
                Return New Rectangle(520, 260, 260, 260)
            Case Else
                Return New Rectangle(0, 0, 0, 0)
        End Select
    End Function
#End Region

End Class