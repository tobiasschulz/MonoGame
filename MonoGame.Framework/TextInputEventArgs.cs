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
	/// <summary>
	/// This class is used for the game window's TextInput event as EventArgs.
	/// </summary>
	public class TextInputEventArgs : EventArgs
	{
		#region Public Properties

		public char Character
		{
			get;
			private set;
		}

		#endregion

		#region Public Constructors

		public TextInputEventArgs(char character)
		{
			Character = character;
		}

		#endregion
	}
}
