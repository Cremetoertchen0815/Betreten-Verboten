Imports System

namespace Betreten_Verboten
    ' <summary>
    ' The main class.
    ' </summary>
    Public NotInheritable Class Program
        ' <summary>
        ' The main entry point for the application.
        ' </summary>
        <STAThread>
        Friend Shared Sub Main()
            Using game as new game1
                game.run
            End using
        End Sub
    End Class
End Namespace