﻿
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Input

Namespace Framework.UI.Controls
    Public Class Button
        Inherits GuiControl
        Public Property Text As String
        Public Overrides ReadOnly Property InnerBounds As Rectangle
            Get
                Return rect
            End Get
        End Property

        Public Event Clicked(ByVal sender As Object, ByVal e As EventArgs)

        Dim rect As Rectangle
        Dim par As IParent
        Sub New(text As String, location As Vector2, size As Vector2)
            Me.Text = text
            Me.Location = location
            Me.Size = size
            Me.Color = Color.White
            Me.Border = New ControlBorder(Color.White, 2)
            Me.BackgroundColor = New Color(40, 40, 40, 255)
        End Sub

        Public Overrides Sub Init(system As IParent)
            If Font Is Nothing Then Font = system.Font
            par = system
        End Sub

        Public Overrides Sub Draw(gameTime As GameTime)
            Graphics.FillRectangle(rect, BackgroundColor)
            Graphics.DrawRectangle(rect, Border.Color, Border.Width)
            SpriteBatch.DrawString(Font, Text, rect.Center.ToVector2 - Font.MeasureString(Text) / 2, Color)
        End Sub

        Public Overrides Sub Update(gameTime As GameTime, mstate As GuiInput, offset As Vector2)
            rect = New Rectangle(Location.X + offset.X, Location.Y + offset.Y, Size.X, Size.Y)
            If mstate.LeftClickOneshot And rect.Contains(mstate.MousePosition) Then
                RaiseEvent Clicked(Me, New EventArgs())
            End If
        End Sub
    End Class
End Namespace