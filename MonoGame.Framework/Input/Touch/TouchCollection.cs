#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using clause
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#endregion Using clause

namespace Microsoft.Xna.Framework.Input.Touch
{
	/// <summary>
	/// Provides state information for a touch screen enabled device.
	/// </summary>
	public struct TouchCollection : IList<TouchLocation>
	{
		private TouchLocation[] _collection;

		private bool _isConnected;

		private static readonly TouchLocation[] emptyCollection = new TouchLocation[0];

		#region Public Properties

		/// <summary>
		/// States if a touch screen is available.
		/// </summary>
		public bool IsConnected 
		{
			get
			{
				return _isConnected;
			}
		}

		#endregion

		#region Public IList<TouchLocation> Properties

		/// <summary>
		/// States if touch collection is read only.
		/// </summary>
		public bool IsReadOnly
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Returns the number of <see cref="TouchLocation"/> items that exist in the
		/// collection.
		/// </summary>
		public int Count
		{
			get
			{
				if (_collection == null)
				{
					return 0;
				}
				return _collection.Length;
			}
		}

		/// <summary>
		/// Gets or sets the item at the specified index of the collection.
		/// </summary>
		/// <param name="index">Position of the item.</param>
		/// <returns><see cref="TouchLocation"/></returns>
		public TouchLocation this[int index]
		{
			get
			{
				if (_collection == null)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				return _collection[index];
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		#endregion

		#region Public Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="TouchCollection"/> with a
		/// pre-determined set of touch locations.
		/// </summary>
		/// <param name="touches">
		/// Array of <see cref="TouchLocation"/> items with which to initialize.
		/// </param>
		public TouchCollection(TouchLocation[] touches)
		{
			_isConnected = true;
			_collection = touches;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Returns <see cref="TouchLocation"/> specified by ID.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="touchLocation"></param>
		/// <returns></returns>
		public bool FindById(int id, out TouchLocation touchLocation)
		{
			if (_collection == null)
			{
				touchLocation = default(TouchLocation);
				return false;
			}
			foreach (TouchLocation location in _collection)
			{
				if (location.Id == id)
				{
					touchLocation = location;
					return true;
				}
			}

			touchLocation = default(TouchLocation);
			return false;
		}

		#endregion

		#region Public IList<TouchLocation> Methods

		/// <summary>
		/// Returns the index of the first occurrence of specified <see cref="TouchLocation"/>
		/// item in the collection.
		/// </summary>
		/// <param name="item"><see cref="TouchLocation"/> to query.</param>
		/// <returns></returns>
		public int IndexOf(TouchLocation item)
		{
			if (_collection == null)
			{
				return -1;
			}
			for (int i = 0; i < _collection.Length; i += 1)
			{
				if (item == _collection[i])
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Inserts a <see cref="TouchLocation"/> item into the indicated position.
		/// </summary>
		/// <param name="index">The position to insert into.</param>
		/// <param name="item">The <see cref="TouchLocation"/> item to insert.</param>
		public void Insert(int index, TouchLocation item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Removes the <see cref="TouchLocation"/> item at specified index.
		/// </summary>
		/// <param name="index">Index of the item that will be removed from collection.</param>
		public void RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Adds a <see cref="TouchLocation"/> to the collection.
		/// </summary>
		/// <param name="item">The <see cref="TouchLocation"/> item to be added. </param>
		public void Add(TouchLocation item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Clears all the items in collection.
		/// </summary>
		public void Clear()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Returns true if specified <see cref="TouchLocation"/> item exists in the
		/// collection, false otherwise./>
		/// </summary>
		/// <param name="item">The <see cref="TouchLocation"/> item to query for.</param>
		/// <returns>Returns true if queried item is found, false otherwise.</returns>
		public bool Contains(TouchLocation item)
		{
			if (_collection == null)
			{
				return false;
			}
			foreach(TouchLocation location in _collection)
			{
				if (item == location)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Copies the <see cref="TouchLocation "/>collection to specified array starting
		/// from the given index.
		/// </summary>
		/// <param name="array">The array to copy <see cref="TouchLocation"/> items.</param>
		/// <param name="arrayIndex">The starting index of the copy operation.</param>
		public void CopyTo(TouchLocation[] array, int arrayIndex)
		{
			if (_collection != null)
			{
				_collection.CopyTo(array, arrayIndex);
			}
		}

		/// <summary>
		/// Removes the specified <see cref="TouchLocation"/> item from the collection.
		/// </summary>
		/// <param name="item">The <see cref="TouchLocation"/> item to remove.</param>
		/// <returns></returns>
		public bool Remove(TouchLocation item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Returns an enumerator for the <see cref="TouchCollection"/>.
		/// </summary>
		/// <returns>Enumerable list of <see cref="TouchLocation"/> objects.</returns>
		public IEnumerator<TouchLocation> GetEnumerator()
		{
			if (_collection == null)
			{
				return emptyCollection.AsEnumerable().GetEnumerator();
			}

			return _collection.AsEnumerable().GetEnumerator();
		}


		/// <summary>
		/// Returns an enumerator for the <see cref="TouchCollection"/>.
		/// </summary>
		/// <returns>Enumerable list of objects.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			if (_collection == null)
			{
				return emptyCollection.GetEnumerator();
			}

			return _collection.GetEnumerator();
		}

		#endregion
	}
}