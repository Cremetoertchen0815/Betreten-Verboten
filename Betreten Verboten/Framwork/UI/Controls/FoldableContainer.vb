Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Input

Namespace Framework.UI.Controls
    Public Class FoldableContainer
        Inherits GuiControl
        Public Overrides ReadOnly Property InnerBounds As Rectangle
            Get
                Return rect
            End Get
        End Property
        Public Overrides ReadOnly Property OuterBounds As Rectangle
            Get
                Return New Rectangle(rect.X, rect.Y - 18, rect.Width, If(Checked, rect.Height, 0) + 18)
            End Get
        End Property

        Public Event CheckedChanged(ByVal sender As Object, ByVal e As EventArgs)

        Public Checked As Boolean = True
        Public Text As String
        Dim rect As Rectangle
        Dim header As Rectangle
        Dim par As IParent

        Sub New(text As String, location As Vector2, size As Vector2)
            Me.Text = text
            Me.Location = location
            Me.Color = Color.White
            Me.BackgroundColor = New Color(8, 8, 8, 225)
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

            If Checked Then
                Graphics.FillRectangle(InnerBounds, BackgroundColor)
                Graphics.DrawRectangle(InnerBounds, Border.Color, Border.Width)
                For Each element In Children
                    If element.Active Then element.Draw(gameTime)
                Next
            End If

            Graphics.FillRectangle(header, Color.DarkMagenta)
            Graphics.DrawRectangle(header, Color.White, 2)
            SpriteBatch.DrawString(DefaultFont, Text, header.Location.ToVector2 - New Vector2(-3, 3), Color)
        End Sub

        Public Overrides Sub Update(gameTime As GameTime, mstate As GuiInput, offset As Vector2)
            rect = New Rectangle(Location.X + offset.X, Location.Y + offset.Y + 18, Size.X, Size.Y - 18)
            header = New Rectangle(Location.X + offset.X, Location.Y + offset.Y, Size.X, 18)

            If mstate.LeftClickOneshot And header.Contains(mstate.MousePosition) Then
                Checked = Not Checked
                RaiseEvent CheckedChanged(Me, New EventArgs)
            End If

            If Checked Then
                For Each element In Children
                    If element.Active Then element.Update(gameTime, mstate, InnerBounds.Location.ToVector2)
                Next
            End If
        End Sub
    End Class
End Namespace