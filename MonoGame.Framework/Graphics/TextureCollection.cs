using OpenTK.Graphics.OpenGL;

namespace Microsoft.Xna.Framework.Graphics
{
    public sealed class TextureCollection
    {
        private readonly Texture[] textures;
        private readonly TextureTarget[] targets;

        internal TextureCollection(int maxTextures)
        {
            textures = new Texture[maxTextures];
            targets = new TextureTarget[maxTextures];
        }

        public Texture this[int index]
        {
            get
            {
                return textures[index];
            }
            set
            {
                textures[index] = value;
            }
        }

        internal void Clear()
        {
            for (int i = 0; i < textures.Length; i += 1)
            {
                textures[i] = null;
                targets[i] = 0;
            }
        }
    }
}