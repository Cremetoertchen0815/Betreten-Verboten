Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Namespace Framework.UI
    Public Interface IParent
        Sub Init(parent As IParent)
        Sub Update(gameTime As GameTime, cstate As GuiInput, offset As Vector2)
        Sub Draw(gameTime As GameTime)
        Property Font As SpriteFont
        ReadOnly Property Bounds As Rectangle
    End Interface
End Namespace