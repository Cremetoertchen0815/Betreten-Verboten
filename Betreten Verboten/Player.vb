﻿Imports Microsoft.Xna.Framework

''' <summary>
''' Kapselt alle wichtigen Eigenschaften und Methoden eine Spielers
''' </summary>
Public Class Player
    ''' <summary>
    ''' Identifiziert den Spieler in der Anwendung
    ''' </summary>
    Public Property Name As String = "Player"

    ''' <summary>
    ''' Deklariert ob der Spieler lokal, durch eine KI, oder über eine Netzwerkverbindung gesteuert wird
    ''' </summary>
    Public Property Typ As SpielerTyp = SpielerTyp.CPU

    '
    ''' <summary>
    ''' Positionen der vier Spielfiguren.<br></br>
    ''' Positionen der Spielfiguren relativ zur Homebase angegeben(-1 = Homebase, 0 = Start-Feld, 1 = erstes Feld nach Start-Feld, ..., 39 = letztes Feld vor Start-Feld, 40 = erstes Feld im Haus, ..., 43 = letztes Feld in Haus)!
    ''' </summary>
    ''' <returns></returns>
    Public Property Spielfiguren As Integer() = {20, 23, 35, 42} '{-1, -1, -1, -1}

    Public Property Schwierigkeit As Difficulty = Difficulty.Easy

    Public Property Bereit As Boolean = True

    Sub New(typ As SpielerTyp, Optional schwierigkeit As Difficulty = Difficulty.Easy)
        Me.Typ = typ
        Me.Schwierigkeit = schwierigkeit
    End Sub

    Sub New()

    End Sub

End Class
