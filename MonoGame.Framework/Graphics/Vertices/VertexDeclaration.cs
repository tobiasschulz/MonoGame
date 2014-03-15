#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using OpenTK.Graphics.OpenGL;

namespace Microsoft.Xna.Framework.Graphics
{
	public class VertexDeclaration : GraphicsResource
	{
		private VertexElement[] _elements;
        private int _vertexStride;

        Dictionary<int, VertexDeclarationAttributeInfo> shaderAttributeInfo = new Dictionary<int, VertexDeclarationAttributeInfo>();

        /// <summary>
        /// A hash value which can be used to compare declarations.
        /// </summary>
        internal int HashKey { get; private set; }


		public VertexDeclaration(params VertexElement[] elements)
            : this( GetVertexStride(elements), elements)
		{
		}

        public VertexDeclaration(int vertexStride, params VertexElement[] elements)
        {
            if ((elements == null) || (elements.Length == 0))
                throw new ArgumentNullException("elements", "Elements cannot be empty");

            var elementArray = (VertexElement[])elements.Clone();
            _elements = elementArray;
            _vertexStride = vertexStride;

            // TODO: Is there a faster/better way to generate a
            // unique hashkey for the same vertex layouts?
            {
                var signature = string.Empty;
                foreach (var element in _elements)
                    signature += element;

                var bytes = System.Text.Encoding.UTF8.GetBytes(signature);
                HashKey = MonoGame.Utilities.Hash.ComputeHash(bytes);
            }
        }

		private static int GetVertexStride(VertexElement[] elements)
		{
			int max = 0;
			for (var i = 0; i < elements.Length; i++)
			{
                var start = elements[i].Offset + elements[i].VertexElementFormat.GetTypeSize();
				if (max < start)
					max = start;
			}

			return max;
		}

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
				throw new ArgumentNullException("vertexType", "Cannot be null");

            if (!vertexType.IsValueType)
            {
				throw new ArgumentException("vertexType", "Must be value type");
			}

            var type = Activator.CreateInstance(vertexType) as IVertexType;
			if (type == null)
			{
				throw new ArgumentException("vertexData does not inherit IVertexType");
			}

            var vertexDeclaration = type.VertexDeclaration;
			if (vertexDeclaration == null)
			{
				throw new Exception("VertexDeclaration cannot be null");
			}

			return vertexDeclaration;
		}
        
        public VertexElement[] GetVertexElements()
		{
			return (VertexElement[])_elements.Clone();
		}

		public int VertexStride
		{
			get
			{
				return _vertexStride;
			}
		}

		internal void Apply(Shader shader, IntPtr offset, int divisor = 0)
		{
            VertexDeclarationAttributeInfo attrInfo;
            int shaderHash = shader.GetHashCode();
            if (!shaderAttributeInfo.TryGetValue(shaderHash, out attrInfo))
            {
                // Get the vertex attribute info and cache it
                attrInfo = new VertexDeclarationAttributeInfo(OpenGLDevice.Instance.MaxVertexAttributes);

                foreach (var ve in _elements)
                {
                    var attributeLocation = shader.GetAttribLocation(ve.VertexElementUsage, ve.UsageIndex);
                    // XNA appears to ignore usages it can't find a match for, so we will do the same
                    if (attributeLocation >= 0)
                    {
                        attrInfo.Elements.Add(new VertexDeclarationAttributeInfo.Element()
                        {
                            Offset = ve.Offset,
                            AttributeLocation = attributeLocation,
                            NumberOfElements = ve.VertexElementFormat.OpenGLNumberOfElements(),
                            VertexAttribPointerType = ve.VertexElementFormat.OpenGLVertexAttribPointerType(),
                            Normalized = ve.OpenGLVertexAttribNormalized(),
                        });
                        attrInfo.EnabledAttributes[attributeLocation] = true;
                    }
                }

                shaderAttributeInfo.Add(shaderHash, attrInfo);
            }

            // Apply the vertex attribute info
            foreach (var element in attrInfo.Elements)
            {
                OpenGLDevice.Instance.AttributeEnabled[element.AttributeLocation] = true;
                OpenGLDevice.Instance.Attributes[element.AttributeLocation].Divisor.Set(divisor);
                OpenGLDevice.Instance.VertexAttribPointer(
                    element.AttributeLocation,
                    element.NumberOfElements,
                    element.VertexAttribPointerType,
                    element.Normalized,
                    VertexStride,
                    (IntPtr) (offset.ToInt64() + element.Offset)
                );
            }
		}

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
    }
}