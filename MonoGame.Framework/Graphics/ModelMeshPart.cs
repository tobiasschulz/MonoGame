#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	public sealed class ModelMeshPart
	{
		#region Public Properties

		public Effect Effect
		{
			get
			{
				return _effect;
			}
			set
			{
				if (value == _effect)
				{
					return;
				}

				if (_effect != null)
				{
					// First check to see any other parts are also using this effect.
					bool removeEffect = true;
					foreach (ModelMeshPart part in parent.MeshParts)
					{
						if (part != this && part._effect == _effect)
						{
							removeEffect = false;
							break;
						}
					}

					if (removeEffect)
					{
						parent.Effects.Remove(_effect);
					}
				}

				// Set the new effect.
				_effect = value;
				parent.Effects.Add(value);
			}
		}

		/// <summary>
		/// Gets the index buffer for this mesh part.
		/// </summary>
		public IndexBuffer IndexBuffer { get; set; }

		/// <summary>
		/// Gets the number of vertices used during a draw call.
		/// </summary>
		public int NumVertices
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the number of primitives to render.
		/// </summary>
		public int PrimitiveCount
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the location in the index array at which to start reading vertices.
		/// </summary>
		public int StartIndex
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets an object identifying this model mesh part.
		/// </summary>
		public object Tag
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the vertex buffer for this mesh part.
		/// </summary>
		public VertexBuffer VertexBuffer
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the offset (in vertices) from the top of vertex buffer.
		/// </summary>
		public int VertexOffset
		{
			get;
			set;
		}

		#endregion

		#region Internal Properties

		internal int VertexBufferIndex
		{
			get;
			set;
		}

		internal int IndexBufferIndex
		{
			get;
			set;
		}

		internal int EffectIndex
		{
			get;
			set;
		}

		#endregion

		#region Private Variables

		/// <summary>
		/// Gets or sets the material Effect for this mesh part. Reference page contains
		/// code sample.
		private Effect _effect;

		#endregion

		#region Internal Variables

		internal ModelMesh parent;

		#endregion
	}
}