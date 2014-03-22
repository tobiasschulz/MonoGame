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
    public struct HalfSingle : IPackedVector<UInt16>, IEquatable<HalfSingle>, IPackedVector
    {

        #region Public Properties

        [CLSCompliant(false)]
        public ushort PackedValue
        {
            get
            {
                return this.packedValue;
            }
            set
            {
                this.packedValue = value;
            }
        }

        #endregion

        #region Private Variables

        UInt16 packedValue;

        #endregion

        #region Public Constructors

        public HalfSingle(float single)
        {
            packedValue = HalfTypeHelper.Convert(single);
        }

        #endregion

        #region Public Methods

        public float ToSingle()
        {
            return HalfTypeHelper.Convert(this.packedValue);
        }

        #endregion

        #region Private Methods

        void IPackedVector.PackFromVector4(Vector4 vector)
        {
            this.packedValue = HalfTypeHelper.Convert(vector.X);
        }

        Vector4 IPackedVector.ToVector4()
        {
            return new Vector4(this.ToSingle(), 0f, 0f, 1f);
        }

        #endregion

        #region Public Static Operators and Override Methods

        public override bool Equals(object obj)
        {
            if (obj != null && obj.GetType() == this.GetType())
            {
                return this == (HalfSingle)obj;
            }

            return false;
        }

        public bool Equals(HalfSingle other)
        {
            return this.packedValue == other.packedValue;
        }

        public override string ToString()
        {
            return this.ToSingle().ToString();
        }

        public override int GetHashCode()
        {
            return this.packedValue.GetHashCode();
        }

        public static bool operator ==(HalfSingle lhs, HalfSingle rhs)
        {
            return lhs.packedValue == rhs.packedValue;
        }

        public static bool operator !=(HalfSingle lhs, HalfSingle rhs)
        {
            return lhs.packedValue != rhs.packedValue;
        }

        #endregion

    }
}
