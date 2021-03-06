﻿Imports System.Collections.Generic
Imports System.Linq
Imports Betreten_Verboten.Framework.Graphics
Imports Betreten_Verboten.Framework.Tweening
Imports Betreten_Verboten.Framework.UI
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports Microsoft.Xna.Framework.Media

''' <summary>
''' Enthällt den eigentlichen Code für das Basis-Spiel
''' </summary>
Public Class GameRoom
    Implements IGameWindow

    'Spiele-Flags und Variables
    Friend Spielers As Player() = {Nothing, Nothing, Nothing, Nothing} 'Enthält sämtliche Spieler, die an dieser Runde teilnehmen
    Friend PlCount As Integer
    Friend NetworkMode As Boolean = False
    Friend SpielerIndex As Integer = -1 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
    Friend UserIndex As Integer 'Gibt den Index des Spielers an, welcher momentan durch diese Spielinstanz repräsentiert wird
    Friend Status As SpielStatus 'Speichert den aktuellen Status des Spiels
    Friend Map As GaemMap 'Gibt die Map an, die verwendet wird
    Private WürfelAktuelleZahl As Integer 'Speichert den WErt des momentanen Würfels
    Private WürfelWerte As Integer() 'Speichert die Werte der Würfel
    Private WürfelTimer As Double 'Wird genutzt um den Würfelvorgang zu halten
    Private WürfelAnimationTimer As Double 'Implementiert einen Cooldown für die Würfelanimation
    Private WürfelTriggered As Boolean 'Gibt an ob gerade gewürfelt wird
    Private StopUpdating As Boolean 'Deaktiviert die Spielelogik
    Private Fahrzahl As Integer 'Anzahl der Felder die gefahren werden kann
    Private DreifachWürfeln As Boolean 'Gibt an(am Anfang des Spiels), dass ma drei Versuche hat um eine 6 zu bekommen
    Private lastmstate As MouseState 'Enthält den Status der Maus aus dem letzten Frame
    Private lastkstate As KeyboardState 'Enthält den Status der Tastatur aus dem letzten Frame
    Private JokerListe As New List(Of Integer) 'Gibt an, welche Spieler ihren Joker bereits eingelöst haben
    Private MoveActive As Boolean = False
    Private RNG As Random 'Zufallsgenerator

    'Assets
    Friend Renderer As Renderer3D
    Private WürfelAugen As Texture2D
    Private WürfelRahmen As Texture2D
    Private SpielfeldVerbindungen As Texture2D
    Private ButtonFont As SpriteFont
    Private ChatFont As SpriteFont
    Private bgm As Song

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
    Friend SelectFader As Transition(Of Single) 'Fader, welcher die zur Auswahl stehenden Figuren blinken lässt
    Friend Feld As Rectangle 'Gibt den Screen-Viewport des Spielfelds an
    Private Center As Vector2 'Gibt den Mittelpunkt des Screen-Viewports des Spielfelds an
    Friend FigurFaderZiel As (Integer, Integer) 'Gibt an welche Figur bewegt werden soll (Spieler ind., Figur ind.)
    Friend FigurFaderEnd As Single 'Gibt an auf welchem Feld der Zug enden soll
    Friend FigurFaderXY As Transition(Of Vector2) 'Bewegt die zu animierende Figur auf der X- und Y-Achse
    Friend FigurFaderZ As Transition(Of Integer)  'Bewegt die zu animierende Figur auf der Z-Achse
    Friend FigurFaderScales As New Dictionary(Of (Integer, Integer), Transition(Of Single)) 'Gibt die Skalierung für einzelne Figuren an Key: (Spieler ID, Figur ID) Value: Transition(Z)
    Friend FigurFaderCamera As New Transition(Of CamKeyframe) With {.Value = New CamKeyframe(-30, -20, -50, 0, 0.75, 0)} 'Bewegt die Kamera 
    Friend CPUTimer As Integer 'Timer-Flag um der CPU etwas "Überlegzeit" zu geben
    Friend PlayStompSound As Boolean 'Gibt an, ob der Stampf-Sound beim Landen(Kicken) gespielt werden soll
    Friend StdCam As New CamKeyframe(-30, -20, -50, 0, 0.75, 0) 'Gibt die Standard-Position der Kamera an

    'Konstanten
    Private Const WürfelDauer As Integer = 370
    Private Const WürfelAnimationCooldown As Integer = 60
    Private Const FigurSpeed As Integer = 550
    Private Const ErrorCooldown As Integer = 1100
    Private Const RollDiceCooldown As Integer = 800
    Private Const CPUThinkingTime As Integer = 1100
    Private Const DopsHöhe As Integer = 100
    Private Const CamSpeed As Integer = 1500

    Friend Sub Init()
        'Bereite Flags und Variablen vor
        Status = SpielStatus.WarteAufOnlineSpieler
        WürfelTimer = 0
        LocalClient.LeaveFlag = False
        LocalClient.IsHost = True
        Chat = New List(Of (String, Color))
        Status = SpielStatus.WarteAufOnlineSpieler
        SpielerIndex = -1
        MoveActive = False
    End Sub

    Friend Sub LoadContent()
        'Lade Assets
        WürfelAugen = Content.Load(Of Texture2D)("würfel_augen")
        WürfelRahmen = Content.Load(Of Texture2D)("würfel_rahmen")
        ButtonFont = Content.Load(Of SpriteFont)("font\ButtonText")
        ChatFont = Content.Load(Of SpriteFont)("font\ChatText")
        bgm = Content.Load(Of Song)("Betreten Verboten")
        MediaPlayer.Play(bgm)
        MediaPlayer.Volume = 0.1
        MediaPlayer.IsRepeating = True
        RNG = New Random()

        'Lade HUD
        HUD = New GuiSystem
        HUDBtnA = New Controls.Button("Exit Game", New Vector2(1500, 50), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDBtnA)
        HUDBtnB = New Controls.Button("Main Menu", New Vector2(1500, 200), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDBtnB)
        HUDBtnC = New Controls.Button("Anger", New Vector2(1500, 350), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDBtnC)
        HUDChat = New Controls.TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(400, 800)) With {.Font = ChatFont, .BackgroundColor = New Color(0, 0, 0, 100), .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow, .LenLimit = 35} : HUD.Controls.Add(HUDChat)
        HUDChatBtn = New Controls.Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDChatBtn)
        HUDInstructions = New Controls.Label("Wait for all Players to arrive...", New Vector2(50, 1005)) With {.Font = Content.Load(Of SpriteFont)("font/InstructionText"), .Color = Color.BlanchedAlmond} : HUD.Controls.Add(HUDInstructions)
        InstructionFader = New PropertyTransition(New TransitionTypes.TransitionType_EaseInEaseOut(700), HUDInstructions, "Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), Nothing) With {.Repeat = RepeatJob.Reverse} : Automator.Add(InstructionFader)
        HUDNameBtn = New Controls.Button("", New Vector2(500, 20), New Vector2(950, 30)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Black, 0), .Color = Color.Yellow} : HUD.Controls.Add(HUDNameBtn)
        HUD.Init()

        'Lade Renderer
        Renderer = New Renderer3D(Me)
        Renderer.LoadContent()

        'Lade Spielfeld
        Feld = New Rectangle(500, 70, 950, 950)
        Center = Feld.Center.ToVector2
        SelectFader = New Transition(Of Single)(New TransitionTypes.TransitionType_EaseInEaseOut(400), 0F, 1.0F, Nothing) With {.Repeat = RepeatJob.Reverse} : Automator.Add(SelectFader)
    End Sub

    ''' <summary>
    ''' Zeichnet den Renderer vor
    ''' </summary>
    Friend Sub PreDraw()
        Renderer.PreDraw()
    End Sub

    ''' <summary>
    ''' Zeichnet den Inhalt des Renderers, sowie die HUD auf den Bildschirm
    ''' </summary>
    ''' <param name="gameTime"></param>
    Friend Sub Draw(ByVal gameTime As GameTime)

        Renderer.Draw(gameTime)

        SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise, Nothing, ScaleMatrix)

        'Zeichne Haupt-Würfel
        If ShowDice And SpielerIndex = UserIndex Then
            SpriteBatch.Draw(WürfelAugen, New Rectangle(1570, 700, 300, 300), GetWürfelSourceRectangle(WürfelAktuelleZahl), HUDColor)
            SpriteBatch.Draw(WürfelRahmen, New Rectangle(1570, 700, 300, 300), Color.Lerp(HUDColor, Color.White, 0.4))
        End If
        'Zeichne Mini-Würfel
        If Status <> SpielStatus.WarteAufOnlineSpieler Then
            For i As Integer = 0 To WürfelWerte.Length - 1 'If(Spielers(SpielerIndex).Typ = SpielerTyp.CPU And Not DreifachWürfeln, 1, WürfelWerte.Length - 1)
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

    ''' <summary>
    ''' Berechnet die Spielelogik.
    ''' </summary>
    ''' <param name="gameTime"></param>
    Friend Sub Update(ByVal gameTime As GameTime)
        Dim mstate As MouseState = Mouse.GetState()
        Dim kstate As KeyboardState = Keyboard.GetState()
        Dim mpos As Point = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScaleMatrix)).ToPoint

        If Not StopUpdating Then

            'Prüfe, ob die Runde gewonnen wurde und beende gegebenenfalls die Runde
            Dim win As Integer = CheckWin()
            If win > -1 Then
                StopUpdating = True
                ShowDice = False
                HUDInstructions.Text = "Game over!"
                'Berechne Rankings
                Dim ranks As New List(Of (Integer, Integer)) '(Spieler ID, Score)
                For i As Integer = 0 To PlCount - 1
                    If i = win Then Continue For
                    ranks.Add((i, GetScore(i)))
                Next
                ranks = ranks.OrderBy(Function(x) x.Item2).ToList()
                ranks.Reverse()

                Automator.Add(New TimerTransition(1000, Sub() PostChat("1st place: " & Spielers(win).Name & "(" & GetScore(win) & ")", Renderer3D.playcolor(win))))
                For i As Integer = 0 To ranks.Count - 1
                    Dim ia As Integer = i
                    Select Case i
                        Case 0
                            Automator.Add(New TimerTransition((2 + i) * 1000, Sub() PostChat("2nd place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", Renderer3D.playcolor(ranks(ia).Item1))))
                        Case 1
                            Automator.Add(New TimerTransition((2 + i) * 1000, Sub() PostChat("3rd place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", Renderer3D.playcolor(ranks(ia).Item1))))
                        Case Else
                            Automator.Add(New TimerTransition((2 + i) * 1000, Sub() PostChat((ia + 2) & "th place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", Renderer3D.playcolor(ranks(ia).Item1))))
                    End Select
                Next
                SendWinFlag(win)
                Status = SpielStatus.SpielZuEnde
                FigurFaderCamera = New Transition(Of CamKeyframe)(New TransitionTypes.TransitionType_EaseInEaseOut(5000), GetCamPos, New CamKeyframe(-90, -240, 0, Math.PI / 4 * 5, Math.PI / 2, 0), Nothing) : Automator.Add(FigurFaderCamera)
                Renderer.AdditionalZPos = New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(5000), 0, 1000, Nothing)
                Automator.Add(Renderer.AdditionalZPos)
            End If

            'Setze den lokalen Spieler
            If SpielerIndex > -1 AndAlso Spielers(SpielerIndex).Typ = SpielerTyp.Local Then UserIndex = SpielerIndex

            'Update die Spielelogik
            Select Case Status
                Case SpielStatus.Würfel

                    Select Case Spielers(SpielerIndex).Typ
                        Case SpielerTyp.Local
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
                                If Math.Floor(WürfelTimer / WürfelAnimationCooldown) <> WürfelAnimationTimer Then WürfelAktuelleZahl = RollDice() : WürfelAnimationTimer = Math.Floor(WürfelTimer / WürfelAnimationCooldown) : SFX(7).Play()

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
                                                                                                    If it >= WürfelWerte.Length - 1 Or (Not DreifachWürfeln And WürfelAktuelleZahl < 6) Or ((DreifachWürfeln Or GetHomebaseCount(SpielerIndex) > 0) And it > 0 AndAlso WürfelWerte(it - 1) >= 6) Or (DreifachWürfeln And it >= 2 And WürfelWerte(2) < 6) Then CalcMoves()
                                                                                                    WürfelAktuelleZahl = 0
                                                                                                End Sub))

                                            'Beende Schleife
                                            Exit For
                                        End If
                                    Next
                                End If
                            End If
                        Case SpielerTyp.CPU
                            'Automatisches Würfeln im Hintergrund für CPU
                            WürfelTimer += gameTime.ElapsedGameTime.TotalMilliseconds
                            If WürfelTimer > CPUThinkingTime Then
                                'Nach kurzem Delay, fülle Würfel-Array mit Zufallszahlen
                                For i As Integer = 0 To WürfelWerte.Length - 1
                                    WürfelWerte(i) = RollDice(Spielers(SpielerIndex).Schwierigkeit = Difficulty.Brainless)
                                Next
                                CalcMoves()
                            End If
                    End Select


                Case SpielStatus.WähleFigur

                    Dim pl As Player = Spielers(SpielerIndex)
                    Select Case pl.Typ
                        Case SpielerTyp.Local
                            'Manuelle Auswahl für lokale Spieler
                            For k As Integer = 0 To 3
                                Dim defaultmov As Integer = Spielers(SpielerIndex).Spielfiguren(k)

                                'Prüfe Figur nach Mouse-Klick
                                If GetFigureRectangle(Map, SpielerIndex, k, Spielers, Center).Contains(mpos) And Spielers(SpielerIndex).Spielfiguren(k) > -1 And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then
                                    If defaultmov + Fahrzahl > PlCount * 10 + 3 Or IsFutureFieldCoveredByOwnFigure(SpielerIndex, defaultmov + Fahrzahl, k) Or IsÜberholingInSeHaus(defaultmov) Then
                                        HUDInstructions.Text = "Incorrect move!"
                                    Else
                                        'Move camera
                                        FigurFaderCamera = New Transition(Of CamKeyframe)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, StdCam, Nothing) : Automator.Add(FigurFaderCamera)

                                        SFX(2).Play()
                                        'Setze flags
                                        Status = SpielStatus.FahreFelder
                                        FigurFaderZiel = (SpielerIndex, k)
                                        'Animiere wie die Figur sich nach vorne bewegt, anschließend prüfe ob andere Spieler rausgeschmissen wurden
                                        StartMoverSub()
                                        SendFigureTransition(SpielerIndex, k, defaultmov + Fahrzahl)
                                        StopUpdating = False
                                    End If
                                    Exit For
                                End If
                            Next

                        Case SpielerTyp.CPU
                            'TODO: FÜge CPU-Code ein(Auswahl, welcher Zug optimal ist)
                            CPUTimer += gameTime.ElapsedGameTime.TotalMilliseconds
                            If CPUTimer > CPUThinkingTime Then
                                CPUTimer = 0

                                Dim k As Integer
                                Dim ichmagzüge As New List(Of Integer)
                                Dim defaultmov As Integer
                                For i As Integer = 0 To 3
                                    defaultmov = pl.Spielfiguren(i)
                                    If defaultmov > -1 And defaultmov + Fahrzahl <= 43 And Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, defaultmov + Fahrzahl, i) And Not IsÜberholingInSeHaus(defaultmov) Then ichmagzüge.Add(i)
                                Next
                                'Prüfe ob Zug möglich
                                If ichmagzüge.Count = 0 Then SwitchPlayer() : Exit Select

                                Select Case Spielers(SpielerIndex).Schwierigkeit
                                    Case Difficulty.Brainless

                                        'Berechne zufällig das zu fahrende Feld
                                        k = ichmagzüge(RNG.Next(0, ichmagzüge.Count))

                                    Case Difficulty.Smart

                                        Dim Scores As New Dictionary(Of Integer, Single) ' im INteger ist der Index der FIgur und im Single der Score
                                        For Each element In ichmagzüge
                                            Scores.Add(element, 1)
                                        Next

                                        'Spielfigurimportans: eine Figur die näher am Ziel ist ist wichtiger
                                        Dim counts As New List(Of (Integer, Integer))
                                        For Each element In ichmagzüge
                                            counts.Add((element, Spielers(SpielerIndex).Spielfiguren(element)))
                                        Next
                                        counts = counts.OrderBy(Function(x) x.Item2).ToList()
                                        For i As Integer = 0 To counts.Count - 1
                                            Scores(counts(i).Item1) *= (1 + i * 0.1)
                                        Next

                                        For Each element In ichmagzüge
                                            ' Safety:ist eine Figur höchstens 6 felder vor einer Feindlichen Figur entfernt, ist sie in einer Gefahrenzone die avoidet werden soll
                                            Dim locpos As Integer() = {Spielers(SpielerIndex).Spielfiguren(element), Spielers(SpielerIndex).Spielfiguren(element) + Fahrzahl}
                                            Dim Globpos As Integer() = {PlayerFieldToGlobalField(locpos(0), SpielerIndex), PlayerFieldToGlobalField(locpos(1), SpielerIndex)}
                                            For ALVSP As Integer = 0 To 3
                                                If ALVSP <> SpielerIndex Then
                                                    For ALVSPF As Integer = 0 To 3
                                                        Dim locposB As Integer = Spielers(ALVSP).Spielfiguren(ALVSPF)
                                                        Dim GlobposB As Integer = PlayerFieldToGlobalField(locposB, ALVSP)
                                                        'Falls momentane Position in Feindlichiem Feld, verbessere Score(fliehen)
                                                        If GlobposB < Globpos(0) And GlobposB >= Globpos(0) - 6 Then
                                                            Scores(element) *= 1.4
                                                        ElseIf GlobposB < Globpos(1) And GlobposB >= Globpos(1) - 6 And locpos(1) < 40 And locposB > -1 And Not (GlobposB < Globpos(0) And GlobposB >= Globpos(0) - 6) Then
                                                            'Falls momentanes Feld nicht in feindlichem Gebiet, aber zukünftiges, verschlechtere Score
                                                            Scores(element) /= 1.5
                                                        End If
                                                    Next
                                                End If
                                            Next

                                            ' Destiny: landet der zug im Haus? 
                                            If locpos(1) >= 40 Then
                                                Scores(element) *= 2.8
                                            End If

                                            ' Attackopportunity: kann der zug einen Feindlichen spieler eleminieren? 
                                            Dim Ergebnis As (Integer, Integer) = GetKickFigur(SpielerIndex, element, Fahrzahl)
                                            If Ergebnis.Item1 <> -1 And Ergebnis.Item2 <> -1 Then
                                                Scores(element) *= 1.8
                                            End If


                                            'Risk: nicht auf das Startfeld/den Eingangsbereich eines gegners stellen da eine neue figur erscheinen könnte.
                                            If locpos(1) > 0 And (locpos(1) Mod 10) = 0 Then
                                                Scores(element) /= 4
                                            ElseIf locpos(1) > 6 And (locpos(1) Mod 10) < 7 And Not (locpos(0) Mod 10) < 7 Then
                                                Scores(element) /= 2
                                            End If

                                            'Flee: führt der Zug die Figur aus einem Startbereich heraus
                                            If locpos(0) > 6 And (locpos(0) Mod 10) < 7 And (locpos(1) Mod 10) > 6 Then
                                                Scores(element) *= 1.3
                                            End If
                                        Next

                                        'Sortieren und besten Zug filtern
                                        Dim NeueLIsteweilIChsehrcreativebin As New List(Of (Integer, Single))
                                        For Each element In ichmagzüge
                                            NeueLIsteweilIChsehrcreativebin.Add((element, Scores(element)))
                                        Next
                                        NeueLIsteweilIChsehrcreativebin = NeueLIsteweilIChsehrcreativebin.OrderBy(Function(x) x.Item2).ToList()
                                        NeueLIsteweilIChsehrcreativebin.Reverse()
                                        k = NeueLIsteweilIChsehrcreativebin(0).Item1
                                End Select


                                defaultmov = pl.Spielfiguren(k)
                                'Setze flags
                                Status = SpielStatus.FahreFelder
                                FigurFaderZiel = (SpielerIndex, k)

                                'Move camera
                                FigurFaderCamera = New Transition(Of CamKeyframe)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, StdCam, Nothing) : Automator.Add(FigurFaderCamera)

                                'Animiere wie die Figur sich nach vorne bewegt, anschließend prüfe ob andere Spieler rausgeschmissen wurden
                                StartMoverSub()
                                SendFigureTransition(SpielerIndex, k, defaultmov + Fahrzahl)
                                StopUpdating = False
                            End If
                    End Select

                Case SpielStatus.WarteAufOnlineSpieler
                    HUDInstructions.Text = "Waiting for all players to connect..."

                    'Prüfe einer die vier Spieler nicht anwesend sind, kehre zurück
                    For Each sp In Spielers
                        If sp Is Nothing OrElse Not sp.Bereit Then Exit Select 'Falls ein SPieler noch nicht belegt/bereit, breche Spielstart ab
                    Next

                    'Falls vollzählig, starte Spiel
                    StopUpdating = True
                    Automator.Add(New TimerTransition(800, Sub()
                                                               PostChat("The game has started!", Color.White)
                                                               SendBeginGaem()
                                                               SwitchPlayer()
                                                           End Sub))
                Case SpielStatus.SpielZuEnde
                    StopUpdating = True
            End Select

            'Set HUD color
            HUDColor = Renderer3D.playcolor(UserIndex)
            HUDBtnA.Color = HUDColor : HUDBtnA.Border = New ControlBorder(HUDColor, HUDBtnA.Border.Width)
            HUDBtnB.Color = HUDColor : HUDBtnB.Border = New ControlBorder(HUDColor, HUDBtnB.Border.Width)
            HUDBtnC.Color = HUDColor : HUDBtnC.Border = New ControlBorder(HUDColor, HUDBtnC.Border.Width)
            HUDChat.Color = HUDColor : HUDChat.Border = New ControlBorder(HUDColor, HUDChat.Border.Width)
            HUDChatBtn.Color = HUDColor : HUDChatBtn.Border = New ControlBorder(HUDColor, HUDChatBtn.Border.Width)
            HUDNameBtn.Text = If(SpielerIndex > -1, Spielers(SpielerIndex).Name, "")
            HUDNameBtn.Color = If(SpielerIndex > -1, Renderer3D.playcolor(SpielerIndex), Color.White)
            HUDInstructions.Active = (Status = SpielStatus.WarteAufOnlineSpieler) OrElse (Spielers(SpielerIndex).Typ = SpielerTyp.Local)
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
        End If

        If NetworkMode Then ReadAndProcessInputData()

        'Misc things
        If kstate.IsKeyDown(Keys.Escape) And lastkstate.IsKeyUp(Keys.Escape) Then MenuButton()
        HUD.Update(gameTime, mstate, Matrix.Identity)
        Renderer.Update(gameTime)
        lastmstate = mstate
        lastkstate = kstate
    End Sub

