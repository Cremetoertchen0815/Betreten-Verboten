Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

Namespace Betreten_Verboten
    Public Class Game1
        Inherits Game

        Private graphics As GraphicsDeviceManager
        Private spriteBatch As SpriteBatch

        Public Sub New()
            MyBase.New()
            graphics = New GraphicsDeviceManager(Me)
            Content.RootDirectory = "Content"
        End Sub

        Protected Overrides Sub Initialize()
            MyBase.Initialize()
        End Sub

        Protected Overrides Sub LoadContent()
            ' Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = New SpriteBatch(GraphicsDevice)
        End Sub

        Protected Overrides Sub UnloadContent()

        End Sub

        Protected Overrides Sub Update(ByVal gameTime As GameTime)
            If GamePad.GetState(PlayerIndex.One).Buttons.Back = ButtonState.Pressed OrElse Keyboard.GetState().IsKeyDown(Keys.Escape) Then
                [Exit]()
            End If

            MyBase.Update(gameTime)
        End Sub

        Protected Overrides Sub Draw(ByVal gameTime As GameTime)
            GraphicsDevice.Clear(Color.Red)

            MyBase.Draw(gameTime)
        End Sub

    End Class
End Namespace