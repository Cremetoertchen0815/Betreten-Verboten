Imports System.Collections.Generic
Imports Microsoft.Xna.Framework

Namespace Framework.UI.Controls
    Public Class TextscrollBox
        Inherits GuiControl

        Public Property Text As String
        Public OutputFormat As Func(Of String()) = Function() {Text}
        Public Overrides ReadOnly Property InnerBounds As Rectangle
            Get
                Return rect
            End Get
        End Property

        Public Event Clicked(ByVal sender As Object, ByVal e As EventArgs)

        Public Property LenLimit As Integer = 30

        Dim scrolloffset As Integer = 0
        Dim maxlines As Integer = 0
        Dim workingtext As String()
        Dim rect As Rectangle
        Dim par As IParent
        Sub New(output As Func(Of String()), location As Vector2, size As Vector2)
            Me.OutputFormat = output
            Me.Location = location
            Me.Color = Color.White
            Me.Size = size
            workingtext = {}
        End Sub

        Public Overrides Sub Init(system As IParent)
            If Font Is Nothing Then Font = system.Font
            par = system
        End Sub

        Public Overrides Sub Draw(gameTime As GameTime)
            Graphics.FillRectangle(rect, BackgroundColor)
            Graphics.DrawRectangle(rect, Border.Color, Border.Width)

            For i As Integer = scrolloffset To Math.Min(scrolloffset + maxlines, workingtext.Length - 1)
                SpriteBatch.DrawString(Font, workingtext(i), rect.Location.ToVector2 + New Vector2(10, Font.LineSpacing * (i - scrolloffset) + 5), Color)
            Next
        End Sub

        Dim tmplist As New List(Of String)
        Public Overrides Sub Update(gameTime As GameTime, mstate As GuiInput, offset As Vector2)

            'Prepare text
            Dim ln As String() = OutputFormat()
            tmplist.Clear()
            For i As Integer = 0 To ln.Length - 1
                For Each element In WrapTextDifferently(ln(i), LenLimit, False).Split(Environment.NewLine)
                    tmplist.Add(element.Replace(Microsoft.VisualBasic.vbLf, ""))
                Next
            Next
            workingtext = tmplist.ToArray

            rect = New Rectangle(Location.X + offset.X, Location.Y + offset.Y, Size.X, Size.Y)

            If rect.Contains(mstate.MousePosition) Then
                If mstate.LeftClickOneshot Then RaiseEvent Clicked(Me, New EventArgs())
                If mstate.ScrollDifference < 0 And scrolloffset + maxlines < workingtext.Length - 1 Then scrolloffset += 1
                If mstate.ScrollDifference > 0 Then scrolloffset -= 1
            End If

            maxlines = Math.Ceiling(Size.Y / Font.LineSpacing - 2)
            scrolloffset = Math.Max(scrolloffset, 0)

        End Sub
    End Class
End Namespace