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
#endregion

namespace Microsoft.Xna.Framework
{
	public class PreparingDeviceSettingsEventArgs : EventArgs
	{
		#region Public Properties

		public GraphicsDeviceInformation GraphicsDeviceInformation
		{
			get;
			private set;
		}

		#endregion

		#region Public Constructor

		public PreparingDeviceSettingsEventArgs(GraphicsDeviceInformation graphicsDeviceInformation)
		{
			GraphicsDeviceInformation = graphicsDeviceInformation;
		}

		#endregion
	}
}