#Region "Netzwerkfunktionen"
    ''' <summary>
    ''' Liest die Daten aus dem Stream des Servers
    ''' </summary>
    Private Sub ReadAndProcessInputData()
        If MoveActive Then Return

        Dim data As String() = LocalClient.ReadStream()
        For Each element In data
            Dim source As Integer = CInt(element(0).ToString)
            Dim command As Char = element(1)
            Select Case command
                Case "a"c 'Player arrived
                    Spielers(source).Name = element.Substring(2)
                    Spielers(source).Bereit = True
                    PostChat(Spielers(source).Name & " arrived!", Color.White)
                    SendPlayerArrived(source, Spielers(source).Name)
                Case "c"c 'Sent chat message
                    Dim text As String = element.Substring(2)
                    PostChat("[" & Spielers(source).Name & "]: " & text, Renderer3D.playcolor(source))
                    SendChatMessage(source, text)
                Case "e"c 'Suspend gaem
                    StopUpdating = True
                    PostChat(Spielers(source).Name & " left!", Color.White)
                    PostChat("The game is being suspended!", Color.White)
                    SendPlayerLeft(source)
                Case "n"c
                    SwitchPlayer()
                Case "r"c 'Player is back
                    Spielers(source).Bereit = True
                    PostChat(Spielers(source).Name & " is back!", Color.White)
                    SendPlayerBack(source)
                    StopUpdating = False
                    If SpielerIndex = source Then SendNewPlayerActive(SpielerIndex)
                Case "s"c
                    Dim figur As Integer = CInt(element(2).ToString)
                    Dim destination As Integer = CInt(element.Substring(3).ToString)
                    SendFigureTransition(source, figur, destination)
                    'Animiere wie die Figur sich nach vorne bewegt, anschließend kehre zurück zum nichts tun
                    Dim defaultmov As Integer = Math.Max(Spielers(source).Spielfiguren(figur), 0)
                    Status = SpielStatus.FahreFelder
                    FigurFaderZiel = (source, figur)
                    StartMoverSub(destination)
            End Select
        Next
    End Sub

    ' ---Methoden um Daten via den Server an die Clients zu senden---
    Private Sub SendPlayerArrived(index As Integer, name As String)
        SendNetworkMessageToAll("a" & index.ToString & name)
    End Sub
    Private Sub SendPlayerBack(index As Integer)
        Dim str As String = Newtonsoft.Json.JsonConvert.SerializeObject(Spielers)
        SendNetworkMessageToAll("r" & index.ToString & str)
    End Sub
    Private Sub SendPlayerLeft(index As Integer)
        LocalClient.WriteStream("e" & index)
    End Sub

    Private Sub SendBeginGaem()
        SendNetworkMessageToAll("b")
    End Sub

    Private Sub SendChatMessage(index As Integer, text As String)
        SendNetworkMessageToAll("c" & index.ToString & text)
    End Sub

    Private Sub SendNewPlayerActive(who As Integer)
        SendNetworkMessageToAll("n" & who.ToString)
    End Sub

    Private Sub SendFigureTransition(who As Integer, figur As Integer, destination As Integer)
        SendNetworkMessageToAll("s" & who.ToString & figur.ToString & destination.ToString)
    End Sub

    Private Sub SendGameClosed()
        SendNetworkMessageToAll("l")
    End Sub

    Private Sub SendWinFlag(who As Integer)
        SendNetworkMessageToAll("w" & who.ToString)
    End Sub

    Private Sub SendNetworkMessageToAll(message As String)
        If NetworkMode Then LocalClient.WriteStream(message)
    End Sub
#End Region

    ''' <summary>
    ''' Prüft nach dem Würfeln, wie der Zug weitergeht(Ist Zug möglich, muss Figur ausgewählt werden, ...)
    ''' </summary>
    Private Sub CalcMoves()
        Dim homebase As Integer = GetHomebaseIndex(SpielerIndex) 'Eine Spielfigur-ID, die sich in der Homebase befindet(-1, falls Homebase leer ist)
        Dim startfd As Boolean = IsFieldCoveredByOwnFigure(SpielerIndex, 0) 'Ob das Start-Feld blockiert ist
        ShowDice = False
        Fahrzahl = GetNormalDiceSum() 'Setzt die Anzahl der zu fahrenden Felder im voraus(kann im Fall einer vollen Homebase überschrieben werden)

        If Is6InDiceList() And homebase > -1 And Not startfd Then 'Falls Homebase noch eine Figur enthält und 6 gewürfelt wurde, setze Figur auf Feld 0 und fahre anschließend x Felder nach vorne
            'Bereite das Homebase-verlassen vor
            Fahrzahl = GetSecondDiceAfterSix(SpielerIndex)
            HUDInstructions.Text = "Move Character out of your homebase and move him " & Fahrzahl & " spaces!"
            FigurFaderZiel = (SpielerIndex, homebase)
            'Animiere wie die Figur sich nach vorne bewegt, anschließend prüfe ob andere Spieler rausgeschmissen wurden
            If Not IsFieldCoveredByOwnFigure(SpielerIndex, Fahrzahl) Then
                StartMoverSub()
                SendFigureTransition(SpielerIndex, homebase, Fahrzahl)
            Else
                StopUpdating = True
                HUDInstructions.Text = "Field already covered! Move with the other piece!"
                Automator.Add(New TimerTransition(ErrorCooldown, Sub()
                                                                     'Move camera
                                                                     FigurFaderCamera = New Transition(Of CamKeyframe)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New CamKeyframe(0, 0, 0, 0, 0, 0), Nothing) : Automator.Add(FigurFaderCamera)
                                                                     Status = SpielStatus.WähleFigur
                                                                     StopUpdating = False
                                                                 End Sub))
            End If
        ElseIf Is6InDiceList() And homebase > -1 And startfd Then 'Gibt an, dass das Start-Feld von einer eigenen Figur belegt ist(welche nicht gekickt werden kann) und dass selbst beim Wurf einer 6 keine weitere Figur die Homebase verlassen kann
            HUDInstructions.Text = "Start field blocked! Move pieces out of the way first!"

            If IsFutureFieldCoveredByOwnFigure(SpielerIndex, 0, -1) AndAlso Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, Fahrzahl, -1) Then 'Spieler auf dem Start-Feld muss wenn mögl.  bewegt werden
                homebase = GetFieldID(SpielerIndex, 0).Item2
                FigurFaderZiel = (SpielerIndex, homebase)
                StartMoverSub()
                SendFigureTransition(SpielerIndex, homebase, Fahrzahl)
                'ElseIf IsFutureFieldCoveredByOwnFigure(SpielerIndex, Fahrzahl, -1) AndAlso Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, Fahrzahl * 2, -1) Then 'Wenn Spieler auf dem Start-Feld nicht kann, fahre stattdessen mit nächtem blockierenden Spieler
                '    homebase = GetFieldID(SpielerIndex, WürfelWerte(1)).Item2
                '    FigurFaderZiel = (SpielerIndex, homebase)
                '    StartMoverSub()
                '    SendFigureTransition(SpielerIndex, homebase, Fahrzahl)
            Else 'We can't so s$*!, also schieben wir unsere Probleme einfach auf den nächst besten Deppen, der gleich dran ist

                'Move camera
                FigurFaderCamera = New Transition(Of CamKeyframe)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New CamKeyframe(0, 0, 0, 0, 0, 0), Nothing) : Automator.Add(FigurFaderCamera)

                Status = SpielStatus.WähleFigur
                StopUpdating = True
                Automator.Add(New TimerTransition(ErrorCooldown, Sub() StopUpdating = False))
            End If
        ElseIf (GetHomebaseCount(SpielerIndex) = 4 And Not Is6InDiceList()) OrElse Not CanDoAMove() Then 'Falls Homebase komplett voll ist(keine Figur auf Spielfeld) und keine 6 gewürfelt wurde(oder generell kein Zug mehr möglich ist), ist kein Zug möglich und der nächste Spieler ist an der Reihe
            StopUpdating = True
            HUDInstructions.Text = "No move possible!"
            Automator.Add(New TimerTransition(1000, Sub()
                                                        SwitchPlayer()
                                                        StopUpdating = False
                                                    End Sub))
        Else 'Ansonsten fahre x Felder nach vorne mit der Figur, die anschließend ausgewählt wird
            'TODO: Add code for handling normal dice rolls and movement, as well as kicking
            HUDInstructions.Text = "Select piece to be moved " & Fahrzahl & " spaces!"
            Status = SpielStatus.WähleFigur

            'Move camera
            FigurFaderCamera = New Transition(Of CamKeyframe)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New CamKeyframe(0, 0, 0, 0, 0, 0), Nothing) : Automator.Add(FigurFaderCamera)
        End If
    End Sub

