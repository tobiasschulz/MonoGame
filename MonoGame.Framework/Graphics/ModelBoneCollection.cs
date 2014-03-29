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
using System.Collections.ObjectModel;

#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	/// <summary>
	/// Represents a set of bones associated with a model.
	/// </summary>
	public class ModelBoneCollection : ReadOnlyCollection<ModelBone>
	{
		#region Public Properties

		/// <summary>
		/// Retrieves a ModelBone from the collection, given the name of the bone.
		/// </summary>
		/// <param name="boneName">
		/// The name of the bone to retrieve.
		/// </param>
		public ModelBone this[string boneName]
		{
			get
			{
				ModelBone ret;
				if (TryGetValue(boneName, out ret))
				{
					return ret;
				}
				throw new KeyNotFoundException();
			}
		}

		#endregion

		#region Public Constructor

		public ModelBoneCollection(IList<ModelBone> list)
			: base(list)
		{
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Finds a bone with a given name if it exists in the collection.
		/// </summary>
		/// <param name="boneName">
		/// The name of the bone to find.
		/// </param>
		/// <param name="value">
		/// [OutAttribute] The bone named boneName, if found.
		/// </param>
		public bool TryGetValue(string boneName, out ModelBone value)
		{
			foreach (ModelBone bone in base.Items)
			{
				if (bone.Name == boneName)
				{
					value = bone;
					return true;
				}
			}
			value = null;
			return false;
		}

		#endregion
	}
}