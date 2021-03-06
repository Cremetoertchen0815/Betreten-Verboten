﻿Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Input

Namespace Framework.UI.Controls
    Public Class SimpleContainer
        Inherits GuiControl
        Public Overrides ReadOnly Property InnerBounds As Rectangle
            Get
                Return rect
            End Get
        End Property

        Dim rect As Rectangle
        Dim par As IParent

        Sub New(location As Vector2, size As Vector2)
            Me.Location = location
            Me.Color = Color.White
            Me.Size = size
        End Sub

        Public Overrides Sub Init(system As IParent)
            If Font Is Nothing Then Font = system.Font
            par = system

            For Each element In Me.Children
                element.Init(Me)
            Next
        End Sub

        Public Overrides Sub Unload()
            MyBase.Unload()
            For Each element In Children
                element.Unload()
                element = Nothing
            Next
        End Sub

        Public Overrides Sub Draw(gameTime As GameTime)
            Graphics.FillRectangle(rect, BackgroundColor)
            Graphics.DrawRectangle(rect, Border.Color, Border.Width)

            For Each element In Children
                If element.Active Then element.Draw(gameTime)
            Next
        End Sub

        Public Overrides Sub Update(gameTime As GameTime, mstate As GuiInput, offset As Vector2)
            rect = New Rectangle(Location.X + offset.X, Location.Y + offset.Y, Size.X, Size.Y)

            For Each element In Children
                If element.Active Then element.Update(gameTime, mstate, rect.Location.ToVector2)
            Next
        End Sub
    End Class
End Namespace