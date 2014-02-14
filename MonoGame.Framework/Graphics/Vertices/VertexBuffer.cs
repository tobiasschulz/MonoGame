using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

#if OPENGL
#if SDL2
using OpenTK.Graphics.OpenGL;
#elif GLES
using OpenTK.Graphics.ES20;
using BufferTarget = OpenTK.Graphics.ES20.All;
using BufferUsageHint = OpenTK.Graphics.ES20.All;
#endif
#elif PSM
using Sce.PlayStation.Core.Graphics;
#endif

namespace Microsoft.Xna.Framework.Graphics
{
	public class VertexBuffer : GraphicsResource
    {
        internal bool _isDynamic;

		internal uint vbo;
	
		public int VertexCount { get; private set; }
		public VertexDeclaration VertexDeclaration { get; private set; }
		public BufferUsage BufferUsage { get; private set; }
		
		protected VertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage bufferUsage, bool dynamic)
		{
			if (graphicsDevice == null)
                throw new ArgumentNullException("Graphics Device Cannot Be null");

            this.GraphicsDevice = graphicsDevice;
            this.VertexDeclaration = vertexDeclaration;
            this.VertexCount = vertexCount;
            this.BufferUsage = bufferUsage;

            // Make sure the graphics device is assigned in the vertex declaration.
            if (vertexDeclaration.GraphicsDevice != graphicsDevice)
                vertexDeclaration.GraphicsDevice = graphicsDevice;

            _isDynamic = dynamic;
            Threading.BlockOnUIThread(GenerateIfRequired);
		}

