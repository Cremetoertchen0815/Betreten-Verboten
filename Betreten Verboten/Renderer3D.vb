Imports System.Collections.Generic
Imports System.IO
Imports Betreten_Verboten.Framework.Graphics
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Public Class Renderer3D

    Private figur_model As STLDefinition.STLObject
    Private EffectA As BasicEffect
    Private EffectB As BasicEffect
    Private SpielfeldTextur As RenderTarget2D
    Private FinalTextur As RenderTarget2D
    Private SpielfeldVerbindungen As Texture2D
    Private Pfeil As Texture2D
    Private MapBuffer As VertexBuffer

    Private View As Matrix
    Private Projection As Matrix
    Private Camera As CamKeyframe
    Private CamMatrix As Matrix

    Private Game As IGameWindow
    Private Feld As Rectangle
    Private Center As Vector2
    Private transmatrices As Matrix() = {Matrix.CreateRotationZ(MathHelper.PiOver2 * 3), Matrix.Identity, Matrix.CreateRotationZ(MathHelper.PiOver2), Matrix.CreateRotationZ(MathHelper.Pi)}
    Private playcolor As Color() = {Color.Magenta, Color.Lime, Color.Cyan, Color.Orange}
    Sub New(game As IGameWindow)
        Me.Game = game
    End Sub

    Friend Sub LoadContent()
        figur_model = STLDefinition.LoadSTL(New StreamReader("Content\mesh\Playing_Piece.stl").BaseStream)
        SpielfeldVerbindungen = Content.Load(Of Texture2D)("playfield_connections")
        Pfeil = Content.Load(Of Texture2D)("arrow_right")

        Dim vertices As New List(Of VertexPositionColorTexture)
        vertices.Add(New VertexPositionColorTexture(New Vector3(-475, 475, 0), Color.White, Vector2.UnitX))
        vertices.Add(New VertexPositionColorTexture(New Vector3(475, 475, 0), Color.White, Vector2.Zero))
        vertices.Add(New VertexPositionColorTexture(New Vector3(-475, -475, 0), Color.White, Vector2.One))
        vertices.Add(New VertexPositionColorTexture(New Vector3(475, 475, 0), Color.White, Vector2.Zero))
        vertices.Add(New VertexPositionColorTexture(New Vector3(475, -475, 0), Color.White, Vector2.UnitY))
        vertices.Add(New VertexPositionColorTexture(New Vector3(-475, -475, 0), Color.White, Vector2.One))
        MapBuffer = New VertexBuffer(Dev, GetType(VertexPositionColorTexture), vertices.Count, BufferUsage.WriteOnly)
        MapBuffer.SetData(Of VertexPositionColorTexture)(vertices.ToArray)

        Feld = New Rectangle(500, 70, 950, 950)
        Center = Feld.Center.ToVector2

        SpielfeldTextur = New RenderTarget2D(
            Graphics.GraphicsDevice,
            950,
            950,
            False,
            Graphics.GraphicsDevice.PresentationParameters.BackBufferFormat,
            DepthFormat.Depth24, 0, RenderTargetUsage.DiscardContents) With {.Name = "TmpA"}

        FinalTextur = New RenderTarget2D(
            Graphics.GraphicsDevice,
            GameSize.X,
            GameSize.Y,
            False,
            Graphics.GraphicsDevice.PresentationParameters.BackBufferFormat,
            DepthFormat.Depth24, 0, RenderTargetUsage.DiscardContents) With {.Name = "TmpA"}



        EffectA = New BasicEffect(Graphics.GraphicsDevice) With {.Alpha = 1.0F,
            .VertexColorEnabled = True,
            .LightingEnabled = False,
            .TextureEnabled = True
        }

        EffectB = New BasicEffect(Graphics.GraphicsDevice) With {.Alpha = 1.0F,
            .VertexColorEnabled = True,
            .TextureEnabled = False,
            .LightingEnabled = True, '// turn on the lighting subsystem.
            .AmbientLightColor = New Vector3(0.1F, 0.1F, 0.1F),
            .EmissiveColor = New Vector3(0.2F, 0.2F, 0.2F),
            .PreferPerPixelLighting = True
        }

        CreateBoxBuffer()

    End Sub

    'Debug Box Buffer
    Dim BoxVertexBuffer As VertexBuffer
    Dim BoxIndexBuffer As IndexBuffer
    Private Sub CreateBoxBuffer()

        '---VERTICES---
        Dim genboxvert = New VertexPositionColor(23) {}
        'Left|Top|Front
        genboxvert(0) = New VertexPositionColor(New Vector3(0, 0, 0), Color.Lime)
        genboxvert(1) = New VertexPositionColor(New Vector3(20, 20, 0), Color.Lime)
        genboxvert(2) = New VertexPositionColor(New Vector3(0, 20, 0), Color.Lime)
        'Right|Top|Front
        genboxvert(3) = New VertexPositionColor(New Vector3(1920, 0, 0), Color.Lime)
        genboxvert(4) = New VertexPositionColor(New Vector3(1920, 20, 0), Color.Lime)
        genboxvert(5) = New VertexPositionColor(New Vector3(1900, 20, 0), Color.Lime)
        'Left|Bottom|Front
        genboxvert(6) = New VertexPositionColor(New Vector3(1900, 1060, 0), Color.Lime)
        genboxvert(7) = New VertexPositionColor(New Vector3(1920, 1060, 0), Color.Lime)
        genboxvert(8) = New VertexPositionColor(New Vector3(1920, 1080, 0), Color.Lime)
        'Right|Bottom|Front
        genboxvert(9) = New VertexPositionColor(New Vector3(0, 1080, 0), Color.Lime)
        genboxvert(10) = New VertexPositionColor(New Vector3(0, 1060, 0), Color.Lime)
        genboxvert(11) = New VertexPositionColor(New Vector3(20, 1060, 0), Color.Lime)
        'Left|Top|Back
        genboxvert(12) = New VertexPositionColor(New Vector3(0, 0, 1000), Color.Magenta)
        genboxvert(13) = New VertexPositionColor(New Vector3(20, 20, 1000), Color.Magenta)
        genboxvert(14) = New VertexPositionColor(New Vector3(0, 20, 1000), Color.Magenta)
        'Right|Top|Back
        genboxvert(15) = New VertexPositionColor(New Vector3(1920, 0, 1000), Color.Magenta)
        genboxvert(16) = New VertexPositionColor(New Vector3(1920, 20, 1000), Color.Magenta)
        genboxvert(17) = New VertexPositionColor(New Vector3(1900, 20, 1000), Color.Magenta)
        'Left|Bottom|Back
        genboxvert(18) = New VertexPositionColor(New Vector3(1900, 1060, 1000), Color.Magenta)
        genboxvert(19) = New VertexPositionColor(New Vector3(1920, 1060, 1000), Color.Magenta)
        genboxvert(20) = New VertexPositionColor(New Vector3(1920, 1080, 1000), Color.Magenta)
        'Right|Bottom|Back
        genboxvert(21) = New VertexPositionColor(New Vector3(0, 1080, 1000), Color.Magenta)
        genboxvert(22) = New VertexPositionColor(New Vector3(0, 1060, 1000), Color.Magenta)
        genboxvert(23) = New VertexPositionColor(New Vector3(20, 1060, 1000), Color.Magenta)


        '---INDICES---
        Dim genboxind As Integer() = {0, 3, 4,
                                          0, 4, 2,
                                          5, 4, 7,
                                          5, 7, 6,
                                          10, 7, 8,
                                          10, 8, 9,
                                          2, 1, 11,
                                          2, 11, 10,
                                          12, 15, 16,
                                          12, 16, 14,
                                          17, 16, 19,
                                          17, 19, 18,
                                          22, 19, 20,
                                          22, 20, 21,
                                          14, 13, 23,
                                          14, 23, 22,
                                          12, 0, 2, 'Side Left
                                          12, 2, 14,
                                          22, 10, 9,
                                          22, 9, 21,
                                          3, 15, 16,
                                          3, 16, 4,
                                          7, 19, 20,
                                          7, 20, 8}

        BoxVertexBuffer = New VertexBuffer(Dev, GetType(VertexPositionColor), genboxvert.Length, BufferUsage.WriteOnly)
        BoxVertexBuffer.SetData(genboxvert)

        BoxIndexBuffer = New IndexBuffer(Dev, GetType(Integer), genboxind.Length, BufferUsage.WriteOnly)
        BoxIndexBuffer.SetData(genboxind)
    End Sub

    Friend Sub PreDraw()
        Dev.SetRenderTarget(SpielfeldTextur)
        Dev.Clear(Color.Transparent)

        SpriteBatch.Begin()
        'Draw fields
        For j = 0 To 3
            'Zeichne Spielfeld
            For i = 0 To 17
                Dim loc As Vector2 = New Vector2(475) + Vector2.Transform(GameRoom.GetSpielfeldPositionen(i), transmatrices(j))
                Select Case i
                    Case PlayFieldPos.Haus1, PlayFieldPos.Haus2, PlayFieldPos.Haus3, PlayFieldPos.Haus4, PlayFieldPos.Home1, PlayFieldPos.Home2, PlayFieldPos.Home3, PlayFieldPos.Home4
                        DrawCircle(loc, 20, 25, playcolor(j), 2)
                    Case PlayFieldPos.Feld1
                        DrawCircle(loc, 28, 30, playcolor(j), 3)
                        'DrawArrow(loc, playcolor(j), j)
                    Case Else
                        DrawCircle(loc, 28, 30, Color.White, 3)
                End Select
            Next
        Next

        SpriteBatch.Draw(SpielfeldVerbindungen, New Rectangle(0, 0, 950, 950), Color.White)

        SpriteBatch.End()

        Dev.SetRenderTarget(FinalTextur)
        Dev.Clear(Color.Transparent)

        Dev.RasterizerState = RasterizerState.CullNone
        Dev.DepthStencilState = DepthStencilState.Default

        EffectA.World = Matrix.Identity
        EffectA.View = View
        EffectA.Projection = Projection
        EffectA.TextureEnabled = True
        EffectA.Texture = SpielfeldTextur

        For Each pass As EffectPass In EffectA.CurrentTechnique.Passes
            Dev.SetVertexBuffer(MapBuffer)
            pass.Apply()

            Graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, MapBuffer.VertexCount)
        Next

        'Zeichne Spielfiguren
        EffectB.View = View
        EffectB.Projection = Projection
        EffectB.LightingEnabled = True
        EffectB.DirectionalLight0.Direction = New Vector3(0, 0.8, 1.5)
        EffectB.DirectionalLight0.SpecularColor = New Vector3(1, 1, 1) '// with white highlights
        EffectB.AmbientLightColor = Color.White.ToVector3 * 0.0

        For j = 0 To 3
            Dim pl As Player = Game.Spielers(j)
            Dim color As Color = playcolor(j) * If(Game.Status = SpielStatus.WähleFigur And j = Game.SpielerIndex And (pl.Typ = SpielerTyp.Local Or pl.Typ = SpielerTyp.Online), Game.SelectFader.Value, 1.0F)
            For k As Integer = 0 To 3
                Dim scale As Single = If(Game.FigurFaderScales.ContainsKey((j, k)), Game.FigurFaderScales((j, k)).Value, 1)

                If Game.Status = SpielStatus.FahreFelder And Game.FigurFaderZiel.Item1 = j And Game.FigurFaderZiel.Item2 = k Then
                    DrawChr(Game.FigurFaderXY.Value, color, Game.FigurFaderZ.Value)
                ElseIf pl.Spielfiguren(k) = -1 Then 'Zeichne Figur in Homebase
                    DrawChr(Game.GetSpielfeldVector(j, k), playcolor(j), 0, scale)
                Else 'Zeichne Figur in Haus
                    DrawChr(Game.GetSpielfeldVector(j, k), color, 0, scale)
                End If
            Next
        Next

        'Camera = New CamKeyframe(0, -150, -150, Math.PI / 2, Math.PI / 2, 0)
    End Sub

    Friend Sub Draw(gameTime As GameTime)
        SpriteBatch.Begin(SpriteSortMode.Deferred, Nothing, Nothing, Nothing, Nothing, Nothing, ScaleMatrix)
        SpriteBatch.Draw(FinalTextur, New Rectangle(0, 0, GameSize.X, GameSize.Y), Color.White)
        SpriteBatch.End()
    End Sub

    Private Sub DrawChr(pos As Vector2, color As Color, Optional zpos As Integer = 0, Optional scale As Single = 1)
        EffectB.World = figur_model.STLHeader.CenterMatrix * Matrix.CreateTranslation(0, 0, 12) * Matrix.CreateScale(3.5 * scale * New Vector3(1, 1, -1)) * Matrix.CreateRotationY(Math.PI) * Matrix.CreateTranslation(-pos.X, -pos.Y, -zpos)
        EffectB.EmissiveColor = color.ToVector3 * 0.15
        EffectB.DirectionalLight0.DiffuseColor = color.ToVector3 * 0.5 '// a gray light
        For Each pass As EffectPass In EffectB.CurrentTechnique.Passes
            pass.Apply()

            Graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, figur_model.Vertices, 0, figur_model.STLHeader.nfacets, VertexPositionColorNormal.VertexDeclaration)
        Next
    End Sub

    Friend Sub Update(gameTime As GameTime)
        CamMatrix = Matrix.CreateFromYawPitchRoll(Camera.Yaw, Camera.Pitch, Camera.Roll) * Matrix.CreateTranslation(Camera.Location)
        View = CamMatrix * Matrix.CreateScale(1, 1, 1 / 1080) * Matrix.CreateLookAt(New Vector3(0, 0, -1), New Vector3(0, 0, 0), Vector3.Up)
        Projection = Matrix.CreateScale(100) * Matrix.CreateTranslation(New Vector3(-GameSize.X / 2, -GameSize.Y / 2, 0)) * ScaleMatrix * Matrix.CreateTranslation(New Vector3(GameSize.X / 2 + 1500, GameSize.Y / 2 - 500, 0)) * Matrix.CreatePerspective(Dev.Viewport.Width, Dev.Viewport.Height, 1, 100000)
    End Sub

    Private Sub DrawArrow(vc As Vector2, color As Color, iteration As Integer)
        SpriteBatch.Draw(Pfeil, New Rectangle(vc.X, vc.Y, 35, 35), Nothing, color, MathHelper.PiOver2 * (iteration + 3), New Vector2(35, 35) / 2, SpriteEffects.None, 0)
    End Sub
End Class
