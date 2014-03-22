#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.InteropServices;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	internal struct VertexColorTexture
    {

        #region Public Fields

        public Vector2 Vertex;
		public uint Color;
		public Vector2 TexCoord;

        #endregion

        #region Public Constructor

        public VertexColorTexture ( Vector2 vertex, Color color, Vector2 texCoord )
		{
			Vertex = vertex;
			Color = color.PackedValue;
			TexCoord = texCoord;
        }

        #endregion

    }
}

