#region License
/*
Microsoft Public License (Ms-PL)
MonoGame - Copyright © 2009 The MonoGame Team

All rights reserved.

This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
U.S. copyright law.

A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
code form, you may only do so under a license that complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
purpose and non-infringement.
*/
#endregion License
//

using System;
using System.Diagnostics;

using OpenTK.Graphics.OpenGL;

namespace Microsoft.Xna.Framework.Graphics
{
    public abstract class Texture : GraphicsResource
    {
        #region Public Properties

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

        #endregion

        #region Internal OpenGL Variables

        internal OpenGLDevice.OpenGLTexture texture;

        #endregion

        #region Protected OpenGL Variables

        [CLSCompliant(false)]
        protected PixelInternalFormat glInternalFormat;

        [CLSCompliant(false)]
        protected PixelFormat glFormat;

        [CLSCompliant(false)]
        protected PixelType glType;

        #endregion

        #region Protected Dispose Method

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                GraphicsDevice.AddDisposeAction(() =>
                {
                    texture.Dispose();
                });
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Internal Surface Pitch Calculator

        internal int GetPitch(int width)
        {
            Debug.Assert(width > 0, "The width is negative!");

            if (    Format == SurfaceFormat.Dxt1 ||
                    Format == SurfaceFormat.Dxt3 ||
                    Format == SurfaceFormat.Dxt5    )
            {
                return ((width + 3) / 4) * Format.Size();
            }
            return width * Format.Size();
        }

        #endregion

        #region Internal Context Reset Method

        internal protected override void GraphicsDeviceResetting()
        {
            // FIXME: Do we even want to bother with DeviceResetting for GL? -flibit
        }

        #endregion

        #region Mipmap Level Calculator

        internal static int CalculateMipLevels(
            int width,
            int height = 0,
            int depth = 0
        ) {
            int levels = 1;
            for (
                int size = Math.Max(Math.Max(width, height), depth);
                size > 1;
                levels += 1
            ) {
                size /= 2;
            }
            return levels;
        }

        #endregion

        #region Protected XNA->GL SurfaceFormat Conversion Method

        protected void GetGLSurfaceFormat()
        {
            switch (Format)
            {
                case SurfaceFormat.Color:
                    glInternalFormat = PixelInternalFormat.Rgba;
                    glFormat = PixelFormat.Rgba;
                    glType = PixelType.UnsignedByte;
                    break;
                case SurfaceFormat.Bgr565:
                    glInternalFormat = PixelInternalFormat.Rgb;
                    glFormat = PixelFormat.Rgb;
                    glType = PixelType.UnsignedShort565;
                    break;
                case SurfaceFormat.Bgra4444:
                    glInternalFormat = PixelInternalFormat.Rgba4;
                    glFormat = PixelFormat.Rgba;
                    glType = PixelType.UnsignedShort4444;
                    break;
                case SurfaceFormat.Bgra5551:
                    glInternalFormat = PixelInternalFormat.Rgba;
                    glFormat = PixelFormat.Rgba;
                    glType = PixelType.UnsignedShort5551;
                    break;
                case SurfaceFormat.Alpha8:
                    glInternalFormat = PixelInternalFormat.Luminance;
                    glFormat = PixelFormat.Luminance;
                    glType = PixelType.UnsignedByte;
                    break;
                case SurfaceFormat.Dxt1:
                    glInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt1Ext;
                    glFormat = (PixelFormat)All.CompressedTextureFormats;
                    break;
                case SurfaceFormat.Dxt3:
                    glInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt3Ext;
                    glFormat = (PixelFormat)All.CompressedTextureFormats;
                    break;
                case SurfaceFormat.Dxt5:
                    glInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt5Ext;
                    glFormat = (PixelFormat)All.CompressedTextureFormats;
                    break;
                case SurfaceFormat.Single:
                    glInternalFormat = PixelInternalFormat.R32f;
                    glFormat = PixelFormat.Red;
                    glType = PixelType.Float;
                    break;
                case SurfaceFormat.HalfVector2:
                    glInternalFormat = PixelInternalFormat.Rg16f;
                    glFormat = PixelFormat.Rg;
                    glType = PixelType.HalfFloat;
                    break;
                case SurfaceFormat.HdrBlendable:
                case SurfaceFormat.HalfVector4:
                    glInternalFormat = PixelInternalFormat.Rgba16f;
                    glFormat = PixelFormat.Rgba;
                    glType = PixelType.HalfFloat;
                    break;
                case SurfaceFormat.HalfSingle:
                    glInternalFormat = PixelInternalFormat.R16f;
                    glFormat = PixelFormat.Red;
                    glType = PixelType.HalfFloat;
                    break;
                case SurfaceFormat.Vector2:
                    glInternalFormat = PixelInternalFormat.Rg32f;
                    glFormat = PixelFormat.Rg;
                    glType = PixelType.Float;
                    break;
                case SurfaceFormat.Vector4:
                    glInternalFormat = PixelInternalFormat.Rgba32f;
                    glFormat = PixelFormat.Rgba;
                    glType = PixelType.Float;
                    break;
                case SurfaceFormat.NormalizedByte2:
                    glInternalFormat = PixelInternalFormat.Rg8i;
                    glFormat = PixelFormat.Rg;
                    glType = PixelType.Byte;
                    break;
                case SurfaceFormat.NormalizedByte4:
                    glInternalFormat = PixelInternalFormat.Rgba8i;
                    glFormat = PixelFormat.Rgba;
                    glType = PixelType.Byte;
                    break;
                case SurfaceFormat.Rg32:
                    glInternalFormat = PixelInternalFormat.Rg16ui;
                    glFormat = PixelFormat.Rg;
                    glType = PixelType.UnsignedShort;
                    break;
                case SurfaceFormat.Rgba64:
                    glInternalFormat = PixelInternalFormat.Rgba16ui;
                    glFormat = PixelFormat.Rgba;
                    glType = PixelType.UnsignedShort;
                    break;
                case SurfaceFormat.Rgba1010102:
                    glInternalFormat = PixelInternalFormat.Rgb10A2ui;
                    glFormat = PixelFormat.Rgba;
                    glType = PixelType.UnsignedInt1010102;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        #endregion
    }
}