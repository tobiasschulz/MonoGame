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
#endregion

namespace Microsoft.Xna.Framework.Input.Touch
{
	/// <summary>
	/// Represents data from a multi-touch gesture over a span of time.
	/// </summary>
	public struct GestureSample
	{

		#region Public Properties

		/// <summary>
		/// Gets the type of the gesture.
		/// </summary>
		public GestureType GestureType
		{
			get
			{
				return this._gestureType;
			}
		}

		/// <summary>
		/// Gets the starting time for this multi-touch gesture sample.
		/// </summary>
		public TimeSpan Timestamp
		{
			get
			{
				return this._timestamp;
			}
		}

		/// <summary>
		/// Gets the position of the first touch-point in the gesture sample.
		/// </summary>
		public Vector2 Position
		{
			get
			{
				return this._position;
			}
		}

		/// <summary>
		/// Gets the position of the second touch-point in the gesture sample.
		/// </summary>
		public Vector2 Position2
		{
			get
			{
				return this._position2;
			}
		}

		/// <summary>
		/// Gets the delta information for the first touch-point in the gesture sample.
		/// </summary>
		public Vector2 Delta
		{
			get
			{
				return this._delta;
			}
		}

		/// <summary>
		/// Gets the delta information for the second touch-point in the gesture sample.
		/// </summary>
		public Vector2 Delta2
		{
			get
			{
				return this._delta2;
			}
		}

		#endregion

		#region Private Variables

		// Attributes
		private GestureType _gestureType;
		private TimeSpan _timestamp;
		private Vector2 _position;
		private Vector2 _position2;
		private Vector2 _delta;
		private Vector2 _delta2;

		#endregion

		#region Public Constructor

		/// <summary>
		/// Initializes a new <see cref="GestureSample"/>.
		/// </summary>
		/// <param name="gestureType"><see cref="GestureType"/></param>
		/// <param name="timestamp"></param>
		/// <param name="position"></param>
		/// <param name="position2"></param>
		/// <param name="delta"></param>
		/// <param name="delta2"></param>
		public GestureSample(
			GestureType gestureType,
			TimeSpan timestamp,
			Vector2 position,
			Vector2 position2,
			Vector2 delta,
			Vector2 delta2
		) {
			this._gestureType = gestureType;
			this._timestamp = timestamp;
			this._position = position;
			this._position2 = position2;
			this._delta = delta;
			this._delta2 = delta2;
		}

		#endregion

	}
}

