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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace Microsoft.Xna.Framework.Graphics
{
	// Summary:
	//     Represents a collection of effects associated with a model.
	public sealed class ModelEffectCollection : ReadOnlyCollection<Effect>
    {

        #region Public Constructor

        public ModelEffectCollection(IList<Effect> list)
			: base(list)
		{
		}

        #endregion

        #region Internal Constructor

        internal ModelEffectCollection() : base(new List<Effect>())
	    {
	    }

        #endregion

        #region Public Methods

        // Summary:
        //     Returns a ModelEffectCollection.Enumerator that can iterate through a ModelEffectCollection.
        public new ModelEffectCollection.Enumerator GetEnumerator()
        {
            return new ModelEffectCollection.Enumerator((List<Effect>)Items);
        }

        #endregion

        #region Internal Methods

        //ModelMeshPart needs to be able to add to ModelMesh's effects list
		internal void Add(Effect item)
		{
			Items.Add (item);
		}
		internal void Remove(Effect item)
		{
			Items.Remove (item);
		}

        #endregion

        #region Public Enumerator struct

        // Summary:
	    //     Provides the ability to iterate through the bones in an ModelEffectCollection.
	    public struct Enumerator : IEnumerator<Effect>, IDisposable, IEnumerator
	    {

            // Summary:
            //     Gets the current element in the ModelEffectCollection.
            public Effect Current { get { return enumerator.Current; } }
            
            List<Effect>.Enumerator enumerator;
            bool disposed;

			internal Enumerator(List<Effect> list)
			{
				enumerator = list.GetEnumerator();
                disposed = false;
			}

	        // Summary:
	        //     Immediately releases the unmanaged resources used by this object.
	        public void Dispose()
            {
                if (!disposed)
                {
                    enumerator.Dispose();
                    disposed = true;
                }
            }

            //
	        // Summary:
	        //     Advances the enumerator to the next element of the ModelEffectCollection.
	        public bool MoveNext() { return enumerator.MoveNext(); }

	        object IEnumerator.Current
	        {
	            get { return Current; }
	        }

	        void IEnumerator.Reset()
	        {
				IEnumerator resetEnumerator = enumerator;
				resetEnumerator.Reset ();
				enumerator = (List<Effect>.Enumerator)resetEnumerator;
	        }

        }

        #endregion
    
    }
}
