Imports System.Collections.Generic
Imports Betreten_Verboten.Framework.Graphics
Imports Betreten_Verboten.Framework.Tweening
Imports Microsoft.Xna.Framework

Public Interface IGameWindow
    ReadOnly Property Spielers As Player()
    ReadOnly Property FigurFaderScales As Dictionary(Of (Integer, Integer), Transition(Of Single))
    ReadOnly Property Status As SpielStatus
    ReadOnly Property Map As GaemMap
    ReadOnly Property SelectFader As Transition(Of Single) 'Fader, welcher die zur Auswahl stehenden Figuren blinken lässt
    ReadOnly Property FigurFaderZiel As (Integer, Integer) 'Gibt an welche Figur bewegt werden soll (Spieler ind., Figur ind.)
    ReadOnly Property SpielerIndex As Integer 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
    ReadOnly Property UserIndex As Integer 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
    ReadOnly Property FigurFaderXY As Transition(Of Vector2)
    ReadOnly Property FigurFaderZ As Transition(Of Integer)
    Function GetCamPos() As CamKeyframe
End Interface
