Imports System
Imports System.Diagnostics
Imports System.IO
Imports Betreten_Verboten.Framework.Tweening
Imports Betreten_Verboten.Networking
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
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
    Friend Property LocalClient As Client
    Friend Property SFX As SoundEffect()

    ''' <summary>
    ''' Hier steigt die Anwendung ein.
    ''' </summary>
    <STAThread>
    Friend Sub Main()
        'Using-Block gibt nach Beendigung des Spiels Resourcen frei und ruft game.Dispose() auf.
        Try
            GameClassInstance = New GameInstance
            GameClassInstance.Run() 'Führe Spiel aus.
            GameClassInstance.Dispose()
        Catch ex As Exception
            WriteErrorToFile(ex)
        End Try
        StopServer()
        Process.GetCurrentProcess.Kill()
    End Sub



    Friend Sub WriteErrorToFile(ex As Exception)
        Dim strFile As String = "yourfile.txt"
        Dim fileExists As Boolean = File.Exists(strFile)
        Using sw As New StreamWriter(File.Open(strFile, FileMode.OpenOrCreate))
            sw.WriteLine(
    If(fileExists,
        "Error Message in  Occured at-- " & DateTime.Now,
        "Start Error Log for today"))
            sw.WriteLine("Type: " & ex.GetType.Name)
            sw.WriteLine("Message: " & ex.Message)
            sw.WriteLine("Stacktrace: " & ex.StackTrace)
            sw.WriteLine("---------------------------------------------------------------")
            sw.WriteLine()

        End Using

    End Sub
End Module