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
	// This should really be XNA's VertexPositionColorTexture
	// but I'm not sure we want to use Vector3s if we don't have to.
	internal struct VertexPosition2ColorTexture : IVertexType
	{
		#region Private Properties

		VertexDeclaration IVertexType.VertexDeclaration
		{
			get
			{
				return VertexDeclaration;
			}
		}

		#endregion

		#region Public Variables

		public Vector2 Position;
		public Color Color;
		public Vector2 TextureCoordinate;

		#endregion

		#region Public Static Variables

		public static readonly VertexDeclaration VertexDeclaration;

		#endregion

		#region Private Static Constructor

		static VertexPosition2ColorTexture()
		{
			VertexDeclaration = new VertexDeclaration(
				new VertexElement[]
				{
					new VertexElement(
						0,
						VertexElementFormat.Vector2,
						VertexElementUsage.Position,
						0
					),
					new VertexElement(
						8,
						VertexElementFormat.Color,
						VertexElementUsage.Color,
						0
					),
					new VertexElement(
						12,
						VertexElementFormat.Vector2,
						VertexElementUsage.TextureCoordinate,
						0
					)
				}
			);
		}

		#endregion

		#region Public Constructor

		public VertexPosition2ColorTexture(
			Vector2 position,
			Color color,
			Vector2 texCoord
		) {
			Position = position;
			Color = color;
			TextureCoordinate = texCoord;
		}

		#endregion

		#region Public Static Methods

		public static int GetSize()
		{
				return (sizeof(float) * 4) + sizeof(uint);
		}

		#endregion

		#region Public Static Operators and Override Methods

		public override int GetHashCode()
		{
			// TODO: Fix GetHashCode
			return 0;
		}

		public override string ToString()
		{
			return string.Format(
				"{{Position:{0} Color:{1} TextureCoordinate:{2}}}",
				new object[]
				{
					Position,
					Color,
					TextureCoordinate
				}
			);
		}

		public static bool operator ==(VertexPosition2ColorTexture left, VertexPosition2ColorTexture right)
		{
			return (	(left.Position == right.Position) &&
					(left.Color == right.Color) &&
					(left.TextureCoordinate == right.TextureCoordinate)	);
		}

		public static bool operator !=(VertexPosition2ColorTexture left, VertexPosition2ColorTexture right)
		{
			return !(left == right);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			if (obj.GetType() != base.GetType())
			{
				return false;
			}

			return (this == ((VertexPosition2ColorTexture) obj));
		}

		#endregion
	}
 
}
