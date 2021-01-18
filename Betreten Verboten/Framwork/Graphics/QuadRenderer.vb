Imports Betreten_Verboten.Framework.Misc
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Framework.Graphics

    <TestState(TestState.Finalized)>
    Public Class QuadRenderer
        Private ReadOnly _vertexBuffer As VertexPositionTexture()
        Private ReadOnly _indexBuffer As Short()

        Public Sub New(ByVal graphicsDevice As GraphicsDevice)
            _vertexBuffer = New VertexPositionTexture(3) {}
            _vertexBuffer(0) = New VertexPositionTexture(New Vector3(-1, 1, 1), New Vector2(0, 0))
            _vertexBuffer(1) = New VertexPositionTexture(New Vector3(1, 1, 1), New Vector2(1, 0))
            _vertexBuffer(2) = New VertexPositionTexture(New Vector3(-1, -1, 1), New Vector2(0, 1))
            _vertexBuffer(3) = New VertexPositionTexture(New Vector3(1, -1, 1), New Vector2(1, 1))
            _indexBuffer = New Short() {0, 3, 2, 0, 1, 3}
        End Sub

        Public Sub RenderQuad(ByVal graphicsDevice As GraphicsDevice, ByVal v1 As Vector2, ByVal v2 As Vector2)
            _vertexBuffer(0).Position.X = v1.X
            _vertexBuffer(0).Position.Y = v2.Y
            _vertexBuffer(1).Position.X = v2.X
            _vertexBuffer(1).Position.Y = v2.Y
            _vertexBuffer(2).Position.X = v1.X
            _vertexBuffer(2).Position.Y = v1.Y
            _vertexBuffer(3).Position.X = v2.X
            _vertexBuffer(3).Position.Y = v1.Y
            graphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertexBuffer, 0, 4, _indexBuffer, 0, 2)
        End Sub


        Public Shared Sub CreateQuad(texture As Texture2D, destination As Rectangle, source As Rectangle, ByRef vertexlist As System.Collections.Generic.List(Of VertexPositionColorTexture))
            Dim tl As Vector2 = source.Location.ToVector2 / texture.Bounds.Size.ToVector2
            Dim tr As Vector2 = New Vector2(source.Right, source.Top) / texture.Bounds.Size.ToVector2
            Dim br As Vector2 = New Vector2(source.Right, source.Bottom) / texture.Bounds.Size.ToVector2
            Dim bl As Vector2 = New Vector2(source.Left, source.Bottom) / texture.Bounds.Size.ToVector2
            't1
            vertexlist.Add(New VertexPositionColorTexture(New Vector3(destination.Left, destination.Top, 0), Color.White, tl))
            vertexlist.Add(New VertexPositionColorTexture(New Vector3(destination.Left, destination.Bottom, 0), Color.White, bl))
            vertexlist.Add(New VertexPositionColorTexture(New Vector3(destination.Right, destination.Bottom, 0), Color.White, br))
            't2
            vertexlist.Add(New VertexPositionColorTexture(New Vector3(destination.Right, destination.Bottom, 0), Color.White, br))
            vertexlist.Add(New VertexPositionColorTexture(New Vector3(destination.Right, destination.Top, 0), Color.White, tr))
            vertexlist.Add(New VertexPositionColorTexture(New Vector3(destination.Left, destination.Top, 0), Color.White, tl))
        End Sub

        Public Shared Sub SetQuad(texture As Texture2D, destination As Rectangle, source As Rectangle, ByRef vertexlist As VertexPositionColorTexture(), index As Integer)
            Dim tl As Vector2 = source.Location.ToVector2 / texture.Bounds.Size.ToVector2
            Dim tr As Vector2 = New Vector2(source.Right, source.Top) / texture.Bounds.Size.ToVector2
            Dim br As Vector2 = New Vector2(source.Right, source.Bottom) / texture.Bounds.Size.ToVector2
            Dim bl As Vector2 = New Vector2(source.Left, source.Bottom) / texture.Bounds.Size.ToVector2
            't1
            vertexlist((index * 6) + 0) = (New VertexPositionColorTexture(New Vector3(destination.Left, destination.Top, 0), Color.White, tl))
            vertexlist((index * 6) + 1) = (New VertexPositionColorTexture(New Vector3(destination.Left, destination.Bottom, 0), Color.White, bl))
            vertexlist((index * 6) + 2) = (New VertexPositionColorTexture(New Vector3(destination.Right, destination.Bottom, 0), Color.White, br))
            't2
            vertexlist((index * 6) + 3) = (New VertexPositionColorTexture(New Vector3(destination.Right, destination.Bottom, 0), Color.White, br))
            vertexlist((index * 6) + 4) = (New VertexPositionColorTexture(New Vector3(destination.Right, destination.Top, 0), Color.White, tr))
            vertexlist((index * 6) + 5) = (New VertexPositionColorTexture(New Vector3(destination.Left, destination.Top, 0), Color.White, tl))
        End Sub
        Public Shared Sub SetQuad(texture As Texture2D, destination As Rectangle, source As Rectangle, ByRef vertexlist As VertexPositionColorTexture(), ByRef indexlist As Integer(), quadnr As Integer)
            Dim tl As Vector2 = source.Location.ToVector2 / texture.Bounds.Size.ToVector2
            Dim tr As Vector2 = New Vector2(source.Right, source.Top) / texture.Bounds.Size.ToVector2
            Dim br As Vector2 = New Vector2(source.Right, source.Bottom) / texture.Bounds.Size.ToVector2
            Dim bl As Vector2 = New Vector2(source.Left, source.Bottom) / texture.Bounds.Size.ToVector2

            'Set vertices
            vertexlist(quadnr * 4 + 0) = (New VertexPositionColorTexture(New Vector3(destination.Left, destination.Top, 0), Color.White, tl))
            vertexlist(quadnr * 4 + 1) = (New VertexPositionColorTexture(New Vector3(destination.Left, destination.Bottom, 0), Color.White, bl))
            vertexlist(quadnr * 4 + 2) = (New VertexPositionColorTexture(New Vector3(destination.Right, destination.Bottom, 0), Color.White, br))
            vertexlist(quadnr * 4 + 3) = (New VertexPositionColorTexture(New Vector3(destination.Right, destination.Top, 0), Color.White, tr))

            'Set indices
            indexlist(quadnr * 6 + 0) = quadnr * 4 + 0
            indexlist(quadnr * 6 + 1) = quadnr * 4 + 1
            indexlist(quadnr * 6 + 2) = quadnr * 4 + 2
            indexlist(quadnr * 6 + 3) = quadnr * 4 + 2
            indexlist(quadnr * 6 + 4) = quadnr * 4 + 3
            indexlist(quadnr * 6 + 5) = quadnr * 4 + 0
        End Sub

        Public Shared Sub SetAlphaQuad(destination As Rectangle, ByRef vertexlist As VertexPositionColorTexture(), index As Integer)
            vertexlist((index * 6) + 0) = (Nothing)
            vertexlist((index * 6) + 1) = (Nothing)
            vertexlist((index * 6) + 2) = (Nothing)
            vertexlist((index * 6) + 3) = (Nothing)
            vertexlist((index * 6) + 4) = (Nothing)
            vertexlist((index * 6) + 5) = (Nothing)
        End Sub
        Public Shared Sub CreateAlphaQuad(destination As Rectangle, ByRef vertexlist As System.Collections.Generic.List(Of VertexPositionColorTexture))
            vertexlist.Add(Nothing)
            vertexlist.Add(Nothing)
            vertexlist.Add(Nothing)
            vertexlist.Add(Nothing)
            vertexlist.Add(Nothing)
            vertexlist.Add(Nothing)
        End Sub
    End Class
End Namespace
