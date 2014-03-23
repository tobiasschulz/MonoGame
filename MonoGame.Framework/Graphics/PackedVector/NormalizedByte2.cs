#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector
{
    public struct NormalizedByte2 : IPackedVector<ushort>, IEquatable<NormalizedByte2>
    {
        #region Public Properties

        [CLSCompliant(false)]
        public ushort PackedValue
        {
            get
            {
                return _packed;
            }
            set
            {
                _packed = value;
            }
        }

        #endregion

        #region Private Variables

        private ushort _packed;

        #endregion

        #region Public Constructors

        public NormalizedByte2(Vector2 vector)
        {
            _packed = Pack(vector.X, vector.Y);
        }

        public NormalizedByte2(float x, float y)
        {
            _packed = Pack(x, y);
        }

        #endregion

        #region Public Methods

        public Vector2 ToVector2()
        {
            return new Vector2(
                ((sbyte)(_packed & 0xFF)) / 127.0f,
                ((sbyte)((_packed >> 8) & 0xFF)) / 127.0f);
        }

        #endregion

        #region Private Methods

        void IPackedVector.PackFromVector4(Vector4 vector)
        {
            _packed = Pack(vector.X, vector.Y);
        }

        Vector4 IPackedVector.ToVector4()
        {
            return new Vector4(ToVector2(), 0.0f, 1.0f);
        }

        #endregion

        #region Public Static Operators and Override Methods

        public static bool operator !=(NormalizedByte2 a, NormalizedByte2 b)
        {
            return a._packed != b._packed;
        }

        public static bool operator ==(NormalizedByte2 a, NormalizedByte2 b)
        {
            return a._packed == b._packed;
        }

        public override bool Equals(object obj)
        {
            return (obj is NormalizedByte2) &&
                    ((NormalizedByte2)obj)._packed == _packed;
        }

        public bool Equals(NormalizedByte2 other)
        {
            return _packed == other._packed;
        }

        public override int GetHashCode()
        {
            return _packed.GetHashCode();
        }

        public override string ToString()
        {
            return _packed.ToString("X");
        }

        #endregion

        #region Private Static Methods

        private static ushort Pack(float x, float y)
        {
            var byte2 = (((ushort)(MathHelper.Clamp(x, -1.0f, 1.0f) * 127.0f)) << 0) & 0x00FF;
            var byte1 = (((ushort)(MathHelper.Clamp(y, -1.0f, 1.0f) * 127.0f)) << 8) & 0xFF00;

            return (ushort)(byte2 | byte1);
        }

        #endregion
    }
}
