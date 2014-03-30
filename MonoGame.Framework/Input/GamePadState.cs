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

namespace Microsoft.Xna.Framework.Input
{
	/// <summary>
	/// Represents specific information about the state of an Xbox 360 Controller,
	/// including the current state of buttons and sticks. Reference page contains
	/// links to related code samples.
	/// </summary>
	public struct GamePadState
	{

		#region Public Properties

		/// <summary>
		/// Indicates whether the Xbox 360 Controller is connected. Reference page contains
		/// links to related code samples.
		/// </summary>
		public bool IsConnected
		{
			get;
			internal set;
		}
		/// <summary>
		/// Gets the packet number associated with this state. Reference page contains
		/// links to related code samples.
		/// </summary>
		public int PacketNumber
		{
			get;
			internal set;
		}
		/// <summary>
		/// Returns a structure that identifies what buttons on the Xbox 360 controller
		/// are pressed. Reference page contains links to related code samples.
		/// </summary>
		public GamePadButtons Buttons
		{
			get;
			internal set;
		}
		/// <summary>
		/// Returns a structure that identifies what directions of the directional pad
		/// on the Xbox 360 Controller are pressed.
		/// </summary>
		public GamePadDPad DPad
		{
			get;
			internal set;
		}
		/// <summary>
		/// Returns a structure that indicates the position of the Xbox 360 Controller
		/// sticks (thumbsticks).
		/// </summary>
		public GamePadThumbSticks ThumbSticks
		{
			get;
			internal set;
		}
		/// <summary>
		/// Returns a structure that identifies the position of triggers on the Xbox
		/// 360 controller.
		/// </summary>
		public GamePadTriggers Triggers
		{
			get;
			internal set;
		}

		#endregion

		#region Internal Properties

		internal static GamePadState InitializedState
		{
			get
			{
				return initializedGamePadState;
			}
		}

		#endregion

		#region Private Static Variables

		private static GamePadState initializedGamePadState = new GamePadState();

		#endregion

		#region Public Constructors

		/// <summary>
		/// Initializes a new instance of the GamePadState class using the specified
		/// GamePadThumbSticks, GamePadTriggers, GamePadButtons, and GamePadDPad.
		/// </summary>
		/// <param name="thumbSticks">Initial thumbstick state.</param>
		/// <param name="triggers">Initial trigger state.</param>
		/// <param name="buttons">Initial button state.</param>
		/// <param name="dPad">Initial directional pad state.</param>
		public GamePadState(
			GamePadThumbSticks thumbSticks,
			GamePadTriggers triggers,
			GamePadButtons buttons,
			GamePadDPad dPad
		) : this()
		{
			ThumbSticks = thumbSticks;
			Triggers = triggers;
			Buttons = buttons;
			DPad = dPad;
			IsConnected = true;
			PacketNumber = 0;
		}

		/// <summary>
		/// Initializes a new instance of the GamePadState class with the specified stick,
		/// trigger, and button values.
		/// </summary>
		/// <param name="leftThumbStick">
		/// Left stick value. Each axis is clamped between 1.0 and 1.0.
		/// </param>
		/// <param name="rightThumbStick">
		/// Right stick value. Each axis is clamped between 1.0 and 1.0.
		/// </param>
		/// <param name="leftTrigger">
		/// Left trigger value. This value is clamped between 0.0 and 1.0.
		/// </param>
		/// <param name="rightTrigger">
		/// Right trigger value. This value is clamped between 0.0 and 1.0.
		/// </param>
		/// <param name="buttons">
		/// Array or parameter list of Buttons to initialize as pressed.
		/// </param>
		public GamePadState(
			Vector2 leftThumbStick,
			Vector2 rightThumbStick,
			float leftTrigger,
			float rightTrigger,
			params Buttons[] buttons
		) : this(
			new GamePadThumbSticks(leftThumbStick, rightThumbStick),
			new GamePadTriggers(leftTrigger, rightTrigger),
			new GamePadButtons(buttons),
			new GamePadDPad()
		) { }

		#endregion

		#region Public Methods

		/// <summary>
		/// Determines whether specified input device buttons are pressed in this GamePadState.
		/// </summary>
		/// <param name="button">
		/// Buttons to query. Specify a single button, or combine multiple buttons using
		/// a bitwise OR operation.
		/// </param>
		public bool IsButtonDown(Buttons button)
		{
		    return (GetVirtualButtons() & button) == button;
		}

		/// <summary>
		/// Determines whether specified input device buttons are up (not pressed) in this GamePadState.
		/// </summary>
		/// <param name="button">
		/// Buttons to query. Specify a single button, or combine multiple buttons using
		/// a bitwise OR operation.
		/// </param>
		public bool IsButtonUp(Buttons button)
		{
			return (GetVirtualButtons() & button) != button;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the button mask along with 'virtual buttons' like LeftThumbstickLeft.
		/// </summary>
		private Buttons GetVirtualButtons ()
		{
			var result = Buttons.buttons;
			var sticks = ThumbSticks;
			sticks.ApplyDeadZone(GamePadDeadZone.IndependentAxes, 7849 / 32767f);

			if (sticks.Left.X < 0)
			{
				result |= Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickLeft;
			}
			else if (sticks.Left.X > 0)
			{
				result |= Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickRight;
			}

			if (sticks.Left.Y < 0)
			{
				result |= Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickDown;
			}
			else if (sticks.Left.Y > 0)
			{
				result |= Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickUp;
			}

			if (sticks.Right.X < 0)
			{
				result |= Microsoft.Xna.Framework.Input.Buttons.RightThumbstickLeft;
			}
			else if (sticks.Right.X > 0)
			{
				result |= Microsoft.Xna.Framework.Input.Buttons.RightThumbstickRight;
			}

			if (sticks.Right.Y < 0)
			{
				result |= Microsoft.Xna.Framework.Input.Buttons.RightThumbstickDown;
			}
			else if (sticks.Right.Y > 0)
			{
				result |= Microsoft.Xna.Framework.Input.Buttons.RightThumbstickUp;
			}

			return result;
		}

		#endregion

		#region Public Static Operators and Override Methods

		/// <summary>
		/// Determines whether two GamePadState instances are not equal.
		/// </summary>
		/// <param name="left">Object on the left of the equal sign.</param>
		/// <param name="right">Object on the right of the equal sign.</param>
		public static bool operator !=(GamePadState left, GamePadState right)
		{
			return !left.Equals(right);
		}
		/// <summary>
		/// Determines whether two GamePadState instances are equal.
		/// </summary>
		/// <param name="left">Object on the left of the equal sign.</param>
		/// <param name="right">Object on the right of the equal sign.</param>
		public static bool operator ==(GamePadState left, GamePadState right)
		{
			return left.Equals(right);
		}
		/// <summary>
		/// Returns a value that indicates whether the current instance is equal to a specified object.
		/// </summary>
		/// <param name="obj">Object with which to make the comparison.</param>
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}
		/// <summary>
		/// Gets the hash code for this instance.
		/// </summary>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		/// <summary>
		/// Retrieves a string representation of this object.
		/// </summary>
		public override string ToString()
		{
			return base.ToString();
		}

		#endregion
	}
}
