#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	// Summary:
	//     Represents bone data for a model. Reference page contains links to related
	//     conceptual articles.
	public sealed class ModelBone
    {

        #region Public Properties

        public List<ModelMesh> Meshes
        {
            get
            {
                return this.meshes;
            }
            private set
            {
                meshes = value;
            }
        }

        // Summary:
        //     Gets a collection of bones that are children of this bone.
        public ModelBoneCollection Children { get; private set; }
        //
        // Summary:
        //     Gets the index of this bone in the Bones collection.
        public int Index { get; set; }
        //
        // Summary:
        //     Gets the name of this bone.
        public string Name { get; set; }
        //
        // Summary:
        //     Gets the parent of this bone.
        public ModelBone Parent { get; set; }
        public Matrix Transform
        {
            get { return this.transform; }
            set { this.transform = value; }
        }

        /// <summary>
        /// Transform of this node from the root of the model not from the parent
        /// </summary>
        public Matrix ModelTransform
        {
            get;
            set;
        }

        #endregion

        #region Internal Variables

        //
        // Summary:
        //     Gets or sets the matrix used to transform this bone relative to its parent
        //     bone.
        internal Matrix transform;

        #endregion

        #region Private Variables

        private List<ModelBone> children = new List<ModelBone>();
		
		private List<ModelMesh> meshes = new List<ModelMesh>();

        #endregion

        #region Public Constructor

        public ModelBone()
        {
            Children = new ModelBoneCollection(new List<ModelBone>());
        }

        #endregion

        #region Public Methods

        public void AddMesh(ModelMesh mesh)
		{
			meshes.Add(mesh);
		}

		public void AddChild(ModelBone modelBone)
		{
			children.Add(modelBone);
			Children = new ModelBoneCollection(children);
        }

        #endregion

    }

	//// Summary:
	////     Represents bone data for a model. Reference page contains links to related
	////     conceptual articles.
	//public sealed class ModelBone
	//{
	//    // Summary:
	//    //     Gets a collection of bones that are children of this bone.
	//    public ModelBoneCollection Children { get { throw new NotImplementedException(); } }
	//    //
	//    // Summary:
	//    //     Gets the index of this bone in the Bones collection.
	//    public int Index { get { throw new NotImplementedException(); } }
	//    //
	//    // Summary:
	//    //     Gets the name of this bone.
	//    public string Name { get { throw new NotImplementedException(); } }
	//    //
	//    // Summary:
	//    //     Gets the parent of this bone.
	//    public ModelBone Parent { get { throw new NotImplementedException(); } }
	//    //
	//    // Summary:
	//    //     Gets or sets the matrix used to transform this bone relative to its parent
	//    //     bone.
	//    public Matrix Transform { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
	//}
}
