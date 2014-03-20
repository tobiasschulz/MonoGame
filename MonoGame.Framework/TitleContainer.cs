#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;
using System.IO;

namespace Microsoft.Xna.Framework
{
    public static class TitleContainer
    {
        static TitleContainer() 
        {
            Location = AppDomain.CurrentDomain.BaseDirectory;
        }

        static internal string Location { get; private set; }

        /// <summary>
        /// Returns an open stream to an exsiting file in the title storage area.
        /// </summary>
        /// <param name="name">The filepath relative to the title storage area.</param>
        /// <returns>A open stream or null if the file is not found.</returns>
        public static Stream OpenStream(string name)
        {
            // Normalize the file path.
            var safeName = GetFilename(name);

            // We do not accept absolute paths here.
            if (Path.IsPathRooted(safeName))
                throw new ArgumentException("Invalid filename. TitleContainer.OpenStream requires a relative path.");

            var absolutePath = Path.Combine(Location, safeName);
            return File.OpenRead(absolutePath);
        }

        // TODO: This is just path normalization.  Remove this
        // and replace it with a proper utility function.  I'm sure
        // this same logic is duplicated all over the code base.
        internal static string GetFilename(string name)
        {
            // Replace Windows path separators with local path separators
            name = name.Replace('\\', Path.DirectorySeparatorChar);
            return name;
        }
    }
}

