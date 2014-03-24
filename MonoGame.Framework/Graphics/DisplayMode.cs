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

Authors:
 * Rob Loach

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
#endregion License

#region Using Statements
using System;
using System.Globalization;
using System.Runtime.Serialization;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	[DataContract]
	public class DisplayMode
	{
		#region Public Properties

		public float AspectRatio
		{
			get
			{
				return (float) Width / (float) Height;
			}
		}

		public SurfaceFormat Format
		{
			get;
			private set;
		}

		public int Height
		{
			get;
			private set;
		}

		public int Width
		{
			get;
			private set;
		}

		public Rectangle TitleSafeArea
		{
			get
			{
				return new Rectangle(0, 0, Width, Height);
			}
		}

		#endregion

		#region Internal Constructor

		internal DisplayMode(int width, int height, SurfaceFormat format)
		{
			Width = width;
			Height = height;
			Format = format;
		}

		#endregion

		#region Public Static Operators and Override Methods

		public static bool operator !=(DisplayMode left, DisplayMode right)
		{
			// If we don't do this cast to (object), we'll get a stack overflow.
			object leftObj = (object) left;
			object rightObj = (object) right;
			if (leftObj == null && rightObj == null)
			{
				return false;
			}
			if (leftObj == null || rightObj == null)
			{
				return true;
			}
			return !(	(left.Format == right.Format) &&
					(left.Height == right.Height) &&
					(left.Width == right.Width)	);
		}

		public static bool operator ==(DisplayMode left, DisplayMode right)
		{
			if (left == null && right == null)
			{
				return true;
			}
			if (left == null || right == null)
			{
				return false;
			}
			return (	(left.Format == right.Format) &&
					(left.Height == right.Height) &&
					(left.Width == right.Width)	);
		}

		public override bool Equals(object obj)
		{
			return obj is DisplayMode && this == (DisplayMode)obj;
		}

		public override int GetHashCode()
		{
			return (Width.GetHashCode() ^ Height.GetHashCode() ^ Format.GetHashCode());
		}

		public override string ToString()
		{
			return string.Format(
				CultureInfo.CurrentCulture,
				"{{Width:{0} Height:{1} Format:{2}}}",
				new object[]
				{
					Width,
					Height,
					Format
				}
			);
		}

		#endregion
	}
}
