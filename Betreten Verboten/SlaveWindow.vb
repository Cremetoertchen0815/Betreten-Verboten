Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports Betreten_Verboten.Framework.Graphics
Imports Betreten_Verboten.Framework.UI
Imports System.Collections.Generic
Imports Betreten_Verboten.Framework.Tweening
Imports Betreten_Verboten.Framework.Graphics.PostProcessing

''' <summary>
''' Enthällt den eigentlichen Code für das Basis-Spiel
''' </summary>
Public Class SlaveWindow
    Implements IGameWindow

    'Spiele-Flags und Variables
    Friend Spielers As Player() = {Nothing, Nothing, Nothing, Nothing} 'Enthält sämtliche Spieler, die an dieser Runde teilnehmen
    Private SpielerIndex As Integer = -1 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
    Private NetworkMode As Boolean = True
    Friend UserIndex As Integer 'Gibt den Index des Spielers an, welcher momentan durch diese Spielinstanz repräsentiert wird
    Private Status As SpielStatus 'Speichert den aktuellen Status des Spiels
    Private WürfelAktuelleZahl As Integer 'Speichert den WErt des momentanen Würfels
    Private WürfelWerte As Integer() 'Speichert die Werte der Würfel
    Private WürfelTimer As Double 'Wird genutzt um den Würfelvorgang zu halten
    Private WürfelAnimationTimer As Double 'Implementiert einen Cooldown für die Würfelanimation
    Private WürfelTriggered As Boolean 'Gibt an ob gerade gewürfelt wird
    Private StopUpdating As Boolean 'Deaktiviert die Spielelogik
    Private Fahrzahl As Integer 'Anzahl der Felder die gefahren werden kann
    Private DreifachWürfeln As Boolean 'Gibt an(am Anfang des Spiels), dass ma drei Versuche hat um eine 6 zu bekommen
    Private lastmstate As MouseState
    Private lastkstate As KeyboardState

    'Assets
    Friend Renderer As Renderer3D
    Private WürfelAugen As Texture2D
    Private WürfelRahmen As Texture2D
    Private SpielfeldVerbindungen As Texture2D
    Private Pfeil As Texture2D
    Private ButtonFont As SpriteFont
    Private ChatFont As SpriteFont
    Private RNG As Random 'Zufallsgenerator

    'HUD
    Private WithEvents HUD As GuiSystem
    Private WithEvents HUDBtnA As Controls.Button
    Private WithEvents HUDBtnB As Controls.Button
    Private WithEvents HUDBtnC As Controls.Button
    Private WithEvents HUDChat As Controls.TextscrollBox
    Private WithEvents HUDChatBtn As Controls.Button
    Private WithEvents HUDInstructions As Controls.Label
    Private WithEvents HUDNameBtn As Controls.Button
    Private InstructionFader As PropertyTransition
    Private ShowDice As Boolean = False
    Private HUDColor As Color
    Private Chat As List(Of (String, Color))

    'Spielfeld
    Private Center As Vector2
    Private transmatrices As Matrix() = {Matrix.CreateRotationZ(MathHelper.PiOver2 * 3), Matrix.Identity, Matrix.CreateRotationZ(MathHelper.PiOver2), Matrix.CreateRotationZ(MathHelper.Pi)}
    Private playcolor As Color() = {Color.Magenta, Color.Lime, Color.Cyan, Color.Orange}
    Private SelectFader As Transition(Of Single) 'Fader, welcher die zur Auswahl stehenden Figuren blinken lässt
    Friend FigurFaderZiel As (Integer, Integer) 'Gibt an welche Figur bewegt werden soll (Spieler ind., Figur ind.)
    Friend FigurFaderKickZiel As (Integer, Integer) 'Gibt an welche Figur bewegt werden soll (Spieler ind., Figur ind.)
    Friend FigurFaderEnd As Single
    Friend FigurFaderXY As Transition(Of Vector2)
    Friend FigurFaderZ As Transition(Of Integer)
    Friend FigurFaderScales As New Dictionary(Of (Integer, Integer), Transition(Of Single))
    Friend FigurFaderCamera As New Transition(Of CamKeyframe)
    'Friend FigurFaderEndMethod As Action

    Private Const FDist As Integer = 85
    Private Const WürfelDauer As Integer = 400
    Private Const WürfelAnimationCooldown As Integer = 62
    Private Const FigurSpeed As Integer = 600
    Private Const ErrorCooldown As Integer = 1200
    Private Const RollDiceCooldown As Integer = 800
    Private Const CPUThinkingTime As Integer = 1500
    Private Const DopsHöhe As Integer = 100

    Friend Sub Init()
        'Bereite Flags und Variablen vor
        Status = SpielStatus.WarteAufOnlineSpieler
        WürfelTimer = 0
        LocalClient.LeaveFlag = False
        NetworkMode = True
        SpielerIndex = -1
        LocalClient.IsHost = False
        Chat = New List(Of (String, Color))
    End Sub

    Friend Sub LoadContent()
        'Lade Assets
        WürfelAugen = Content.Load(Of Texture2D)("würfel_augen")
        WürfelRahmen = Content.Load(Of Texture2D)("würfel_rahmen")
        SpielfeldVerbindungen = Content.Load(Of Texture2D)("playfield_connections")
        Pfeil = Content.Load(Of Texture2D)("arrow_right")
        ButtonFont = Content.Load(Of SpriteFont)("font\ButtonText")
        ChatFont = Content.Load(Of SpriteFont)("font\ChatText")
        RNG = New Random()

        'Lade HUD
        HUD = New GuiSystem
        HUDBtnA = New Controls.Button("Exit Game", New Vector2(1500, 50), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDBtnA)
        HUDBtnB = New Controls.Button("Main Menu", New Vector2(1500, 200), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDBtnB)
        HUDBtnC = New Controls.Button("Anger", New Vector2(1500, 350), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDBtnC)
        HUDChat = New Controls.TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(400, 800)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow, .LenLimit = 35} : HUD.Controls.Add(HUDChat)
        HUDChatBtn = New Controls.Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDChatBtn)
        HUDInstructions = New Controls.Label("Wait for all Players to arrive...", New Vector2(50, 1005)) With {.Font = Content.Load(Of SpriteFont)("font/InstructionText"), .Color = Color.BlanchedAlmond} : HUD.Controls.Add(HUDInstructions)
        InstructionFader = New PropertyTransition(New TransitionTypes.TransitionType_EaseInEaseOut(700), HUDInstructions, "Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), Nothing) With {.Repeat = RepeatJob.Reverse} : Automator.Add(InstructionFader)
        HUDNameBtn = New Controls.Button("", New Vector2(500, 20), New Vector2(950, 30)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Black, 0), .Color = Color.Yellow} : HUD.Controls.Add(HUDNameBtn)
        HUD.Init()

        'Lade Renderer
        Renderer = New Renderer3D(Me)
        Renderer.LoadContent()

        'Lade Spielfeld
        Center = New Rectangle(500, 70, 950, 950).Center.ToVector2
        SelectFader = New Transition(Of Single)(New TransitionTypes.TransitionType_EaseInEaseOut(400), 0F, 1.0F, Nothing) With {.Repeat = RepeatJob.Reverse} : Automator.Add(SelectFader)
    End Sub

    Friend Sub PreDraw()
        Renderer.PreDraw()
    End Sub

    Private Sub StartMoverSub(Optional destination As Integer = -1)
        'Set values
        FigurFaderEnd = If(destination < 0, Math.Max(Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2), 0) + Fahrzahl, destination)
        Dim FigurFaderVectors = (GetSpielfeldVector(FigurFaderZiel.Item1, FigurFaderZiel.Item2), GetSpielfeldVector(FigurFaderZiel.Item1, FigurFaderZiel.Item2, 1))
        Status = SpielStatus.FahreFelder

        'Initiate
        If IsFieldCovered(FigurFaderZiel.Item1, FigurFaderZiel.Item2, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1) Then
            Dim key As (Integer, Integer) = GetFieldID(FigurFaderZiel.Item1, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1)
            If (Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) = FigurFaderEnd - 1) Or (Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1 = 0) Then
                Dim kickID As Integer = CheckKick(1)
                Dim trans As New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(FigurSpeed), 1, 0, Sub()
                                                                                                                              If kickID = key.Item2 Then Spielers(key.Item1).Spielfiguren(key.Item2) = -1
                                                                                                                              If FigurFaderScales.ContainsKey(key) Then FigurFaderScales.Remove(key)
                                                                                                                              Dim transB As New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(FigurSpeed), 0, 1, Nothing)
                                                                                                                              Automator.Add(transB)
                                                                                                                              FigurFaderScales.Add(key, transB)
                                                                                                                          End Sub)
                If key.Item1 >= 0 And key.Item2 >= 0 Then Automator.Add(trans) : FigurFaderScales.Add(key, trans)
            Else
                Dim trans As New Transition(Of Single)(New TransitionTypes.TransitionType_Bounce(FigurSpeed * 2), 1, 0, Nothing)
                If key.Item1 >= 0 And key.Item2 >= 0 Then Automator.Add(trans) : FigurFaderScales.Add(key, trans)
            End If
        End If
        FigurFaderXY = New Transition(Of Vector2)(New TransitionTypes.TransitionType_EaseInEaseOut(FigurSpeed), FigurFaderVectors.Item1, FigurFaderVectors.Item2, AddressOf MoverSub) : Automator.Add(FigurFaderXY)
        FigurFaderZ = New Transition(Of Integer)(New TransitionTypes.TransitionType_Parabole(FigurSpeed), 0, DopsHöhe, Nothing) : Automator.Add(FigurFaderZ)
    End Sub

    Private Sub MoverSub()
        Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) += 1

        Dim FigurFaderVectors = (GetSpielfeldVector(FigurFaderZiel.Item1, FigurFaderZiel.Item2), GetSpielfeldVector(FigurFaderZiel.Item1, FigurFaderZiel.Item2, 1))

        If Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) < FigurFaderEnd Then
            If IsFieldCovered(FigurFaderZiel.Item1, FigurFaderZiel.Item2, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1) Then
                Dim key As (Integer, Integer) = GetFieldID(FigurFaderZiel.Item1, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1)
                If Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) = FigurFaderEnd - 1 Then
                    Dim kickID As Integer = CheckKick(1)
                    Dim trans As New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(FigurSpeed), 1, 0, Sub()
                                                                                                                                  If kickID = key.Item2 Then Spielers(key.Item1).Spielfiguren(key.Item2) = -1
                                                                                                                                  If FigurFaderScales.ContainsKey(key) Then FigurFaderScales.Remove(key)
                                                                                                                                  Dim transB As New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(FigurSpeed), 0, 1, Nothing)
                                                                                                                                  Automator.Add(transB)
                                                                                                                                  FigurFaderScales.Add(key, transB)
                                                                                                                              End Sub)
                    If key.Item1 >= 0 And key.Item2 >= 0 Then Automator.Add(trans) : FigurFaderScales.Add(key, trans)
                Else
                    Dim trans As New Transition(Of Single)(New TransitionTypes.TransitionType_Bounce(FigurSpeed * 2), 1, 0, Nothing)
                    If key.Item1 >= 0 And key.Item2 >= 0 Then Automator.Add(trans) : FigurFaderScales.Add(key, trans)
                End If
            End If
            FigurFaderXY = New Transition(Of Vector2)(New TransitionTypes.TransitionType_Linear(FigurSpeed), FigurFaderVectors.Item1, FigurFaderVectors.Item2, AddressOf MoverSub) : Automator.Add(FigurFaderXY)
            FigurFaderZ = New Transition(Of Integer)(New TransitionTypes.TransitionType_Parabole(FigurSpeed), 0, DopsHöhe, Nothing) : Automator.Add(FigurFaderZ)
        End If
    End Sub

    Friend Sub Draw(ByVal gameTime As GameTime)

        Renderer.Draw(gameTime)

        SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise, Nothing, ScaleMatrix)


        'Zeichne Haupt-Würfel
        If ShowDice And SpielerIndex = UserIndex Then
            SpriteBatch.Draw(WürfelAugen, New Rectangle(1570, 700, 300, 300), GetWürfelSourceRectangle(WürfelAktuelleZahl), HUDColor)
            SpriteBatch.Draw(WürfelRahmen, New Rectangle(1570, 700, 300, 300), Color.Lerp(HUDColor, Color.White, 0.4))
        End If
        'Zeichne Mini-Würfel
        If (Status = SpielStatus.Würfel Or Status = SpielStatus.WähleFigur Or Status = SpielStatus.FahreFelder) And SpielerIndex = UserIndex Then
            For i As Integer = 0 To WürfelWerte.Length - 1
                If SpielerIndex = UserIndex And WürfelWerte(i) > 0 Then
                    SpriteBatch.Draw(WürfelAugen, New Rectangle(1590 + i * 70, 600, 50, 50), GetWürfelSourceRectangle(WürfelWerte(i)), HUDColor)
                    SpriteBatch.Draw(WürfelRahmen, New Rectangle(1590 + i * 70, 600, 50, 50), Color.Lerp(HUDColor, Color.White, 0.4))
                End If
                If Spielers(SpielerIndex).Typ = SpielerTyp.CPU And Not DreifachWürfeln And WürfelWerte(i) <> 6 Then Exit For
            Next
        End If

        SpriteBatch.End()

        HUD.Draw(gameTime)
    End Sub

    Private Sub DrawChr(vc As Vector2, color As Color)
        FillRectangle(New Rectangle(vc.X - 10, vc.Y - 10, 20, 20), color)
    End Sub

    Private Sub DrawArrow(vc As Vector2, color As Color, iteration As Integer)
        SpriteBatch.Draw(Pfeil, New Rectangle(vc.X, vc.Y, 35, 35), Nothing, color, MathHelper.PiOver2 * (iteration + 3), New Vector2(35, 35) / 2, SpriteEffects.None, 0)
    End Sub

    Private Function GetChrRect(vc As Vector2) As Rectangle
        Return New Rectangle(vc.X - 15, vc.Y - 15, 30, 30)
    End Function

    Friend Sub Update(ByVal gameTime As GameTime)
        Dim mstate As MouseState = Mouse.GetState()
        Dim kstate As KeyboardState = Keyboard.GetState()
        Dim mpos As Point = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScaleMatrix)).ToPoint

        If Not StopUpdating Then

            'Update die Spielelogik
            Select Case Status
                Case SpielStatus.Würfel

                    'Manuelles Würfeln für lokalen Spieler
                    'Prüft und speichert, ob der Würfel-Knopf gedrückt wurde
                    If (New Rectangle(1570, 700, 300, 300).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released) Or (kstate.IsKeyDown(Keys.Space) And lastkstate.IsKeyUp(Keys.Space)) Then
                        WürfelTriggered = True
                        WürfelTimer = 0
                        WürfelAnimationTimer = -1
                    End If

                    'Solange Knopf gedrückt, generiere zufällige Zahl in einem Intervall von 50ms
                    If WürfelTriggered Then

                        WürfelTimer += gameTime.ElapsedGameTime.TotalMilliseconds
                        'Implementiere einen Cooldown für die Würfelanimation
                        If Math.Floor(WürfelTimer / WürfelAnimationCooldown) <> WürfelAnimationTimer Then WürfelAktuelleZahl = RNG.Next(1, 7) : WürfelAnimationTimer = Math.Floor(WürfelTimer / WürfelAnimationCooldown)

                        If WürfelTimer > WürfelDauer Then
                            WürfelTimer = 0
                            WürfelTriggered = False
                            'Gebe Würfe-Ergebniss auf dem Bildschirm aus
                            HUDInstructions.Text = "You got a " & WürfelAktuelleZahl.ToString & "!"
                            StopUpdating = True

                            For i As Integer = 0 To WürfelWerte.Length - 1
                                If WürfelWerte(i) = 0 Then
                                    'Speiechere Würfel-Wert nach kurzer Pause und wiederhole
                                    Dim it As Integer = i 'Zwischenvariable zur problemlosen Verwendung von i im Lambda-Ausdruck in der nächsten Zeile
                                    Automator.Add(New TimerTransition(RollDiceCooldown, Sub()
                                                                                            WürfelWerte(it) = WürfelAktuelleZahl
                                                                                            StopUpdating = False
                                                                                            'Prüfe, ob Würfeln beendet werden soll
                                                                                            If it >= WürfelWerte.Length - 1 Or (Not DreifachWürfeln And Not (it = 0 And WürfelAktuelleZahl >= 6)) Or (DreifachWürfeln And it > 0 AndAlso WürfelWerte(it - 1) >= 6) Or (DreifachWürfeln And it >= 2 And WürfelWerte(2) < 6) Then CalcMoves()
                                                                                            WürfelAktuelleZahl = 0
                                                                                        End Sub))

                                    'Beende Schleife
                                    Exit For
                                End If
                            Next
                        End If
                    End If

                Case SpielStatus.WähleFigur

                    Dim pl As Player = Spielers(SpielerIndex)
                    'Manuelle Auswahl für lokale Spieler
                    For k As Integer = 0 To 3
                        Dim chr As Integer = pl.Spielfiguren(k)
                        Dim vec As Vector2 = Vector2.Zero
                        Dim matrx As Matrix = transmatrices(SpielerIndex)
                        Select Case chr
                            Case -1
                            Case 40, 41, 42, 43 'Figur in Haus
                                vec = GetSpielfeldPositionen(chr - 26)
                            Case Else 'Figur auf Feld
                                vec = GetSpielfeldPositionen((chr Mod 10) + 4)
                                matrx = transmatrices((SpielerIndex + Math.Floor(chr / 10)) Mod 4)
                        End Select

                        'Anti-Stuck-Fuck
                        Dim defaultmov As Integer = Spielers(SpielerIndex).Spielfiguren(k)

                        'Prüfe Figur nach Mouse-Klick
                        If GetChrRect(Center + Vector2.Transform(vec, matrx)).Contains(mpos) And chr > -1 And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then
                            If defaultmov + Fahrzahl > 43 Or IsFutureFieldCoveredByOwnFigure(SpielerIndex, defaultmov + Fahrzahl, k) Then
                                HUDInstructions.Text = "Incorrect move!"
                            Else
                                'Setze flags
                                Status = SpielStatus.Waitn
                                SubmitResults(k, defaultmov + Fahrzahl)
                                StopUpdating = False
                            End If
                            Exit For
                        End If
                    Next
                Case SpielStatus.SpielZuEnde
                    StopUpdating = True
            End Select

            'Set HUD color
            HUDColor = playcolor(UserIndex)
            HUDBtnA.Color = HUDColor : HUDBtnA.Border = New ControlBorder(HUDColor, HUDBtnA.Border.Width)
            HUDBtnB.Color = HUDColor : HUDBtnB.Border = New ControlBorder(HUDColor, HUDBtnB.Border.Width)
            HUDBtnC.Color = HUDColor : HUDBtnC.Border = New ControlBorder(HUDColor, HUDBtnC.Border.Width)
            HUDChat.Color = HUDColor : HUDChat.Border = New ControlBorder(HUDColor, HUDChat.Border.Width)
            HUDChatBtn.Color = HUDColor : HUDChatBtn.Border = New ControlBorder(HUDColor, HUDChatBtn.Border.Width)
            HUDNameBtn.Text = If(SpielerIndex > -1, Spielers(SpielerIndex).Name, "")
            HUDNameBtn.Color = If(SpielerIndex > -1, playcolor(SpielerIndex), Color.White)
            HUDInstructions.Active = (Status = SpielStatus.WarteAufOnlineSpieler) OrElse (SpielerIndex = UserIndex)
        End If


        Dim scheiß As New List(Of (Integer, Integer))
        For Each element In FigurFaderScales
            If element.Value.State = TransitionState.Done Then scheiß.Add(element.Key)
        Next

        For Each element In scheiß
            FigurFaderScales.Remove(element)
        Next

        'Network stuff
        If NetworkMode Then
            If Not LocalClient.Connected And Status <> SpielStatus.SpielZuEnde Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Connection lost!") : GameClassInstance.SwitchToSubmenu(0)
            If LocalClient.LeaveFlag And Status <> SpielStatus.SpielZuEnde Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Player left! Game was ended!") : GameClassInstance.SwitchToSubmenu(0)
            'If  Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Internal error!") : GameClassInstance.SwitchToSubmenu(0)
        End If

        If NetworkMode Then ReadAndProcessInputData()

        'Misc stuff
        If kstate.IsKeyDown(Keys.Escape) And lastkstate.IsKeyUp(Keys.Escape) Then MenuButton()
        HUD.Update(gameTime, mstate, Matrix.Identity)
        Renderer.Update(gameTime)
        lastmstate = mstate
        lastkstate = kstate
    End Sub

#Region "Netzwerkfunktionen"
    Private Sub ReadAndProcessInputData()
        Dim data As String() = LocalClient.ReadStream()
        For Each element In data
            Dim command As Char = element(0)
            Select Case command
                Case "a"c 'Player arrived
                    Dim source As Integer = CInt(element(1).ToString)
                    Spielers(source).Name = element.Substring(2)
                    Spielers(source).Bereit = True
                    PostChat(Spielers(source).Name & " arrived!", Color.White)
                Case "b"c 'Begin gaem
                    Status = SpielStatus.Waitn
                    PostChat("The game has started!", Color.White)
                Case "c"c 'Sent chat message
                    Dim source As Integer = CInt(element(1).ToString)
                    PostChat("[" & Spielers(source).Name & "]: " & element.Substring(2), playcolor(source))
                Case "n"c 'Next player
                    Dim who As Integer = CInt(element(1).ToString)
                    SpielerIndex = who
                    If who = UserIndex Then
                        PrepareMove()
                    Else
                        Status = SpielStatus.Waitn
                    End If
                Case "s"c 'Create transition
                    Dim playr As Integer = CInt(element(1).ToString)
                    Dim figur As Integer = CInt(element(2).ToString)
                    Dim destination As Integer = CInt(element.Substring(3).ToString)

                    Status = SpielStatus.FahreFelder
                    FigurFaderZiel = (playr, figur)
                    'Animiere wie die Figur sich nach vorne bewegt, anschließend kehre zurück zum nichts tun
                    Dim defaultmov As Integer = Math.Max(Spielers(playr).Spielfiguren(figur), 0)
                    StartMoverSub(destination)
                Case "w"c 'Spieler hat gewonnen
                    Dim playr As Integer = CInt(element(1).ToString)
                    ShowDice = False
                    HUDInstructions.Text = "Game over!"
                    PostChat(Spielers(playr).Name & " won!", Color.White)
                    Status = SpielStatus.SpielZuEnde
            End Select
        Next
    End Sub

    Friend Sub SendArrived()
        LocalClient.WriteStream("a" & My.Settings.Username)
    End Sub

    Private Sub SendChatMessage(text As String)
        LocalClient.WriteStream("c" & text)
    End Sub
    Private Sub SendGameClosed()
        LocalClient.WriteStream("l")
    End Sub

    Private Sub SubmitResults(figur As Integer, destination As Integer)
        Automator.Add(New TimerTransition(500, Sub()
                                                   If destination < 0 Then
                                                       LocalClient.WriteStream("n")
                                                   Else
                                                       LocalClient.WriteStream("s" & figur & destination)
                                                   End If
                                               End Sub))
    End Sub
#End Region

#Region "Hilfsfunktionen"
    Private Sub CalcMoves()
        Dim homebase As Integer = GetHomebaseIndex(SpielerIndex) 'Eine Spielfigur-ID, die sich in der Homebase befindet(-1, falls Homebase leer ist)
        Dim startfd As Boolean = IsFieldCoveredByOwnFigure(SpielerIndex, 0) 'Ob das Start-Feld blockiert ist
        ShowDice = False
        Fahrzahl = If(WürfelWerte(0) = 6, WürfelWerte(0) + WürfelWerte(1), WürfelWerte(0)) 'Setzt die Anzahl der zu fahrenden Felder im voraus(kann im Fall einer vollen Homebase überschrieben werden)

        If Is6InDiceList() And homebase > -1 And Not startfd Then 'Falls Homebase noch eine Figur enthält und 6 gewürfelt wurde, setze Figur auf Feld 0 und fahre anschließend x Felder nach vorne
            'Bereite das Homebase-verlassen vor
            Fahrzahl = GetSecondDiceAfterSix(SpielerIndex)
            HUDInstructions.Text = "Move Character out of your homebase and move him " & Fahrzahl & " spaces!"
            FigurFaderZiel = (SpielerIndex, homebase)
            'Animiere wie die Figur sich nach vorne bewegt, anschließend prüfe ob andere Spieler rausgeschmissen wurden
            If Not IsFieldCoveredByOwnFigure(SpielerIndex, Fahrzahl) Then
                SubmitResults(homebase, Fahrzahl)
            Else
                StopUpdating = True
                HUDInstructions.Text = "Field already covered! Move with the other piece!"
                Automator.Add(New TimerTransition(ErrorCooldown, Sub()
                                                                     Status = SpielStatus.WähleFigur
                                                                     StopUpdating = False
                                                                 End Sub))
            End If
        ElseIf Is6InDiceList() And homebase > -1 And startfd Then 'Gibt an, dass das Start-Feld von einer eigenen Figur belegt ist(welche nicht gekickt werden kann) und dass selbst beim Wurf einer 6 keine weitere Figur die Homebase verlassen kann
            HUDInstructions.Text = "Start field blocked! Move pieces out of the way first!"
            Fahrzahl = If(WürfelWerte(0) = 6, WürfelWerte(0) + WürfelWerte(1), WürfelWerte(0))
            Status = SpielStatus.WähleFigur
            StopUpdating = True
            Automator.Add(New TimerTransition(ErrorCooldown, Sub() StopUpdating = False))
        ElseIf (GetHomebaseCount(SpielerIndex) = 4 And Not Is6InDiceList()) OrElse Not CanDoAMove() Then 'Falls Homebase komplett voll ist(keine Figur auf Spielfeld) und keine 6 gewürfelt wurde(oder generell kein Zug mehr möglich ist), ist kein Zug möglich und der nächste Spieler ist an der Reihe
            StopUpdating = True
            HUDInstructions.Text = "No move possible!"
            Automator.Add(New TimerTransition(1000, Sub()
                                                        SubmitResults(0, -2)
                                                        StopUpdating = False
                                                    End Sub))
        Else 'Ansonsten fahre x Felder nach vorne mit der Figur, die anschließend ausgewählt wird
            'TODO: Add code for handling normal dice rolls and movement, as well as kicking
            Fahrzahl = If(WürfelWerte(0) = 6, WürfelWerte(0) + WürfelWerte(1), WürfelWerte(0))
            HUDInstructions.Text = "Select piece to be moved " & Fahrzahl & " spaces!"
            Status = SpielStatus.WähleFigur
        End If
    End Sub

    Private Function CheckKick(Optional Increment As Integer = 0) As Integer
        'Berechne globale Spielfeldposition der rauswerfenden Figur
        Dim playerA As Integer = FigurFaderZiel.Item1
        Dim fieldA As Integer = Spielers(playerA).Spielfiguren(FigurFaderZiel.Item2) + Increment
        Dim fa As Integer = PlayerFieldToGlobalField(fieldA, playerA)
        'Loope durch andere Spieler
        For i As Integer = playerA + 1 To playerA + 3
            'Loope durch alle Spielfiguren eines jeden Spielers
            For j As Integer = 0 To 3
                'Berechne globale Spielfeldposition der rauszuwerfenden Spielfigur
                Dim playerB As Integer = i Mod 4
                Dim fieldB As Integer = Spielers(playerB).Spielfiguren(j)
                Dim fb As Integer = PlayerFieldToGlobalField(fieldB, playerB)
                'Falls globale Spielfeldposition identisch und 
                If fieldB >= 0 And fieldB <= 40 And fb = fa Then
                    PostChat(Spielers(playerA).Name & " kicked " & Spielers(playerB).Name & "!", Color.White)
                    Return j
                End If
            Next
        Next
        Return -1
    End Function

    Private Function CanDoAMove() As Boolean
        Dim pl As Player = Spielers(SpielerIndex)

        'Wähle alle möglichen Zügen aus
        Dim ichmagzüge As New List(Of Integer)
        Dim defaultmov As Integer
        For i As Integer = 0 To 3
            defaultmov = pl.Spielfiguren(i)
            'Prüfe ob Zug mit dieser Figur möglich ist(Nicht in homebase, nicht über Ziel hinaus und Zielfeld nicht mit eigener Figur belegt
            If defaultmov > -1 And defaultmov + Fahrzahl <= 43 And Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, defaultmov + Fahrzahl, i) Then ichmagzüge.Add(i)
        Next

        'Prüfe ob Zug möglich
        Return ichmagzüge.Count > 0
    End Function

    Private Function GetFieldID(player As Integer, field As Integer) As (Integer, Integer)
        Dim fa As Integer = PlayerFieldToGlobalField(field, player)
        For j As Integer = 0 To 3
            For i As Integer = 0 To 3
                If fa = PlayerFieldToGlobalField(Spielers(j).Spielfiguren(i), j) Then Return (j, i)
            Next
        Next
        Return (-1, -1)
    End Function

    Friend Function GetSpielfeldVector(player As Integer, figur As Integer, Optional increment As Integer = 0) As Vector2 Implements IGameWindow.GetSpielfeldVector
        Dim pl As Player = Spielers(player)
        Dim chr As Integer = pl.Spielfiguren(figur) + increment
        Select Case chr
            Case -1 'Zeichne Figur in Homebase
                Return Vector2.Transform(GetSpielfeldPositionen(figur), transmatrices(player))
                'DrawChr(Vector2.Transform(GameRoom.GetSpielfeldPositionen(figur), transmatrices(player)), playcolor(player))
            Case 40, 41, 42, 43 'Zeichne Figur in Haus
                Return Vector2.Transform(GetSpielfeldPositionen(chr - 26), transmatrices(player))
                'DrawChr(Vector2.Transform(GameRoom.GetSpielfeldPositionen(chr - 26), transmatrices(player)), Color)
            Case Else 'Zeichne Figur auf Feld
                Dim matrx As Matrix = transmatrices((player + Math.Floor(chr / 10)) Mod 4)
                Return Vector2.Transform(GetSpielfeldPositionen((chr Mod 10) + 4), matrx)
                'DrawChr(Vector2.Transform(GameRoom.GetSpielfeldPositionen((chr Mod 10) + 4), matrx), Color)
        End Select
    End Function

    Private Function IsFieldCovered(player As Integer, figur As Integer, fieldA As Integer) As Boolean
        If fieldA < 0 Or fieldA > 40 Then Return False

        Dim fa As Integer = PlayerFieldToGlobalField(fieldA, player)
        For i As Integer = 0 To 3
            'Loope durch alle Spielfiguren eines jeden Spielers
            For j As Integer = 0 To 3
                'Berechne globale Spielfeldposition der rauszuwerfenden Spielfigur
                Dim fieldB As Integer = Spielers(i).Spielfiguren(j)
                Dim fb As Integer = PlayerFieldToGlobalField(fieldB, i)
                'Falls globale Spielfeldposition identisch und 
                If fieldB > -1 And fieldB < 41 And (player <> i Or figur <> j) And fb = fa Then Return True
            Next
        Next

        Return False
    End Function

    Private Function IsFieldCoveredByOwnFigure(player As Integer, field As Integer) As Boolean
        For i As Integer = 0 To 3
            If Spielers(player).Spielfiguren(i) = field Then Return True
        Next
        Return False
    End Function

    Private Function IsFutureFieldCoveredByOwnFigure(player As Integer, futurefield As Integer, fieldindx As Integer) As Boolean
        For i As Integer = 0 To 3
            If Spielers(player).Spielfiguren(i) = futurefield And i <> fieldindx Then Return True
        Next
        Return False
    End Function

    'Gibt den Index ein Spielfigur zurück, die sich noch in der Homebase befindet. Falls keine Figur mehr in der Homebase, gibt die Fnkt. -1 zurück.
    Private Function GetHomebaseIndex(player As Integer) As Integer
        For i As Integer = 0 To 3
            If Spielers(player).Spielfiguren(i) = -1 Then Return i
        Next
        Return -1
    End Function

    Private Function GetHomebaseCount(player As Integer) As Integer
        Dim count As Integer = 0
        For i As Integer = 0 To 3
            If Spielers(player).Spielfiguren(i) = -1 Then count += 1
        Next
        Return count
    End Function

    Private Function Is6InDiceList() As Boolean
        For i As Integer = 0 To WürfelWerte.Length - 2
            If WürfelWerte(i) = 6 Then Return True
        Next
        Return False
    End Function

    Private Function PlayerFieldToGlobalField(field As Integer, player As Integer) As Integer
        Return ((field + player * 10 + 1) Mod 40) - 1
    End Function

    Private Function GetSecondDiceAfterSix(player As Integer) As Integer
        For i As Integer = 0 To WürfelWerte.Length - 2
            If WürfelWerte(i) = 6 Then Return WürfelWerte(i + 1)
        Next
        Return -1
    End Function

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

    Private Function GetSpielfeldPositionen(ps As PlayFieldPos) As Vector2
        Select Case ps
            Case PlayFieldPos.Home1
                Return New Vector2(-420, -420)
            Case PlayFieldPos.Home2
                Return New Vector2(-350, -420)
            Case PlayFieldPos.Home3
                Return New Vector2(-420, -350)
            Case PlayFieldPos.Home4
                Return New Vector2(-350, -350)
            Case PlayFieldPos.Haus1
                Return New Vector2(-FDist * 4, 0)
            Case PlayFieldPos.Haus2
                Return New Vector2(-FDist * 3, 0)
            Case PlayFieldPos.Haus3
                Return New Vector2(-FDist * 2, 0)
            Case PlayFieldPos.Haus4
                Return New Vector2(-FDist, 0)
            Case PlayFieldPos.Feld1
                Return New Vector2(-FDist * 5, -FDist)
            Case PlayFieldPos.Feld2
                Return New Vector2(-FDist * 4, -FDist)
            Case PlayFieldPos.Feld3
                Return New Vector2(-FDist * 3, -FDist)
            Case PlayFieldPos.Feld4
                Return New Vector2(-FDist * 2, -FDist)
            Case PlayFieldPos.Feld5
                Return New Vector2(-FDist, -FDist)
            Case PlayFieldPos.Feld6
                Return New Vector2(-FDist, -FDist * 2)
            Case PlayFieldPos.Feld7
                Return New Vector2(-FDist, -FDist * 3)
            Case PlayFieldPos.Feld8
                Return New Vector2(-FDist, -FDist * 4)
            Case PlayFieldPos.Feld9
                Return New Vector2(-FDist, -FDist * 5)
            Case PlayFieldPos.Feld10
                Return New Vector2(0, -FDist * 5)
            Case Else
                Return Vector2.Zero
        End Select
    End Function

    Private Sub PostChat(txt As String, color As Color)
        Chat.Add((txt, color))
        HUDChat.Scroll(Chat.Count - 1)
    End Sub

    Private Sub PrepareMove()
        'Setze benötigte Flags
        SpielerIndex = UserIndex
        Status = SpielStatus.Würfel
        ShowDice = True
        StopUpdating = False
        HUDInstructions.Text = "Roll the Dice!"
        DreifachWürfeln = GetHomebaseCount(SpielerIndex) = 4 'Falls noch alle Figuren un der Homebase sind
        WürfelTimer = 0
        ReDim WürfelWerte(3)
        For i As Integer = 0 To WürfelWerte.Length - 1
            WürfelWerte(i) = 0
        Next
    End Sub
#End Region

#Region "Knopfgedrücke"
    Private Sub ExitButton() Handles HUDBtnA.Clicked
        SendGameClosed()
        NetworkMode = False
        GameClassInstance.Exit()
    End Sub

    Dim chatbtnpressed As Boolean = False

    Private Sub ChatSendButton() Handles HUDChatBtn.Clicked
        If Not chatbtnpressed Then
            chatbtnpressed = True
            Dim txt As String = Microsoft.VisualBasic.InputBox("Enter your message: ", "Send message", "")
            If txt <> "" Then
                SendChatMessage(txt)
            End If
            chatbtnpressed = False
        End If
    End Sub
    Private Sub MenuButton() Handles HUDBtnB.Clicked
        SendGameClosed()
        NetworkMode = False
        GameClassInstance.SwitchToSubmenu(0)
    End Sub
    Private Sub AngerButton() Handles HUDBtnC.Clicked

    End Sub
#End Region


#Region "Schnittstellenimplementation"


    Private ReadOnly Property IGameWindow_Spielers As Player() Implements IGameWindow.Spielers
        Get
            Return Spielers
        End Get
    End Property

    Private ReadOnly Property IGameWindow_FigurFaderScales As Dictionary(Of (Integer, Integer), Transition(Of Single)) Implements IGameWindow.FigurFaderScales
        Get
            Return FigurFaderScales
        End Get
    End Property

    Private ReadOnly Property IGameWindow_Status As SpielStatus Implements IGameWindow.Status
        Get
            Return Status
        End Get
    End Property

    Private ReadOnly Property IGameWindow_SelectFader As Transition(Of Single) Implements IGameWindow.SelectFader
        Get
            Return SelectFader
        End Get
    End Property

    Private ReadOnly Property IGameWindow_FigurFaderZiel As (Integer, Integer) Implements IGameWindow.FigurFaderZiel
        Get
            Return FigurFaderZiel
        End Get
    End Property

    Private ReadOnly Property IGameWindow_SpielerIndex As Integer Implements IGameWindow.SpielerIndex
        Get
            Return SpielerIndex
        End Get
    End Property

    Private ReadOnly Property IGameWindow_UserIndex As Integer Implements IGameWindow.UserIndex
        Get
            Return UserIndex
        End Get
    End Property

    Private ReadOnly Property IGameWindow_FigurFaderXY As Transition(Of Vector2) Implements IGameWindow.FigurFaderXY
        Get
            Return FigurFaderXY
        End Get
    End Property

    Private ReadOnly Property IGameWindow_FigurFaderZ As Transition(Of Integer) Implements IGameWindow.FigurFaderZ
        Get
            Return FigurFaderZ
        End Get
    End Property
#End Region
End Class