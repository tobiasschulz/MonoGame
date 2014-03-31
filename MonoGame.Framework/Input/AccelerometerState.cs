#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;

namespace Microsoft.Xna.Framework.Input
{
	public struct AccelerometerState
	{
		#region Public Properties

		public Vector3 Acceleration
		{
			get
			{
				return acceleration;
			}
			internal set
			{
				acceleration = value;
			}
		}

		/*
		public Matrix GetRotation()
		{
			throw new NotImplementedException();
		}
		*/

		public bool IsConnected
		{
			get
			{
				return true;
			}
		}

		#endregion

		#region Private Variables

		private Vector3 acceleration;

		#endregion
	}
}
