#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using System;

namespace Microsoft.Xna.Framework.Media
{
    public static class MediaPlayer
    {
		/* Need to hold onto this to keep track of how many songs
		 * have played when in shuffle mode.
		 */
		private static int numSongsInQueuePlayed = 0;

		public static event EventHandler<EventArgs> ActiveSongChanged;

		static MediaPlayer()
		{
			Queue = new MediaQueue();
		}

		#region Properties

		public static MediaQueue Queue 
		{
			get;
			private set;
		}

		private static bool INTERNAL_isMuted = false;
		public static bool IsMuted
		{
			get
			{
				return INTERNAL_isMuted;
			}

			set
			{
				INTERNAL_isMuted = value;

				if (Queue.Count == 0)
				{
					return;
				}

				Queue.SetVolume(value ? 0.0f : Volume);
			}
		}

		public static bool IsRepeating
		{
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
				if (Queue.ActiveSong == null)
				{
					return TimeSpan.Zero;
				}

				return Queue.ActiveSong.Position;
			}
		}

		private static MediaState INTERNAL_state = MediaState.Stopped;
		public static MediaState State
		{
			get
			{
				return INTERNAL_state;
			}

			private set
			{
				if (INTERNAL_state != value)
				{
					INTERNAL_state = value;
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
				/* This is based on whether or not the player is playing custom
				 * music, rather than yours.
				 * -flibit
				 */
				return true;
			}
		}

		private static float INTERNAL_volume = 1.0f;
		public static float Volume
		{
			get
			{
				return INTERNAL_volume;
			}
			set
			{
				INTERNAL_volume = value;

				if (Queue.ActiveSong == null)
				{
					return;
				}

				Queue.SetVolume(IsMuted ? 0.0f : value);
			}
		}

		#endregion

		public static void Pause()
		{
			if (State != MediaState.Playing || Queue.ActiveSong == null)
			{
				return;
			}

			Queue.ActiveSong.Pause();

			State = MediaState.Paused;
		}

		/// <summary>
		/// Play clears the current playback queue, and then queues up the specified song for playback.
		/// Playback starts immediately at the beginning of the song.
		/// </summary>
		public static void Play(Song song)
		{
			Queue.Clear();
			numSongsInQueuePlayed = 0;
			Queue.Add(song);
			Queue.ActiveSongIndex = 0;

			PlaySong(song);
		}

		public static void Play(SongCollection collection, int index = 0)
		{
			Queue.Clear();
			numSongsInQueuePlayed = 0;

			foreach (Song song in collection)
			{
				Queue.Add(song);
			}

			Queue.ActiveSongIndex = index;

			PlaySong(Queue.ActiveSong);
		}

		private static void PlaySong(Song song)
		{
			song.SetEventHandler(OnSongFinishedPlaying);
			song.Volume = IsMuted ? 0.0f : Volume;
			song.Play();
			State = MediaState.Playing;
		}

		internal static void OnSongFinishedPlaying(object sender, EventArgs args)
		{
			// TODO: Check args to see if song sucessfully played.
			numSongsInQueuePlayed += 1;

			if (numSongsInQueuePlayed >= Queue.Count)
			{
				numSongsInQueuePlayed = 0;
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

			Queue.ActiveSong.Resume();
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
				Queue.ActiveSong.Stop();
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
			Song nextSong = Queue.GetNextSong(direction, IsShuffled);

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
