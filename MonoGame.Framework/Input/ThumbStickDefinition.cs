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
	public class ThumbStickDefinition
	{
		public Texture2D Texture {get;set;}
		public Vector2 Position {get;set;}
		public Rectangle TextureRect {get;set;}
		internal Vector2 InitialHit {get;set;}
		internal Vector2 Offset {get;set;}
	}
}
