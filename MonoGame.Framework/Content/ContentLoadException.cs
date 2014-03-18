#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;

namespace Microsoft.Xna.Framework.Content
{
	public class ContentLoadException : Exception
	{
		public ContentLoadException() : base()
		{
		}

		public ContentLoadException(string message) : base(message)
		{
		}

		public ContentLoadException(string message, Exception innerException) : base(message,innerException)
		{
		}
	}
}

