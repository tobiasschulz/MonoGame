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
using System.IO;
using System.Runtime.InteropServices;

using SDL2;
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
            this.GetData(
                0,
                null,
                data,
                0,
                data.Length
            );
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

        #region Public Texture2D Load Method

        public static Texture2D FromStream(GraphicsDevice graphicsDevice, Stream stream)
        {
            // Load the Stream into an SDL_RWops*
            byte[] mem = new byte[stream.Length];
            stream.Read(mem, 0, mem.Length);
            IntPtr rwops = SDL.SDL_RWFromMem(mem, mem.Length);

            // Load the SDL_Surface* from RWops, get the image data
            IntPtr surface = SDL_image.IMG_Load_RW(rwops, 1);
            surface = INTERNAL_convertSurfaceFormat(surface);
            int width = INTERNAL_getSurfaceWidth(surface);
            int height = INTERNAL_getSurfaceHeight(surface);
            byte[] pixels = new byte[width * height * 4]; // MUST be SurfaceFormat.Color!
            Marshal.Copy(INTERNAL_getSurfacePixels(surface), pixels, 0, pixels.Length);

            // Create the Texture2D from the SDL_Surface
            Texture2D result = new Texture2D(
                graphicsDevice,
                width,
                height
            );
            result.SetData(pixels);
            return result;
        }

        #endregion

        #region Public Texture2D Save Methods

        public void SaveAsJpeg(Stream stream, int width, int height)
        {
            // dealwithit.png -flibit
            throw new NotSupportedException("It's 2014. Time to move on.");
        }

        public void SaveAsPng(Stream stream, int width, int height)
        {
            // Get the Texture2D pixels
            byte[] data = new byte[Width * Height * 4];
            GetData(data);

            // Create an SDL_Surface*, write the pixel data
            IntPtr surface = SDL.SDL_CreateRGBSurface(
                0,
                Width,
                Height,
                32,
                0x000000FF,
                0x0000FF00,
                0x00FF0000,
                0xFF000000
            );
            SDL.SDL_LockSurface(surface);
            Marshal.Copy(
                data,
                0,
                INTERNAL_getSurfacePixels(surface),
                data.Length
            );
            SDL.SDL_UnlockSurface(surface);
            data = null; // We're done with the original pixel data.

            // Blit to a scaled surface of the size we want, if needed.
            if (width != Width || height != Height)
            {
                IntPtr scaledSurface = SDL.SDL_CreateRGBSurface(
                    0,
                    width,
                    height,
                    32,
                    0x000000FF,
                    0x0000FF00,
                    0x00FF0000,
                    0xFF000000
                );
                SDL.SDL_BlitScaled(
                    surface,
                    IntPtr.Zero,
                    scaledSurface,
                    IntPtr.Zero
                );
                SDL.SDL_FreeSurface(surface);
                surface = scaledSurface;
            }

            // Create an SDL_RWops*, save PNG to RWops
            byte[] pngOut = new byte[width * height * 4]; // Max image size
            IntPtr dst = SDL.SDL_RWFromMem(pngOut, pngOut.Length);
            SDL_image.IMG_SavePNG_RW(surface, dst, 1);
            SDL.SDL_FreeSurface(surface); // We're done with the surface.

            // Get PNG size, write to Stream
            int size = (
                (pngOut[33] << 24) |
                (pngOut[34] << 16) |
                (pngOut[35] << 8) |
                (pngOut[36])
            ) + 41 + 57; // 41 - header, 57 - footer
            stream.Write(pngOut, 0, size);
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

        #region Private SDL_Surface Interop

        [StructLayout(LayoutKind.Sequential)]
        private struct SDL_Surface
        {
#pragma warning disable 0169
            UInt32 flags;
            public IntPtr format;
            public Int32 w;
            public Int32 h;
            Int32 pitch;
            public IntPtr pixels;
            IntPtr userdata;
            Int32 locked;
            IntPtr lock_data;
            SDL.SDL_Rect clip_rect;
            IntPtr map;
            Int32 refcount;
#pragma warning restore 0169
        }

        private static unsafe IntPtr INTERNAL_convertSurfaceFormat(IntPtr surface)
        {
            SDL_Surface* surPtr = (SDL_Surface*)surface;
            SDL.SDL_PixelFormat* pixelFormatPtr = (SDL.SDL_PixelFormat*)surPtr->format;
            // SurfaceFormat.Color is SDL_PIXELFORMAT_ARGB8888
            if (pixelFormatPtr->format != SDL.SDL_PIXELFORMAT_ARGB8888)
            {
                surface = SDL.SDL_ConvertSurfaceFormat(surface, SDL.SDL_PIXELFORMAT_ARGB8888, 0);
            }
            return surface;
        }

        private static unsafe IntPtr INTERNAL_getSurfacePixels(IntPtr surface)
        {
            IntPtr result;
            unsafe
            {
                SDL_Surface* surPtr = (SDL_Surface*) surface;
                result = surPtr->pixels;
            }
            return result;
        }

        private static unsafe int INTERNAL_getSurfaceWidth(IntPtr surface)
        {
            int result;
            unsafe
            {
                SDL_Surface* surPtr = (SDL_Surface*) surface;
                result = surPtr->w;
            }
            return result;
        }

        private static unsafe int INTERNAL_getSurfaceHeight(IntPtr surface)
        {
            int result;
            unsafe
            {
                SDL_Surface* surPtr = (SDL_Surface*) surface;
                result = surPtr->h;
            }
            return result;
        }

        #endregion
    }
}