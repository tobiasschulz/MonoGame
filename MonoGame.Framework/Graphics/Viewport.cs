#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System.Runtime.Serialization;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	[DataContract]
	public struct Viewport
	{
		#region Public Properties

		[DataMember]
		public int Height
		{
			get
			{
				return height;
			}
			set
			{
				height = value;
			}
		}

		[DataMember]
		public float MaxDepth
		{
			get
			{
				return maxDepth;
			}
			set
			{
				maxDepth = value;
			}
		}

		[DataMember]
		public float MinDepth
		{
			get
			{
				return minDepth;
			}
			set
			{
				minDepth = value;
			}
		}

		[DataMember]
		public int Width
		{
			get
			{
				return width;
			}
			set
			{
				width = value;
			}
		}

		[DataMember]
		public int Y
		{
			get
			{
				return y;

			}
			set
			{
				y = value;
			}
		}

		[DataMember]
		public int X
		{
			get
			{
				return x;
			}
			set
			{
				x = value;
			}
		}

		public float AspectRatio
		{
			get
			{
				if ((height != 0) && (width != 0))
				{
					return (((float) width) / ((float) height));
				}
				return 0.0f;
			}
		}

		public Rectangle Bounds
		{
			get
			{
				return new Rectangle(
					x,
					y,
					width,
					height
				);
			}

			set
			{
				x = value.X;
				y = value.Y;
				width = value.Width;
				height = value.Height;
			}
		}

		public Rectangle TitleSafeArea
		{
			get
			{
				return Bounds;
			}
		}

		#endregion

		#region Private Variables

		private int x;
		private int y;
		private int width;
		private int height;
		private float minDepth;
		private float maxDepth;

		#endregion

		#region Public Constructors

		public Viewport(int x, int y, int width, int height)
		{
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
			minDepth = 0.0f;
			maxDepth = 1.0f;
		}
		
		public Viewport(Rectangle bounds)
		{
			x = bounds.X;
			y = bounds.Y;
			width = bounds.Width;
			height = bounds.Height;
			minDepth = 0.0f;
			maxDepth = 1.0f;
		}

		#endregion

		#region Public Methods

		public Vector3 Project(
			Vector3 source,
			Matrix projection,
			Matrix view,
			Matrix world
		) {
			Matrix matrix = Matrix.Multiply(
				Matrix.Multiply(world, view),
				projection
			);
			Vector3 vector = Vector3.Transform(source, matrix);

			float a = (((source.X * matrix.M14) + (source.Y * matrix.M24)) + (source.Z * matrix.M34)) + matrix.M44;
			if (!WithinEpsilon(a, 1f))
			{
				vector.X = vector.X / a;
				vector.Y = vector.Y / a;
				vector.Z = vector.Z / a;
			}

			vector.X = (((vector.X + 1f) * 0.5f) * Width) + X;
			vector.Y = (((-vector.Y + 1f) * 0.5f) * Height) + Y;
			vector.Z = (vector.Z * (MaxDepth - MinDepth)) + MinDepth;
			return vector;
		}

		public Vector3 Unproject(Vector3 source, Matrix projection, Matrix view, Matrix world)
		{
			Matrix matrix = Matrix.Invert(
				Matrix.Multiply(
					Matrix.Multiply(world, view),
					projection
				)
			);
			source.X = (((source.X - X) / ((float) Width)) * 2f) - 1f;
			source.Y = -((((source.Y - Y) / ((float) Height)) * 2f) - 1f);
			source.Z = (source.Z - MinDepth) / (MaxDepth - MinDepth);
			Vector3 vector = Vector3.Transform(source, matrix);

			float a = (((source.X * matrix.M14) + (source.Y * matrix.M24)) + (source.Z * matrix.M34)) + matrix.M44;
			if (!WithinEpsilon(a, 1f))
			{
				vector.X = vector.X / a;
				vector.Y = vector.Y / a;
				vector.Z = vector.Z / a;
			}

			return vector;
		}

		#endregion

		#region Public Override for ToString Method

		public override string ToString()
		{
			return (
				"{" +
				"X:" + x +
				" Y:" + y +
				" Width:" + width +
				" Height:" + height +
				" MinDepth:" + minDepth +
				" MaxDepth:" + maxDepth +
				"}"
			);
		}

		#endregion

		#region Private Static Methods

		private static bool WithinEpsilon(float a, float b)
		{
			float num = a - b;
			return ((-1.401298E-45f <= num) && (num <= float.Epsilon));
		}

		#endregion
	}
}
