Imports System
Imports Betreten_Verboten.Framework.Tweening
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Content
Imports Microsoft.Xna.Framework.Graphics

''' <summary>
''' Die Startklasse, welche die Engine und das Spiel lädt und startet
''' </summary>
Public Module Program

    'Globale Variablen, die für das Ausführen des Spiels benötigt werden.
    Friend Property Graphics As GraphicsDeviceManager
    Friend Property SpriteBatch As SpriteBatch
    Friend Property Content As ContentManager
    Friend Property GameSize As Vector2 = New Vector2(1920, 1080)
    Friend Property ReferencePixel As Texture2D
    Friend Property Automator As TweenManager
    Friend Property Dev As GraphicsDevice
    Friend Property ScaleMatrix As Matrix
    Friend Property DefaultFont As SpriteFont
    Friend Property GameClassInstance As GameInstance

    ''' <summary>
    ''' Hier steigt die Anwendung ein.
    ''' </summary>
    <STAThread>
    Friend Sub Main()
        'Using-Block gibt nach Beendigung des Spiels Resourcen frei und ruft game.Dispose() auf.
        GameClassInstance = New GameInstance
        GameClassInstance.Run() 'Führe Spiel aus.
        GameClassInstance.Dispose()
    End Sub
End Module