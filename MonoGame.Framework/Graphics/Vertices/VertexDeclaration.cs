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
using System.Collections.Generic;

using OpenTK.Graphics.OpenGL;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	public class VertexDeclaration : GraphicsResource
	{
		#region Public Properties

		public int VertexStride
		{
			get;
			private set;
		}

		#endregion

		#region Private Variables

		private VertexElement[] elements;

		private Dictionary<int, VertexDeclarationAttributeInfo> shaderAttributeInfo = new Dictionary<int, VertexDeclarationAttributeInfo>();

		#endregion

		#region Public Constructors

		public VertexDeclaration(
			params VertexElement[] elements
		) : this(GetVertexStride(elements), elements) {
		}

		public VertexDeclaration(
			int vertexStride,
			params VertexElement[] elements
		) {
			if ((elements == null) || (elements.Length == 0))
			{
				throw new ArgumentNullException("elements", "Elements cannot be empty");
			}

			this.elements = (VertexElement[]) elements.Clone();
			VertexStride = vertexStride;
		}

		#endregion

		#region Public Methods

		public VertexElement[] GetVertexElements()
		{
			return (VertexElement[]) elements.Clone();
		}

		#endregion

		#region Internal Methods

		internal void Apply(Shader shader, IntPtr offset, int divisor = 0)
		{
			VertexDeclarationAttributeInfo attrInfo;
			int shaderHash = shader.GetHashCode();
			if (!shaderAttributeInfo.TryGetValue(shaderHash, out attrInfo))
			{
				// Get the vertex attribute info and cache it
				attrInfo = new VertexDeclarationAttributeInfo(OpenGLDevice.Instance.MaxVertexAttributes);

				foreach (VertexElement ve in elements)
				{
					int attributeLocation = shader.GetAttribLocation(ve.VertexElementUsage, ve.UsageIndex);

					// XNA appears to ignore usages it can't find a match for, so we will do the same
					if (attributeLocation >= 0)
					{
						attrInfo.Elements.Add(new VertexDeclarationAttributeInfo.Element()
						{
							Offset = ve.Offset,
							AttributeLocation = attributeLocation,
							NumberOfElements = OpenGLNumberOfElements(ve.VertexElementFormat),
							VertexAttribPointerType = OpenGLVertexAttribPointerType(ve.VertexElementFormat),
							Normalized = OpenGLVertexAttribNormalized(ve),
						});
						attrInfo.EnabledAttributes[attributeLocation] = true;
					}
				}

				shaderAttributeInfo.Add(shaderHash, attrInfo);
			}

			// Apply the vertex attribute info
			foreach (VertexDeclarationAttributeInfo.Element element in attrInfo.Elements)
			{
				OpenGLDevice.Instance.AttributeEnabled[element.AttributeLocation] = true;
				OpenGLDevice.Instance.Attributes[element.AttributeLocation].Divisor.Set(divisor);
				OpenGLDevice.Instance.VertexAttribPointer(
					element.AttributeLocation,
					element.NumberOfElements,
					element.VertexAttribPointerType,
					element.Normalized,
					VertexStride,
					(IntPtr)(offset.ToInt64() + element.Offset)
				);
			}
		}

		#endregion

		#region Internal Static Methods

		/// <summary>
		/// Returns the VertexDeclaration for Type.
		/// </summary>
		/// <param name="vertexType">A value type which implements the IVertexType interface.</param>
		/// <returns>The VertexDeclaration.</returns>
		/// <remarks>
		/// Prefer to use VertexDeclarationCache when the declaration lookup
		/// can be performed with a templated type.
		/// </remarks>
		internal static VertexDeclaration FromType(Type vertexType)
		{
			if (vertexType == null)
			{
				throw new ArgumentNullException("vertexType", "Cannot be null");
			}

			if (!vertexType.IsValueType)
			{
				throw new ArgumentException("vertexType", "Must be value type");
			}

			IVertexType type = Activator.CreateInstance(vertexType) as IVertexType;
			if (type == null)
			{
				throw new ArgumentException("vertexData does not inherit IVertexType");
			}

			VertexDeclaration vertexDeclaration = type.VertexDeclaration;
			if (vertexDeclaration == null)
			{
				throw new Exception("VertexDeclaration cannot be null");
			}

			return vertexDeclaration;
		}

		#endregion

		#region Private Static VertexElement Methods

		private static int GetVertexStride(VertexElement[] elements)
		{
			int max = 0;

			for (int i = 0; i < elements.Length; i += 1)
			{
				int start = elements[i].Offset + GetTypeSize(elements[i].VertexElementFormat);
				if (max < start)
				{
					max = start;
				}
			}

			return max;
		}

		private static int GetTypeSize(VertexElementFormat elementFormat)
		{
			switch (elementFormat)
			{
				case VertexElementFormat.Single:
					return 4;
				case VertexElementFormat.Vector2:
					return 8;
				case VertexElementFormat.Vector3:
					return 12;
				case VertexElementFormat.Vector4:
					return 16;
				case VertexElementFormat.Color:
					return 4;
				case VertexElementFormat.Byte4:
					return 4;
				case VertexElementFormat.Short2:
					return 4;
				case VertexElementFormat.Short4:
					return 8;
				case VertexElementFormat.NormalizedShort2:
					return 4;
				case VertexElementFormat.NormalizedShort4:
					return 8;
				case VertexElementFormat.HalfVector2:
					return 4;
				case VertexElementFormat.HalfVector4:
					return 8;
			}
			return 0;
		}

		#endregion

		#region Private Static OpenGL VertexElement Info Methods

		private static int OpenGLNumberOfElements(VertexElementFormat elementFormat)
		{
			switch (elementFormat)
			{
				case VertexElementFormat.Single:
					return 1;
				case VertexElementFormat.Vector2:
					return 2;
				case VertexElementFormat.Vector3:
					return 3;
				case VertexElementFormat.Vector4:
					return 4;
				case VertexElementFormat.Color:
					return 4;
				case VertexElementFormat.Byte4:
					return 4;
				case VertexElementFormat.Short2:
					return 2;
				case VertexElementFormat.Short4:
					return 2;
				case VertexElementFormat.NormalizedShort2:
					return 2;
				case VertexElementFormat.NormalizedShort4:
					return 4;
				case VertexElementFormat.HalfVector2:
					return 2;
				case VertexElementFormat.HalfVector4:
					return 4;
			}

			throw new ArgumentException();
		}

		private static VertexAttribPointerType OpenGLVertexAttribPointerType(VertexElementFormat elementFormat)
		{
			switch (elementFormat)
			{
				case VertexElementFormat.Single:
					return VertexAttribPointerType.Float;
				case VertexElementFormat.Vector2:
					return VertexAttribPointerType.Float;
				case VertexElementFormat.Vector3:
					return VertexAttribPointerType.Float;
				case VertexElementFormat.Vector4:
					return VertexAttribPointerType.Float;
				case VertexElementFormat.Color:
					return VertexAttribPointerType.UnsignedByte;
				case VertexElementFormat.Byte4:
					return VertexAttribPointerType.UnsignedByte;
				case VertexElementFormat.Short2:
					return VertexAttribPointerType.Short;
				case VertexElementFormat.Short4:
					return VertexAttribPointerType.Short;
				case VertexElementFormat.NormalizedShort2:
					return VertexAttribPointerType.Short;
				case VertexElementFormat.NormalizedShort4:
					return VertexAttribPointerType.Short;
				case VertexElementFormat.HalfVector2:
					return VertexAttribPointerType.HalfFloat;
				case VertexElementFormat.HalfVector4:
					return VertexAttribPointerType.HalfFloat;
			}

			throw new ArgumentException();
		}

		private static bool OpenGLVertexAttribNormalized(VertexElement element)
		{
			/* TODO: This may or may not be the right behavior.
			 *
			 * For instance the VertexElementFormat.Byte4 format is not supposed
			 * to be normalized, but this line makes it so.
			 *
			 * The question is in MS XNA are types normalized based on usage or
			 * normalized based to their format?
			 */
			if (element.VertexElementUsage == VertexElementUsage.Color)
			{
				return true;
			}

			switch (element.VertexElementFormat)
			{
				case VertexElementFormat.NormalizedShort2:
				case VertexElementFormat.NormalizedShort4:
					return true;

				default:
					return false;
			}
		}

		#endregion

		#region Private OpenGL VertexDeclarationAttributeInfo Class

		/// <summary>
		/// Vertex attribute information for a particular shader/vertex declaration combination.
		/// </summary>
		class VertexDeclarationAttributeInfo
		{
			internal bool[] EnabledAttributes;

			internal class Element
			{
				public int Offset;
				public int AttributeLocation;
				public int NumberOfElements;
				public VertexAttribPointerType VertexAttribPointerType;
				public bool Normalized;
			}

			internal List<Element> Elements;

			internal VertexDeclarationAttributeInfo(int maxVertexAttributes)
			{
				EnabledAttributes = new bool[maxVertexAttributes];
				Elements = new List<Element>();
			}
		}

		#endregion
	}
}
