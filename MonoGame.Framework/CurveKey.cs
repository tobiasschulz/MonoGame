#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */

/*
MIT License
Copyright (C) 2006 The Mono.Xna Team

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

#region Using Statements
using System;
using System.Runtime.Serialization;
#endregion

namespace Microsoft.Xna.Framework
{
	[DataContract]
	public class CurveKey : IEquatable<CurveKey>, IComparable<CurveKey>
	{
		#region Public Properties

		[DataMember]
		public CurveContinuity Continuity
		{
			get;
			set;
		}

		[DataMember]
		public float Position
		{
			get;
			private set;
		}

		[DataMember]
		public float TangentIn
		{
			get;
			set;
		}

		[DataMember]
		public float TangentOut
		{
			get;
			set;
		}

		[DataMember]
		public float Value
		{
			get;
			set;
		}

		#endregion

		#region Public Constructors

		public CurveKey(
			float position,
			float value
		) : this(
			position,
			value,
			0,
			0,
			CurveContinuity.Smooth
		) {
		}

		public CurveKey(
			float position,
			float value,
			float tangentIn,
			float tangentOut
		) : this(
			position,
			value,
			tangentIn,
			tangentOut,
			CurveContinuity.Smooth
		) {
		}

		public CurveKey(
			float position,
			float value,
			float tangentIn,
			float tangentOut,
			CurveContinuity continuity
		) {
			Position = position;
			Value = value;
			TangentIn = tangentIn;
			TangentOut = tangentOut;
			Continuity = continuity;
		}

		#endregion

		#region Public Methods

		public CurveKey Clone()
		{
			return new CurveKey(
				Position,
				Value,
				TangentIn,
				TangentOut,
				Continuity
			);
		}

		public int CompareTo(CurveKey other)
		{
			return Position.CompareTo(other.Position);
		}

		public bool Equals(CurveKey other)
		{
			return (this == other);
		}

		#endregion

		#region Public Static Operators and Override Methods

		public static bool operator !=(CurveKey a, CurveKey b)
		{
			return !(a == b);
		}

		public static bool operator ==(CurveKey a, CurveKey b)
		{
			if (object.Equals(a, null))
			{
				return object.Equals(b, null);
			}

			if (object.Equals(b, null))
			{
				return object.Equals(a, null);
			}

			return (	(a.Position == b.Position) &&
					(a.Value == b.Value) &&
					(a.TangentIn == b.TangentIn) &&
					(a.TangentOut == b.TangentOut) &&
					(a.Continuity == b.Continuity)	);
		}

		public override bool Equals(object obj)
		{
			return (obj as CurveKey) == this;
		}

		public override int GetHashCode()
		{
			return (
				Position.GetHashCode() ^
				Value.GetHashCode() ^
				TangentIn.GetHashCode() ^
				TangentOut.GetHashCode() ^
				Continuity.GetHashCode()
			);
		}

		#endregion
	}
}
