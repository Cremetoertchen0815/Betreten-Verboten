Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports Betreten_Verboten.Framework.Graphics
Imports Betreten_Verboten.Framework.UI
Imports System.Collections.Generic

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
    Private Chat As List(Of String)

    'Spielfeld
    Private Const FDist As Integer = 85
    Private Feld As Rectangle
    Private Center As Vector2

    'Renderingtt
    Private rt As RenderTarget2D

    Friend Sub Init()
        'Bereite Flags und Variablen vor
        Status = SpielStatus.WarteAufOnlineSpieler
        WürfelTimer = 0
        'DEBUG: Setze sinnvolle Werte in Variablen ein, da das Menu noch nicht funktioniert.
        Spielers = {New Player, New Player, New Player, New Player}
        Chat = New List(Of String)
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
        HUDChat = New Controls.TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(400, 900)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow, .LenLimit = 35} : HUD.Controls.Add(HUDChat)
        HUDChatBtn = New Controls.Button("Send Message", New Vector2(50, 970), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDChatBtn)
        HUD.Init()

        'Lade Spielfeld
        Feld = New Rectangle(500, 50, 950, 950)
        Center = Feld.Center.ToVector2

        'Bereite das Rendering vor
        rt = New RenderTarget2D(Dev, GameSize.X, GameSize.Y, False, SurfaceFormat.Color, DepthFormat.Depth24)
        ScaleMatrix = Matrix.CreateScale(Dev.Viewport.Width / GameSize.X, Dev.Viewport.Height / GameSize.Y, 1)
    End Sub

    Friend Sub Draw(ByVal gameTime As GameTime)
        'Setze das Render-Ziel zunächst auf das RenderTarget "rt", um später PPFX(post processing effects) hinzuzufügen zu können
        Dev.SetRenderTarget(rt)
        Dev.Clear(Color.Black)

        'Zeichne HUD
        SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise, Nothing, ScaleMatrix)
        SpriteBatch.Draw(WürfelAugen, New Rectangle(1570, 700, 300, 300), GetWürfelSourceRectangle(WürfelWert), Color.Yellow)
        SpriteBatch.Draw(WürfelRahmen, New Rectangle(1570, 700, 300, 300), Color.GreenYellow)
        DrawRectangle(Feld, Color.White)

        'Draw fields
        Dim fields As New List(Of Vector2)
        Dim transmatrices As Matrix() = {Matrix.Identity, Matrix.CreateRotationZ(MathHelper.PiOver2), Matrix.CreateRotationZ(MathHelper.Pi), Matrix.CreateRotationZ(MathHelper.PiOver2 * 3)}
        Dim playcolor As Color() = {Color.Blue, Color.Lime, Color.Cyan, Color.Yellow}
        For j = 0 To 3
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

        'Draw connecting lines
        For i = 0 To fields.Count - 1
            Dim p1 As Vector2 = fields(i)
            Dim p2 As Vector2 = If(i = fields.Count - 1, fields(0), fields(i + 1))
            DrawLine({p1, p2}, Color.White, 1)
        Next

        SpriteBatch.End()

        HUD.Draw(gameTime)


        Dev.SetRenderTarget(Nothing) 'Setze des Render-Ziel auf den Backbuffer, aka den "Bildschirm"
        SpriteBatch.Begin(SpriteSortMode.Deferred, Nothing, SamplerState.AnisotropicClamp)
        SpriteBatch.Draw(rt, New Rectangle(0, 0, GameSize.X, GameSize.Y), Color.White)
        SpriteBatch.End()
    End Sub

    Friend Sub Update(ByVal gameTime As GameTime)
        Dim mstate As MouseState = Mouse.GetState()
        Dim kstate As KeyboardState = Keyboard.GetState()
        Dim mpos As Point = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScaleMatrix)).ToPoint

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
                Chat.Add("[User]: " & txt)
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

    Private Function GetSpielfeldPositionen(ps As PlayfieldPos) As Vector2
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
#End Region

End Class