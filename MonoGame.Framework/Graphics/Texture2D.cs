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

#region Using Statements
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL;
using GLPixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
    public class Texture2D : Texture
    {
        #region Public Properties

        public int Width
        {
            get;
            private set;
        }

        public int Height
        {
            get;
            private set;
        }

        public Rectangle Bounds
        {
            get
            {
                return new Rectangle(0, 0, Width, Height);
            }
        }

        #endregion

        #region Public Constructors

        public Texture2D(
            GraphicsDevice graphicsDevice,
            int width,
            int height
        ) : this(
            graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.Color,
            false
        ) {
        }

        public Texture2D(
            GraphicsDevice graphicsDevice,
            int width,
            int height,
            bool mipmap,
            SurfaceFormat format
        ) : this(
            graphicsDevice,
            width,
            height,
            mipmap,
            format,
            false
        ) {
        }

        #endregion

        #region Protected Constructor

        protected Texture2D(
            GraphicsDevice graphicsDevice,
            int width,
            int height,
            bool mipmap,
            SurfaceFormat format,
            bool renderTarget
        ) {
            if (graphicsDevice == null)
            {
                throw new ArgumentNullException("graphicsDevice");
            }

            GraphicsDevice = graphicsDevice;
            Width = width;
            Height = height;
            LevelCount = mipmap ? CalculateMipLevels(width, height) : 1;

            Format = format;
            GetGLSurfaceFormat();

            Threading.BlockOnUIThread(() =>
            {
                GenerateGLTextureIfRequired();
                GL.BindTexture(TextureTarget.Texture2D, texture.Handle);

                if (    Format == SurfaceFormat.Dxt1 ||
                        Format == SurfaceFormat.Dxt3 ||
                        Format == SurfaceFormat.Dxt5    )
                {
                    PixelInternalFormat internalFormat;
                    if (Format == SurfaceFormat.Dxt1)
                    {
                        internalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt1Ext;
                    }
                    else if (Format == SurfaceFormat.Dxt3)
                    {
                        internalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt3Ext;
                    }
                    else if (Format == SurfaceFormat.Dxt5)
                    {
                        internalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt5Ext;
                    }
                    else
                    {
                        throw new Exception("Unhandled SurfaceFormat: " + Format);
                    }
                    GL.CompressedTexImage2D(
                        TextureTarget.Texture2D,
                        0,
                        internalFormat,
                        this.Width,
                        this.Height,
                        0,
                        ((this.Width + 3) / 4) * ((this.Height + 3) / 4) * format.Size(),
                        IntPtr.Zero
                    );
                    GraphicsExtensions.CheckGLError();
                }
                else
                {
                    GL.TexImage2D(
                        TextureTarget.Texture2D,
                        0,
                        glInternalFormat,
                        this.Width,
                        this.Height,
                        0,
                        glFormat,
                        glType,
                        IntPtr.Zero
                    );
                    GraphicsExtensions.CheckGLError();
                }
                texture.Flush(true);

                // Restore the bound texture.
                GL.BindTexture(
                    OpenGLDevice.Instance.Samplers[0].Target.GetCurrent(),
                    OpenGLDevice.Instance.Samplers[0].Texture.GetCurrent().Handle
                );
                GraphicsExtensions.CheckGLError();
            });
        }

        #endregion

        #region Public SetData Methods

        public void SetData<T>(T[] data) where T : struct
        {
            this.SetData(
                0,
                null,
                data,
                0,
                data.Length
            );
        }

        public void SetData<T>(
            T[] data,
            int startIndex,
            int elementCount
        ) where T : struct {
            this.SetData(
                0,
                null,
                data,
                startIndex,
                elementCount
            );
        }

        public void SetData<T>(
            int level,
            Rectangle? rect,
            T[] data,
            int startIndex,
            int elementCount
        ) where T : struct {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            Threading.BlockOnUIThread(() =>
            {
                GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

                try
                {
                    int elementSizeInBytes = Marshal.SizeOf(typeof(T));
                    int startByte = startIndex * elementSizeInBytes;
                    IntPtr dataPtr = (IntPtr) (dataHandle.AddrOfPinnedObject().ToInt64() + startByte);

                    int dataLength;
                    if (elementCount > 0)
                    {
                        dataLength = elementCount * elementSizeInBytes;
                    }
                    else
                    {
                        dataLength = data.Length - startByte;
                    }
                    int x, y, w, h;
                    if (rect.HasValue)
                    {
                        x = rect.Value.X;
                        y = rect.Value.Y;
                        w = rect.Value.Width;
                        h = rect.Value.Height;
                    }
                    else
                    {
                        x = 0;
                        y = 0;
                        w = Math.Max(Width >> level, 1);
                        h = Math.Max(Height >> level, 1);

                        // For DXT textures the width and height of each level is a multiple of 4.
                        // OpenGL only: The last two mip levels require the width and height to be 
                        // passed as 2x2 and 1x1, but there needs to be enough data passed to occupy 
                        // a 4x4 block. 
                        // Ref: http://www.mentby.com/Group/mac-opengl/issue-with-dxt-mipmapped-textures.html 
                        if (    Format == SurfaceFormat.Dxt1 ||
                                Format == SurfaceFormat.Dxt3 ||
                                Format == SurfaceFormat.Dxt5    )
                        {
                            if (w > 4)
                            {
                                w = (w + 3) & ~3;
                            }
                            if (h > 4)
                            {
                                h = (h + 3) & ~3;
                            }
                        }
                    }

                    GenerateGLTextureIfRequired();
                    GL.BindTexture(TextureTarget.Texture2D, texture.Handle);
                    if (glFormat == (GLPixelFormat) All.CompressedTextureFormats)
                    {
                        if (rect.HasValue)
                        {
                            GL.CompressedTexSubImage2D(
                                TextureTarget.Texture2D,
                                level,
                                x,
                                y,
                                w,
                                h,
                                glFormat,
                                dataLength,
                                dataPtr
                            );
                            GraphicsExtensions.CheckGLError();
                        }
                        else
                        {
                            GL.CompressedTexImage2D(
                                TextureTarget.Texture2D,
                                level,
                                glInternalFormat,
                                w,
                                h,
                                0,
                                dataLength,
                                dataPtr
                            );
                            GraphicsExtensions.CheckGLError();
                        }
                    }
                    else
                    {
                        // Set pixel alignment to match texel size in bytes
                        GL.PixelStore(PixelStoreParameter.UnpackAlignment, GraphicsExtensions.Size(this.Format));
                        if (rect.HasValue)
                        {
                            GL.TexSubImage2D(
                                TextureTarget.Texture2D,
                                level,
                                x,
                                y,
                                w,
                                h,
                                glFormat,
                                glType,
                                dataPtr
                            );
                            GraphicsExtensions.CheckGLError();
                        }
                        else
                        {
                            GL.TexImage2D(
                                TextureTarget.Texture2D,
                                level,
                                glInternalFormat,
                                w,
                                h,
                                0,
                                glFormat,
                                glType,
                                dataPtr
                            );
                            GraphicsExtensions.CheckGLError();
                        }
                        // Return to default pixel alignment
                        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
                    }

                    GL.Finish();

                    GL.BindTexture(
                        OpenGLDevice.Instance.Samplers[0].Target.GetCurrent(),
                        OpenGLDevice.Instance.Samplers[0].Texture.GetCurrent().Handle
                    );
                }
                finally
                {
                    dataHandle.Free();
                }
            });
        }

        #endregion

        #region Public GetData Methods

        public void GetData<T>(T[] data) where T : struct
        {
            this.GetData(0, null, data, 0, data.Length);
        }

        public void GetData<T>(
            T[] data,
            int startIndex,
            int elementCount
        ) where T : struct {
            GetData(
                0,
                null,
                data,
                startIndex,
                elementCount
            );
        }

        public void GetData<T>(
            int level,
            Rectangle? rect,
            T[] data,
            int startIndex,
            int elementCount
        ) where T : struct {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("data cannot be null");
            }
            if (data.Length < startIndex + elementCount)
            {
                throw new ArgumentException(
                    "The data passed has a length of " + data.Length +
                    " but " + elementCount + " pixels have been requested."
                );
            }

            GL.BindTexture(TextureTarget.Texture2D, texture.Handle);

            if (glFormat == (GLPixelFormat) All.CompressedTextureFormats)
            {
                throw new NotImplementedException("GetData, CompressedTexture");
            }
            else if (rect == null)
            {
                // Just throw the whole texture into the user array.
                GL.GetTexImage(
                    TextureTarget.Texture2D,
                    0,
                    glFormat,
                    glType,
                    data
                );
            }
            else
            {
                // Get the whole texture...
                T[] texData = new T[Width * Height];
                GL.GetTexImage(
                    TextureTarget.Texture2D,
                    0,
                    glFormat,
                    glType,
                    texData
                );

                // Now, blit the rect region into the user array.
                Rectangle region = rect.Value;
                int curPixel = -1;
                for (int row = region.Y; row < region.Y + region.Height; row += 1)
                {
                    for (int col = region.X; col < region.X + region.Width; col += 1)
                    {
                        curPixel += 1;
                        if (curPixel < startIndex)
                        {
                            // If we're not at the start yet, just keep going...
                            continue;
                        }
                        if (curPixel > elementCount)
                        {
                            // If we're past the end, we're done!
                            return;
                        }
                        data[curPixel - startIndex] = texData[(row * Width) + col];
                    }
                }
            }
        }

        #endregion

        #region Public Texture2D Load Methods

        public static Texture2D FromStream(GraphicsDevice graphicsDevice, Stream stream)
        {
            // TODO: SDL2.SDL_image.IMG_Load(); -flibit
            using (Bitmap image = (Bitmap) Bitmap.FromStream(stream))
            {
                // Fix up the Image to match the expected format
                image.RGBToBGR();

                byte[] data = new byte[image.Width * image.Height * 4];

                BitmapData bitmapData = image.LockBits(
                    new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb
                );
                if (bitmapData.Stride != image.Width * 4)
                {
                    throw new NotImplementedException();
                }
                Marshal.Copy(bitmapData.Scan0, data, 0, data.Length);
                image.UnlockBits(bitmapData);

                Texture2D texture = new Texture2D(graphicsDevice, image.Width, image.Height);
                texture.SetData(data);
                return texture;
            }
        }

        // THIS IS AN EXTENSION OF THE XNA4 API! USE AS A LAST RESORT! -flibit
        public static Texture2D FromFile(GraphicsDevice device, string filePath)
        {
            throw new NotImplementedException("flibit put this here.");
        }

        #endregion

        #region Public Texture2D Save Methods

        public void SaveAsJpeg(Stream stream, int width, int height)
        {
            // FIXME: throw new NotSupportedException("It's 2014. Time to move on."); -flibit
            SaveAsImage(stream, width, height, ImageFormat.Jpeg);
        }

        public void SaveAsPng(Stream stream, int width, int height)
        {
            // FIXME: SDL2.SDL_image.IMG_SavePNG(); -flibit
            SaveAsImage(stream, width, height, ImageFormat.Png);
        }

        #endregion

        #region Private Master Image Save Method

        private void SaveAsImage(Stream stream, int width, int height, ImageFormat format)
        {
            // FIXME: This method needs to die ASAP. -flibit
            if (stream == null)
            {
                throw new ArgumentNullException(
                    "stream",
                    "'stream' cannot be null"
                );
            }
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "width",
                    width,
                    "'width' cannot be less than or equal to zero"
                );
            }
            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "height",
                    height,
                    "'height' cannot be less than or equal to zero"
                );
            }
            if (format == null)
            {
                throw new ArgumentNullException(
                    "format",
                    "'format' cannot be null"
                );
            }

            byte[] data = null;
            GCHandle? handle = null;
            Bitmap bitmap = null;
            try
            {
                data = new byte[width * height * 4];
                handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                GetData(data);

                // internal structure is BGR while bitmap expects RGB
                for(int i = 0; i < data.Length; i += 4)
                {
                    byte temp = data[i + 0];
                    data[i + 0] = data[i + 2];
                    data[i + 2] = temp;
                }

                bitmap = new Bitmap(
                    width,
                    height,
                    width * 4,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb,
                    handle.Value.AddrOfPinnedObject()
                );

                bitmap.Save(stream, format);
            }
            finally
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                }
                if (handle.HasValue)
                {
                    handle.Value.Free();
                }
                if (data != null)
                {
                    data = null;
                }
            }
        }

        #endregion

        #region Internal GenerateMipmaps Method

        // TODO: You could extend the XNA API with this...
        internal void GenerateMipmaps()
        {
            Threading.BlockOnUIThread(() =>
            {
                texture.Generate2DMipmaps();
            });
        }

        #endregion

        #region Private glGenTexture Method

        private void GenerateGLTextureIfRequired()
        {
            if (texture == null || texture.Handle == 0)
            {
                texture = new OpenGLDevice.OpenGLTexture(
                    TextureTarget.Texture2D,
                    Format,
                    LevelCount > 1
                );

                if (((Width & (Width - 1)) != 0) || ((Height & (Height - 1)) != 0))
                {
                    texture.WrapS.Set(TextureAddressMode.Clamp);
                    texture.WrapT.Set(TextureAddressMode.Clamp);
                }
            }
        }

        #endregion
    }
}