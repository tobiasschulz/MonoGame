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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct VertexPositionTexture : IVertexType
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

        public Vector3 Position;
        public Vector2 TextureCoordinate;

        #endregion

        #region Public Static Fields

        public static readonly VertexDeclaration VertexDeclaration;

        #endregion

        #region Private Static Constructor

        static VertexPositionTexture()
        {
            VertexElement[] elements = new VertexElement[] { new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0) };
            VertexDeclaration declaration = new VertexDeclaration(elements);
            VertexDeclaration = declaration;
        }

        #endregion

        #region Public Constructor

        public VertexPositionTexture(Vector3 position, Vector2 textureCoordinate)
        {
            this.Position = position;
            this.TextureCoordinate = textureCoordinate;
        }

        #endregion

        #region Public Static Operators and Override Methods

        public override int GetHashCode()
        {
            // TODO: Fix get hashcode
            return 0;
        }

        public override string ToString()
        {
            return string.Format("{{Position:{0} TextureCoordinate:{1}}}", new object[] { this.Position, this.TextureCoordinate });
        }

        public static bool operator ==(VertexPositionTexture left, VertexPositionTexture right)
        {
            return ((left.Position == right.Position) && (left.TextureCoordinate == right.TextureCoordinate));
        }

        public static bool operator !=(VertexPositionTexture left, VertexPositionTexture right)
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
            return (this == ((VertexPositionTexture)obj));
        }

        #endregion

    }
}
