#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;
using Microsoft.Xna.Framework.Audio;
using System.Linq;

namespace Microsoft.Xna.Framework.Media
{
    public static class MediaPlayer
    {
		/* Need to hold onto this to keep track of how many songs
		 * have played when in shuffle mode.
		 */
		private static int _numSongsInQueuePlayed = 0;
		private static MediaState _state = MediaState.Stopped;
		private static float _volume = 1.0f;
		private static bool _isMuted = false;
		private static readonly MediaQueue _queue = new MediaQueue();

		public static event EventHandler<EventArgs> ActiveSongChanged;

		static MediaPlayer()
		{
		}

		#region Properties

		public static MediaQueue Queue 
		{
			get
			{
				return _queue;
			}
		}

		public static bool IsMuted
		{
			get
			{
				return _isMuted;
			}

			set
			{
				_isMuted = value;

				if (_queue.Count == 0)
				{
					return;
				}

				float newVolume = value ? 0.0f : _volume;
				_queue.SetVolume(newVolume);
			}
		}

		public static bool IsRepeating {
			get;
			set;
		}

		public static bool IsShuffled
		{
			get;
			set;
		}

		public static bool IsVisualizationEnabled
		{
			get
			{
				return false;
			}
		}

		public static TimeSpan PlayPosition
		{
			get
			{
				if (_queue.ActiveSong == null)
				{
					return TimeSpan.Zero;
				}

				return _queue.ActiveSong.Position;
			}
		}

		public static MediaState State
		{
			get
			{
				return _state;
			}

			private set
			{
				if (_state != value)
				{
					_state = value;
					if (MediaStateChanged != null)
					{
						MediaStateChanged(null, EventArgs.Empty);
					}
				}
			}
		}

		public static event EventHandler<EventArgs> MediaStateChanged;

		public static bool GameHasControl
		{
			get
			{
				// TODO: Fix me!
				return true;
			}
		}

		public static float Volume
		{
			get
			{
				return _volume;
			}
			set
			{
				_volume = value;

				if (_queue.ActiveSong == null)
				{
					return;
				}

				_queue.SetVolume(_isMuted ? 0.0f : value);
			}
		}

		#endregion

		public static void Pause()
		{
			if (State != MediaState.Playing || _queue.ActiveSong == null)
			{
				return;
			}

			_queue.ActiveSong.Pause();

			State = MediaState.Paused;
		}

		/// <summary>
		/// Play clears the current playback queue, and then queues up the specified song for playback.
		/// Playback starts immediately at the beginning of the song.
		/// </summary>
		public static void Play(Song song)
		{
			_queue.Clear();
			_numSongsInQueuePlayed = 0;
			_queue.Add(song);
			_queue.ActiveSongIndex = 0;

			PlaySong(song);
		}

		public static void Play(SongCollection collection, int index = 0)
		{
			_queue.Clear();
			_numSongsInQueuePlayed = 0;

			foreach (Song song in collection)
			{
				_queue.Add(song);
			}

			_queue.ActiveSongIndex = index;

			PlaySong(_queue.ActiveSong);
		}

		private static void PlaySong(Song song)
		{
			song.SetEventHandler(OnSongFinishedPlaying);
			song.Volume = _isMuted ? 0.0f : _volume;
			song.Play();
			State = MediaState.Playing;
		}

		internal static void OnSongFinishedPlaying(object sender, EventArgs args)
		{
			// TODO: Check args to see if song sucessfully played.
			_numSongsInQueuePlayed += 1;

			if (_numSongsInQueuePlayed >= _queue.Count)
			{
				_numSongsInQueuePlayed = 0;
				if (!IsRepeating)
				{
					Stop();

					if (ActiveSongChanged != null)
					{
						ActiveSongChanged.Invoke(null, null);
					}

					return;
				}
			}

			MoveNext();
		}

		public static void Resume()
		{
			if (State != MediaState.Paused)
			{
				return;
			}

			_queue.ActiveSong.Resume();
			State = MediaState.Playing;
		}

		public static void Stop()
		{
			if (State == MediaState.Stopped)
			{
				return;
			}

			// Loop through so that we reset the PlayCount as well.
			foreach (Song song in Queue.Songs)
			{
				_queue.ActiveSong.Stop();
			}

			State = MediaState.Stopped;
		}

		public static void MoveNext()
		{
			NextSong(1);
		}

		public static void MovePrevious()
		{
			NextSong(-1);
		}

		private static void NextSong(int direction)
		{
			Song nextSong = _queue.GetNextSong(direction, IsShuffled);

			if (nextSong == null)
			{
				Stop();
			}
			else
			{
				PlaySong(nextSong);
			}

			if (ActiveSongChanged != null)
			{
				ActiveSongChanged.Invoke(null, null);
			}
		}
	}
}
