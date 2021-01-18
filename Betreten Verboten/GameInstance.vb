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
    Private rt As RenderTarget2D

    Public Sub New()
        MyBase.New()

        Graphics = New GraphicsDeviceManager(Me)
        Graphics.PreferredDepthStencilFormat = DepthFormat.Depth24
        Me.Content.RootDirectory = "Content"
        Me.IsMouseVisible = True
        Program.Content = Me.Content
    End Sub

    Protected Overrides Sub Initialize()
        MyBase.Initialize()
    End Sub

    Protected Overrides Sub LoadContent()
        'Erstelle SpriteBatchch
        spriteBatch = New SpriteBatch(GraphicsDevice)

        Program.Automator = New Framework.Tweening.TweenManager
        Program.ReferencePixel = New Texture2D(Graphics.GraphicsDevice, 1, 1)
        Program.ReferencePixel.SetData(Of Color)({Color.White})

        'Generiere Test-Spiel
        AktuellesSpiel = New GameRoom
        AktuellesSpiel.LoadContent()
        AktuellesSpiel.Init()

        rt = New RenderTarget2D(GraphicsDevice, GameSize.X, GameSize.Y, False, SurfaceFormat.Color, DepthFormat.Depth24)
    End Sub

    Protected Overrides Sub UnloadContent()

    End Sub

    Protected Overrides Sub Update(ByVal gameTime As GameTime)
        If InGame Then
            AktuellesSpiel.Update(gameTime)
        End If

        MyBase.Update(gameTime)
    End Sub

    Protected Overrides Sub Draw(ByVal gameTime As GameTime)
        GraphicsDevice.Clear(Color.Red)
        GraphicsDevice.SetRenderTarget(rt)

        If InGame Then
            AktuellesSpiel.Draw(gameTime)
        End If


        GraphicsDevice.SetRenderTarget(Nothing)
        SpriteBatch.Begin()
        SpriteBatch.Draw(rt, New Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.White)
        SpriteBatch.End()

        MyBase.Draw(gameTime)
    End Sub


End Class
