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
using System.Diagnostics;
#endregion

namespace Microsoft.Xna.Framework.Input.Touch
{
	public struct TouchLocation : IEquatable<TouchLocation>
	{
		#region Public Properties

		public int Id
		{
			get
			{
				return id;
			}
		}

		public Vector2 Position
		{
			get
			{
				return position;
			}
		}

		public float Pressure
		{
			get
			{
				return pressure;
			}
		}

		public TouchLocationState State
		{
			get
			{
				return state;
			}
		}

		#endregion

		#region Internal Properties

		internal Vector2 PressPosition
		{
			get
			{
				return pressPosition;
			}
		}

		internal TimeSpan PressTimestamp
		{
			get
			{
				return pressTimestamp;
			}
		}

		internal TimeSpan Timestamp
		{
			get
			{
				return timestamp;
			}
		}

		internal Vector2 Velocity
		{
			get
			{
				return velocity;
			}
		}

		#endregion

		#region Internal Static TouchLocation

		/// <summary>
		/// Helper for assigning an invalid touch location.
		/// </summary>
		internal static readonly TouchLocation Invalid = new TouchLocation();

		#endregion

		#region Private Variables

		/// <summary>
		/// Attributes
		/// </summary>
		private int id;
		private Vector2 position;
		private Vector2 previousPosition;
		private TouchLocationState state;
		private TouchLocationState previousState;

		// Only used in Android, for now
		private float pressure;
		private float previousPressure;

		// Used for gesture recognition.
		private Vector2 velocity;
		private Vector2 pressPosition;
		private TimeSpan pressTimestamp;
		private TimeSpan timestamp;

		#endregion

		#region Public Constructors

		public TouchLocation(
			int id,
			TouchLocationState state,
			Vector2 position
		) : this(
			id,
			state,
			position,
			TouchLocationState.Invalid,
			Vector2.Zero
		) {
		}

		public TouchLocation(
			int id,
			TouchLocationState state,
			Vector2 position,
			TouchLocationState previousState,
			Vector2 previousPosition
		) {
			this.id = id;
			this.state = state;
			this.position = position;
			pressure = 0.0f;

			this.previousState = previousState;
			this.previousPosition = previousPosition;
			previousPressure = 0.0f;

			timestamp = TimeSpan.FromTicks(DateTime.Now.Ticks);
			velocity = Vector2.Zero;

			/* If this is a pressed location then store the current position
			 * and timestamp as pressed.
			 */
			if (state == TouchLocationState.Pressed)
			{
				pressPosition = this.position;
				pressTimestamp = this.timestamp;
			}
			else
			{
				pressPosition = Vector2.Zero;
				pressTimestamp = TimeSpan.Zero;
			}
		}

		#endregion

		#region Public Methods

		public bool TryGetPreviousLocation(out TouchLocation aPreviousLocation)
		{
			if (previousState == TouchLocationState.Invalid)
			{
				aPreviousLocation.id = -1;
				aPreviousLocation.state = TouchLocationState.Invalid;
				aPreviousLocation.position = Vector2.Zero;
				aPreviousLocation.previousState = TouchLocationState.Invalid;
				aPreviousLocation.previousPosition = Vector2.Zero;
				aPreviousLocation.pressure = 0.0f;
				aPreviousLocation.previousPressure = 0.0f;
				aPreviousLocation.timestamp = TimeSpan.Zero;
				aPreviousLocation.pressPosition = Vector2.Zero;
				aPreviousLocation.pressTimestamp = TimeSpan.Zero;
				aPreviousLocation.velocity = Vector2.Zero;
				return false;
			}

			aPreviousLocation.id = id;
			aPreviousLocation.state = previousState;
			aPreviousLocation.position = previousPosition;
			aPreviousLocation.previousState = TouchLocationState.Invalid;
			aPreviousLocation.previousPosition = Vector2.Zero;
			aPreviousLocation.pressure = previousPressure;
			aPreviousLocation.previousPressure = 0.0f;
			aPreviousLocation.timestamp = timestamp;
			aPreviousLocation.pressPosition = pressPosition;
			aPreviousLocation.pressTimestamp = pressTimestamp;
			aPreviousLocation.velocity = velocity;
			return true;
		}

		#endregion

		#region Public IEquatable Methods and Operator Overrides

		public override int GetHashCode()
		{
			return id;
		}

		public override string ToString()
		{
			return (
				"Touch id:" + id +
				" state:" + state +
				" position:" + position +
				" pressure:" + pressure +
				" prevState:" + previousState +
				" prevPosition:" + previousPosition +
				" previousPressure:" + previousPressure
			);
		}

		public override bool Equals(object obj)
		{
			if (obj is TouchLocation)
			{
				return Equals((TouchLocation)obj);
			}

			return false;
		}

		public bool Equals(TouchLocation other)
		{
			return (	id.Equals(other.id) &&
					position.Equals(other.position) &&
					previousPosition.Equals(other.previousPosition)	);
		}

		public static bool operator !=(TouchLocation value1, TouchLocation value2)
		{
			return (	value1.id != value2.id ||
					value1.state != value2.state ||
					value1.position != value2.position ||
					value1.previousState != value2.previousState ||
					value1.previousPosition != value2.previousPosition	);
		}

		public static bool operator ==(TouchLocation value1, TouchLocation value2)
		{
			return (	value1.id == value2.id &&
					value1.state == value2.state &&
					value1.position == value2.position &&
					value1.previousState == value2.previousState &&
					value1.previousPosition == value2.previousPosition	);
		}

		#endregion

		#region Internal Methods

		/// <summary>
		/// Returns a copy of the touch with the state changed to moved.
		/// </summary>
		/// <returns>The new touch location.</returns>
		internal TouchLocation AsMovedState()
		{
			TouchLocation touch = this;

			// Store the current state as the previous.
			touch.previousState = touch.state;
			touch.previousPosition = touch.position;
			touch.previousPressure = touch.pressure;

			// Set the new state.
			touch.state = TouchLocationState.Moved;

			return touch;
		}

		/// <summary>
		/// Updates the touch location using the new event.
		/// </summary>
		/// <param name="touchEvent">The next event for this touch location.</param>
		internal bool UpdateState(TouchLocation touchEvent)
		{
			Debug.Assert(
				Id == touchEvent.Id,
				"The touch event must have the same Id!"
			);
			Debug.Assert(
				State != TouchLocationState.Released,
				"We shouldn't be changing state on a released location!"
			);
			Debug.Assert(
				touchEvent.State == TouchLocationState.Moved || touchEvent.State == TouchLocationState.Released,
				"The new touch event should be a move or a release!"
			);
			Debug.Assert(
				touchEvent.Timestamp >= timestamp,
				"The touch event is older than our timestamp!"
			);

			// Store the current state as the previous one.
			previousPosition = position;
			previousState = state;
			previousPressure = pressure;

			// Set the new state.
			position = touchEvent.position;
			state = touchEvent.state;
			pressure = touchEvent.pressure;

			// If time has elapsed then update the velocity.
			Vector2 delta = position - previousPosition;
			TimeSpan elapsed = touchEvent.Timestamp - timestamp;
			if (elapsed > TimeSpan.Zero)
			{
				// Use a simple low pass filter to accumulate velocity.
				Vector2 vel = delta / (float) elapsed.TotalSeconds;
				velocity += (vel - velocity) * 0.45f;
			}

			// Set the new timestamp.
			timestamp = touchEvent.Timestamp;

			// Return true if the state actually changed.
			return state != previousState || delta.LengthSquared() > 0.001f;
		}

		#endregion
	}
}
