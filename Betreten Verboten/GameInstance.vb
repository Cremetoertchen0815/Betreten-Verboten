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
        Graphics.GraphicsProfile = GraphicsProfile.HiDef
        Graphics.PreferredDepthStencilFormat = DepthFormat.Depth24
        Graphics.PreferredBackBufferWidth = 1280
        Graphics.PreferredBackBufferHeight = 720
        Graphics.ApplyChanges()

        Me.Content.RootDirectory = "Content"
        Me.IsMouseVisible = True
        Me.Window.AllowUserResizing = True
        Me.Window.Title = "Betreten Verboten"
        Program.Content = Me.Content

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

        MyBase.Draw(gameTime)
    End Sub


End Class
