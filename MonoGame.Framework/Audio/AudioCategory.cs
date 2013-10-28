using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Audio
{
	// http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.audio.audiocategory.aspx
	public struct AudioCategory : IEquatable<AudioCategory>
	{
		private class FloatInstance
		{
			public float Value;
			public FloatInstance(float initial)
			{
				Value = initial;
			}
		}

		private List<Cue> managedCues;
		private List<Cue> unmanagedCues;

		private Dictionary<string, int> cueInstanceCounts;

		// Grumble, struct returns...
		private FloatInstance INTERNAL_volume;

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
			INTERNAL_volume = new FloatInstance(volume);
			managedCues = new List<Cue>();
			unmanagedCues = new List<Cue>();
			cueInstanceCounts = new Dictionary<string, int>();
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
			INTERNAL_volume.Value = volume;
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
			return Name.GetHashCode();
		}

		public bool Equals(AudioCategory other)
		{
			return (GetHashCode() == other.GetHashCode());
		}

		public override bool Equals(Object obj)
		{
			if (obj is AudioCategory)
			{
				return Equals((AudioCategory) obj);
			}
			return false;
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
					cueInstanceCounts[unmanagedCues[i].Name] -= 1;
					unmanagedCues.RemoveAt(i);
					i--;
				}
				else
				{
					unmanagedCues[i].SetVariable(
						"NumCueInstances",
						cueInstanceCounts[unmanagedCues[i].Name]
					);
					unmanagedCues[i].INTERNAL_update();
				}
			}

			// Managed Cues are removed when they have stopped playing.
			for (int i = 0; i < managedCues.Count; i++)
			{
				if (!managedCues[i].IsPlaying)
				{
					cueInstanceCounts[managedCues[i].Name] -= 1;
					managedCues[i].Dispose();
					managedCues.RemoveAt(i);
					i--;
				}
				else
				{
					managedCues[i].SetVariable(
						"NumCueInstances",
						cueInstanceCounts[managedCues[i].Name]
					);
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
			if (cueInstanceCounts.ContainsKey(newCue.Name))
			{
				cueInstanceCounts[newCue.Name] += 1;
			}
			else
			{
				cueInstanceCounts.Add(newCue.Name, 1);
			}
			newCue.SetVariable("NumCueInstances", cueInstanceCounts[newCue.Name]);
			newCue.SetVariable("Volume", INTERNAL_volume.Value);
		}

		internal bool INTERNAL_removeOldestCue(string name)
		{
			// Try to remove a managed Cue first
			for (int i = 0; i < managedCues.Count; i++)
			{
				if (managedCues[i].Name.Equals(name))
				{
					cueInstanceCounts[name] -= 1;
					managedCues[i].Stop(AudioStopOptions.AsAuthored);
					managedCues[i].Dispose();
					managedCues.RemoveAt(i);
					return true;
				}
			}
			foreach (Cue curCue in unmanagedCues)
			{
				if (curCue.Name.Equals(name) && curCue.IsPlaying)
				{
					// We can't remove the instance, only stop it.
					curCue.Stop(AudioStopOptions.AsAuthored);
					return true;
				}
			}

			// Didn't find anything...
			return false;
		}

		internal bool INTERNAL_removeQuietestCue(string name)
		{
			float lowestVolume = float.MaxValue;
			int lowestIndex = -1;
			for (int i = 0; i < managedCues.Count; i++)
			{
				if (	managedCues[i].Name.Equals(name) &&
					managedCues[i].GetVariable("Volume") < lowestVolume	)
				{
					lowestVolume = managedCues[i].GetVariable("Volume");
					lowestIndex = i;
				}
			}
			for (int i = 0; i < unmanagedCues.Count; i++)
			{
				if (	unmanagedCues[i].Name.Equals(name) &&
					unmanagedCues[i].GetVariable("Volume") < lowestVolume	)
				{
					lowestVolume = unmanagedCues[i].GetVariable("Volume");
					lowestIndex = i + managedCues.Count;
				}
			}

			if (lowestIndex == -1)
			{
				// Didn't find anything...
				return false;
			}

			if (lowestIndex >= managedCues.Count)
			{
				// We can't remove the instance, only stop it.
				unmanagedCues[lowestIndex - managedCues.Count].Stop(AudioStopOptions.AsAuthored);
				return true;
			}
			else
			{
				cueInstanceCounts[name] -= 1;
				managedCues[lowestIndex].Stop(AudioStopOptions.AsAuthored);
				managedCues[lowestIndex].Dispose();
				managedCues.RemoveAt(lowestIndex);
				return true;
			}
		}
	}
}
