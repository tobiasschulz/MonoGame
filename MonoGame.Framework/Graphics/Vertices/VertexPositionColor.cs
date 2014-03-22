#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
    [DataContract]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct VertexPositionColor : IVertexType
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

        #region Public Fields

        [DataMember]
		public Vector3 Position;
        
        [DataMember]
		public Color Color;

        #endregion

        #region Public Static Fields

        public static readonly VertexDeclaration VertexDeclaration;

        #endregion

        #region Private Static Constructor

        static VertexPositionColor()
        {
            VertexElement[] elements = new VertexElement[] { new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0) };
            VertexDeclaration declaration = new VertexDeclaration(elements);
            VertexDeclaration = declaration;
        }

        #endregion

        #region Public Constructor

        public VertexPositionColor (Vector3 position, Color color)
		{
			this.Position = position;
			Color = color;
		}

        #endregion

        #region Public Static Operators and Override Methods

        public override int GetHashCode ()
		{
			// TODO: Fix gethashcode
			return 0;
		}

        public override string ToString ()
		{
			return string.Format ("{{Position:{0} Color:{1}}}", new object[] { this.Position, this.Color });
		}

		public static bool operator == (VertexPositionColor left, VertexPositionColor right)
		{
			return ((left.Color == right.Color) && (left.Position == right.Position));
		}

		public static bool operator != (VertexPositionColor left, VertexPositionColor right)
		{
			return !(left == right);
		}

		public override bool Equals (object obj)
		{
			if (obj == null) {
				return false;
			}
			if (obj.GetType () != base.GetType ()) {
				return false;
			}
			return (this == ((VertexPositionColor)obj));
		}

        #endregion
    
    }
}
