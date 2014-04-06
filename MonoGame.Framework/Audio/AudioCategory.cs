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

namespace Microsoft.Xna.Framework.Audio
{
	// http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.audio.audiocategory.aspx
	public struct AudioCategory : IEquatable<AudioCategory>
	{
		#region Private Float Instance Class

		private class FloatInstance
		{
			public float Value;
			public FloatInstance(float initial)
			{
				Value = initial;
			}
		}

		#endregion

		#region Public Properties

		private string INTERNAL_name;
		public string Name
		{
			get
			{
				return INTERNAL_name;
			}
		}

		#endregion

		#region Private Variables

		private List<Cue> activeCues;

		private Dictionary<string, int> cueInstanceCounts;

		// Grumble, struct returns...
		private FloatInstance INTERNAL_volume;

		#endregion

		#region Internal Constructor

		internal AudioCategory(
			string name,
			float volume
		) {
			INTERNAL_name = name;
			INTERNAL_volume = new FloatInstance(volume);
			activeCues = new List<Cue>();
			cueInstanceCounts = new Dictionary<string, int>();
		}

		#endregion

		#region Public Methods

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
			while (activeCues.Count > 0)
			{
				Cue curCue = activeCues[0];
				curCue.Stop(options);
				curCue.SetVariable("NumCueInstances", 0);
				cueInstanceCounts[curCue.Name] -= 1;
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

		#endregion

		#region Internal Methods

		internal void INTERNAL_update()
		{
			/* Believe it or not, someone might run the update on a thread.
			 * So, we're going to give a lock to this method.
			 * -flibit
			 */
			lock (activeCues)
			{
				// Unmanaged Cues are only removed when the user disposes them.
				for (int i = 0; i < activeCues.Count; i += 1)
				{
					if (!activeCues[i].INTERNAL_update())
					{
						cueInstanceCounts[activeCues[i].Name] -= 1;
						activeCues.RemoveAt(i);
						i -= 1;
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
			for (int i = 0; i < activeCues.Count; i += 1)
			{
				if (activeCues[i].Name.Equals(name))
				{
					activeCues[i].Stop(AudioStopOptions.AsAuthored);
					return;
				}
			}
		}

		internal void INTERNAL_removeQuietestCue(string name)
		{
			float lowestVolume = float.MaxValue;
			int lowestIndex = -1;

			for (int i = 0; i < activeCues.Count; i += 1)
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
			}
		}

		internal void INTERNAL_removeActiveCue(Cue cue)
		{
			if (activeCues.Contains(cue))
			{
				activeCues.Remove(cue);
			}
			if (cueInstanceCounts.ContainsKey(cue.Name))
			{
				cueInstanceCounts[cue.Name] -= 1;
			}
		}

		#endregion
	}
}
