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
        Me.Content.RootDirectory = "Content"
        Me.IsMouseVisible = True
        Program.Content = Me.Content
    End Sub

    Protected Overrides Sub Initialize()
        MyBase.Initialize()
    End Sub

    Protected Overrides Sub LoadContent()
        'Erstelle SpriteBatch
        SpriteBatch = New SpriteBatch(GraphicsDevice)

        'Generiere Test-Spiel
        AktuellesSpiel = New GameRoom
        AktuellesSpiel.LoadContent()
        AktuellesSpiel.Init()
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

        If InGame Then
            AktuellesSpiel.Draw(gameTime)
        End If

        MyBase.Draw(gameTime)
    End Sub


End Class
