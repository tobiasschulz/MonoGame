#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework
{
	public class GraphicsDeviceInformation
	{
		public GraphicsDeviceInformation ()
		{
		}
		
		public GraphicsAdapter Adapter { get; set; }
		
		public GraphicsProfile GraphicsProfile { get; set; }
		
		public PresentationParameters PresentationParameters { get; set; }
	}
}

