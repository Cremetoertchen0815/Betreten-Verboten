Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

''' <summary>
''' Enthällt den eigentlichen Code für das Basis-Spiel
''' </summary>
Public Class GameRoom

    'Spiele-Flags und Variables
    Private Spielers As Player() 'Enthält sämtliche SPieler, die an dieser Runde teilnehmen
    Private SpielerIndex As Integer 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.

    'Assets
    Private WürfelAugen As Texture2D
    Private WürfelRahmen As Texture2D

    Protected Sub LoadContent()
        'Lade Assets
        WürfelAugen = Content.Load(Of Texture2D)("würfel_augen")
        WürfelRahmen = Content.Load(Of Texture2D)("würfel_rahmen")
    End Sub

    Protected Sub UnloadContent()

    End Sub

    Protected Sub Update(ByVal gameTime As GameTime)

    End Sub

    Protected Sub Draw(ByVal gameTime As GameTime)

        SpriteBatch.Begin()
        SpriteBatch.Draw(WürfelAugen, New Rectangle(100, 100, 250, 250), GetWürfelSourceRectangle(2), Color.White)
        SpriteBatch.Draw(WürfelRahmen, New Rectangle(100, 100, 250, 250), Color.White)
        SpriteBatch.End()
    End Sub

#Region "Helper Function"
    Private Function GetWürfelSourceRectangle(augenzahl As Integer) As Rectangle
        Select Case augenzahl
            Case 1
                Return New Rectangle(0, 0, 260, 260)
            Case 2
                Return New Rectangle(260, 0, 260, 260)
            Case 3
                Return New Rectangle(520, 0, 260, 260)
            Case 4
                Return New Rectangle(0, 260, 260, 260)
            Case 5
                Return New Rectangle(260, 260, 260, 260)
            Case 6
                Return New Rectangle(520, 260, 260, 260)
            Case Else
                Return New Rectangle(0, 0, 0, 0)
        End Select
    End Function
#End Region

End Class