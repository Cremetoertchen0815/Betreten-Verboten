﻿Imports System.Collections.Generic
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

Namespace Framework.UI
    Public MustInherit Class GuiControl
        Implements IParent
        Public Property Parameter As Object
        Public Property Active As Boolean = True
        Public Property Location As Vector2
        Public Overridable Property Size As Vector2
        Public Property BackgroundColor As Color
        Public Property Border As New ControlBorder With {.Color = Color.White, .Width = 0}
        Public Property Color As Color
        Public Property Font As SpriteFont Implements IParent.Font
        Public Property Children As New List(Of GuiControl)
        Public MustOverride ReadOnly Property InnerBounds As Rectangle Implements IParent.Bounds
        Public Overridable ReadOnly Property OuterBounds As Rectangle
            Get
                Return InnerBounds
            End Get
        End Property

        Public MustOverride Sub Init(system As IParent) Implements IParent.Init
        Public Overridable Sub Unload()
        End Sub
        Public MustOverride Sub Draw(gameTime As GameTime) Implements IParent.Draw
        Public MustOverride Sub Update(gameTime As GameTime, cstate As GuiInput, offset As Vector2) Implements IParent.Update
    End Class
End Namespace