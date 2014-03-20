#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;

namespace Microsoft.Xna.Framework
{
    /// <summary>
    /// This class is used for the game window's TextInput event as EventArgs.
    /// </summary>
    public class TextInputEventArgs : EventArgs
    {
        char character;
        public TextInputEventArgs(char character)
        {
            this.character = character;
        }
        public char Character
        {
            get
            {
                return character;
            }
        }
    }
}
