Imports System.IO
Imports Betreten_Verboten.Framework.Graphics
Imports Betreten_Verboten.Framework.Graphics.PostProcessing
Imports Betreten_Verboten.Framework.Tweening
Imports Betreten_Verboten.Networking
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

''' <summary>
''' Enthällt das Menu des Spiels und verwaltet die Spiele-Session
''' </summary>
Public Class GameInstance
    Inherits Microsoft.Xna.Framework.Game

    'Menü Flags
    Friend InGame As Boolean = False 'Gibt an, ob das Menü geupdatet werden soll, oder der GameRoom
    Private AktuellesSpiel As GameRoom
    Private Timer As Integer = 0 'Fungiert nicht nur als Zeit-Puffer für den Start-Text, sondern auch als Flag für die Menü-Überblendung
    Private MenuAktiviert As Boolean = False
    Private SingleGameFrame As Boolean = False 'Helper-Flag für den ersten Frame des Spiels
    Private Submenu As Integer = 0 'Gibt an in welchem Untermenü sich der User befindet
    Private lastmstate As MouseState
    Private NewGamePlayers As SpielerTyp() = {SpielerTyp.Local, SpielerTyp.Local, SpielerTyp.Local, SpielerTyp.Local}
    Private Schwierigkeitsgrad As Difficulty
    Private ChangeNameButtonPressed As Boolean = False
    Private OnlineGameInstances As OnlineGameInstance()
    Private ServerRefreshTimer As Integer = 0
    Private MenschenOnline As Integer = 0

    'Konstanten
    Friend Const FadeOverTime As Integer = 800
    Friend Const ServerAutoRefresh As Integer = 500

    'Assets & Faders
    Protected Schwarzblende As ShaderTransition
    Protected Blinker As Transition(Of Single)
    Protected FgColor As Color
    Protected TitleFont As SpriteFont
    Protected MediumFont As SpriteFont

    'Rendering
    Friend BrightFX As Effect
    Protected ffx As BloomFilter
    Protected TempTarget As RenderTarget2D

    Public Sub New()
        MyBase.New()
        Graphics = New GraphicsDeviceManager(Me)
    End Sub

    Protected Overrides Sub Initialize()
        Me.Content.RootDirectory = "Content"
        Me.IsMouseVisible = False
        Me.Window.AllowUserResizing = True
        Me.Window.Title = "Betreten Verboten"
        Program.Content = Me.Content

        Graphics.GraphicsProfile = GraphicsProfile.HiDef
        Graphics.PreferredDepthStencilFormat = DepthFormat.Depth24
        Graphics.ApplyChanges()

        Graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width
        Graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height
        Graphics.SynchronizeWithVerticalRetrace = True
        Graphics.ApplyChanges()

        MyBase.Initialize()
    End Sub

    Protected Overrides Sub LoadContent()

        'Erstelle SpriteBatchch
        SpriteBatch = New SpriteBatch(GraphicsDevice)
        DefaultFont = Content.Load(Of SpriteFont)("font\fnt_HKG_17_M")
        TitleFont = Content.Load(Of SpriteFont)("font\MenuTitle")
        MediumFont = Content.Load(Of SpriteFont)("font\MenuMain")

        Program.Automator = New Framework.Tweening.TweenManager
        Program.ReferencePixel = New Texture2D(Graphics.GraphicsDevice, 1, 1)
        Program.ReferencePixel.SetData(Of Color)({Color.White})
        Program.Dev = GraphicsDevice

        'Generiere Test-Spiel
        AktuellesSpiel = New GameRoom
        AktuellesSpiel.LoadContent()
        AktuellesSpiel.Init()


        'Generate temporary main rendertarget for bloom effect
        TempTarget = New RenderTarget2D(
            Graphics.GraphicsDevice,
            GameSize.X,
            GameSize.Y,
            False,
            Graphics.GraphicsDevice.PresentationParameters.BackBufferFormat,
            DepthFormat.Depth24, 0, RenderTargetUsage.DiscardContents) With {.Name = "TmpA"}


        'Generate Bloom filter
        ffx = New BloomFilter
        ffx.Load(GraphicsDevice, Content, GameSize.X, GameSize.Y)
        ffx.BloomPreset = BloomFilter.BloomPresets.SuperWide
        ffx.BloomThreshold = 0.02
        ffx.BloomStrengthMultiplier = 0.67
        ffx.BloomUseLuminance = True


        Blinker = New Transition(Of Single) With {.Value = 0}
        FgColor = New Color(0, 180, 0)
        BrightFX = Content.Load(Of Effect)("fx/fx_fadetocolor")
        BrightFX.Parameters("amount").SetValue(0.0F)
        If My.Settings.Username = "" Then My.Settings.Username = Environment.UserName : My.Settings.Save()
        LocalClient = New Client
    End Sub

    Protected Overrides Sub UnloadContent()

    End Sub

    Protected Overrides Sub Update(ByVal gameTime As GameTime)
        Dim mstate As MouseState = Mouse.GetState()
        Dim kstate As KeyboardState = Keyboard.GetState()
        Dim mpos As Point = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScaleMatrix)).ToPoint
        Dim OneshotPressed As Boolean = mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released

        'Blende Start-Bildschirm ein(auf erstem Frame)
        If Not SingleGameFrame Then
            SingleGameFrame = True
            Schwarzblende = New ShaderTransition(New TransitionTypes.TransitionType_Linear(FadeOverTime), 0F, 1.0F, BrightFX, "amount", Nothing)
            Automator.Add(Schwarzblende)
        End If

        'Update die Spieleinstanz
        If InGame Then
            AktuellesSpiel.Update(gameTime)
        Else
            'Implementiere Start-Puffer
            If Timer >= 0 Then Timer += gameTime.ElapsedGameTime.TotalMilliseconds
            If Timer > 2000 Then
                Timer = -1
                Blinker = New Transition(Of Single)(New TransitionTypes.TransitionType_Linear(800), 0F, 1.0F, Nothing) With {.Repeat = RepeatJob.Reverse}
                Automator.Add(Blinker)
            End If

            'Implementiere das Wechseln vom Start-Bildschirm zum Menü
            If Timer < 0 Then
                'Menü-Fuktionen
                If Timer = -1 And kstate.IsKeyDown(Keys.Enter) Then
                    Timer = -2
                    Schwarzblende = New ShaderTransition(New TransitionTypes.TransitionType_Linear(FadeOverTime), 1.0F, 0F, BrightFX, "amount", Sub()
                                                                                                                                                    MenuAktiviert = True
                                                                                                                                                    Schwarzblende = New ShaderTransition(New TransitionTypes.TransitionType_Linear(1000), 0F, 1.0F, BrightFX, "amount", Nothing)
                                                                                                                                                    Automator.Add(Schwarzblende)
                                                                                                                                                End Sub)
                    Automator.Add(Schwarzblende)
                End If
            End If

            If MenuAktiviert And Schwarzblende.State <> TransitionState.InProgress And Not ChangeNameButtonPressed Then
                Select Case Submenu
                    Case 0
                        'Wähle Menüpunkt
                        If New Rectangle(560, 200, 800, 100).Contains(mpos) And OneshotPressed Then SwitchToSubmenu(1)
                        If New Rectangle(560, 350, 800, 100).Contains(mpos) And OneshotPressed And LocalClient.Connected Then SwitchToSubmenu(2)
                        If New Rectangle(560, 500, 800, 100).Contains(mpos) And OneshotPressed Then SwitchToSubmenu(3)
                        If New Rectangle(560, 650, 800, 100).Contains(mpos) And OneshotPressed Then
                            Schwarzblende = New ShaderTransition(New TransitionTypes.TransitionType_Linear(FadeOverTime), 1.0F, 0F, BrightFX, "amount", Sub() [Exit]())
                            Automator.Add(Schwarzblende)
                        End If
                        'Wechsel Benutzername
                        If New Rectangle(New Point(20, 40), MediumFont.MeasureString("Username: " & My.Settings.Username).ToPoint).Contains(mpos) And OneshotPressed Then
                            OpenInputbox("Enter the new username: ", "Change username", Sub(x)
                                                                                            My.Settings.Username = x
                                                                                            My.Settings.Save()
                                                                                        End Sub, My.Settings.Username)

                        End If
                    Case 1
                        If New Rectangle(560, 200, 800, 100).Contains(mpos) And OneshotPressed Then NewGamePlayers(0) = (NewGamePlayers(0) + 1) Mod 2
                        If New Rectangle(560, 350, 800, 100).Contains(mpos) And OneshotPressed Then NewGamePlayers(1) = (NewGamePlayers(1) + 1) Mod If(IsConnectedToServer, 3, 2)
                        If New Rectangle(560, 500, 800, 100).Contains(mpos) And OneshotPressed Then NewGamePlayers(2) = (NewGamePlayers(2) + 1) Mod If(IsConnectedToServer, 3, 2)
                        If New Rectangle(560, 650, 800, 100).Contains(mpos) And OneshotPressed Then NewGamePlayers(3) = (NewGamePlayers(3) + 1) Mod If(IsConnectedToServer, 3, 2)
                        If New Rectangle(560, 900, 400, 100).Contains(mpos) And OneshotPressed Then SwitchToSubmenu(0)
                        If New Rectangle(960, 900, 400, 100).Contains(mpos) And OneshotPressed Then
                            AktuellesSpiel.Spielers(0) = New Player(NewGamePlayers(0), My.Settings.Schwierigkeitsgrad) With {.Name = If(NewGamePlayers(0) = SpielerTyp.Local, My.Settings.Username, "CPU 1")}
                            For i As Integer = 1 To 3
                                Select Case NewGamePlayers(i)
                                    Case SpielerTyp.Local
                                        AktuellesSpiel.Spielers(i) = New Player(SpielerTyp.Local, My.Settings.Schwierigkeitsgrad) With {.Name = My.Settings.Username & "-" & (i + 1).ToString}
                                    Case SpielerTyp.CPU
                                        AktuellesSpiel.Spielers(i) = New Player(SpielerTyp.CPU, My.Settings.Schwierigkeitsgrad) With {.Name = "CPU " & (i + 1).ToString}
                                    Case SpielerTyp.Online
                                        AktuellesSpiel.Spielers(i) = New Player(SpielerTyp.Online, My.Settings.Schwierigkeitsgrad) With {.Bereit = False}
                                End Select
                            Next
                            Schwarzblende = New ShaderTransition(New TransitionTypes.TransitionType_Linear(FadeOverTime), 1.0F, 0F, BrightFX, "amount", Sub()
                                                                                                                                                            InGame = True
                                                                                                                                                            Schwarzblende = New ShaderTransition(New TransitionTypes.TransitionType_Linear(1000), 0F, 1.0F, BrightFX, "amount", Nothing)
                                                                                                                                                            Automator.Add(Schwarzblende)
                                                                                                                                                        End Sub)
                            Automator.Add(Schwarzblende)
                        End If
                    Case 2
                        If New Rectangle(560, 200, 400, 100).Contains(mpos) And OneshotPressed Then NewGamePlayers(0) = (NewGamePlayers(0) + 1) Mod 2
                        If New Rectangle(960, 200, 400, 100).Contains(mpos) And OneshotPressed Then NewGamePlayers(0) = (NewGamePlayers(0) + 1) Mod 2
                        If New Rectangle(560, 350, 800, 100).Contains(mpos) And OneshotPressed Then NewGamePlayers(1) = (NewGamePlayers(1) + 1) Mod If(IsConnectedToServer, 3, 2)
                        If New Rectangle(560, 500, 800, 100).Contains(mpos) And OneshotPressed Then NewGamePlayers(2) = (NewGamePlayers(2) + 1) Mod If(IsConnectedToServer, 3, 2)
                        If New Rectangle(560, 650, 800, 100).Contains(mpos) And OneshotPressed Then SwitchToSubmenu(0)
                    Case 3
                        If New Rectangle(560, 200, 800, 100).Contains(mpos) And OneshotPressed Then NewGamePlayers(0) = (NewGamePlayers(0) + 1) Mod 2
                        If New Rectangle(560, 350, 800, 100).Contains(mpos) And OneshotPressed Then
                            If ServerActive Then
                                LocalClient.Disconnect()
                                StopServer()
                            End If
                        End If
                        If New Rectangle(560, 500, 800, 100).Contains(mpos) And OneshotPressed And Not ServerActive Then
                            'If LocalClient.Connected Then LocalClient.Disconnect()
                            StartServer()
                            LocalClient.Connect("127.0.0.1", My.Settings.Username)
                        End If
                        If New Rectangle(560, 650, 800, 100).Contains(mpos) And OneshotPressed Then SwitchToSubmenu(0)
                End Select
            End If


            'Grab data from Server in a set interval
            If IsConnectedToServer Then
                ServerRefreshTimer += gameTime.ElapsedGameTime.TotalMilliseconds
                If ServerRefreshTimer > ServerAutoRefresh Then
                    ServerRefreshTimer = 0
                    MenschenOnline = LocalClient.GetOnlineMemberCount
                    OnlineGameInstances = LocalClient.GetGamesList
                End If
            End If
        End If

        'Berechne die Skalierungsmatrix
        ScaleMatrix = Matrix.CreateScale(Dev.Viewport.Width / GameSize.X, Dev.Viewport.Height / GameSize.Y, 1)

        'Update den Tweening-Manager(für Timer und animierte Übergänge)
        Automator.Update(gameTime)
        lastmstate = mstate
        MyBase.Update(gameTime)
    End Sub

    Protected Overrides Sub Draw(ByVal gameTime As GameTime)

        'Setze das Render-Ziel zunächst auf das RenderTarget "TempTarget", um später PPFX(post processing effects) hinzuzufügen zu können
        GraphicsDevice.SetRenderTargets(TempTarget)
        GraphicsDevice.Clear(Color.Black)

        'Zeichne die Spieleinstanz
        If InGame Then
            AktuellesSpiel.Draw(gameTime)
        Else

            SpriteBatch.Begin(SpriteSortMode.Deferred, Nothing, SamplerState.AnisotropicClamp, Nothing, Nothing, Nothing, ScaleMatrix)

            If MenuAktiviert Then
                'Zeichne Rechtecke
                DrawRectangle(New Rectangle(560, 200, 800, 100), FgColor)
                DrawRectangle(New Rectangle(560, 350, 800, 100), FgColor)
                DrawRectangle(New Rectangle(560, 500, 800, 100), FgColor)
                DrawRectangle(New Rectangle(560, 650, 800, 100), FgColor)

                'Zeichne Menü
                Select Case Submenu
                    Case 0
                        'Zeichne Serverheading
                        'Zeichne Texte
                        SpriteBatch.DrawString(MediumFont, "Start Round", New Vector2(GameSize.X / 2 - MediumFont.MeasureString("Start Game").X / 2, 225), FgColor)
                        SpriteBatch.DrawString(MediumFont, "Join Round", New Vector2(GameSize.X / 2 - MediumFont.MeasureString("Join Round").X / 2, 375), If(IsConnectedToServer, FgColor, Color.Red))
                        SpriteBatch.DrawString(MediumFont, "Server Settings", New Vector2(GameSize.X / 2 - MediumFont.MeasureString("Server Settings").X / 2, 525), FgColor)
                        SpriteBatch.DrawString(MediumFont, "Exit Game", New Vector2(GameSize.X / 2 - MediumFont.MeasureString("Exit Game").X / 2, 675), FgColor)
                    Case 1
                        SpriteBatch.DrawString(MediumFont, "Start Round", New Vector2(GameSize.X / 2 - MediumFont.MeasureString("Start Round").X / 2, 50), FgColor)

                        SpriteBatch.DrawString(MediumFont, "Player 1: " & NewGamePlayers(0).ToString, New Vector2(GameSize.X / 2 - MediumFont.MeasureString("Player 1: " & NewGamePlayers(0).ToString).X / 2, 225), FgColor)
                        SpriteBatch.DrawString(MediumFont, "Player 2: " & NewGamePlayers(1).ToString, New Vector2(GameSize.X / 2 - MediumFont.MeasureString("Player 2: " & NewGamePlayers(1).ToString).X / 2, 375), FgColor)
                        SpriteBatch.DrawString(MediumFont, "Player 3: " & NewGamePlayers(2).ToString, New Vector2(GameSize.X / 2 - MediumFont.MeasureString("Player 3: " & NewGamePlayers(2).ToString).X / 2, 525), FgColor)
                        SpriteBatch.DrawString(MediumFont, "Player 4: " & NewGamePlayers(3).ToString, New Vector2(GameSize.X / 2 - MediumFont.MeasureString("Player 4: " & NewGamePlayers(3).ToString).X / 2, 675), FgColor)
                        DrawRectangle(New Rectangle(560, 900, 800, 100), FgColor)
                        DrawLine(New Vector2(GameSize.X / 2, 900), New Vector2(GameSize.X / 2, 1000), FgColor)
                        SpriteBatch.DrawString(MediumFont, "Back", New Vector2(GameSize.X / 2 - 200 - MediumFont.MeasureString("Back").X / 2, 925), FgColor)
                        SpriteBatch.DrawString(MediumFont, "Start Round", New Vector2(GameSize.X / 2 + 200 - MediumFont.MeasureString("Start Round").X / 2, 925), FgColor)
                    Case 2
                        DrawLine(New Vector2(GameSize.X / 2, 200), New Vector2(GameSize.X / 2, 300), FgColor)
                        SpriteBatch.DrawString(MediumFont, "←", New Vector2(GameSize.X / 2 - 200 - MediumFont.MeasureString("←").X / 2, 225), FgColor)
                        SpriteBatch.DrawString(MediumFont, "→", New Vector2(GameSize.X / 2 + 200 - MediumFont.MeasureString("→").X / 2, 225), FgColor)
                        SpriteBatch.DrawString(MediumFont, "[No open rounds]", New Vector2(GameSize.X / 2 - MediumFont.MeasureString("[No open rounds]").X / 2, 375), If(IsConnectedToServer, FgColor, Color.Red))
                        SpriteBatch.DrawString(MediumFont, "Join Round", New Vector2(GameSize.X / 2 - MediumFont.MeasureString("Join Round").X / 2, 525), FgColor)
                        SpriteBatch.DrawString(MediumFont, "Back to Main Menu", New Vector2(GameSize.X / 2 - MediumFont.MeasureString("Back to Main Menu").X / 2, 675), FgColor)
                    Case 3
                        SpriteBatch.DrawString(MediumFont, "Connect to Server", New Vector2(GameSize.X / 2 - MediumFont.MeasureString("Connect to Server").X / 2, 225), If(IsConnectedToServer Or ServerActive, Color.Red, FgColor))
                        SpriteBatch.DrawString(MediumFont, "Disconnect Server", New Vector2(GameSize.X / 2 - MediumFont.MeasureString("Disconnect Server").X / 2, 375), If(IsConnectedToServer Or ServerActive, FgColor, Color.Red))
                        SpriteBatch.DrawString(MediumFont, "Open local Server", New Vector2(GameSize.X / 2 - MediumFont.MeasureString("Open local Server").X / 2, 525), If(IsConnectedToServer Or ServerActive, Color.Red, FgColor))
                        SpriteBatch.DrawString(MediumFont, "Back to Main Menu", New Vector2(GameSize.X / 2 - MediumFont.MeasureString("Back to Main Menu").X / 2, 675), FgColor)
                End Select

                'Zeichne Info
                Dim txtA As String = "Username: " & My.Settings.Username
                Dim txtB As String = If(IsConnectedToServer, "Connected to: " & Environment.NewLine & LocalClient.Hostname & Environment.NewLine & MenschenOnline & " human(s) online", "No server connected")
                SpriteBatch.DrawString(MediumFont, txtA, New Vector2(20, 40), FgColor)
                SpriteBatch.DrawString(MediumFont, txtB, New Vector2(GameSize.X - MediumFont.MeasureString(txtB).X - 20, 40), FgColor)
            Else
                'Zeichne Startbildschirm
                Dim titletxt As String = "Betreten Verboten!"
                Dim starttxt As String = "---PRESS ENTER---"
                Dim copyrighttxt As String = "Made by der cooleren Informatikgruppe }:)"
                SpriteBatch.DrawString(TitleFont, titletxt, New Vector2((GameSize.X - TitleFont.MeasureString(titletxt).X) / 2, 200), FgColor)
                SpriteBatch.DrawString(MediumFont, starttxt, New Vector2((GameSize.X - MediumFont.MeasureString(starttxt).X) / 2, 800), FgColor * Blinker.Value)
                SpriteBatch.DrawString(MediumFont, copyrighttxt, New Vector2((GameSize.X - MediumFont.MeasureString(copyrighttxt).X) / 2, 950), FgColor)
            End If

            SpriteBatch.End()
        End If



        'Generiere bloom
        GraphicsDevice.SetRenderTargets(Nothing)
        Dim txbloom As Texture2D = ffx.Draw(TempTarget, GameSize.X, GameSize.Y)
        GraphicsDevice.SetRenderTargets(Nothing)
        'Zeichne bloom
        SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.AnisotropicClamp, Nothing, Nothing, BrightFX, Nothing)
        SpriteBatch.Draw(TempTarget, New Rectangle(0, 0, GameSize.X, GameSize.Y), Color.White)
        SpriteBatch.Draw(txbloom, New Rectangle(0, 0, GameSize.X, GameSize.Y), Color.White)
        SpriteBatch.End()

        'Zeichne Cursor
        SpriteBatch.Begin(SpriteSortMode.Deferred, Nothing, SamplerState.AnisotropicClamp, Nothing, Nothing, Nothing, ScaleMatrix)
        Dim MousePos As Vector2 = Vector2.Transform(Mouse.GetState.Position.ToVector2, Matrix.Invert(ScaleMatrix))
        Primitives2D.DrawLine(New Vector2(MousePos.X + 15, MousePos.Y + 2), New Vector2(MousePos.X - 15, MousePos.Y + 2), Color.Black, 6)
        Primitives2D.DrawLine(New Vector2(MousePos.X - 2, MousePos.Y + 15), New Vector2(MousePos.X - 2, MousePos.Y - 15), Color.Black, 6)
        Primitives2D.DrawLine(New Vector2(MousePos.X + 15, MousePos.Y), New Vector2(MousePos.X - 15, MousePos.Y), Color.Wheat, 2)
        Primitives2D.DrawLine(New Vector2(MousePos.X, MousePos.Y + 15), New Vector2(MousePos.X, MousePos.Y - 15), Color.Wheat, 2)
        SpriteBatch.End()

        MyBase.Draw(gameTime)
    End Sub


    Friend Sub SwitchToSubmenu(submenu As Integer, Optional InBetweenOperation As Action = Nothing)
        'Bereite Submenu vor
        Select Case submenu
            Case 1
                NewGamePlayers = {SpielerTyp.Local, SpielerTyp.Local, SpielerTyp.Local, SpielerTyp.Local}
        End Select

        'Blende über
        Schwarzblende = New ShaderTransition(New TransitionTypes.TransitionType_Linear(FadeOverTime), 1.0F, 0F, BrightFX, "amount", Sub()
                                                                                                                                        If InBetweenOperation IsNot Nothing Then InBetweenOperation()
                                                                                                                                        InGame = False
                                                                                                                                        Me.Submenu = submenu
                                                                                                                                        Schwarzblende = New ShaderTransition(New TransitionTypes.TransitionType_Linear(1000), 0F, 1.0F, BrightFX, "amount", Nothing)
                                                                                                                                        Automator.Add(Schwarzblende)
                                                                                                                                    End Sub)
        Automator.Add(Schwarzblende)
    End Sub

    Private Sub OpenInputbox(message As String, title As String, finalaction As Action(Of String), Optional defaultvalue As String = "")
        If Not ChangeNameButtonPressed Then
            ChangeNameButtonPressed = True
            Dim txt As String = Microsoft.VisualBasic.InputBox(message, title, defaultvalue)
            If txt <> "" Then
                finalaction.Invoke(txt)
            End If
            ChangeNameButtonPressed = False
        End If
    End Sub

    Private ReadOnly Property IsConnectedToServer() As Boolean
        Get
            Return LocalClient.Connected
        End Get
    End Property

End Class
