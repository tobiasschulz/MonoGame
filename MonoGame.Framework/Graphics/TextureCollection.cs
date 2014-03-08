namespace Microsoft.Xna.Framework.Graphics
{
    public sealed class TextureCollection
    {
        #region Private Variables

        private readonly Texture[] textures;

        #endregion

        #region Internal Constructor

        internal TextureCollection(int maxTextures)
        {
            textures = new Texture[maxTextures];
        }

        #endregion

        #region Public Array Access Method

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

        #endregion

        #region Internal Array Clear Method

        internal void Clear()
        {
            for (int i = 0; i < textures.Length; i += 1)
            {
                textures[i] = null;
            }
        }

        #endregion
    }
}