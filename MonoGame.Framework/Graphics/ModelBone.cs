#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System.Collections.Generic;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	/// <summary>
	/// Represents bone data for a model. Reference page contains links to related
	/// conceptual articles.
	/// </summary>
	public sealed class ModelBone
	{
		#region Public Properties

		public List<ModelMesh> Meshes
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets a collection of bones that are children of this bone.
		/// </summary>
		public ModelBoneCollection Children
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the index of this bone in the Bones collection.
		/// </summary>
		public int Index
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets the name of this bone.
		/// </summary>
		public string Name
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets the parent of this bone.
		/// </summary>
		public ModelBone Parent
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets or sets the matrix used to transform this bone relative to its parent
		/// bone.
		/// </summary>
		public Matrix Transform
		{
			get;
			set;
		}

		#endregion

		#region Internal Properties

		/// <summary>
		/// Transform of this node from the root of the model not from the parent
		/// </summary>
		internal Matrix ModelTransform
		{
			get;
			set;
		}

		#endregion

		#region Private Variables

		private List<ModelBone> children = new List<ModelBone>();

		#endregion

		#region Public Constructor

		public ModelBone()
		{
			Children = new ModelBoneCollection(new List<ModelBone>());
			Meshes = new List<ModelMesh>();
		}

		#endregion

		#region Public Methods

		public void AddMesh(ModelMesh mesh)
		{
			Meshes.Add(mesh);
		}

		public void AddChild(ModelBone modelBone)
		{
			children.Add(modelBone);
			Children = new ModelBoneCollection(children);
		}

		#endregion
	}
}