#Region "Hilfsfunktionen"
    Private Function CheckKick(Optional Increment As Integer = 0) As Integer
        'Berechne globale Spielfeldposition der rauswerfenden Figur
        Dim playerA As Integer = FigurFaderZiel.Item1
        Dim fieldA As Integer = Spielers(playerA).Spielfiguren(FigurFaderZiel.Item2) + Increment
        Dim fa As Integer = PlayerFieldToGlobalField(fieldA, playerA)
        'Loope durch andere Spieler
        For i As Integer = playerA + 1 To playerA + PlCount - 1
            'Loope durch alle Spielfiguren eines jeden Spielers
            For j As Integer = 0 To 3
                'Berechne globale Spielfeldposition der rauszuwerfenden Spielfigur
                Dim playerB As Integer = i Mod PlCount
                Dim fieldB As Integer = Spielers(playerB).Spielfiguren(j)
                Dim fb As Integer = PlayerFieldToGlobalField(fieldB, playerB)
                'Falls globale Spielfeldposition identisch und 
                If fieldB >= 0 And fieldB <= PlCount * 10 And fb = fa Then
                    Automator.Add(New TimerTransition(1000, Sub() PostChat(Spielers(playerA).Name & " kicked " & Spielers(playerB).Name & "!", Color.White)))
                    Spielers(playerA).Kicks += 1
                    Return j
                End If
            Next
        Next
        Return -1
    End Function
    Private Function GetKickFigur(player As Integer, figur As Integer, Optional Increment As Integer = 0) As (Integer, Integer)
        'Berechne globale Spielfeldposition der rauswerfenden Figur
        Dim playerA As Integer = player
        Dim fieldA As Integer = Spielers(playerA).Spielfiguren(figur) + Increment
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
                    Return (i, j)
                End If
            Next
        Next
        Return (-1, -1)
    End Function


    Private Function GetNormalDiceSum() As Integer
        Dim sum As Integer = 0
        For i As Integer = 0 To WürfelWerte.Length - 1
            sum += WürfelWerte(i)
            If WürfelWerte(i) <> 6 Then Exit For
        Next
        Return sum
    End Function

    Private Function CheckWin() As Integer
        For i As Integer = 0 To PlCount - 1
            Dim pl As Player = Spielers(i)
            Dim check As Boolean = True
            For j As Integer = 0 To 3
                If pl.Spielfiguren(j) < PlCount * 10 Then check = False
            Next
            If check Then Return i
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
            If defaultmov > -1 And defaultmov + Fahrzahl <= PlCount * 10 + 3 And Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, defaultmov + Fahrzahl, i) And Not IsÜberholingInSeHaus(defaultmov) Then ichmagzüge.Add(i)
        Next

        'Prüfe ob Zug möglich
        Return ichmagzüge.Count > 0
    End Function

    Private Function GetScore(pl As Integer) As Integer
        Dim ret As Integer = Spielers(pl).Kicks * 2
        For Each element In Spielers(pl).Spielfiguren
            ret += element
        Next
        Return ret * 10
    End Function

    Private Function IsÜberholingInSeHaus(defaultmov As Integer) As Boolean
        If defaultmov + Fahrzahl < PlCount * 10 Then Return False

        For i As Integer = defaultmov + 1 To defaultmov + Fahrzahl
            If IsFieldCovered(SpielerIndex, -1, i) And i >= PlCount * 10 Then Return True
        Next

        Return False
    End Function


    Private Function IsFieldCovered(player As Integer, figur As Integer, fieldA As Integer) As Boolean
        If fieldA < 0 Then Return False

        Dim fa As Integer = PlayerFieldToGlobalField(fieldA, player)
        For i As Integer = 0 To PlCount - 1
            'Loope durch alle Spielfiguren eines jeden Spielers
            For j As Integer = 0 To 3
                'Berechne globale Spielfeldposition der rauszuwerfenden Spielfigur
                Dim fieldB As Integer = Spielers(i).Spielfiguren(j)
                Dim fb As Integer = PlayerFieldToGlobalField(fieldB, i)
                'Falls globale Spielfeldposition identisch und 
                If fieldB > -1 And ((fieldA < PlCount * 10 AndAlso (player <> i Or figur <> j) And fb = fa) OrElse (fieldB < 45 And player = i And figur <> j And fieldA = fieldB)) Then Return True
            Next
        Next

        Return False
    End Function

    Private Function GetFieldID(player As Integer, field As Integer) As (Integer, Integer)
        Dim fa As Integer = PlayerFieldToGlobalField(field, player)
        For j As Integer = 0 To PlCount - 1
            For i As Integer = 0 To 3
                Dim fieldB As Integer = Spielers(j).Spielfiguren(i)
                If fieldB >= 0 And fieldB < PlCount * 10 And fa = PlayerFieldToGlobalField(fieldB, j) Then Return (j, i)
            Next
        Next
        Return (-1, -1)
    End Function

    'Prüft, ob man dreimal würfeln darf
    Private Function CanRollThrice(player As Integer) As Boolean
        Dim fieldlst As New List(Of Integer)
        For i As Integer = 0 To 3
            Dim tm As Integer = Spielers(player).Spielfiguren(i)
            If tm >= 0 And tm < PlCount * 10 Then Return False 'Falls sich Spieler auf dem Spielfeld befindet, ist dreimal würfeln unmöglich
            If tm > PlCount * 10 - 1 Then fieldlst.Add(tm) 'Merke FIguren, die sich im Haus befinden
        Next

        'Wenn nicht alle FIguren bis an den Anschlag gefahren wurden, darf man nicht dreifach würfeln
        For i As Integer = PlCount * 10 + 3 To (PlCount * 10 + 4 - fieldlst.Count) Step -1
            If Not fieldlst.Contains(i) Then Return False
        Next

        Return True
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
        Return (field + player * 10) Mod (PlCount * 10)
    End Function

    Private Function GetSecondDiceAfterSix(player As Integer) As Integer
        For i As Integer = 0 To WürfelWerte.Length - 2
            If WürfelWerte(i) = 6 Then Return WürfelWerte(i + 1)
        Next
        Return -1
    End Function

    Private Sub StartMoverSub(Optional destination As Integer = -1)
        'Set values
        MoveActive = True
        FigurFaderEnd = If(destination < 0, Math.Max(Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2), 0) + Fahrzahl, destination)
        Dim FigurFaderVectors = (GetSpielfeldVector(FigurFaderZiel.Item1, FigurFaderZiel.Item2), GetSpielfeldVector(FigurFaderZiel.Item1, FigurFaderZiel.Item2, 1))
        Status = SpielStatus.FahreFelder
        PlayStompSound = False

        'Initiate
        If IsFieldCovered(FigurFaderZiel.Item1, FigurFaderZiel.Item2, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1) Then
            Dim key As (Integer, Integer) = GetFieldID(FigurFaderZiel.Item1, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1)
            If (Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) = FigurFaderEnd - 1) Or (Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1 = 0) Then
                Dim kickID As Integer = CheckKick(1)
                Dim trans As New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(FigurSpeed), 1, 0, Sub()
                                                                                                                              SFX(4).Play()
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

        Dim FigurFaderVectors = (GetSpielfeldVector(FigurFaderZiel.Item1, FigurFaderZiel.Item2, 0), GetSpielfeldVector(FigurFaderZiel.Item1, FigurFaderZiel.Item2, 1))

        If Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) < FigurFaderEnd Then
            SFX(3).Play()
            If IsFieldCovered(FigurFaderZiel.Item1, FigurFaderZiel.Item2, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1) Then
                Dim key As (Integer, Integer) = GetFieldID(FigurFaderZiel.Item1, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1)
                If Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) = FigurFaderEnd - 1 Then
                    Dim kickID As Integer = CheckKick(1)
                    PlayStompSound = True
                    Dim trans As New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(FigurSpeed), 1, 0, Sub()
                                                                                                                                  SFX(4).Play()
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
        Else
            If Not PlayStompSound Then SFX(2).Play()
            SwitchPlayer()
            MoveActive = False
        End If

    End Sub

    Private Function GetSpielfeldVector(player As Integer, figur As Integer, Optional increment As Integer = 0) As Vector2
        Return GetMapVectorPos(Map, player, figur, Spielers(player).Spielfiguren(figur) + increment)
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


    Private Sub PostChat(txt As String, color As Color)
        Chat.Add((txt, color))
        HUDChat.Scroll(Chat.Count - 1)
    End Sub

    Private Sub SwitchPlayer()
        'Setze benötigte Flags
        SpielerIndex = (SpielerIndex + 1) Mod PlCount
        Do While Spielers(SpielerIndex).Typ = SpielerTyp.None
            SpielerIndex = (SpielerIndex + 1) Mod PlCount
        Loop
        HUDBtnC.Active = Not JokerListe.Contains(SpielerIndex)
        If Spielers(SpielerIndex).Typ <> SpielerTyp.Online Then Status = SpielStatus.Würfel Else Status = SpielStatus.Waitn
        SendNewPlayerActive(SpielerIndex)
        If Spielers(SpielerIndex).Typ = SpielerTyp.Local Then UserIndex = SpielerIndex
        ShowDice = True
        StopUpdating = False
        HUDInstructions.Text = "Roll the Dice!"
        DreifachWürfeln = CanRollThrice(SpielerIndex) 'Falls noch alle Figuren un der Homebase sind
        WürfelTimer = 0
        ReDim WürfelWerte(5)
        For i As Integer = 0 To WürfelWerte.Length - 1
            WürfelWerte(i) = 0
        Next
    End Sub
