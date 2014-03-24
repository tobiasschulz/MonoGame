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
	public struct VertexElement
	{
		#region Public Properties

		public int Offset
		{
			get
			{
				return offset;
			}
			set
			{
				offset = value;
			}
		}

		public VertexElementFormat VertexElementFormat
		{
			get
			{
				return format;
			}
			set
			{
				format = value;
			}
		}

		public VertexElementUsage VertexElementUsage
		{
			get
			{
				return usage;
			}
			set
			{
				usage = value;
			}
		}

		public int UsageIndex
		{
			get
			{
				return usageIndex;
			}
			set
			{
				usageIndex = value;
			}
		}

		#endregion

		#region Internal Varialbes

		private int offset;
		private VertexElementFormat format;
		private VertexElementUsage usage;
		private int usageIndex;

		#endregion

		#region Public Constructor

		public VertexElement(
			int offset,
			VertexElementFormat elementFormat,
			VertexElementUsage elementUsage,
			int usageIndex
		) {
			this.offset = offset;
			this.usageIndex = usageIndex;
			this.format = elementFormat;
			this.usage = elementUsage;
		}

		#endregion

		#region Public Static Operators and Override Methods

		public override int GetHashCode()
		{
			// TODO: Fix hashes
			return 0;
		}

		public override string ToString()
		{
			return string.Format(
				"{{Offset:{0} Format:{1} Usage:{2} UsageIndex:{3}}}",
				new object[]
				{
					this.Offset,
					this.VertexElementFormat,
					this.VertexElementUsage,
					this.UsageIndex
				}
			);
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
			return (this == ((VertexElement) obj));
		}

		public static bool operator ==(VertexElement left, VertexElement right)
		{
			return (	(left.Offset == right.Offset) &&
					(left.UsageIndex == right.UsageIndex) &&
					(left.VertexElementUsage == right.VertexElementUsage) &&
					(left.VertexElementFormat == right.VertexElementFormat)	);
		}

		public static bool operator !=(VertexElement left, VertexElement right)
		{
			return !(left == right);
		}

		#endregion
	}
}
