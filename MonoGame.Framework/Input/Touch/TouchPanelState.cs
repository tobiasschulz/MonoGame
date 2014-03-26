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
#endregion

namespace Microsoft.Xna.Framework.Input.Touch
{
	public class TouchPanelState
	{
		#region Public Properties

		/// <summary>
		/// The window handle of the touch panel. Purely for Xna compatibility.
		/// </summary>
		public IntPtr WindowHandle
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the display height of the touch panel.
		/// </summary>
		public int DisplayHeight
		{
			get
			{
				return displaySize.Y;
			}
			set
			{
				displaySize.Y = value;
				UpdateTouchScale();
			}
		}

		/// <summary>
		/// Gets or sets the display orientation of the touch panel.
		/// </summary>
		public DisplayOrientation DisplayOrientation
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the display width of the touch panel.
		/// </summary>
		public int DisplayWidth
		{
			get
			{
				return displaySize.X;
			}
			set
			{
				displaySize.X = value;
				UpdateTouchScale();
			}
		}

		/// <summary>
		/// Gets or sets enabled gestures.
		/// </summary>
		public GestureType EnabledGestures
		{
			get;
			set;
		}

		public bool EnableMouseTouchPoint
		{
			get;
			set;
		}

		public bool EnableMouseGestures
		{
			get;
			set;
		}

		/// <summary>
		/// Returns true if a touch gesture is available.
		/// </summary>
		public bool IsGestureAvailable
		{
			get
			{
				// Process the pending gesture events.
				while (gestureEvents.Count > 0)
				{
					bool stateChanged = Refresh(
						true,
						gestureState,
						gestureEvents
					);
					UpdateGestures(stateChanged);
				}

				return GestureList.Count > 0;
			}
		}

		#endregion

		#region Internal Variables

		internal readonly Queue<GestureSample> GestureList = new Queue<GestureSample>();

		internal readonly GameWindow Window;

		#endregion

		#region Private Variables

		/// <summary>
		/// The maximum number of events to allow in the touch or gesture event lists.
		/// </summary>
		private const int MaxEvents = 100;

		/// <summary>
		/// The reserved touchId for all mouse touch points.
		/// </summary>
		private const int MouseTouchId = 1;

		/// <summary>
		/// The current touch state.
		/// </summary>
		private readonly List<TouchLocation> touchState = new List<TouchLocation>();

		/// <summary>
		/// The touch events to be processed and added to the touch state.
		/// </summary>
		private readonly List<TouchLocation> touchEvents = new List<TouchLocation>();

		/// <summary>
		/// The current gesture state.
		/// </summary>
		private readonly List<TouchLocation> gestureState = new List<TouchLocation>();

		/// <summary>
		/// The gesture events to be processed and added to the gesture state.
		/// </summary>
		private readonly List<TouchLocation> gestureEvents = new List<TouchLocation>();

		/// <summary>
		/// The positional scale to apply to touch input.
		/// </summary>
		private Vector2 touchScale = Vector2.One;

		/// <summary>
		/// The current size of the display.
		/// </summary>
		private Point displaySize = Point.Zero;

		/// <summary>
		/// The next touch location identifier.
		/// The value 1 is reserved for the mouse touch point.
		/// </summary>
		private int nextTouchId = 2;

		/// <summary>
		/// The mapping between platform specific touch ids
		/// and the touch ids we assign to touch locations.
		/// </summary>
		private readonly Dictionary<int, int> touchIds = new Dictionary<int, int>();

		private TouchPanelCapabilities capabilities = new TouchPanelCapabilities();

		#endregion

		#region Private Gesture Recognition Variables

		/// <summary>
		/// Maximum distance a touch location can wiggle and
		/// not be considered to have moved.
		/// </summary>
		private const float TapJitterTolerance = 35.0f;

		private static readonly TimeSpan maxTicksToProcessHold = TimeSpan.FromMilliseconds(1024);

		/// <summary>
		/// The pinch touch locations.
		/// </summary>
		private readonly TouchLocation[] pinchTouch = new TouchLocation[2];

