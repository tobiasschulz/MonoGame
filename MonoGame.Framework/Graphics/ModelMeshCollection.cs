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
using System.Collections.ObjectModel;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	/// <summary>
	/// Represents a collection of ModelMesh objects.
    /// </summary>
    public sealed class ModelMeshCollection : ReadOnlyCollection<ModelMesh>
    {
        #region Public Properties

        /// <summary>
        /// Retrieves a ModelMesh from the collection, given the name of the mesh.
        /// </summary>
        /// <param name="meshName">
        ///  The name of the mesh to retrieve.
        /// </param>
        public ModelMesh this[string meshName]
        {
            get
            {
                ModelMesh ret;
                if (!this.TryGetValue(meshName, out ret))
                {
                    throw new KeyNotFoundException();
                }
                return ret;
            }
        }

        #endregion

        #region Internal Constructor

        internal ModelMeshCollection(IList<ModelMesh> list)
            : base(list)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Finds a mesh with a given name if it exists in the collection.
        /// </summary>
        /// <param name="meshName">
        /// The name of the mesh to find.
        /// </param>
        /// <param name="value">
        /// [OutAttribute] The mesh named meshName, if found.
        /// </param>
        public bool TryGetValue(string meshName, out ModelMesh value)
        {
            if (string.IsNullOrEmpty(meshName))
            {
                throw new ArgumentNullException("meshName");
            }

            foreach (ModelMesh mesh in this)
            {
                if (string.Compare(mesh.Name, meshName, StringComparison.Ordinal) == 0)
                {
                    value = mesh;
                    return true;
                }
            }

            value = null;
            return false;
        }

        #endregion
    }
}