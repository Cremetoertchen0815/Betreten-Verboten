Imports System.Collections.Generic
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

Namespace Framework.UI
    Public Class GuiSystem
        Implements IParent

        Public Property Controls As New List(Of GuiControl)
        Public Property GlobalFont As SpriteFont Implements IParent.Font
        Public ReadOnly Property Bounds As Rectangle = New Rectangle(0, 0, GameSize.X, GameSize.Y) Implements IParent.Bounds


        Public Shared FastScrollThreshold As UInteger = 400

        'Fast Scrolling
        Dim lens As Integer
        Dim cnt As Integer
        Dim fullblast As Boolean

        Dim lastmstate As MouseState

        Public Sub Init()
            Me.Init(Nothing)
        End Sub

        Private Sub Init(parent As IParent) Implements IParent.Init
            GlobalFont = DefaultFont
            For Each element In Controls
                element.Init(Me)
            Next
        End Sub

        Public Sub Unload()
            For Each element In Controls
                element.Unload()
                element = Nothing
            Next
        End Sub

        Public Sub Draw(gameTime As GameTime) Implements IParent.Draw
            SpriteBatch.Begin(SpriteSortMode.Deferred, Nothing, Nothing, Nothing, Nothing, Nothing, ScaleMatrix)
            For Each element In Controls
                If element.Active Then element.Draw(gameTime)
            Next
            SpriteBatch.End()
        End Sub
        Public Sub Update(gameTime As GameTime, mstate As MouseState, transmatrix As Matrix)
            'Fullblast
            If mstate.LeftButton Then
                lens += gameTime.ElapsedGameTime.TotalMilliseconds
            Else
                lens = 0
                fullblast = False
            End If

            If lens > FastScrollThreshold Then
                cnt += gameTime.ElapsedGameTime.TotalMilliseconds
                If cnt > 30 Then
                    fullblast = True
                    cnt = 0
                Else
                    fullblast = False
                End If
            End If

            Dim cstate As New GuiInput With {
            .MousePosition = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScaleMatrix)),
            .MousePositionTransformed = Vector2.Transform(mstate.Position.ToVector2, ScaleMatrix * Matrix.Invert(transmatrix)),
            .LeftClick = mstate.LeftButton = ButtonState.Pressed,
            .LeftClickOneshot = mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released,
            .ScrollDifference = (mstate.ScrollWheelValue - lastmstate.ScrollWheelValue) / 120,
            .LeftClickFullBlast = fullblast
            }

            For Each element In Controls
                If element.Active Then element.Update(gameTime, cstate, Vector2.Zero)
            Next

            lastmstate = mstate
        End Sub

        Public Sub Update(gameTime As GameTime, cstate As GuiInput, offset As Vector2) Implements IParent.Update

        End Sub
    End Class
End Namespace