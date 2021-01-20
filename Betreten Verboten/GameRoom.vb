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
    Private SpielerIndex As Integer 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
    Private UserIndex As Integer 'Gibt den Index des Spielers an, welcher momentan durch diese Spielinstanz repräsentiert wird
    Private Status As SpielStatus 'Speichert den aktuellen Status des Spiels
    Private WürfelWert As Integer 'Speichert zu erst den Wert des ersten, nach erneutem Würfeln den Wert des zweiten Würfels
    Private WürfelZweiter As Integer = 0 'Speichert den Wert des ersten Würfels zwischen, währen der zweite Gewürfelt wird
    Private WürfelTimer As Double 'Implementiert einen Cooldown für die Würfelanimation
    Private StopUpdating As Boolean 'Deaktiviert die Spielelogik

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
    Private InstructionFader As PropertyTransition
    Private ShowDice As Boolean = False
    Private HUDColor As Color
    Private Chat As List(Of (String, Color))

    'Spielfeld
    Private Const FDist As Integer = 85
    Private Feld As Rectangle
    Private Center As Vector2
    Private transmatrices As Matrix() = {Matrix.CreateRotationZ(MathHelper.PiOver2 * 3), Matrix.Identity, Matrix.CreateRotationZ(MathHelper.PiOver2), Matrix.CreateRotationZ(MathHelper.Pi)}
    Private playcolor As Color() = {Color.Magenta, Color.Lime, Color.Cyan, Color.Yellow}
    Private SelectFader As Transition(Of Single)

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
        HUDInstructions = New Controls.Label("Wait for all Players to arrive...", New Vector2(50, 970)) With {.Font = Content.Load(Of SpriteFont)("font/InstructionText"), .Color = Color.BlanchedAlmond} : HUD.Controls.Add(HUDInstructions)
        InstructionFader = New PropertyTransition(New TransitionTypes.TransitionType_EaseInEaseOut(700), HUDInstructions, "Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), Nothing) With {.Repeat = RepeatJob.Reverse} : Automator.Add(InstructionFader)
        HUD.Init()

        'Lade Spielfeld
        Feld = New Rectangle(500, 50, 950, 950)
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
        If ShowDice Or True Then
            SpriteBatch.Draw(WürfelAugen, New Rectangle(1570, 700, 300, 300), GetWürfelSourceRectangle(WürfelWert), HUDColor)
            SpriteBatch.Draw(WürfelRahmen, New Rectangle(1570, 700, 300, 300), Color.Lerp(HUDColor, Color.White, 0.4))
        End If
        'Zeichne Mini-Würfel
        If WürfelWert <> 0 Then
            Dim xoffset As Integer = If(WürfelZweiter <> 0, 80, 0)
            SpriteBatch.Draw(WürfelAugen, New Rectangle(1650 + xoffset, 600, 50, 50), GetWürfelSourceRectangle(WürfelWert), HUDColor)
            SpriteBatch.Draw(WürfelRahmen, New Rectangle(1650 + xoffset, 600, 50, 50), Color.Lerp(HUDColor, Color.White, 0.4))
        End If
        If WürfelZweiter <> 0 Then
            SpriteBatch.Draw(WürfelAugen, New Rectangle(1650, 600, 50, 50), GetWürfelSourceRectangle(WürfelZweiter), HUDColor)
            SpriteBatch.Draw(WürfelRahmen, New Rectangle(1650, 600, 50, 50), Color.Lerp(HUDColor, Color.White, 0.4))
        End If
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

    Friend Sub Update(ByVal gameTime As GameTime)
        Dim mstate As MouseState = Mouse.GetState()
        Dim kstate As KeyboardState = Keyboard.GetState()
        Dim mpos As Point = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScaleMatrix)).ToPoint

        HUDInstructions.Location = New Vector2(50, 1000)

        If Not StopUpdating Then
            Select Case Status
                Case SpielStatus.Würfel
                    'Prüft und speichert, ob der Würfel-Knopf gedrückt wurde
                    Dim WürfelBtnGedrückt As Boolean = New Rectangle(1570, 700, 300, 300).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed

                    'Solange Knopf gedrückt, generiere zufällige Zahl in einem Intervall von 50ms
                    If WürfelBtnGedrückt Then
                        WürfelTimer += gameTime.ElapsedGameTime.TotalMilliseconds

                        If WürfelTimer > 50 Then
                            WürfelTimer = 0
                            WürfelWert = RNG.Next(1, 7)
                        End If
                    ElseIf Not WürfelBtnGedrückt And WürfelWert > 0 Then
                        'Gebe Würfe-Ergebniss auf dem Bildschirm aus
                        HUDInstructions.Text = "You got a " & WürfelWert.ToString & "!"
                        StopUpdating = True

                        If WürfelZweiter = 0 Then 'Soeben gewürfelter Wurf ist der Erste
                            'Speicher ersten Würfelwert zwischen und nach kurzer Pause, erwarte zweiten Würfel-Wurf
                            Automator.Add(New TimerTransition(1000, Sub()
                                                                        WürfelZweiter = WürfelWert
                                                                        StopUpdating = False
                                                                        WürfelWert = 0
                                                                    End Sub))
                        Else 'Soeben gewürfelter Wurf ist der Zweite
                            'Wenn Knopf losgelassen wurde, fahre fort mit der Figurwahl(nach kurzem delay)
                            Automator.Add(New TimerTransition(1000, Sub()
                                                                        Status = SpielStatus.WähleFigur
                                                                        ShowDice = False
                                                                        HUDInstructions.Text = "Select the piece you want to move!"
                                                                    End Sub))
                        End If

                    End If
                Case SpielStatus.WähleFigur

                Case SpielStatus.FahreFelder

                Case SpielStatus.WarteAufOnlineSpieler
                    'Prüfe einer die vier Spieler nicht anwesend sind, kehre zurück
                    For Each sp In Spielers
                        If sp Is Nothing Then Return
                    Next

                    'Falls vollzählig, starte Spiel
                    SwitchPlayer(0)
            End Select
        End If

        'Set HUD color
        HUDColor = playcolor(UserIndex)
        HUDBtnA.Color = HUDColor : HUDBtnA.Border = New ControlBorder(HUDColor, HUDBtnA.Border.Width)
        HUDBtnB.Color = HUDColor : HUDBtnB.Border = New ControlBorder(HUDColor, HUDBtnB.Border.Width)
        HUDBtnC.Color = HUDColor : HUDBtnC.Border = New ControlBorder(HUDColor, HUDBtnC.Border.Width)
        HUDChat.Color = HUDColor : HUDChat.Border = New ControlBorder(HUDColor, HUDChat.Border.Width)
        HUDChatBtn.Color = HUDColor : HUDChatBtn.Border = New ControlBorder(HUDColor, HUDChatBtn.Border.Width)

        HUD.Update(gameTime, mstate, Matrix.Identity)
    End Sub

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

    Private Sub SwitchPlayer(indx As Integer)
        Status = SpielStatus.Würfel
        SpielerIndex = indx
        WürfelWert = 0
        ShowDice = True
        StopUpdating = False
        PostChat("It's Player " & indx.ToString & "'s Turn!", Color.White)
        HUDInstructions.Text = "Roll the Dice twice!"
    End Sub
#End Region

End Class