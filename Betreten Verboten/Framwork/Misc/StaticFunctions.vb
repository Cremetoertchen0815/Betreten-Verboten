Imports System.Collections.Generic
Imports System.Text
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Namespace Framework.Misc

    <TestState(TestState.WorkInProgress)>
    Public Module StaticFunctions
        Public Const DegToRad = Math.PI / 180
        Public Function RotateVector(vec As Vector2, radians As Double) As Vector2
            Dim ca As Double = Math.Cos(radians)
            Dim sa As Double = Math.Sin(radians)
            Return New Vector2(ca * vec.X - sa * vec.Y, sa * vec.X + ca * vec.Y)
        End Function
        Public Function shortAngleDist(a As Single, b As Single) As Single
            Dim max = Math.PI * 2
            Dim da = (b - a) Mod max
            Return 2 * da Mod max - da
        End Function

        Public Function LerpAngle(a As Single, b As Single, amount As Single) As Single
            Return a + shortAngleDist(a, b) * amount
        End Function

        Public Function disctance2D(v1 As Vector2, v2 As Vector2) As Single
            Return Math.Sqrt(((v2.X - v1.X) * 0.5) ^ 2 + ((v2.Y - v1.Y) * 2) ^ 2)
        End Function

        Public Function WrapText(spriteFont As SpriteFont, text As String, maxLineWidth As Single, ByRef height As Integer) As String
            height = 0
            Dim words As String() = text.Split(" ")
            Dim sb As New StringBuilder
            Dim linewidth As Single = 0F
            Dim spaceWidth = spriteFont.MeasureString(" ").X

            For Each word In words
                Dim size As Vector2 = spriteFont.MeasureString(word)

                If linewidth + size.X < maxLineWidth Then
                    sb.Append(word & " ")
                    linewidth += size.X + spaceWidth
                Else
                    sb.Append(Environment.NewLine & word & " ")
                    linewidth = size.X + spaceWidth
                End If
            Next

            Return sb.ToString()
        End Function

        Public Function WrapTextDifferently(ByVal text As String, ByVal width As Integer, ByVal overflow As Boolean) As String
            Dim result As StringBuilder = New StringBuilder()
            Dim index As Integer = 0
            Dim column As Integer = 0

            While index < text.Length
                Dim spaceIndex As Integer = text.IndexOfAny({" "c, Microsoft.VisualBasic.vbTab, Microsoft.VisualBasic.vbCr, Microsoft.VisualBasic.vbLf}, index)

                If spaceIndex = -1 Then
                    Exit While
                ElseIf spaceIndex = index Then
                    index += 1
                Else
                    AddWord(text.Substring(index, spaceIndex - index), width, overflow, column, result)
                    index = spaceIndex + 1
                End If
            End While

            If index < text.Length Then AddWord(text.Substring(index), width, overflow, column, result)
            Return result.ToString()
        End Function

        Private Sub AddWord(ByVal word As String, ByVal width As Integer, ByVal overflow As Boolean, ByRef column As Integer, ByRef result As StringBuilder)
            If Not overflow AndAlso word.Length > width Then
                Dim wordIndex As Integer = 0

                While wordIndex < word.Length
                    Dim subWord As String = word.Substring(wordIndex, Math.Min(width, word.Length - wordIndex))
                    AddWord(subWord, width, overflow, column, result)
                    wordIndex += subWord.Length
                End While
            Else

                If column + word.Length >= width Then

                    If column > 0 Then
                        result.AppendLine()
                        column = 0
                    End If
                ElseIf column > 0 Then
                    result.Append(" ")
                    column += 1
                End If

                result.Append(word)
                column += word.Length
            End If
        End Sub


        'Returns an outline rectangle around a section of tiles
        Public Function GetSectionBoundaries(sec As Vector2(), Optional proper As Boolean = False) As Rectangle
            Dim xa As Single = 0
            Dim xb As Single = 0
            Dim ya As Single = 0
            Dim yb As Single = 0
            For Each element In sec
                If element.X > xb Then xb = element.X
                If element.Y > yb Then yb = element.Y
            Next
            xa = xb
            ya = yb
            For Each element In sec
                If element.X < xa Then xa = element.X
                If element.Y < ya Then ya = element.Y
            Next
            If proper Then
                Return New Rectangle(xa, ya, xb - xa, yb - ya)
            Else
                Return New Rectangle(xa, ya - 1, xb - xa + 1, ya - yb - 1)
            End If
        End Function

        Public Function UnionRectangle(a As Rectangle, b As Rectangle) As Rectangle
            Dim left As Single = Math.Min(a.Left, b.Left)
            Dim right As Single = Math.Max(a.Right, b.Right)
            Dim top As Single = Math.Min(a.Top, b.Top)
            Dim bottom As Single = Math.Max(a.Bottom, b.Bottom)
            Return New Rectangle(left, top, right - left, bottom - top)
        End Function

        Public Function NormalizeVector(input As Vector2) As Vector2
            Dim len As Single = Math.Sqrt(input.X ^ 2 + input.Y ^ 2)
            If len > 0 Then Return input / len
            Return Vector2.Zero
        End Function

        Public Function GetSectionBoundaries(sec As Dictionary(Of Vector2, Integer)) As Rectangle
            Dim xa As Single = 0
            Dim xb As Single = 0
            Dim ya As Single = 0
            Dim yb As Single = 0
            For Each element In sec
                If element.Key.X > xb Then xb = element.Key.X
                If element.Key.Y > yb Then yb = element.Key.Y
            Next
            xa = xb
            ya = yb
            For Each element In sec
                If element.Key.X < xa Then xa = element.Key.X
                If element.Key.Y < ya Then ya = element.Key.Y
            Next
            Return New Rectangle(xa, ya - 1, xb - xa + 1, ya - yb - 1)
        End Function

        Public Function IntersectRectangle(vec As Vector2(), ByVal rect As Rectangle) As Boolean
            Dim minX As Double = Math.Min(vec(0).X, vec(1).X)
            Dim maxX As Double = Math.Max(vec(0).X, vec(1).X)

            If maxX > rect.Right Then
                maxX = rect.Right
            End If

            If minX < rect.Left Then
                minX = rect.Left
            End If

            If minX > maxX Then
                Return False
            End If

            Dim minY As Double = Math.Min(vec(0).Y, vec(1).Y)
            Dim maxY As Double = Math.Max(vec(0).Y, vec(1).Y)

            If maxY > rect.Bottom Then
                maxY = rect.Bottom
            End If

            If minY < rect.Top Then
                minY = rect.Top
            End If

            If minY > maxY Then
                Return False
            End If

            Return True
        End Function

        Public Function InversionToString(i As Integer) As String
            If i = -1 Then
                Return "-"
            Else
                Return ""
            End If
        End Function


        Public Function interpolate(ByVal d1 As Double, ByVal d2 As Double, ByVal dPercentage As Double) As Double
            Dim dDifference As Double = d2 - d1
            Dim dDistance As Double = dDifference * dPercentage
            Dim dResult As Double = d1 + dDistance
            Return dResult
        End Function

        Public Function interpolate(ByVal i1 As Integer, ByVal i2 As Integer, ByVal dPercentage As Double) As Integer
            Return CInt(interpolate(CDbl(i1), CDbl(i2), dPercentage))
        End Function

        Public Function interpolate(ByVal f1 As Single, ByVal f2 As Single, ByVal dPercentage As Double) As Single
            Return CSng(interpolate(CDbl(f1), CDbl(f2), dPercentage))
        End Function

        Public Function convertLinearToEaseInEaseOut(ByVal dElapsed As Double) As Double
            Dim dFirstHalfTime As Double = If((dElapsed > 0.5), 0.5, dElapsed)
            Dim dSecondHalfTime As Double = If((dElapsed > 0.5), dElapsed - 0.5, 0.0)
            Dim dResult As Double = 2 * dFirstHalfTime * dFirstHalfTime + 2 * dSecondHalfTime * (1.0 - dSecondHalfTime)
            Return dResult
        End Function

        Public Function convertLinearToAcceleration(ByVal dElapsed As Double) As Double
            Return dElapsed * dElapsed
        End Function

        Public Function convertLinearToDeceleration(ByVal dElapsed As Double) As Double
            Return dElapsed * (2.0 - dElapsed)
        End Function

        '---Vector functions---

        'Returns the CrossProduct of P1 and P2 with the current Point being the Vertex
        Public Function CrossProduct(vertex As Vector2, P1 As Vector2, P2 As Vector2) As Double
            'Ax * By - Bx * Ay
            Return (P1.X - vertex.X) * (P2.Y - vertex.Y) - (P2.X - vertex.X) * (P1.Y - vertex.Y)
        End Function
        Public Function CrossProduct(a As Vector3, b As Vector3) As Vector3
            Return New Vector3(a.Y * b.Z - b.Y * a.Z, a.Z * b.X - b.Z * a.X, a.X * b.Y - b.X * a.Y)
        End Function

        'Returns the DotProduct of P1 and P2 with the current Point being the Vertex
        Public Function DotProduct(vertex As Vector2, P1 As Vector2, P2 As Vector2) As Double
            'Ax * Bx + Ay * Cy
            Return (P1.X - vertex.X) * (P2.X - vertex.X) + (P1.Y - vertex.Y) * (P2.Y - vertex.Y)
        End Function
        Public Function DotProduct(vertex As Vector3, P1 As Vector3, P2 As Vector3) As Double
            Return (P1.X - vertex.X) * (P2.X - vertex.X) + (P1.Y - vertex.Y) * (P2.Y - vertex.Y) + (P1.Z - vertex.Z) * (P2.Z - vertex.Z)
        End Function

        Public Function VectorAngle(v1 As Vector3, v2 As Vector3) As Single
            Return Math.Acos(DotProduct(Vector3.Zero, v1, v2) / (v1.Length * v2.Length))
        End Function

        'Rotates point around Axis
        Public Function RotatePoint(Point As Vector2, Degrees As Double, Axis As Vector2) As Vector2
            'Rotate around Axis
            Return New Vector2(
                (Point.X - Axis.X) * Math.Cos(Degrees / 180.0 * Math.PI) - (Point.Y - Axis.Y) * Math.Sin(Degrees / 180.0 * Math.PI) + Axis.X,
                (Point.X - Axis.X) * Math.Sin(Degrees / 180.0 * Math.PI) + Math.Cos(Degrees / 180.0 * Math.PI) * (Point.Y - Axis.Y) + Axis.Y)
        End Function
    End Module

End Namespace