		/// <summary>
		/// If true the pinch touch locations are valid and
		/// a pinch gesture has begun.
		/// </summary>
		private bool pinchGestureStarted;

		/// <summary>
		/// Used to disable emitting of tap gestures.
		/// </summary>
		private bool tapDisabled;

		/// <summary>
		/// Used to disable emitting of hold gestures.
		/// </summary>
		private bool holdDisabled;

		private TouchLocation lastTap;

		private GestureType dragGestureStarted = GestureType.None;

		#endregion

		#region Internal Constructor

		internal TouchPanelState(GameWindow window)
		{
			Window = window;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Returns capabilities of touch panel device.
		/// </summary>
		/// <returns><see cref="TouchPanelCapabilities"/></returns>
		public TouchPanelCapabilities GetCapabilities()
		{
			capabilities.Initialize();
			return capabilities;
		}

		public TouchCollection GetState()
		{
			// Process the touch state.
			bool consumeState = true;
			while (Refresh(consumeState, touchState, touchEvents))
			{
				consumeState = false;
			}

			return new TouchCollection(touchState.ToArray());
		}

		/// <summary>
		/// Returns the next available gesture on touch panel device.
		/// </summary>
		/// <returns><see cref="GestureSample"/></returns>
		public GestureSample ReadGesture()
		{
			// Return the next gesture.
			return GestureList.Dequeue();
		}

		#endregion

		#region Internal Methods

		internal bool Refresh(
			bool consumeState,
			List<TouchLocation> state,
			List<TouchLocation> events
		) {
			bool stateChanged = false;

			// Update the existing touch locations.
			for (int i = 0; i < state.Count; i += 1)
			{
				// Get the next touch location for update.
				TouchLocation touch = state[i];

				// If this location isn't in the move state yet then skip it.
				if (!consumeState && touch.State != TouchLocationState.Moved)
				{
					continue;
				}

				/* If the touch state has been consumed then we can
				 * remove old release locations.
				 */
				if (consumeState && touch.State == TouchLocationState.Released)
				{
					state.RemoveAt(i);
					i -= 1;
					continue;
				}

				bool foundEvent = false;

				/* Remove the next pending event with the
				 * same id and make it the new touch state.
				 */
				for (int j = 0; j < events.Count; j += 1)
				{
					TouchLocation newTouch = events[j];

					/* Don't let a release event occur until we're
					 * ready to consume state again.
					 */
					if (!consumeState && newTouch.State == TouchLocationState.Released)
					{
						continue;
					}

					if (newTouch.Id == touch.Id)
					{
						stateChanged |= touch.UpdateState(newTouch);
						foundEvent = true;
						events.RemoveAt(j);
						j -= 1;

						// Consume unchanged events.
						if (!stateChanged)
						{
							continue;
						}
						break;
					}
				}

				// If a new event was found then store it.
				if (foundEvent)
				{
					state[i] = touch;
				}

				/* Else if no event has come in then promote it to
				 * the moved state, but only when we're consuming state.
				 */
				else if (consumeState)
				{
					state[i] = touch.AsMovedState();
				}
			}

			/* We add new pressed events last so they are not
			 * consumed before the touch state is returned.
			 */
			for (int i = 0; i < events.Count; )
			{
				TouchLocation loc = events[i];

				if (loc.State == TouchLocationState.Pressed)
				{
					state.Add(loc);
					events.RemoveAt(i);
					stateChanged = true;
					continue;
				}

				i += 1;
			}

			return stateChanged;
		}

		internal void AddEvent(int id, TouchLocationState state, Vector2 position)
		{
			AddEvent(id, state, position, false);
		}

		internal void AddEvent(int id, TouchLocationState state, Vector2 position, bool isMouse)
		{
			/* Different platforms return different touch identifiers
			 * based on the specifics of their implementation and the
			 * system drivers.
			 *
			 * Sometimes these ids are suitable for our use, but other
			 * times it can recycle ids or do cute things like return
			 * the same id for double tap events.
			 *
			 * We instead provide consistent ids by generating them
			 * ourselves on the press and looking them up on move
			 * and release events.
			 */
			if (state == TouchLocationState.Pressed)
			{
				if (isMouse)
				{
					// Mouse pointing devices always use a reserved Id
					touchIds[id] = MouseTouchId;
				}
				else
				{
					touchIds[id] = nextTouchId += 1;
				}
			}

			// Try to find the touch id.
			int touchId;
			if (!touchIds.TryGetValue(id, out touchId))
			{
				/* If we got here that means either the device is sending
				 * us bad, out of order, or old touch events.  In any case
				 * just ignore them.
				 */
				return;
			}

			if (!isMouse || EnableMouseTouchPoint || EnableMouseGestures)
			{
				/* Add the new touch event keeping the list from getting
				 * too large if no one happens to be requesting the state.
				 */
				TouchLocation evt = new TouchLocation(touchId, state, position * touchScale);

				if (!isMouse || EnableMouseTouchPoint)
				{
					touchEvents.Add(evt);
					if (touchEvents.Count > MaxEvents)
					{
						touchEvents.RemoveRange(
							0,
							touchEvents.Count - MaxEvents
						);
					}
				}

				/* If we have gestures enabled then start to collect
				 * events for those too.
				 */
				if (EnabledGestures != GestureType.None && (!isMouse || EnableMouseGestures))
				{
					gestureEvents.Add(evt);
					if (gestureEvents.Count > MaxEvents)
					{
						gestureEvents.RemoveRange(
							0,
							gestureEvents.Count - MaxEvents
						);
					}
				}
			}

			// If this is a release unmap the hardware id.
			if (state == TouchLocationState.Released)
			{
				touchIds.Remove(id);
			}
		}

		/// <summary>
		/// This will release all touch locations.  It should only be
		/// called on platforms where touch state is reset all at once.
		/// </summary>
		internal void ReleaseAllTouches()
		{
			// Clear any pending events.
			touchEvents.Clear();
			gestureEvents.Clear();

			// Submit a new event for each non-released location.
			foreach (TouchLocation touch in touchState)
			{
				if (touch.State != TouchLocationState.Released)
				{
					touchEvents.Add(
						new TouchLocation(
							touch.Id,
							TouchLocationState.Released,
							touch.Position
						)
					);
				}
			}
			foreach (TouchLocation touch in gestureState)
			{
				if (touch.State != TouchLocationState.Released)
				{
					gestureEvents.Add(
						new TouchLocation(
							touch.Id,
							TouchLocationState.Released,
							touch.Position
						)
					);
				}
			}

			// Release all the touch id mappings.
			touchIds.Clear();
		}

		#endregion

		#region Private Methods

		private void UpdateTouchScale()
		{
			// Get the window size.
			Vector2 windowSize = new Vector2(
				Window.ClientBounds.Width,
				Window.ClientBounds.Height
			);

			// Recalculate the touch scale.
			touchScale = new Vector2(
				displaySize.X / windowSize.X,
				displaySize.Y / windowSize.Y
			);
		}

		#endregion

		#region Private Gesture Recognition Methods

		private bool GestureIsEnabled(GestureType gestureType)
		{
			return (EnabledGestures & gestureType) != 0;
		}

		private void UpdateGestures(bool stateChanged)
		{
			/* These are observed XNA gesture rules which we follow below.  Please
			 * add to them if a new case is found.
			 *
			 *  - Tap occurs on release.
			 *  - DoubleTap occurs on the first press after a Tap.
			 *  - Tap, Double Tap, and Hold are disabled if a drag begins or more than one finger is pressed.
			 *  - Drag occurs when one finger is down and actively moving.
			 *  - Pinch occurs if 2 or more fingers are down and at least one is moving.
			 *  - If you enter a Pinch during a drag a DragComplete is fired.
			 *  - Drags are classified as horizontal, vertical, free, or none and stay that way.
			 */

			// First get a count of touch locations which are not in the released state.
			int heldLocations = 0;
			foreach (TouchLocation touch in gestureState)
			{
				heldLocations += touch.State != TouchLocationState.Released ? 1 : 0;
			}

			/* As soon as we have more than one held point then
			 * tap and hold gestures are disabled until all the
			 * points are released.
			 */
			if (heldLocations > 1)
			{
				tapDisabled = true;
				holdDisabled = true;
			}

			// Process the touch locations for gestures.
			foreach (TouchLocation touch in gestureState)
			{
				switch (touch.State)
				{
				case TouchLocationState.Pressed:
				case TouchLocationState.Moved:
				{
					/* The DoubleTap event is emitted on first press as
					 * opposed to Tap which happens on release.
					 */
					if (touch.State == TouchLocationState.Pressed && ProcessDoubleTap(touch))
					{
						break;
					}

					/* Any time more than one finger is down and pinch is
					 * enabled then we exclusively do pinch processing.
					 */
					if (GestureIsEnabled(GestureType.Pinch) && heldLocations > 1)
					{
						// Save or update the first pinch point.
						if (pinchTouch[0].State == TouchLocationState.Invalid || pinchTouch[0].Id == touch.Id)
						{
							pinchTouch[0] = touch;
						}
						// Save or update the second pinch point.
						else if (pinchTouch[1].State == TouchLocationState.Invalid || pinchTouch[1].Id == touch.Id)
						{
							pinchTouch[1] = touch;
						}

						/* NOTE: Actual pinch processing happens outside and
						 * below this loop to ensure both points are updated
						 * before gestures are emitted.
						 */
						break;
					}

					// If we're not dragging try to process a hold event.
					float dist = Vector2.Distance(
						touch.Position,
						touch.PressPosition
					);
					if (dragGestureStarted == GestureType.None && dist < TapJitterTolerance)
					{
						ProcessHold(touch);
						break;
					}

					// If the touch state has changed then do a drag gesture.
					if (stateChanged)
					{
						ProcessDrag(touch);
					}
					break;
				}
				case TouchLocationState.Released:
				{
					/* If the touch state hasn't changed then this
					 * is an old release event... skip it.
					 */
					if (!stateChanged)
					{
						break;
					}

					/* If this is one of the pinch locations then we
					 * need to fire off the complete event and stop
					 * the pinch gesture operation.
					 */
					if (	pinchGestureStarted &&
						(	touch.Id == pinchTouch[0].Id ||
							touch.Id == pinchTouch[1].Id	)	)
					{
						if (GestureIsEnabled(GestureType.PinchComplete))
						{
							GestureList.Enqueue(
								new GestureSample(
									GestureType.PinchComplete,
									touch.Timestamp,
									Vector2.Zero,
									Vector2.Zero,
									Vector2.Zero,
									Vector2.Zero
								)
							);
						}

						pinchGestureStarted = false;
						pinchTouch[0] = TouchLocation.Invalid;
						pinchTouch[1] = TouchLocation.Invalid;
						break;
					}

					/* If there are still other pressed locations then there
					 * is nothing more we can do with this release.
					 */
					if (heldLocations != 0)
					{
						break;
					}

					/* From testing XNA it seems we need a velocity
					 * of about 100 to classify this as a flick.
					 */
					float dist = Vector2.Distance(touch.Position, touch.PressPosition);
					if (	dist > TapJitterTolerance &&
						touch.Velocity.Length() > 100.0f &&
						GestureIsEnabled(GestureType.Flick)	)
					{
						GestureList.Enqueue(
							new GestureSample(
								GestureType.Flick,
								touch.Timestamp,
								Vector2.Zero,
								Vector2.Zero,
								touch.Velocity,
								Vector2.Zero
							)
						);

						/* If we got a flick then stop the drag operation
						 * so that no DragComplete occurs.
						 */
						dragGestureStarted = GestureType.None;
						break;
					}

					// If a drag is active then we need to finalize it.
					if (dragGestureStarted != GestureType.None)
					{
						if (GestureIsEnabled(GestureType.DragComplete))
						{
							GestureList.Enqueue(
								new GestureSample(
									GestureType.DragComplete,
									touch.Timestamp,
									Vector2.Zero,
									Vector2.Zero,
									Vector2.Zero,
									Vector2.Zero
								)
							);
						}

						dragGestureStarted = GestureType.None;
						break;
					}

					// If all else fails try to process it as a tap.
					ProcessTap(touch);
					break;
				}
				}
			}

			/* If the touch state hasn't changed then there is no
			 * cleanup to do and no pinch to process.
			 */
			if (!stateChanged)
			{
				return;
			}

			// If we have two pinch points then update the pinch state.
			if (	GestureIsEnabled(GestureType.Pinch) &&
				pinchTouch [0].State != TouchLocationState.Invalid &&
				pinchTouch [1].State != TouchLocationState.Invalid	)
			{
				ProcessPinch(pinchTouch);
			}
			else
			{
				// Make sure a partial pinch state is not left hanging around.
				pinchGestureStarted = false;
				pinchTouch[0] = TouchLocation.Invalid;
				pinchTouch[1] = TouchLocation.Invalid;
			}

			// If all points are released then clear some states.
			if (heldLocations == 0)
			{
				tapDisabled = false;
				holdDisabled = false;
				dragGestureStarted = GestureType.None;
			}
		}

		private void ProcessHold(TouchLocation touch)
		{
			if (!GestureIsEnabled(GestureType.Hold) || holdDisabled)
			{
				return;
			}

			TimeSpan elapsed = TimeSpan.FromTicks(DateTime.Now.Ticks) - touch.PressTimestamp;
			if (elapsed < maxTicksToProcessHold)
			{
				return;
			}

			holdDisabled = true;

			GestureList.Enqueue(
				new GestureSample(
					GestureType.Hold,
					touch.Timestamp,
					touch.Position,
					Vector2.Zero,
					Vector2.Zero,
					Vector2.Zero
				)
			);
		}

		private bool ProcessDoubleTap(TouchLocation touch)
		{
			if (!GestureIsEnabled(GestureType.DoubleTap) || tapDisabled)
			{
				return false;
			}

			/* If the new tap is too far away from the last then
			 * this cannot be a double tap event.
			 */
			float dist = Vector2.Distance(touch.Position, lastTap.Position);
			if (dist > TapJitterTolerance)
			{
				return false;
			}

			/* Check that this tap happened within the standard
			 * double tap time threshold of 300 milliseconds.
			 */
			TimeSpan elapsed = touch.Timestamp - lastTap.Timestamp;
			if (elapsed.TotalMilliseconds > 300)
			{
				return false;
			}

			GestureList.Enqueue(
				new GestureSample(
					GestureType.DoubleTap,
					touch.Timestamp,
					touch.Position,
					Vector2.Zero,
					Vector2.Zero,
					Vector2.Zero
				)
			);

			// Disable taps until after the next release.
			tapDisabled = true;

			return true;
		}

		private void ProcessTap(TouchLocation touch)
		{
			if (!GestureIsEnabled(GestureType.Tap) || tapDisabled)
			{
				return;
			}

			/* If the release is too far away from the press
			 * position then this cannot be a tap event.
			 */
			float dist = Vector2.Distance(touch.PressPosition, touch.Position);
			if (dist > TapJitterTolerance)
			{
				return;
			}

			/* If we pressed and held too long then don't
			 * generate a tap event for it.
			 */
			TimeSpan elapsed = TimeSpan.FromTicks(DateTime.Now.Ticks) - touch.PressTimestamp;
			if (elapsed > maxTicksToProcessHold)
			{
				return;
			}

			// Store the last tap for double tap processing.
			lastTap = touch;

			// Fire off the tap event immediately.
			GestureSample tap = new GestureSample(
				GestureType.Tap,
				touch.Timestamp,
				touch.Position,
				Vector2.Zero,
				Vector2.Zero,
				Vector2.Zero
			);
			GestureList.Enqueue(tap);
		}

		private void ProcessDrag(TouchLocation touch)
		{
			bool dragH = GestureIsEnabled(GestureType.HorizontalDrag);
			bool dragV = GestureIsEnabled(GestureType.VerticalDrag);
			bool dragF = GestureIsEnabled(GestureType.FreeDrag);

			if (!dragH && !dragV && !dragF)
			{
				return;
			}

			/* Make sure this is a move event and that we have
			 * a previous touch location.
			 */
			TouchLocation prevTouch;
			if (	touch.State != TouchLocationState.Moved ||
				!touch.TryGetPreviousLocation(out prevTouch)	)
			{
				return;
			}

			Vector2 delta = touch.Position - prevTouch.Position;

			// If we're free dragging then stick to it.
			if (dragGestureStarted != GestureType.FreeDrag)
			{
				bool isHorizontalDelta = Math.Abs(delta.X) > Math.Abs(delta.Y * 2.0f);
				bool isVerticalDelta = Math.Abs(delta.Y) > Math.Abs(delta.X * 2.0f);
				bool classify = dragGestureStarted == GestureType.None;

				/* Once we enter either vertical or horizontal drags
				 * we stick to it... regardless of the delta.
				 */
				if (	dragH &&
					(	(classify && isHorizontalDelta) ||
						dragGestureStarted == GestureType.HorizontalDrag	)	)
				{
					delta.Y = 0;
					dragGestureStarted = GestureType.HorizontalDrag;
				}
				else if (	dragV &&
						(	(classify && isVerticalDelta) ||
							dragGestureStarted == GestureType.VerticalDrag	)	)
				{
					delta.X = 0;
					dragGestureStarted = GestureType.VerticalDrag;
				}

				/* If the delta isn't either horizontal or vertical
				 * then it could be a free drag if not classified.
				 */
				else if (dragF && classify)
				{
					dragGestureStarted = GestureType.FreeDrag;
				}
				else
				{
					/* If we couldn't classify the drag then
					 * it is nothing... set it to complete.
					 */
					dragGestureStarted = GestureType.DragComplete;
				}
			}

			// If the drag could not be classified then no gesture.
			if (	dragGestureStarted == GestureType.None ||
				dragGestureStarted == GestureType.DragComplete	)
			{
				return;
			}

			tapDisabled = true;
			holdDisabled = true;

			GestureList.Enqueue(
				new GestureSample(
					dragGestureStarted,
					touch.Timestamp,
					touch.Position,
					Vector2.Zero,
					delta, Vector2.Zero
				)
			);
		}

		private void ProcessPinch(TouchLocation[] touches)
		{
			TouchLocation prevPos0;
			TouchLocation prevPos1;

			if (!touches [0].TryGetPreviousLocation(out prevPos0))
			{
				prevPos0 = touches [0];
			}

			if (!touches [1].TryGetPreviousLocation(out prevPos1))
			{
				prevPos1 = touches [1];
			}

			Vector2 delta0 = touches[0].Position - prevPos0.Position;
			Vector2 delta1 = touches[1].Position - prevPos1.Position;

			// Get the newest timestamp.
			TimeSpan timestamp = (touches[0].Timestamp > touches[1].Timestamp) ?
				touches[0].Timestamp : touches[1].Timestamp;

			// If we were already in a drag state then fire off the drag completion event.
			if (dragGestureStarted != GestureType.None)
			{
				if (GestureIsEnabled(GestureType.DragComplete))
				{
					GestureList.Enqueue(
						new GestureSample(
							GestureType.DragComplete,
							timestamp,
							Vector2.Zero,
							Vector2.Zero,
							Vector2.Zero,
							Vector2.Zero
						)
					);
				}

				dragGestureStarted = GestureType.None;
			}

			GestureList.Enqueue(
				new GestureSample(
					GestureType.Pinch,
					timestamp,
					touches[0].Position,
					touches[1].Position,
					delta0,
					delta1
				)
			);

			pinchGestureStarted = true;
			tapDisabled = true;
			holdDisabled = true;
		}

		#endregion
	}
}
