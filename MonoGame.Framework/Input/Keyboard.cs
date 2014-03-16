#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Input
{
    /// <summary>
    /// Allows getting keystrokes from keyboard.
    /// </summary>
	public static class Keyboard
	{
        static List<Keys> _keys;

        /// <summary>
        /// Returns the current keyboard state.
        /// </summary>
        /// <returns>Current keyboard state.</returns>
		public static KeyboardState GetState()
		{
            return new KeyboardState(_keys);
		}
		
        /// <summary>
        /// Returns the current keyboard state for a given player.
        /// </summary>
        /// <param name="playerIndex">Player index of the keyboard.</param>
        /// <returns>Current keyboard state.</returns>
        [Obsolete("Use GetState() instead. In future versions this method can be removed.")]
        public static KeyboardState GetState(PlayerIndex playerIndex)
		{
            return new KeyboardState(_keys);
		}

        internal static void SetKeys(List<Keys> keys)
        {
            _keys = keys;
        }
	}
}
