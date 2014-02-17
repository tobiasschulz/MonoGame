// #region License
// /*
// Microsoft Public License (Ms-PL)
// MonoGame - Copyright © 2009 The MonoGame Team
// 
// All rights reserved.
// 
// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
// accept the license, do not use the software.
// 
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
// U.S. copyright law.
// 
// A "contribution" is the original software, or any additions or changes to the software.
// A "contributor" is any person that distributes its contribution under this license.
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
// your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
// notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
// a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
// code form, you may only do so under a license that complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
// or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
// permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
// purpose and non-infringement.
// */
// #endregion License
//

using System;
using System.Diagnostics;

using OpenTK.Graphics.OpenGL;

namespace Microsoft.Xna.Framework.Graphics
{
    public abstract class Texture : GraphicsResource
    {
        internal OpenGLDevice.OpenGLTexture texture;

        public SurfaceFormat Format
        {
            get;
            protected set;
        }

        public int LevelCount
        {
            get;
            protected set;
        }

        [CLSCompliant(false)]
        protected PixelInternalFormat glInternalFormat;
        [CLSCompliant(false)]
        protected PixelFormat glFormat;
        [CLSCompliant(false)]
        protected PixelType glType;

        internal static int CalculateMipLevels(int width, int height = 0, int depth = 0)
        {
            int levels = 1;
            int size = Math.Max(Math.Max(width, height), depth);
            while (size > 1)
            {
                size = size / 2;
                levels++;
            }
            return levels;
        }

        internal int GetPitch(int width)
        {
            Debug.Assert(width > 0, "The width is negative!");

            int pitch;

            switch (Format)
            {
                case SurfaceFormat.Dxt1:
                case SurfaceFormat.Dxt1a:
                case SurfaceFormat.Dxt3:
                case SurfaceFormat.Dxt5:
                    Debug.Assert(MathHelper.IsPowerOfTwo(width), "This format must be power of two!");
                    pitch = ((width + 3) / 4) * Format.Size();
                    break;

                default:
                    pitch = width * Format.Size();
                    break;
            };

            return pitch;
        }

        internal protected override void GraphicsDeviceResetting()
        {
            // FIXME: Do we even want to bother with DeviceResetting for GL? -flibit
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                GraphicsDevice.AddDisposeAction(() =>
                {
                    texture.Dispose();
                    texture = null;
                });
            }
            base.Dispose(disposing);
        }
    }
}