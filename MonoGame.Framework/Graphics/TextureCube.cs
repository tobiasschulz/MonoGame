using System;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL;

namespace Microsoft.Xna.Framework.Graphics
{
    public class TextureCube : Texture
    {
        /// <summary>
        /// Gets the width and height of the cube map face in pixels.
        /// </summary>
        /// <value>The width and height of a cube map face in pixels.</value>
        public int Size
        {
            get;
            private set;
        }

        public TextureCube (GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format)
            : this(graphicsDevice, size, mipMap, format, false)
        {
        }

        internal TextureCube(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format, bool renderTarget)
        {
            if (graphicsDevice == null)
                throw new ArgumentNullException("graphicsDevice");

            this.GraphicsDevice = graphicsDevice;
            this.Size = size;
            this.Format = format;
            this.LevelCount = mipMap ? CalculateMipLevels(size) : 1;

            texture = new OpenGLDevice.OpenGLTexture(TextureTarget.TextureCubeMap, Format, LevelCount > 1);
            texture.WrapS.Set(TextureAddressMode.Clamp);
            texture.WrapT.Set(TextureAddressMode.Clamp);

            format.GetGLFormat (out glInternalFormat, out glFormat, out glType);

            for (int i = 0; i < 6; i += 1)
            {
                TextureTarget target = GetGLCubeFace((CubeMapFace)i);

                if (glFormat == (PixelFormat)All.CompressedTextureFormats)
                {
                    throw new NotImplementedException();
                }
                else 
                {
                    GL.TexImage2D (target, 0, glInternalFormat, size, size, 0, glFormat, glType, IntPtr.Zero);
                    GraphicsExtensions.CheckGLError();
                }
            }
            texture.Flush(true);

            if (mipMap)
            {
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.GenerateMipmap, (int)All.True);
                GraphicsExtensions.CheckGLError();
            }
        }

        /// <summary>
        /// Gets a copy of cube texture data specifying a cubemap face.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cubeMapFace">The cube map face.</param>
        /// <param name="data">The data.</param>
        public void GetData<T>(CubeMapFace cubeMapFace, T[] data) where T : struct
        {
            TextureTarget target = GetGLCubeFace(cubeMapFace);
            GL.BindTexture(target, texture.Handle);
            // 4 bytes per pixel
            if (data.Length < Size * Size * 4)
                throw new ArgumentException("data");

            GL.GetTexImage<T>(target, 0, PixelFormat.Bgra,
                PixelType.UnsignedByte, data);
        }

        public void SetData<T> (CubeMapFace face, T[] data) where T : struct
        {
            SetData(face, 0, null, data, 0, data.Length);
        }

        public void SetData<T>(CubeMapFace face, T[] data, int startIndex, int elementCount) where T : struct
        {
            SetData(face, 0, null, data, startIndex, elementCount);
        }

        public void SetData<T>(CubeMapFace face, int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
        {
            if (data == null) 
                throw new ArgumentNullException("data");

            var elementSizeInByte = Marshal.SizeOf(typeof(T));
            var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            // Use try..finally to make sure dataHandle is freed in case of an error
            try
            {
                var dataPtr = (IntPtr)(dataHandle.AddrOfPinnedObject().ToInt64() + startIndex * elementSizeInByte);

                int xOffset, yOffset, width, height;
                if (rect.HasValue)
                {
                    xOffset = rect.Value.X;
                    yOffset = rect.Value.Y;
                    width = rect.Value.Width;
                    height = rect.Value.Height;
                }
                else
                {
                    xOffset = 0;
                    yOffset = 0;
                    width = Math.Max(1, this.Size >> level);
                    height = Math.Max(1, this.Size >> level);

                    // For DXT textures the width and height of each level is a multiple of 4.
                    // OpenGL only: The last two mip levels require the width and height to be 
                    // passed as 2x2 and 1x1, but there needs to be enough data passed to occupy 
                    // a 4x4 block. 
                    // Ref: http://www.mentby.com/Group/mac-opengl/issue-with-dxt-mipmapped-textures.html 
                    if (Format == SurfaceFormat.Dxt1 ||
                        Format == SurfaceFormat.Dxt1a ||
                        Format == SurfaceFormat.Dxt3 ||
                        Format == SurfaceFormat.Dxt5)
                    {
                        if (width > 4)
                            width = (width + 3) & ~3;
                        if (height > 4)
                            height = (height + 3) & ~3;
                    }
                }

                GL.BindTexture(TextureTarget.TextureCubeMap, texture.Handle);
                GraphicsExtensions.CheckGLError();

                TextureTarget target = GetGLCubeFace(face);
                if (glFormat == (PixelFormat)All.CompressedTextureFormats)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    GL.TexSubImage2D(target, level, xOffset, yOffset, width, height, glFormat, glType, dataPtr);
                    GraphicsExtensions.CheckGLError();
                }
            }
            finally
            {
                dataHandle.Free();
            }
        }

        private TextureTarget GetGLCubeFace(CubeMapFace face)
        {
            switch (face)
            {
                case CubeMapFace.PositiveX: return TextureTarget.TextureCubeMapPositiveX;
                case CubeMapFace.NegativeX: return TextureTarget.TextureCubeMapNegativeX;
                case CubeMapFace.PositiveY: return TextureTarget.TextureCubeMapPositiveY;
                case CubeMapFace.NegativeY: return TextureTarget.TextureCubeMapNegativeY;
                case CubeMapFace.PositiveZ: return TextureTarget.TextureCubeMapPositiveZ;
                case CubeMapFace.NegativeZ: return TextureTarget.TextureCubeMapNegativeZ;
            }
            throw new ArgumentException();
        }
    }
}