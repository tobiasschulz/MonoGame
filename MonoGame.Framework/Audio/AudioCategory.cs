using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Audio
{
	// http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.audio.audiocategory.aspx
	public struct AudioCategory : IEquatable<AudioCategory>
	{
		private List<Cue> managedCues;
		private List<Cue> unmanagedCues;

		private float INTERNAL_volume;

		private string INTERNAL_name;
		public string Name
		{
			get
			{
				return INTERNAL_name;
			}
		}

		internal AudioCategory(
			string name,
			float volume
		) {
			INTERNAL_name = name;
			INTERNAL_volume = volume;
			managedCues = new List<Cue>();
			unmanagedCues = new List<Cue>();
		}

		public void Pause()
		{
			foreach (Cue curCue in managedCues)
			{
				curCue.Pause();
			}
			foreach (Cue curCue in unmanagedCues)
			{
				curCue.Pause();
			}
		}

		public void Resume()
		{
			foreach (Cue curCue in managedCues)
			{
				curCue.Resume();
			}
			foreach (Cue curCue in unmanagedCues)
			{
				curCue.Resume();
			}
		}

		public void SetVolume(float volume)
		{
			INTERNAL_volume = volume;
			foreach (Cue curCue in managedCues)
			{
				curCue.SetVariable("Volume", volume);
			}
			foreach (Cue curCue in unmanagedCues)
			{
				curCue.SetVariable("Volume", volume);
			}
		}

		public void Stop(AudioStopOptions options)
		{
			foreach (Cue curCue in managedCues)
			{
				curCue.Stop(options);
			}
			foreach (Cue curCue in unmanagedCues)
			{
				curCue.Stop(options);
			}
		}

		public override int GetHashCode()
		{
			// FIXME: Verify this!
			return Name.GetHashCode();
		}

		public bool Equals(AudioCategory other)
		{
			return (GetHashCode() == other.GetHashCode());
		}

		public override bool Equals(Object obj)
		{
			// FIXME: Check obj type
			return (GetHashCode() == obj.GetHashCode());
		}

		public static bool op_Equality(
			AudioCategory value1,
			AudioCategory value2
		) {
			return value1.Equals(value2);
		}

		public static bool op_Inequality(
			AudioCategory value1,
			AudioCategory value2
		) {
			return !(value1.Equals(value2));
		}

		internal void INTERNAL_update()
		{
			// Unmanaged Cues are only removed when the user disposes them.
			for (int i = 0; i < unmanagedCues.Count; i++)
			{
				if (unmanagedCues[i].IsDisposed)
				{
					unmanagedCues.RemoveAt(i);
					i--;
				}
				else
				{
					unmanagedCues[i].INTERNAL_update();
				}
			}

			// Managed Cues are removed when they have stopped playing.
			for (int i = 0; i < managedCues.Count; i++)
			{
				if (!managedCues[i].IsPlaying)
				{
					managedCues[i].Dispose();
					managedCues.RemoveAt(i);
					i--;
				}
				else
				{
					managedCues[i].INTERNAL_update();
				}
			}
		}

		internal void INTERNAL_addCue(Cue newCue, bool managed)
		{
			if (managed)
			{
				managedCues.Add(newCue);
			}
			else
			{
				unmanagedCues.Add(newCue);
			}
			newCue.SetVariable("Volume", INTERNAL_volume);
		}
	}
}
