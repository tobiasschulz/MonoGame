#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	public sealed class TextureCollection
	{
        #region Public Properties

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
        
        #region Private Variables

		private readonly Texture[] textures;

		#endregion

		#region Internal Constructor

		internal TextureCollection(int maxTextures)
		{
			textures = new Texture[maxTextures];
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
