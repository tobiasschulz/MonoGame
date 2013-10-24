using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Audio
{
	// http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.audio.cue.aspx
	public sealed class Cue : IDisposable
	{
		private AudioEngine INTERNAL_baseEngine;

		private CueData INTERNAL_data;
		private XACTSound INTERNAL_activeSound;
		private List<SoundEffectInstance> INTERNAL_instancePool;

		private bool INTERNAL_isPositional;
		private AudioListener INTERNAL_listener;
		private AudioEmitter INTERNAL_emitter;

		private List<Variable> INTERNAL_variables;

		private static Random random = new Random();

		public bool IsCreated
		{
			get;
			private set;
		}

		public bool IsDisposed
		{
			get;
			private set;
		}

		public bool IsPaused
		{
			get
			{
				if (INTERNAL_instancePool == null)
				{
					return false;
				}
				foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
				{
					if (sfi.State == SoundState.Paused)
					{
						return true;
					}
				}
				return false;
			}
		}

		public bool IsPlaying
		{
			get
			{
				if (INTERNAL_instancePool == null)
				{
					return false;
				}
				foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
				{
					if (sfi.State == SoundState.Playing)
					{
						return true;
					}
				}
				return false;
			}
		}

		public bool IsPrepared
		{
			get;
			private set;
		}

		public bool IsPreparing
		{
			get;
			private set;
		}

		public bool IsStopped
		{
			get
			{
				return !IsPlaying;
			}
		}

		public bool IsStopping
		{
			get
			{
				// FIXME: Authored Stop Options?
				return false;
			}
		}

		public string Name
		{
			get;
			private set;
		}

		public event EventHandler<EventArgs> Disposing;

		internal Cue(
			AudioEngine audioEngine,
			List<string> waveBankNames,
			string name,
			CueData data,
			bool managed
		) {
			INTERNAL_baseEngine = audioEngine;

			Name = name;

			INTERNAL_data = data;
			foreach (XACTSound curSound in data.Sounds)
			{
				if (!curSound.HasLoadedTracks)
				{
					curSound.LoadTracks(
						INTERNAL_baseEngine,
						waveBankNames
					);
				}
			}

			INTERNAL_baseEngine.INTERNAL_addCue(
				this,
				data.Category,
				managed
			);

			INTERNAL_isPositional = false;
		}

		public void Apply3D(AudioListener listener, AudioEmitter emitter)
		{
			if (IsPlaying && !INTERNAL_isPositional)
			{
				throw new InvalidOperationException("Apply3D call after Play!");
			}
			if (listener == null)
			{
				throw new ArgumentNullException("listener");
			}
			if (emitter == null)
			{
				throw new ArgumentNullException("emitter");
			}
			INTERNAL_listener = listener;
			INTERNAL_emitter = emitter;
			SetVariable(
				"Distance",
				Vector3.Distance(
					INTERNAL_emitter.Position,
					INTERNAL_listener.Position
				)
			);
			// TODO: All Internal 3D Audio Variables
			INTERNAL_isPositional = true;
		}

		public void Dispose()
		{
			if (!IsDisposed)
			{
				if (Disposing != null)
				{
					Disposing.Invoke(this, null);
				}
				if (INTERNAL_instancePool != null)
				{
					foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
					{
						sfi.Dispose();
					}
					INTERNAL_instancePool = null;
				}
				IsDisposed = true;
			}
		}

		public float GetVariable(string name)
		{
			if (String.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException("name");
			}
			foreach (Variable curVar in INTERNAL_variables)
			{
				if (name.Equals(curVar.Name))
				{
					return curVar.GetValue();
				}
			}
			throw new Exception("Instance variable not found!");
		}

		public void Pause()
		{
			if (!IsPlaying)
			{
				return;
			}
			foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
			{
				sfi.Pause();
			}
		}

		public void Play()
		{
			if (IsPlaying)
			{
				throw new InvalidOperationException("Cue already playing!");
			}

			double max = 0.0;
			for (int i = 0; i < INTERNAL_data.Probabilities.Length; i++)
			{
				max += INTERNAL_data.Probabilities[i];
			}
			double next = random.NextDouble() * max;
			for (int i = INTERNAL_data.Probabilities.Length - 1; i >= 0; i--)
			{
				if (next > max - INTERNAL_data.Probabilities[i])
				{
					INTERNAL_activeSound = INTERNAL_data.Sounds[i];
					break;
				}
				max -= INTERNAL_data.Probabilities[i];
			}

			INTERNAL_instancePool = INTERNAL_activeSound.GenerateInstances();

			if (INTERNAL_isPositional)
			{
				foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
				{
					sfi.Apply3D(
						INTERNAL_listener,
						INTERNAL_emitter
					);
				}
			}
			foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
			{
				sfi.Play();
			}
		}

		public void Resume()
		{
			if (!IsPaused)
			{
				return;
			}
			foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
			{
				sfi.Resume();
			}
		}

		public void SetVariable(string name, float value)
		{
			if (String.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException("name");
			}
			foreach (Variable curVar in INTERNAL_variables)
			{
				if (name.Equals(curVar.Name))
				{
					curVar.SetValue(value);
					return;
				}
			}
			throw new Exception("Instance variable not found!");
		}

		public void Stop(AudioStopOptions options)
		{
			if (!IsPlaying)
			{
				return;
			}
			foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
			{
				sfi.Stop();
			}
		}

		internal void INTERNAL_update()
		{
			if (INTERNAL_instancePool == null)
			{
				return; // Nothing to do... for now.
			}

			for (int i = 0; i < INTERNAL_instancePool.Count; i++)
			{
				if (INTERNAL_instancePool[i].State == SoundState.Stopped)
				{
					INTERNAL_instancePool[i].Dispose();
					INTERNAL_instancePool.RemoveAt(i);
					i--;
				}
			}

			if (INTERNAL_isPositional)
			{
				foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
				{
					sfi.Apply3D(
						INTERNAL_listener,
						INTERNAL_emitter
					);
				}
			}

			float rpcVolume = 1.0f;
			foreach (uint curCode in INTERNAL_activeSound.RPCCodes)
			{
				RPC curRPC = INTERNAL_baseEngine.INTERNAL_getRPC(curCode);
				float result;
				try
				{
					result = curRPC.CalculateRPC(GetVariable(curRPC.Variable));
				}
				catch
				{
					// It's a global variable we're looking for!
					result = curRPC.CalculateRPC(
						INTERNAL_baseEngine.GetGlobalVariable(
							curRPC.Variable
						)
					);
				}
				if (curRPC.Parameter == RPCParameter.Volume)
				{
					rpcVolume *= 1.0f + (result / 10000.0f);
				}
				else
				{
					throw new Exception("RPC Parameter Type: " + curRPC.Parameter);
				}
			}
			foreach (SoundEffectInstance sfi in INTERNAL_instancePool)
			{
				sfi.Volume = GetVariable("Volume") * rpcVolume;
			}
		}

		internal void INTERNAL_genVariables(List<Variable> cueVariables)
		{
			INTERNAL_variables = cueVariables;
		}
	}
}