#End Region

#Region "Knopfgedrücke"
    Private Sub ExitButton() Handles HUDBtnA.Clicked
        If Microsoft.VisualBasic.MsgBox("Do you really want to leave?", Microsoft.VisualBasic.MsgBoxStyle.YesNo) = Microsoft.VisualBasic.MsgBoxResult.Yes Then
            SFX(2).Play()
            SendGameClosed()
            NetworkMode = False
            MediaPlayer.Stop()
            GameClassInstance.InGame = False
            GameClassInstance.Exit()
        End If
    End Sub

    Dim chatbtnpressed As Boolean = False

    Private Sub ChatSendButton() Handles HUDChatBtn.Clicked
        If Not chatbtnpressed Then
            chatbtnpressed = True
            SFX(2).Play()
            Dim txt As String = Microsoft.VisualBasic.InputBox("Enter your message: ", "Send message", "")
            If txt <> "" Then
                SendChatMessage(SpielerIndex, txt)
                PostChat("[" & Spielers(UserIndex).Name & "]: " & txt, HUDColor)
            End If
            chatbtnpressed = False
        End If
    End Sub
    Private Sub MenuButton() Handles HUDBtnB.Clicked
        If Microsoft.VisualBasic.MsgBox("Do you really want to leave?", Microsoft.VisualBasic.MsgBoxStyle.YesNo) = Microsoft.VisualBasic.MsgBoxResult.Yes Then
            SFX(2).Play()
            SendGameClosed()
            NetworkMode = False
            MediaPlayer.Stop()
            GameClassInstance.SwitchToSubmenu(0)
        End If
    End Sub
    Private Sub AngerButton() Handles HUDBtnC.Clicked
        If Status = SpielStatus.Würfel And Not StopUpdating Then
            StopUpdating = True
            Microsoft.VisualBasic.MsgBox("You get angry, because you suck at this game.", Microsoft.VisualBasic.MsgBoxStyle.OkOnly, "You suck!")
            If Microsoft.VisualBasic.MsgBox("You are granted a single Joker. Do you want to utilize it now?", Microsoft.VisualBasic.MsgBoxStyle.YesNo, "You suck!") = Microsoft.VisualBasic.MsgBoxResult.Yes Then
                Dim res As String = Microsoft.VisualBasic.InputBox("How far do you want to move? (12 fields are the maximum)", "You suck!")
                Try
                    Dim aim As Integer = CInt(res)
                    Do Until aim < 13
                        res = Microsoft.VisualBasic.InputBox("Screw you! I said AT MAXIMUM 12 FIELDS!", "You suck!")
                        aim = CInt(res)
                    Loop
                    WürfelWerte(0) = If(aim > 6, 6, aim)
                    WürfelWerte(1) = If(aim > 6, aim - 6, 0)
                    CalcMoves()
                    JokerListe.Add(SpielerIndex)
                    HUDBtnC.Active = False
                    SFX(2).Play()
                Catch
                    Microsoft.VisualBasic.MsgBox("Alright, then don't.", "You suck!")
                End Try
            End If
            StopUpdating = False
        Else
            SFX(0).Play()
        End If
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

    Public ReadOnly Property MapRet As GaemMap Implements IGameWindow.Map
        Get
            Return Map
        End Get
    End Property

    Public Function GetCamPos() As CamKeyframe Implements IGameWindow.GetCamPos
        If FigurFaderCamera IsNot Nothing Then Return FigurFaderCamera.Value
        Return New CamKeyframe

    End Function
#End Region
End Class