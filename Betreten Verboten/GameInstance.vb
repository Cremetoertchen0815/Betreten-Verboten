Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

''' <summary>
''' Enthällt das Menu des Spiels und verwaltet die Spiele-Session
''' </summary>
Public Class GameInstance
    Inherits Game

    Public Sub New()
        MyBase.New()
        Graphics = New GraphicsDeviceManager(Me)
        Content.RootDirectory = "Content"
        Program.Content = Me.Content
    End Sub

    Protected Overrides Sub Initialize()
        MyBase.Initialize()
    End Sub

    Protected Overrides Sub LoadContent()
        'Erstelle SpriteBatch
        SpriteBatch = New SpriteBatch(GraphicsDevice)
    End Sub

    Protected Overrides Sub UnloadContent()

    End Sub

    Protected Overrides Sub Update(ByVal gameTime As GameTime)

        MyBase.Update(gameTime)
    End Sub

    Protected Overrides Sub Draw(ByVal gameTime As GameTime)
        GraphicsDevice.Clear(Color.Red)

        MyBase.Draw(gameTime)
    End Sub


End Class
