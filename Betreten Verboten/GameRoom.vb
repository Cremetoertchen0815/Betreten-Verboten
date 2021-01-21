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
Public Class GameRoom

    'Spiele-Flags und Variables
    Private Spielers As Player() = {Nothing, Nothing, Nothing, Nothing} 'Enthält sämtliche Spieler, die an dieser Runde teilnehmen
    Private SpielerIndex As Integer = -1 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
    Private UserIndex As Integer 'Gibt den Index des Spielers an, welcher momentan durch diese Spielinstanz repräsentiert wird
    Private Status As SpielStatus 'Speichert den aktuellen Status des Spiels
    Private WürfelAktuelleZahl As Integer 'Speichert den WErt des momentanen Würfels
    Private WürfelWerte As Integer() 'Speichert die Werte der Würfel
    Private WürfelTimer As Double 'Wird genutzt um den Würfelvorgang zu halten
    Private WürfelAnimationTimer As Double 'Implementiert einen Cooldown für die Würfelanimation
    Private WürfelTriggered As Boolean 'Gibt an ob gerade gewürfelt wird
    Private StopUpdating As Boolean 'Deaktiviert die Spielelogik
    Private Fahrzahl As Integer 'Anzahl der Felder die gefahren werden kann
    Private DreifachWürfeln As Boolean
    Private lastmstate As MouseState

    'Assets
    Private WürfelAugen As Texture2D
    Private WürfelRahmen As Texture2D
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
    Private Feld As Rectangle
    Private Center As Vector2
    Private transmatrices As Matrix() = {Matrix.CreateRotationZ(MathHelper.PiOver2 * 3), Matrix.Identity, Matrix.CreateRotationZ(MathHelper.PiOver2), Matrix.CreateRotationZ(MathHelper.Pi)}
    Private playcolor As Color() = {Color.Magenta, Color.Lime, Color.Cyan, Color.Yellow}
    Private SelectFader As Transition(Of Single) 'Fader, welcher die zur Auswahl stehenden Figuren blinken lässt
    Private FigurFader As Transition(Of Integer) 'Fader, welcher die Figuren über das Spielfeld bewegt
    Private FigurFaderZiel As (Integer, Integer) 'Gibt an welche Figur bewegt werden soll (Spieler ind., Figur ind.)

    Private Const FDist As Integer = 85
    Private Const WürfelDauer As Integer = 600
    Private Const WürfelAnimationCooldown As Integer = 70

    'Renderingtt
    Private rt As RenderTarget2D
    Private Bloom As BloomFilter

    Friend Sub Init()
        'Bereite Flags und Variablen vor
        Status = SpielStatus.WarteAufOnlineSpieler
        WürfelTimer = 0
        'DEBUG: Setze sinnvolle Werte in Variablen ein, da das Menu noch nicht funktioniert.
        Spielers = {New Player, New Player, New Player, New Player}
        Chat = New List(Of (String, Color))
    End Sub

    Friend Sub LoadContent()
        'Lade Assets
        WürfelAugen = Content.Load(Of Texture2D)("würfel_augen")
        WürfelRahmen = Content.Load(Of Texture2D)("würfel_rahmen")
        ButtonFont = Content.Load(Of SpriteFont)("font\ButtonText")
        ChatFont = Content.Load(Of SpriteFont)("font\ChatText")
        RNG = New Random()

        'Lade HUD
        HUD = New GuiSystem
        HUDBtnA = New Controls.Button("Exit", New Vector2(1500, 50), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDBtnA)
        HUDBtnB = New Controls.Button("Options", New Vector2(1500, 200), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDBtnB)
        HUDBtnC = New Controls.Button("Anger", New Vector2(1500, 350), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDBtnC)
        HUDChat = New Controls.TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(400, 800)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow, .LenLimit = 35} : HUD.Controls.Add(HUDChat)
        HUDChatBtn = New Controls.Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDChatBtn)
        HUDInstructions = New Controls.Label("Wait for all Players to arrive...", New Vector2(50, 975)) With {.Font = Content.Load(Of SpriteFont)("font/InstructionText"), .Color = Color.BlanchedAlmond} : HUD.Controls.Add(HUDInstructions)
        InstructionFader = New PropertyTransition(New TransitionTypes.TransitionType_EaseInEaseOut(700), HUDInstructions, "Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), Nothing) With {.Repeat = RepeatJob.Reverse} : Automator.Add(InstructionFader)
        HUDNameBtn = New Controls.Button("", New Vector2(500, 20), New Vector2(950, 30)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Black, 0), .Color = Color.Yellow} : HUD.Controls.Add(HUDNameBtn)
        HUD.Init()

        'Lade Spielfeld
        Feld = New Rectangle(500, 80, 950, 950)
        Center = Feld.Center.ToVector2
        SelectFader = New Transition(Of Single)(New TransitionTypes.TransitionType_EaseInEaseOut(400), 0F, 1.0F, Nothing) With {.Repeat = RepeatJob.Reverse} : Automator.Add(SelectFader)

        'Bereite das Rendering vor
        rt = New RenderTarget2D(Dev, GameSize.X, GameSize.Y, False, SurfaceFormat.Color, DepthFormat.Depth24)
        ScaleMatrix = Matrix.CreateScale(Dev.Viewport.Width / GameSize.X, Dev.Viewport.Height / GameSize.Y, 1)

        'Lade Bloom
        Bloom = New BloomFilter()
        Bloom.Load(Dev, Content, GameSize.X, GameSize.Y)
        Bloom.BloomPreset = BloomFilter.BloomPresets.SuperWide
        Bloom.BloomStrengthMultiplier = 0.6
        Bloom.BloomThreshold = 0.3
    End Sub

    Friend Sub Draw(ByVal gameTime As GameTime)
        'Setze das Render-Ziel zunächst auf das RenderTarget "rt", um später PPFX(post processing effects) hinzuzufügen zu können
        Dev.SetRenderTarget(rt)
        Dev.Clear(Color.Black)

        SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise, Nothing, ScaleMatrix)

        Bloom.BloomPreset = BloomFilter.BloomPresets.SuperWide
        Bloom.BloomStrengthMultiplier = 0.63
        Bloom.BloomThreshold = 0
        Bloom.BloomUseLuminance = True

        'Draw fields
        Dim fields As New List(Of Vector2)
        For j = 0 To 3
            'Zeichne Spielfeld
            For i = 0 To 17
                Dim loc As Vector2 = Center + Vector2.Transform(GetSpielfeldPositionen(i), transmatrices(j))
                Select Case i
                    Case PlayFieldPos.Haus1, PlayFieldPos.Haus2, PlayFieldPos.Haus3, PlayFieldPos.Haus4, PlayFieldPos.Home1, PlayFieldPos.Home2, PlayFieldPos.Home3, PlayFieldPos.Home4
                        DrawCircle(loc, 20, 25, playcolor(j), 2)
                    Case PlayFieldPos.Feld1
                        DrawCircle(loc, 28, 30, playcolor(j), 3)
                        fields.Add(loc)
                    Case Else
                        DrawCircle(loc, 28, 30, Color.White, 3)
                        fields.Add(loc)
                End Select
            Next
        Next

        'Zeichne Spielfiguren
        For j = 0 To 3
            Dim pl As Player = Spielers(j)
            Dim color As Color = playcolor(j) * If(Status = SpielStatus.WähleFigur And j = SpielerIndex, SelectFader.Value, 1.0F)
            For k As Integer = 0 To 3
                Dim chr As Integer = pl.Spielfiguren(k)
                Select Case chr
                    Case -1 'Zeichne Figur in Homebase
                        DrawChr(Center + Vector2.Transform(GetSpielfeldPositionen(k), transmatrices(j)), playcolor(j))
                    Case 40, 41, 42, 43 'Zeichne Figur in Haus
                        DrawChr(Center + Vector2.Transform(GetSpielfeldPositionen(chr - 26), transmatrices(j)), color)
                    Case Else 'Zeichne Figur auf Feld
                        Dim matrx As Matrix = transmatrices((j + Math.Floor(chr / 10)) Mod 4)
                        DrawChr(Center + Vector2.Transform(GetSpielfeldPositionen((chr Mod 10) + 4), matrx), color)
                End Select
            Next
        Next


        'Zeichne Haupt-Würfel
        If ShowDice Then
            SpriteBatch.Draw(WürfelAugen, New Rectangle(1570, 700, 300, 300), GetWürfelSourceRectangle(WürfelAktuelleZahl), HUDColor)
            SpriteBatch.Draw(WürfelRahmen, New Rectangle(1570, 700, 300, 300), Color.Lerp(HUDColor, Color.White, 0.4))
        End If
        'Zeichne Mini-Würfel
        For i As Integer = 0 To WürfelWerte.Length - 1
            If SpielerIndex = UserIndex And WürfelWerte(i) > 0 Then
                SpriteBatch.Draw(WürfelAugen, New Rectangle(1590 + i * 70, 600, 50, 50), GetWürfelSourceRectangle(WürfelWerte(i)), HUDColor)
                SpriteBatch.Draw(WürfelRahmen, New Rectangle(1590 + i * 70, 600, 50, 50), Color.Lerp(HUDColor, Color.White, 0.4))
            End If
        Next
        'DrawRectangle(Feld, Color.White)

        SpriteBatch.End()

        HUD.Draw(gameTime)

        'Rendere den Bloom-Effekt
        Dev.SetRenderTarget(Nothing)
        Dim bltxt As Texture2D = Bloom.Draw(rt, GameSize.X, GameSize.Y)

        Dev.SetRenderTarget(Nothing) 'Setze des Render-Ziel auf den Backbuffer, aka den "Bildschirm"
        SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.AnisotropicClamp)
        SpriteBatch.Draw(rt, New Rectangle(0, 0, GameSize.X, GameSize.Y), Color.White)
        SpriteBatch.Draw(bltxt, New Rectangle(0, 0, GameSize.X, GameSize.Y), Color.White)
        SpriteBatch.End()
    End Sub

    Private Sub DrawChr(vc As Vector2, color As Color)
        FillRectangle(New Rectangle(vc.X - 10, vc.Y - 10, 20, 20), color)
    End Sub

    Private Function GetChrRect(vc As Vector2) As Rectangle
        Return New Rectangle(vc.X - 15, vc.Y - 15, 30, 30)
    End Function

    Friend Sub Update(ByVal gameTime As GameTime)
        Dim mstate As MouseState = Mouse.GetState()
        Dim kstate As KeyboardState = Keyboard.GetState()
        Dim mpos As Point = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScaleMatrix)).ToPoint

        HUDInstructions.Location = New Vector2(50, 1000)

        If Not StopUpdating Then
            Dim win As Integer = CheckWin()
            If win > -1 Then
                StopUpdating = True
                HUDInstructions.Text = "Game over!"
                PostChat(Spielers(win).Name & " won!", Color.White)
            End If

            Select Case Status
                Case SpielStatus.Würfel
                    'Prüft und speichert, ob der Würfel-Knopf gedrückt wurde
                    If New Rectangle(1570, 700, 300, 300).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then
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
                                    Automator.Add(New TimerTransition(1000, Sub()
                                                                                WürfelWerte(it) = WürfelAktuelleZahl
                                                                                StopUpdating = False
                                                                                'Prüfe, ob Würfeln beendet werden soll
                                                                                If it >= WürfelWerte.Length - 1 Or (Not DreifachWürfeln And Not (it = 0 And WürfelAktuelleZahl = 6)) Or (DreifachWürfeln And it > 0 AndAlso WürfelWerte(it - 1) = 6) Or (DreifachWürfeln And it >= 2 And WürfelWerte(2) <> 6) Then CalcMoves()
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

                        If GetChrRect(Center + Vector2.Transform(vec, matrx)).Contains(mpos) And chr > -1 And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then
                            Dim defaultmov As Integer = Spielers(SpielerIndex).Spielfiguren(k)
                            If defaultmov + Fahrzahl > 43 Or IsFieldByIndexCoveredByOwnFigure(SpielerIndex, k) Then
                                HUDInstructions.Text = "Incorrect move!"
                            Else
                                Status = SpielStatus.FahreFelder
                                FigurFaderZiel = (SpielerIndex, k)
                                'Animiere wie die Figur sich nach vorne bewegt, anschließend prüfe ob andere Spieler rausgeschmissen wurden
                                FigurFader = New Transition(Of Integer)(New TransitionTypes.TransitionType_Linear(Fahrzahl * 500), defaultmov, defaultmov + Fahrzahl, AddressOf CheckKick)
                                Automator.Add(FigurFader)
                                StopUpdating = False
                            End If
                            Exit For
                        End If
                    Next
                Case SpielStatus.FahreFelder
                    'Fahre Felder
                    If FigurFader IsNot Nothing Then Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) = FigurFader.Value
                    'Falls die Zug-Animation fertig ist
                    If FigurFader IsNot Nothing AndAlso FigurFader.State = TransitionState.Done Then SwitchPlayer()
                Case SpielStatus.WarteAufOnlineSpieler
                    'Prüfe einer die vier Spieler nicht anwesend sind, kehre zurück
                    For Each sp In Spielers
                        If sp Is Nothing Then Return
                    Next

                    'Falls vollzählig, starte Spiel
                    SwitchPlayer()
            End Select
        End If

        'Set HUD color
        HUDColor = playcolor(UserIndex)
        HUDBtnA.Color = HUDColor : HUDBtnA.Border = New ControlBorder(HUDColor, HUDBtnA.Border.Width)
        HUDBtnB.Color = HUDColor : HUDBtnB.Border = New ControlBorder(HUDColor, HUDBtnB.Border.Width)
        HUDBtnC.Color = HUDColor : HUDBtnC.Border = New ControlBorder(HUDColor, HUDBtnC.Border.Width)
        HUDChat.Color = HUDColor : HUDChat.Border = New ControlBorder(HUDColor, HUDChat.Border.Width)
        HUDChatBtn.Color = HUDColor : HUDChatBtn.Border = New ControlBorder(HUDColor, HUDChatBtn.Border.Width)
        HUDNameBtn.Text = Spielers(SpielerIndex).Name
        HUDNameBtn.Color = playcolor(SpielerIndex)

        HUD.Update(gameTime, mstate, Matrix.Identity)
        lastmstate = mstate
    End Sub

#Region "Hilfsfunktionen"
    Private Sub CalcMoves()
        Dim homebase As Integer = GetHomebaseIndex(SpielerIndex) 'Eine Spielfigur-ID, die sich in der Homebase befindet(-1, falls Homebase leer ist)
        Dim startfd As Boolean = IsFieldCoveredByOwnFigure(SpielerIndex, 0) 'Ob das Start-Feld blockiert ist
        ShowDice = False

        If Is6InDiceList() And homebase > -1 And Not startfd Then 'Falls Homebase noch eine Figur enthält und 6 gewürfelt wurde, setze Figur auf Feld 0 und fahre anschließend x Felder nach vorne
            'Bereite das Homebase-verlassen vor
            Fahrzahl = GetSecondDiceAfterSix(SpielerIndex)
            Status = SpielStatus.FahreFelder
            HUDInstructions.Text = "Move Character out of your homebase and move him " & Fahrzahl & " spaces!"
            'Hole Figur aus Homebase und prüfe ob Spieler gekickt wird
            Spielers(SpielerIndex).Spielfiguren(homebase) = 0
            CheckKick()
            'Animiere wie die Figur sich nach vorne bewegt, anschließend prüfe ob andere Spieler rausgeschmissen wurden
            FigurFaderZiel = (SpielerIndex, homebase)
            FigurFader = New Transition(Of Integer)(New TransitionTypes.TransitionType_Linear(Fahrzahl * 500), -1, Fahrzahl, AddressOf CheckKick)
            Automator.Add(FigurFader)
            StopUpdating = False
        ElseIf Is6InDiceList() And homebase > -1 And startfd Then

        ElseIf (homebase = 0 And Not Is6InDiceList()) Then 'Falls Homebase komplett voll ist(keine Figur auf Spielfeld) und keine 6 gewürfelt wurde, oder die Homebase blockiert ist, ist kein Zug möglich und der nächste Spieler ist an der Reihe
            HUDInstructions.Text = "Start field blocked!"
            SwitchPlayer()
        Else 'Ansonsten fahre x Felder nach vorne mit der Figur, die anschließend ausgewählt wird
            'TODO: Add code for handling normal dice rolls and movement, as well as kicking
            Fahrzahl = If(WürfelWerte(0) = 6, WürfelWerte(0) + WürfelWerte(1), WürfelWerte(0))
            HUDInstructions.Text = "Select piece to be moved " & Fahrzahl & " spaces!"
            Status = SpielStatus.WähleFigur
        End If
    End Sub

    Private Sub CheckKick()
        'Berechne globale Spielfeldposition der rauswerfenden Figur
        Dim playerA As Integer = FigurFaderZiel.Item1
        Dim fieldA As Integer = Spielers(playerA).Spielfiguren(FigurFaderZiel.Item2)
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
                If fieldB > 0 And fieldB <= 40 And fb = fa Then
                    Spielers(playerB).Spielfiguren(j) = -1 'Kicke Spielfigur
                    PostChat(Spielers(playerA).Name & " kicked " & Spielers(playerB).Name & "!", Color.White)
                    Exit Sub
                End If
            Next
        Next
    End Sub

    Private Function CheckWin() As Integer
        For i As Integer = 0 To 3
            Dim pl As Player = Spielers(i)
            Dim check As Boolean = True
            For j As Integer = 0 To 3
                If pl.Spielfiguren(j) <= 40 Then check = False
            Next
            If check Then Return i
        Next
        Return -1
    End Function

    Private Function IsFieldCoveredByOwnFigure(player As Integer, field As Integer) As Boolean
        For i As Integer = 0 To 3
            If Spielers(player).Spielfiguren(i) = field Then Return True
        Next
        Return False
    End Function

    Private Function IsFieldByIndexCoveredByOwnFigure(player As Integer, fieldindx As Integer) As Boolean
        For i As Integer = 0 To 3
            If Spielers(player).Spielfiguren(i) = Spielers(player).Spielfiguren(fieldindx) And i <> fieldindx Then Return True
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

    Private Function Is6InDiceList() As Boolean
        For i As Integer = 0 To WürfelWerte.Length - 1
            If WürfelWerte(i) = 6 Then Return True
        Next
        Return False
    End Function

    Private Function PlayerFieldToGlobalField(field As Integer, player As Integer) As Integer
        Return (field + player * 10 - 1) Mod 40 + 1
    End Function

    Private Function GlobalFieldToPlayerField(field As Integer, player As Integer) As Integer
        Return (field - player * 10 - 1) Mod 40 + 1
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
    End Sub

    Private Sub SwitchPlayer()
        Status = SpielStatus.Würfel
        SpielerIndex = (SpielerIndex + 1) Mod 4
        UserIndex = SpielerIndex

        DreifachWürfeln = GetHomebaseIndex(SpielerIndex) = 0 'Falls noch alle Figuren un der Homebase sind
        ReDim WürfelWerte(3)
        For i As Integer = 0 To WürfelWerte.Length - 1
            WürfelWerte(i) = 0
        Next

        ShowDice = True
        StopUpdating = False
        HUDInstructions.Text = "Roll the Dice twice!"
    End Sub
#End Region

#Region "Knopfgedrücke"
    Private Sub ExitButton() Handles HUDBtnA.Clicked
        GameClassInstance.Exit()
    End Sub

    Dim chatbtnpressed As Boolean = False
    Private Sub ChatSendButton() Handles HUDChatBtn.Clicked
        If Not chatbtnpressed Then
            chatbtnpressed = True
            Dim txt As String = Microsoft.VisualBasic.InputBox("Enter your message: ", "Send message", "")
            If txt <> "" Then
                PostChat("[" & Spielers(UserIndex).Name & "]: " & txt, HUDColor)
            End If
            chatbtnpressed = False
        End If
    End Sub
    Private Sub OptionsButton() Handles HUDBtnB.Clicked

    End Sub
    Private Sub AngerButton() Handles HUDBtnC.Clicked

    End Sub
#End Region

End Class