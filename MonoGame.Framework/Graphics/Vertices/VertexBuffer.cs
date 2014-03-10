#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE.txt for details.
 */
#endregion

#region Using Statements
using System;
using System.Linq;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	public class VertexBuffer : GraphicsResource
	{
		#region Public Properties

		public BufferUsage BufferUsage
		{
			get;
			private set;
		}

		public int VertexCount
		{
			get;
			private set;
		}

		public VertexDeclaration VertexDeclaration
		{
			get;
			private set;
		}

		#endregion

		#region Internal Properties

		internal int Handle
		{
			get;
			private set;
		}

		#endregion

		#region Private Variables

		private bool INTERNAL_isDynamic;

		#endregion

		#region Public Constructors

		public VertexBuffer(
			GraphicsDevice graphicsDevice,
			VertexDeclaration vertexDeclaration,
			int vertexCount,
			BufferUsage bufferUsage
		) : this(
			graphicsDevice,
			vertexDeclaration,
			vertexCount,
			bufferUsage,
			false
		) {
		}

		public VertexBuffer(
			GraphicsDevice graphicsDevice,
			Type type,
			int vertexCount,
			BufferUsage bufferUsage
		) : this(
			graphicsDevice,
			VertexDeclaration.FromType(type),
			vertexCount,
			bufferUsage,
			false
		) {
		}

		#endregion

		#region Protected Constructor

		protected VertexBuffer(
			GraphicsDevice graphicsDevice,
			VertexDeclaration vertexDeclaration,
			int vertexCount,
			BufferUsage bufferUsage,
			bool dynamic
		) {
			if (graphicsDevice == null)
			{
				throw new ArgumentNullException("GraphicsDevice cannot be null");
			}

			GraphicsDevice = graphicsDevice;
			VertexDeclaration = vertexDeclaration;
			VertexCount = vertexCount;
			BufferUsage = bufferUsage;

			// Make sure the graphics device is assigned in the vertex declaration.
			if (vertexDeclaration.GraphicsDevice != graphicsDevice)
			{
				vertexDeclaration.GraphicsDevice = graphicsDevice;
			}

			INTERNAL_isDynamic = dynamic;
			Threading.BlockOnUIThread(GenerateIfRequired);
		}

		#endregion

		#region Protected Dispose Method

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				GraphicsDevice.AddDisposeAction(() =>
				{
					OpenGLDevice.Instance.DeleteVertexBuffer(Handle);
					GraphicsExtensions.CheckGLError();
				});
			}
			base.Dispose(disposing);
		}

		#endregion

		#region Public GetData Methods

		public void GetData<T>(T[] data) where T : struct
		{
			GetData<T>(
				0,
				data,
				0,
				data.Count(),
				Marshal.SizeOf(typeof(T))
			);
		}

		public void GetData<T>(
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
			GetData<T>(
				0,
				data,
				startIndex,
				elementCount,
				Marshal.SizeOf(typeof(T))
			);
		}

		public void GetData<T>(
			int offsetInBytes,
			T[] data,
			int startIndex,
			int elementCount,
			int vertexStride
		) where T : struct {
			if (data == null)
			{
				throw new ArgumentNullException(
					"data",
					"This method does not accept null for this parameter."
				);
			}
			if (data.Length < (startIndex + elementCount))
			{
				throw new ArgumentOutOfRangeException(
					"elementCount",
					"This parameter must be a valid index within the array."
				);
			}
			if (BufferUsage == BufferUsage.WriteOnly)
			{
				throw new NotSupportedException("Calling GetData on a resource that was created with BufferUsage.WriteOnly is not supported.");
			}
			if ((elementCount * vertexStride) > (VertexCount * VertexDeclaration.VertexStride))
			{
				throw new InvalidOperationException("The array is not the correct size for the amount of data requested.");
			}

			if (Threading.IsOnUIThread())
			{
				GetBufferData(offsetInBytes, data, startIndex, elementCount, vertexStride);
			}
			else
			{
				Threading.BlockOnUIThread(() => GetBufferData(offsetInBytes, data, startIndex, elementCount, vertexStride));
			}
		}

		#endregion

		#region Internal Master GetData Method

		private void GetBufferData<T>(
			int offsetInBytes,
			T[] data,
			int startIndex,
			int elementCount,
			int vertexStride
		) where T : struct {
			OpenGLDevice.Instance.BindVertexBuffer(Handle);

			IntPtr ptr = GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.ReadOnly);
			GraphicsExtensions.CheckGLError();

			// Pointer to the start of data to read in the index buffer
			ptr = new IntPtr(ptr.ToInt64() + offsetInBytes);

			if (typeof(T) == typeof(byte))
			{
				// If data is already a byte[] we can skip the temporary buffer.
				// Copy from the vertex buffer to the destination array.
				byte[] buffer = data as byte[];
				Marshal.Copy(ptr, buffer, 0, buffer.Length);
			}
			else
			{
				// Temporary buffer to store the copied section of data
				byte[] buffer = new byte[elementCount * vertexStride - offsetInBytes];

				// Copy from the vertex buffer to the temporary buffer
				Marshal.Copy(ptr, buffer, 0, buffer.Length);
				
				var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
				var dataPtr = (IntPtr) (dataHandle.AddrOfPinnedObject().ToInt64() + startIndex * Marshal.SizeOf(typeof(T)));
				
				// Copy from the temporary buffer to the destination array
				int dataSize = Marshal.SizeOf(typeof(T));
				if (dataSize == vertexStride)
				{
					Marshal.Copy(buffer, 0, dataPtr, buffer.Length);
				}
				else
				{
					// If the user is asking for a specific element within the vertex buffer, copy them one by one...
					for (int i = 0; i < elementCount; i++)
					{
						Marshal.Copy(buffer, i * vertexStride, dataPtr, dataSize);
						dataPtr = (IntPtr)(dataPtr.ToInt64() + dataSize);
					}
				}
				
				dataHandle.Free();
				
				//Buffer.BlockCopy(buffer, 0, data, startIndex * elementSizeInByte, elementCount * elementSizeInByte);
			}

			GL.UnmapBuffer(BufferTarget.ArrayBuffer);
			GraphicsExtensions.CheckGLError();
		}

		#endregion

		#region Public SetData Methods

		public void SetData<T>(T[] data) where T : struct
		{
			SetDataInternal<T>(
				0,
				data,
				0,
				data.Length,
				VertexDeclaration.VertexStride,
				SetDataOptions.None
			);
		}

		public void SetData<T>(
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
			SetDataInternal<T>(
				0,
				data,
				startIndex,
				elementCount,
				VertexDeclaration.VertexStride,
				SetDataOptions.None
			);
		}

		public void SetData<T>(
			int offsetInBytes,
			T[] data,
			int startIndex,
			int elementCount,
			int vertexStride
		) where T : struct {
			SetDataInternal<T>(
				offsetInBytes,
				data,
				startIndex,
				elementCount,
				VertexDeclaration.VertexStride,
				SetDataOptions.None
			);
		}

		#endregion

		#region Internal Master SetData Methods

		protected void SetDataInternal<T>(
			int offsetInBytes,
			T[] data,
			int startIndex,
			int elementCount,
			int vertexStride,
			SetDataOptions options
		) where T : struct {
			if (data == null)
			{
				throw new ArgumentNullException("data is null");
			}
			if (data.Length < (startIndex + elementCount))
			{
				throw new InvalidOperationException("The array specified in the data parameter is not the correct size for the amount of data requested.");
			}

			int bufferSize = VertexCount * VertexDeclaration.VertexStride;

			if (	vertexStride > bufferSize ||
			vertexStride < VertexDeclaration.VertexStride	)
			{
				throw new ArgumentOutOfRangeException(
					"One of the following conditions is true:\n" +
					"The vertex stride is larger than the vertex buffer.\n" +
					"The vertex stride is too small for the type of data requested."
				);
			}

			int elementSizeInBytes = Marshal.SizeOf(typeof(T));

			if (Threading.IsOnUIThread())
			{
				SetBufferData(
					bufferSize,
					elementSizeInBytes,
					offsetInBytes,
					data,
					startIndex,
					elementCount,
					vertexStride,
					options
				);
			}
			else
			{
				Threading.BlockOnUIThread(() =>
					SetBufferData(
						bufferSize,
						elementSizeInBytes,
						offsetInBytes,
						data,
						startIndex,
						elementCount,
						vertexStride,
						options
					)
				);
			}
		}

		private void SetBufferData<T>(
			int bufferSize,
			int elementSizeInBytes,
			int offsetInBytes,
			T[] data,
			int startIndex,
			int elementCount,
			int vertexStride,
			SetDataOptions options
		) where T : struct {
			GenerateIfRequired();

			OpenGLDevice.Instance.BindVertexBuffer(Handle);

			int sizeInBytes = elementSizeInBytes * elementCount;
			
			if (options == SetDataOptions.Discard)
			{
				GL.BufferData(
					BufferTarget.ArrayBuffer,
					(IntPtr) bufferSize,
					IntPtr.Zero,
					INTERNAL_isDynamic ? BufferUsageHint.StreamDraw : BufferUsageHint.StaticDraw
				);
				GraphicsExtensions.CheckGLError();
			}

			GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
			IntPtr dataPtr = (IntPtr) (dataHandle.AddrOfPinnedObject().ToInt64() + startIndex * elementSizeInBytes);

			GL.BufferSubData(
				BufferTarget.ArrayBuffer,
				(IntPtr) offsetInBytes,
				(IntPtr) sizeInBytes,
				dataPtr
			);
			GraphicsExtensions.CheckGLError();

			dataHandle.Free();
		}

		#endregion

		#region Private GenBuffer Method

		/// <summary>
		/// If the VBO does not exist, create it.
		/// </summary>
		private void GenerateIfRequired()
		{
			if (Handle == 0)
			{
				Handle = GL.GenBuffer();
				GraphicsExtensions.CheckGLError();

				OpenGLDevice.Instance.BindVertexBuffer(Handle);
				GL.BufferData(
					BufferTarget.ArrayBuffer,
					new IntPtr(VertexDeclaration.VertexStride * VertexCount),
					IntPtr.Zero,
					INTERNAL_isDynamic ? BufferUsageHint.StreamDraw : BufferUsageHint.StaticDraw
				);
				GraphicsExtensions.CheckGLError();
			}
		}

		#endregion

		#region Internal Context Reset Method

		/// <summary>
		/// The GraphicsDevice is resetting, so GPU resources must be recreated.
		/// </summary>
		internal protected override void GraphicsDeviceResetting()
		{
			// FIXME: Do we even want to bother with DeviceResetting for GL? -flibit
		}

		#endregion
	}
}