        public VertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage bufferUsage) :
			this(graphicsDevice, vertexDeclaration, vertexCount, bufferUsage, false)
        {
        }
		
		public VertexBuffer(GraphicsDevice graphicsDevice, Type type, int vertexCount, BufferUsage bufferUsage) :
			this(graphicsDevice, VertexDeclaration.FromType(type), vertexCount, bufferUsage, false)
		{
        }

        /// <summary>
        /// The GraphicsDevice is resetting, so GPU resources must be recreated.
        /// </summary>
        internal protected override void GraphicsDeviceResetting()
        {
            vbo = 0;
        }

        /// <summary>
        /// If the VBO does not exist, create it.
        /// </summary>
        private void GenerateIfRequired()
        {
            if (vbo == 0)
            {
                GL.GenBuffers(1, out this.vbo);
                GraphicsExtensions.CheckGLError();
                GL.BindBuffer(BufferTarget.ArrayBuffer, this.vbo);
                GraphicsExtensions.CheckGLError();
                OpenGLDevice.Instance.BindVertexBuffer(vbo);
                GL.BufferData(BufferTarget.ArrayBuffer,
                              new IntPtr(VertexDeclaration.VertexStride * VertexCount), IntPtr.Zero,
                              _isDynamic ? BufferUsageHint.StreamDraw : BufferUsageHint.StaticDraw);
                GraphicsExtensions.CheckGLError();
            }
        }

        public void GetData<T> (int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride) where T : struct
        {
            if (data == null)
                throw new ArgumentNullException("data", "This method does not accept null for this parameter.");
            if (data.Length < (startIndex + elementCount))
                throw new ArgumentOutOfRangeException("elementCount", "This parameter must be a valid index within the array.");
            if (BufferUsage == BufferUsage.WriteOnly)
                throw new NotSupportedException("Calling GetData on a resource that was created with BufferUsage.WriteOnly is not supported.");
			if ((elementCount * vertexStride) > (VertexCount * VertexDeclaration.VertexStride))
                throw new InvalidOperationException("The array is not the correct size for the amount of data requested.");

            if (Threading.IsOnUIThread())
            {
                GetBufferData(offsetInBytes, data, startIndex, elementCount, vertexStride);
            }
            else
            {
                Threading.BlockOnUIThread (() => GetBufferData(offsetInBytes, data, startIndex, elementCount, vertexStride));
            }
        }

        private void GetBufferData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride) where T : struct
        {
            OpenGLDevice.Instance.BindVertexBuffer(vbo);
            var elementSizeInByte = Marshal.SizeOf(typeof(T));
            IntPtr ptr = GL.MapBuffer (BufferTarget.ArrayBuffer, BufferAccess.ReadOnly);
            GraphicsExtensions.CheckGLError();
            // Pointer to the start of data to read in the index buffer
            ptr = new IntPtr (ptr.ToInt64 () + offsetInBytes);
			if (typeof(T) == typeof(byte)) {
                byte[] buffer = data as byte[];
                // If data is already a byte[] we can skip the temporary buffer
                // Copy from the vertex buffer to the destination array
                Marshal.Copy (ptr, buffer, 0, buffer.Length);
            } else {
                // Temporary buffer to store the copied section of data
                byte[] buffer = new byte[elementCount * vertexStride - offsetInBytes];
                // Copy from the vertex buffer to the temporary buffer
                Marshal.Copy(ptr, buffer, 0, buffer.Length);
                
                var dataHandle = GCHandle.Alloc (data, GCHandleType.Pinned);
                var dataPtr = (IntPtr)(dataHandle.AddrOfPinnedObject ().ToInt64 () + startIndex * elementSizeInByte);
                
                // Copy from the temporary buffer to the destination array
                
                int dataSize = Marshal.SizeOf(typeof(T));
                if (dataSize == vertexStride)
                    Marshal.Copy(buffer, 0, dataPtr, buffer.Length);
                else
                {
                    // If the user is asking for a specific element within the vertex buffer, copy them one by one...
                    for (int i = 0; i < elementCount; i++)
                    {
                        Marshal.Copy(buffer, i * vertexStride, dataPtr, dataSize);
                        dataPtr = (IntPtr)(dataPtr.ToInt64() + dataSize);
                    }
                }
                
                dataHandle.Free ();
                
                //Buffer.BlockCopy(buffer, 0, data, startIndex * elementSizeInByte, elementCount * elementSizeInByte);
            }
            GL.UnmapBuffer(BufferTarget.ArrayBuffer);
            }
        
        public void GetData<T>(T[] data, int startIndex, int elementCount) where T : struct
        {
            var elementSizeInByte = Marshal.SizeOf(typeof(T));
            this.GetData<T>(0, data, startIndex, elementCount, elementSizeInByte);
        }

        public void GetData<T>(T[] data) where T : struct
        {
            var elementSizeInByte = Marshal.SizeOf(typeof(T));
            this.GetData<T>(0, data, 0, data.Count(), elementSizeInByte);
        }

        public void SetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride) where T : struct
        {
            SetDataInternal<T>(offsetInBytes, data, startIndex, elementCount, VertexDeclaration.VertexStride, SetDataOptions.None);
        }
        		
		public void SetData<T>(T[] data, int startIndex, int elementCount) where T : struct
        {
            SetDataInternal<T>(0, data, startIndex, elementCount, VertexDeclaration.VertexStride, SetDataOptions.None);
		}
		
        public void SetData<T>(T[] data) where T : struct
        {
            SetDataInternal<T>(0, data, 0, data.Length, VertexDeclaration.VertexStride, SetDataOptions.None);
        }

        protected void SetDataInternal<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride, SetDataOptions options) where T : struct
        {
            if (data == null)
                throw new ArgumentNullException("data is null");
            if (data.Length < (startIndex + elementCount))
                throw new InvalidOperationException("The array specified in the data parameter is not the correct size for the amount of data requested.");

            var bufferSize = VertexCount * VertexDeclaration.VertexStride;
            if ((vertexStride > bufferSize) || (vertexStride < VertexDeclaration.VertexStride))
                throw new ArgumentOutOfRangeException("One of the following conditions is true:\nThe vertex stride is larger than the vertex buffer.\nThe vertex stride is too small for the type of data requested.");
   
            var elementSizeInBytes = Marshal.SizeOf(typeof(T));

            if (Threading.IsOnUIThread())
            {
                SetBufferData(bufferSize, elementSizeInBytes, offsetInBytes, data, startIndex, elementCount, vertexStride, options);
            }
            else
            {
                Threading.BlockOnUIThread(() => SetBufferData(bufferSize, elementSizeInBytes, offsetInBytes, data, startIndex, elementCount, vertexStride, options));
            }
        }

        private void SetBufferData<T>(int bufferSize, int elementSizeInBytes, int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride, SetDataOptions options) where T : struct
        {
            GenerateIfRequired();
            
            var sizeInBytes = elementSizeInBytes * elementCount;
            OpenGLDevice.Instance.BindVertexBuffer(vbo);
            
            if (options == SetDataOptions.Discard)
            {
                // By assigning NULL data to the buffer this gives a hint
                // to the device to discard the previous content.
                GL.BufferData(  BufferTarget.ArrayBuffer,
                              (IntPtr)bufferSize, 
                              IntPtr.Zero,
                              _isDynamic ? BufferUsageHint.StreamDraw : BufferUsageHint.StaticDraw);
                GraphicsExtensions.CheckGLError();
            }

            var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var dataPtr = (IntPtr)(dataHandle.AddrOfPinnedObject().ToInt64() + startIndex * elementSizeInBytes);

            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)offsetInBytes, (IntPtr)sizeInBytes, dataPtr);
            GraphicsExtensions.CheckGLError();

            dataHandle.Free();
        }
        
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                GraphicsDevice.AddDisposeAction(() =>
                    {
                        GL.DeleteBuffers(1, ref vbo);
                        GraphicsExtensions.CheckGLError();
                    });
            }
            base.Dispose(disposing);
		}
    }
}
