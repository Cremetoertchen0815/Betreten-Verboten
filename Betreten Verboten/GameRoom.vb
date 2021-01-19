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
        SpriteBatch.Draw(WürfelAugen, New Rectangle(1570, 700, 300, 300), GetWürfelSourceRectangle(WürfelWert), Color.White)
        SpriteBatch.Draw(WürfelRahmen, New Rectangle(1570, 700, 300, 300), Color.White)
        FillRectangle(New Rectangle(500, 50, 950, 950), Color.White)
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

        Select Case Status
            Case SpielStatus.Würfel
                'Prüft und speichert, ob der Würfel-Knopf gedrückt wurde
                Dim WürfelBtnGedrückt As Boolean = New Rectangle(1570, 700, 300, 300).Contains(mstate.Position) And mstate.LeftButton = ButtonState.Pressed

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
#End Region

End Class