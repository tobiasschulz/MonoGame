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

		private List<Cue> activeCues;

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
			activeCues = new List<Cue>();
			cueInstanceCounts = new Dictionary<string, int>();
		}

		public void Pause()
		{
			foreach (Cue curCue in activeCues)
			{
				curCue.Pause();
			}
		}

		public void Resume()
		{
			foreach (Cue curCue in activeCues)
			{
				curCue.Resume();
			}
		}

		public void SetVolume(float volume)
		{
			INTERNAL_volume.Value = volume;
			foreach (Cue curCue in activeCues)
			{
				curCue.SetVariable("Volume", volume);
			}
		}

		public void Stop(AudioStopOptions options)
		{
			foreach (Cue curCue in activeCues)
			{
				curCue.Stop(options);
				curCue.SetVariable("NumCueInstances", 0);
				cueInstanceCounts[curCue.Name] -= 1;
				curCue.INTERNAL_checkActive();
			}
			activeCues.Clear();
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
			for (int i = 0; i < activeCues.Count; i++)
			{
				activeCues[i].INTERNAL_startPlayback();
				if (!activeCues[i].INTERNAL_checkActive())
				{
					cueInstanceCounts[activeCues[i].Name] -= 1;
					activeCues.RemoveAt(i);
					i--;
				}
				else
				{
					activeCues[i].INTERNAL_update();
				}
			}
			foreach (Cue curCue in activeCues)
			{
				curCue.SetVariable(
					"NumCueInstances",
					cueInstanceCounts[curCue.Name]
				);
			}
		}

		internal void INTERNAL_initCue(Cue newCue)
		{
			if (!cueInstanceCounts.ContainsKey(newCue.Name))
			{
				cueInstanceCounts.Add(newCue.Name, 0);
			}
			newCue.SetVariable("NumCueInstances", cueInstanceCounts[newCue.Name]);
			newCue.SetVariable("Volume", INTERNAL_volume.Value);
		}

		internal void INTERNAL_addCue(Cue newCue)
		{
			cueInstanceCounts[newCue.Name] += 1;
			newCue.SetVariable("NumCueInstances", cueInstanceCounts[newCue.Name]);
			activeCues.Add(newCue);
		}

		internal void INTERNAL_removeLatestCue()
		{
			Cue toDie = activeCues[activeCues.Count - 1];
			cueInstanceCounts[toDie.Name] -= 1;
			activeCues.RemoveAt(activeCues.Count - 1);
		}

		internal void INTERNAL_removeOldestCue(string name)
		{
			for (int i = 0; i < activeCues.Count; i++)
			{
				if (activeCues[i].Name.Equals(name))
				{
					cueInstanceCounts[name] -= 1;
					activeCues[i].Stop(AudioStopOptions.AsAuthored);
					activeCues[i].INTERNAL_checkActive();
					activeCues.RemoveAt(i);
					return;
				}
			}
		}

		internal void INTERNAL_removeQuietestCue(string name)
		{
			float lowestVolume = float.MaxValue;
			int lowestIndex = -1;

			for (int i = 0; i < activeCues.Count; i++)
			{
				if (	activeCues[i].Name.Equals(name) &&
					activeCues[i].GetVariable("Volume") < lowestVolume	)
				{
					lowestVolume = activeCues[i].GetVariable("Volume");
					lowestIndex = i;
				}
			}

			if (lowestIndex > -1)
			{
				cueInstanceCounts[name] -= 1;
				activeCues[lowestIndex].Stop(AudioStopOptions.AsAuthored);
				activeCues[lowestIndex].INTERNAL_checkActive();
				activeCues.RemoveAt(lowestIndex);
			}
		}
	}
}
