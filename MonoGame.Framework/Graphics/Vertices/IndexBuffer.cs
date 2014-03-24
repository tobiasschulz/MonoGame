#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	public class IndexBuffer : GraphicsResource
	{
		#region Public Properties

		public BufferUsage BufferUsage
		{
			get;
			private set;
		}

		public int IndexCount
		{
			get;
			private set;
		}

		public IndexElementSize IndexElementSize
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

		public IndexBuffer(
			GraphicsDevice graphicsDevice,
			IndexElementSize indexElementSize,
			int indexCount,
			BufferUsage bufferUsage
		) : this(
			graphicsDevice,
			indexElementSize,
			indexCount,
			bufferUsage,
			false
		) {
		}

		public IndexBuffer(
			GraphicsDevice graphicsDevice,
			Type indexType,
			int indexCount,
			BufferUsage usage
		) : this(
			graphicsDevice,
			SizeForType(graphicsDevice, indexType),
			indexCount,
			usage,
			false
		) {
		}

		#endregion

		#region Protected Constructors

		protected IndexBuffer(
			GraphicsDevice graphicsDevice,
			Type indexType,
			int indexCount,
			BufferUsage usage,
			bool dynamic
		) : this(
			graphicsDevice,
			SizeForType(graphicsDevice, indexType),
			indexCount,
			usage,
			dynamic
		) {
		}

		protected IndexBuffer(
			GraphicsDevice graphicsDevice,
			IndexElementSize indexElementSize,
			int indexCount,
			BufferUsage usage,
			bool dynamic
		) {
			if (graphicsDevice == null)
			{
				throw new ArgumentNullException("GraphicsDevice is null");
			}

			GraphicsDevice = graphicsDevice;
			IndexElementSize = indexElementSize;
			IndexCount = indexCount;
			BufferUsage = usage;

			INTERNAL_isDynamic = dynamic;

			Threading.ForceToMainThread(() =>
			{
				Handle = GL.GenBuffer();

				OpenGLDevice.Instance.BindIndexBuffer(Handle);
				GL.BufferData(
					BufferTarget.ElementArrayBuffer,
					(IntPtr) (IndexCount * (IndexElementSize == IndexElementSize.SixteenBits ? 2 : 4)),
					IntPtr.Zero,
					INTERNAL_isDynamic ? BufferUsageHint.StreamDraw : BufferUsageHint.StaticDraw
				);
			});
		}

		#endregion

		#region Protected Dispose Method

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				GraphicsDevice.AddDisposeAction(() =>
				{
					OpenGLDevice.Instance.DeleteIndexBuffer(Handle);
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
				data.Length
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
				elementCount
			);
		}

		public void GetData<T>(
			int offsetInBytes,
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
			if (data == null)
			{
				throw new ArgumentNullException("data is null");
			}
			if (data.Length < (startIndex + elementCount))
			{
				throw new InvalidOperationException("The array specified in the data parameter is not the correct size for the amount of data requested.");
			}
			if (BufferUsage == BufferUsage.WriteOnly)
			{
				throw new NotSupportedException(
					"This IndexBuffer was created with a usage type of BufferUsage.WriteOnly. " +
					"Calling GetData on a resource that was created with BufferUsage.WriteOnly is not supported."
				);
			}

			Threading.ForceToMainThread(() =>
				GetBufferData(
					offsetInBytes,
					data,
					startIndex,
					elementCount
				)
			);
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
                SetDataOptions.None
            );
        }

        public void SetData<T>(
            T[] data,
            int startIndex,
            int elementCount
        ) where T : struct
        {
            SetDataInternal<T>(
                0,
                data,
                startIndex,
                elementCount,
                SetDataOptions.None
            );
        }

        public void SetData<T>(
            int offsetInBytes,
            T[] data,
            int startIndex,
            int elementCount
        ) where T : struct
        {
            SetDataInternal<T>(
                offsetInBytes,
                data,
                startIndex,
                elementCount,
                SetDataOptions.None
            );
        }

        #endregion

		#region Internal Master GetData Method

		private void GetBufferData<T>(
			int offsetInBytes,
			T[] data,
			int startIndex,
			int elementCount
		) where T : struct {
			OpenGLDevice.Instance.BindIndexBuffer(Handle);

			IntPtr ptr = GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.ReadOnly);

			// Pointer to the start of data to read in the index buffer
			ptr = new IntPtr(ptr.ToInt64() + offsetInBytes);

			// If data is already a byte[] we can skip the temporary buffer
			// Copy from the index buffer to the destination array
			if (typeof(T) == typeof(byte))
			{
				byte[] buffer = data as byte[];
				Marshal.Copy(ptr, buffer, 0, buffer.Length);
			}
			else
			{
				int elementSizeInBytes = Marshal.SizeOf(typeof(T));

				// Temporary buffer to store the copied section of data
				byte[] buffer = new byte[elementCount * elementSizeInBytes];
				// Copy from the index buffer to the temporary buffer
				Marshal.Copy(ptr, buffer, 0, buffer.Length);
				// Copy from the temporary buffer to the destination array
				Buffer.BlockCopy(buffer, 0, data, startIndex * elementSizeInBytes, elementCount * elementSizeInBytes);
			}

			GL.UnmapBuffer(BufferTarget.ArrayBuffer);
		}

		#endregion

		#region Internal Master SetData Methods

		protected void SetDataInternal<T>(
			int offsetInBytes,
			T[] data,
			int startIndex,
			int elementCount,
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

			Threading.ForceToMainThread(() =>
				BufferData(
					offsetInBytes,
					data,
					startIndex,
					elementCount,
					options
				)
			);
		}

		private void BufferData<T>(
			int offsetInBytes,
			T[] data,
			int startIndex,
			int elementCount,
			SetDataOptions options
		) where T : struct {
			int elementSizeInByte = Marshal.SizeOf(typeof(T));
			GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

			OpenGLDevice.Instance.BindIndexBuffer(Handle);

			if (options == SetDataOptions.Discard)
			{
				GL.BufferData(
					BufferTarget.ElementArrayBuffer,
					(IntPtr) (IndexCount * (IndexElementSize == IndexElementSize.SixteenBits ? 2 : 4)),
					IntPtr.Zero,
					INTERNAL_isDynamic ? BufferUsageHint.StreamDraw : BufferUsageHint.StaticDraw
				);
			}

			GL.BufferSubData(
				BufferTarget.ElementArrayBuffer,
				(IntPtr) offsetInBytes,
				(IntPtr) (elementSizeInByte * elementCount),
				(IntPtr) (dataHandle.AddrOfPinnedObject().ToInt64() + startIndex * elementSizeInByte)
			);

			dataHandle.Free();
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

		#region Private Type Size Calculator
		
		/// <summary>
		/// Gets the relevant IndexElementSize enum value for the given type.
		/// </summary>
		/// <param name="graphicsDevice">The graphics device.</param>
		/// <param name="type">The type to use for the index buffer</param>
		/// <returns>The IndexElementSize enum value that matches the type</returns>
		private static IndexElementSize SizeForType(GraphicsDevice graphicsDevice, Type type)
		{
			int sizeInBytes = Marshal.SizeOf(type);

			if (sizeInBytes == 2)
			{
				return IndexElementSize.SixteenBits;
			}
			if (sizeInBytes == 4)
			{
				if (graphicsDevice.GraphicsProfile == GraphicsProfile.Reach)
				{
					throw new NotSupportedException(
						"The profile does not support an elementSize of IndexElementSize.ThirtyTwoBits; " +
						"use IndexElementSize.SixteenBits or a type that has a size of two bytes."
					);
				}
				return IndexElementSize.ThirtyTwoBits;
			}

			throw new ArgumentOutOfRangeException("Index buffers can only be created for types that are sixteen or thirty two bits in length");
		}

		#endregion
	}
}
