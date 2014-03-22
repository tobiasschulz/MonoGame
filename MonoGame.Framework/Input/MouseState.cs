#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;

namespace Microsoft.Xna.Framework.Input
{    
    /// <summary>
    /// Represents a mouse state with cursor position and button press information.
    /// </summary>
	public struct MouseState
    {

        #region Public Properties

        /// <summary>
        /// Gets horizontal position of the cursor.
        /// </summary>
        public int X
        {
            get
            {
                return _x;
            }
            internal set
            {
                _x = value;
            }
        }

        /// <summary>
        /// Gets vertical position of the cursor.
        /// </summary>
        public int Y
        {
            get
            {
                return _y;
            }
            internal set
            {
                _y = value;
            }
        }

        /// <summary>
        /// Gets cursor position.
        /// </summary>
        public Point Position
        {
            get { return new Point(_x, _y); }
        }

        /// <summary>
        /// Gets state of the left mouse button.
        /// </summary>
        public ButtonState LeftButton
        {
            get
            {
                return _leftButton;
            }
            internal set { _leftButton = value; }
        }

        /// <summary>
        /// Gets state of the middle mouse button.
        /// </summary>
        public ButtonState MiddleButton
        {
            get
            {
                return _middleButton;
            }
            internal set { _middleButton = value; }
        }

        /// <summary>
        /// Gets state of the right mouse button.
        /// </summary>
        public ButtonState RightButton
        {
            get
            {
                return _rightButton;
            }
            internal set { _rightButton = value; }
        }

        /// <summary>
        /// Returns cumulative scroll wheel value since the game start.
        /// </summary>
        public int ScrollWheelValue
        {
            get
            {
                return _scrollWheelValue;
            }
            internal set { _scrollWheelValue = value; }
        }

        /// <summary>
        /// Gets state of the XButton1.
        /// </summary>
        public ButtonState XButton1
        {
            get
            {
                return ButtonState.Released;
            }
        }

        /// <summary>
        /// Gets state of the XButton2.
        /// </summary>
        public ButtonState XButton2
        {
            get
            {
                return ButtonState.Released;
            }
        }

        #endregion

        #region Private Variables

        int _x, _y;
		int _scrollWheelValue;
		ButtonState _leftButton;
		ButtonState _rightButton;
		ButtonState _middleButton;

        #endregion

        #region Public Constructor

        /// <summary>
        /// Initializes a new instance of the MouseState.
        /// </summary>
        /// <param name="x">Horizontal position of the mouse.</param>
        /// <param name="y">Vertical position of the mouse.</param>
        /// <param name="scrollWheel">Mouse scroll wheel's value.</param>
        /// <param name="leftButton">Left mouse button's state.</param>
        /// <param name="middleButton">Middle mouse button's state.</param>
        /// <param name="rightButton">Right mouse button's state.</param>
        /// <param name="xButton1">XBUTTON1's state.</param>
        /// <param name="xButton2">XBUTTON2's state.</param>
        /// <remarks>Normally <see cref="Mouse.GetState()"/> should be used to get mouse current state. The constructor is provided for simulating mouse input.</remarks>
		public MouseState (
			int x,
			int y,
			int scrollWheel,
			ButtonState leftButton,
			ButtonState middleButton,
			ButtonState rightButton,
			ButtonState xButton1,
			ButtonState xButton2)
		{
			_x = x;
			_y = y;
			_scrollWheelValue = scrollWheel;
			_leftButton = leftButton;
			_middleButton = middleButton;
			_rightButton = rightButton;
		}

        #endregion

        #region Public Static Operators and Override Methods

        /// <summary>
        /// Compares whether two MouseState instances are equal.
        /// </summary>
        /// <param name="left">MouseState instance on the left of the equal sign.</param>
        /// <param name="right">MouseState instance  on the right of the equal sign.</param>
        /// <returns>true if the instances are equal; false otherwise.</returns>
		public static bool operator ==(MouseState left, MouseState right)
		{
			return left._x == right._x &&
				   left._y == right._y &&
				   left._leftButton == right._leftButton &&
				   left._middleButton == right._middleButton &&
				   left._rightButton == right._rightButton &&
                   left._scrollWheelValue == right._scrollWheelValue;
		}
		
        /// <summary>
        /// Compares whether two MouseState instances are not equal.
        /// </summary>
        /// <param name="left">MouseState instance on the left of the equal sign.</param>
        /// <param name="right">MouseState instance  on the right of the equal sign.</param>
        /// <returns>true if the objects are not equal; false otherwise.</returns>
		public static bool operator !=(MouseState left, MouseState right)
		{
			return !(left == right);
		}

        /// <summary>
        /// Compares whether current instance is equal to specified object.
        /// </summary>
        /// <param name="obj">The MouseState to compare.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is MouseState)
                return this == (MouseState)obj;
            return false;
        }

        /// <summary>
        /// Gets the hash code for MouseState instance.
        /// </summary>
        /// <returns>Hash code of the object.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

    }
}

