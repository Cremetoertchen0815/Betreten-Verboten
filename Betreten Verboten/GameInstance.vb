Imports System.IO
Imports Betreten_Verboten.Framework.Graphics
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

''' <summary>
''' Enthällt das Menu des Spiels und verwaltet die Spiele-Session
''' </summary>
Public Class GameInstance
    Inherits Game

    Private AktuellesSpiel As GameRoom
    Private InGame As Boolean = True 'Gibt an, ob das Menü geupdatet werden soll, oder der GameRoom

    Public Sub New()
        MyBase.New()
        Graphics = New GraphicsDeviceManager(Me)
    End Sub

    Protected Overrides Sub Initialize()
        Me.Content.RootDirectory = "Content"
        Me.IsMouseVisible = False
        Me.Window.AllowUserResizing = False
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

        Program.Automator = New Framework.Tweening.TweenManager
        Program.ReferencePixel = New Texture2D(Graphics.GraphicsDevice, 1, 1)
        Program.ReferencePixel.SetData(Of Color)({Color.White})
        Program.Dev = GraphicsDevice

        'Generiere Test-Spiel
        AktuellesSpiel = New GameRoom
        AktuellesSpiel.LoadContent()
        AktuellesSpiel.Init()
    End Sub

    Protected Overrides Sub UnloadContent()

    End Sub

    Protected Overrides Sub Update(ByVal gameTime As GameTime)
        'Update die Spieleinstanz
        If InGame Then
            AktuellesSpiel.Update(gameTime)
        End If

        'Berechne die Skalierungsmatrix
        ScaleMatrix = Matrix.CreateScale(Dev.Viewport.Width / GameSize.X, Dev.Viewport.Height / GameSize.Y, 1)

        'Update den Tweening-Manager(für Timer und animierte Übergänge)
        Automator.Update(gameTime)

        MyBase.Update(gameTime)
    End Sub

    Protected Overrides Sub Draw(ByVal gameTime As GameTime)
        GraphicsDevice.Clear(Color.Black)

        'Zeichne die Spieleinstanz
        If InGame Then
            AktuellesSpiel.Draw(gameTime)
        End If

        SpriteBatch.Begin()

        'Draw Cursor
        Dim MousePos As Vector2 = Vector2.Transform(Mouse.GetState.Position.ToVector2, Matrix.Invert(ScaleMatrix))
        Primitives2D.DrawLine(New Vector2(MousePos.X + 15, MousePos.Y + 2), New Vector2(MousePos.X - 15, MousePos.Y + 2), Color.Black, 6)
        Primitives2D.DrawLine(New Vector2(MousePos.X - 2, MousePos.Y + 15), New Vector2(MousePos.X - 2, MousePos.Y - 15), Color.Black, 6)
        Primitives2D.DrawLine(New Vector2(MousePos.X + 15, MousePos.Y), New Vector2(MousePos.X - 15, MousePos.Y), Color.Wheat, 2)
        Primitives2D.DrawLine(New Vector2(MousePos.X, MousePos.Y + 15), New Vector2(MousePos.X, MousePos.Y - 15), Color.Wheat, 2)

        SpriteBatch.End()

        MyBase.Draw(gameTime)
    End Sub


End Class
