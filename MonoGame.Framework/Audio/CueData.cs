using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Xna.Framework.Audio
{
	internal class CueData
	{
		public enum MaxInstanceBehavior : byte
		{
			Fail,
			Queue,
			ReplaceOldest,
			ReplaceQuietest,
			ReplaceLowestPriority
		}

		public XACTSound[] Sounds
		{
			get;
			private set;
		}

		public ushort Category
		{
			get;
			private set;
		}

		public float[] Probabilities
		{
			get;
			private set;
		}

		public byte InstanceLimit
		{
			get;
			private set;
		}

		public MaxInstanceBehavior MaxCueBehavior
		{
			get;
			private set;
		}

		public CueData(XACTSound sound)
		{
			Sounds = new XACTSound[1];
			Probabilities = new float[1];

			Sounds[0] = sound;
			Category = sound.Category;
			Probabilities[0] = 1.0f;

			// Assume we can have max instances, for now.
			InstanceLimit = 255;
			MaxCueBehavior = MaxInstanceBehavior.ReplaceOldest;
		}

		public CueData(XACTSound[] sounds, float[] probabilities)
		{
			Sounds = sounds;
			Category = Sounds[0].Category; // FIXME: Assumption!
			Probabilities = probabilities;
		}

		public void SetLimit(byte instanceLimit, byte behavior)
		{
			InstanceLimit = instanceLimit;
			MaxCueBehavior = (MaxInstanceBehavior) (behavior >> 3);
		}
	}

	internal class XACTSound
	{
		private XACTClip[] INTERNAL_clips;

		public float Volume
		{
			get;
			private set;
		}

		public float Pitch
		{
			get;
			private set;
		}

		public ushort Category
		{
			get;
			private set;
		}

		public bool HasLoadedTracks
		{
			get;
			private set;
		}

		public uint[] RPCCodes
		{
			get;
			private set;
		}

		public uint[] DSPCodes
		{
			get;
			private set;
		}

		public XACTSound(ushort track, byte waveBank)
		{
			INTERNAL_clips = new XACTClip[1];
			INTERNAL_clips[0] = new XACTClip(track, waveBank);
			Category = 0;
			Volume = 1.0f;
			HasLoadedTracks = false;
		}

		public XACTSound(BinaryReader reader)
		{
			// Sound Effect Flags
			byte soundFlags = reader.ReadByte();
			bool complex = (soundFlags & 0x01) != 0;

			// AudioCategory Index
			Category = reader.ReadUInt16();

			// Sound Volume
			Volume = XACTCalculator.CalculateVolume(reader.ReadByte());

			// Sound Pitch
			Pitch = (reader.ReadInt16() / 1000.0f);

			// Unknown value
			reader.ReadByte();

			// Length of Sound Entry, unused
			reader.ReadUInt16();

			// Number of Sound Clips
			if (complex)
			{
				INTERNAL_clips = new XACTClip[reader.ReadByte()];
			}
			else
			{
				// Simple Sounds always have 1 PlayWaveEvent.
				INTERNAL_clips = new XACTClip[1];
				ushort track = reader.ReadUInt16();
				byte waveBank = reader.ReadByte();
				INTERNAL_clips[0] = new XACTClip(track, waveBank);
			}

			// Parse RPC Properties
			RPCCodes = new uint[0]; // Eww... -flibit
			if ((soundFlags & 0x0E) != 0)
			{
				// RPC data length, unused
				reader.ReadUInt16();

				// Number of RPC Presets
				RPCCodes = new uint[reader.ReadByte()];

				// Obtain RPC curve codes
				for (byte i = 0; i < RPCCodes.Length; i++)
				{
					RPCCodes[i] = reader.ReadUInt32();
				}
			}

			// Parse DSP Presets
			DSPCodes = new uint[0]; // Eww... -flibit
			if ((soundFlags & 0x10) != 0)
			{
				// DSP Presets Length, unused
				reader.ReadUInt16();

				// Number of DSP Presets
				DSPCodes = new uint[reader.ReadByte()];

				// Obtain DSP Preset codes
				for (byte j = 0; j < DSPCodes.Length; j++)
				{
					DSPCodes[j] = reader.ReadUInt32();
				}
			}

			// Parse Sound Events
			if (complex)
			{
				for (int i = 0; i < INTERNAL_clips.Length; i++)
				{
					// Unknown value
					reader.ReadByte();

					// XACT Clip Offset in Bank
					uint offset = reader.ReadUInt32();

					// Unknown value
					reader.ReadUInt32();

					// Store this for when we're done reading the clip.
					long curPos = reader.BaseStream.Position;

					// Go to the Clip in the Bank.
					reader.BaseStream.Seek(offset, SeekOrigin.Begin);

					// Parse the Clip.
					INTERNAL_clips[i] = new XACTClip(reader);

					// Back to where we were...
					reader.BaseStream.Seek(curPos, SeekOrigin.Begin);
				}
			}

			HasLoadedTracks = false;
		}

		public void LoadTracks(AudioEngine audioEngine, List<string> waveBankNames)
		{
			foreach (XACTClip curClip in INTERNAL_clips)
			{
				curClip.LoadTracks(audioEngine, waveBankNames);
			}
			HasLoadedTracks = true;
		}

		public List<SoundEffectInstance> GenerateInstances()
		{
			// Get the SoundEffectInstance List
			List<SoundEffectInstance> result = new List<SoundEffectInstance>();
			foreach (XACTClip curClip in INTERNAL_clips)
			{
				curClip.GenerateInstances(result);
			}

			// Apply authored volume, pitch
			foreach (SoundEffectInstance sfi in result)
			{
				// Respect volume/pitch variations!
				sfi.Volume *= Volume;
				sfi.Pitch *= Pitch;
			}

			return result;
		}
	}

	internal class XACTClip
	{
		private XACTEvent[] INTERNAL_events;

		public XACTClip(ushort track, byte waveBank)
		{
			INTERNAL_events = new XACTEvent[1];
			INTERNAL_events[0] = new PlayWaveEvent(track, waveBank, 0);
		}

		public XACTClip(BinaryReader reader)
		{
			// Number of XACT Events
			INTERNAL_events = new XACTEvent[reader.ReadByte()];

			for (int i = 0; i < INTERNAL_events.Length; i++)
			{
				// Full Event information
				uint eventInfo = reader.ReadUInt32();

				// XACT Event Type
				uint eventType = eventInfo & 0x0000001F;

				// Load the Event
				if (eventType == 1)
				{
					// Unknown value
					reader.ReadUInt32();

					// WaveBank Track Index
					ushort track = reader.ReadUInt16();

					// WaveBank Index
					byte waveBank = reader.ReadByte();

					// Number of times to loop wave (255 is infinite)
					byte loopCount = reader.ReadByte();

					// Unknown value
					reader.ReadUInt32();

					// Finally.
					INTERNAL_events[i] = new PlayWaveEvent(
						track,
						waveBank,
						loopCount
					);
				}
				else if (eventType == 3)
				{
					// Unknown values
					reader.ReadBytes(9);

					// Number of WaveBank tracks
					ushort numTracks = reader.ReadUInt16();

					// Unknown value
					reader.ReadUInt16();

					// Unknown values
					reader.ReadBytes(4);

					// Obtain WaveBank track information
					ushort[] tracks = new ushort[numTracks];
					byte[] waveBanks = new byte[numTracks];
					byte[] weights = new byte[numTracks];
					for (ushort j = 0; j < numTracks; j++)
					{
						tracks[j] = reader.ReadUInt16();
						waveBanks[j] = reader.ReadByte();
						byte minWeight = reader.ReadByte();
						byte maxWeight = reader.ReadByte();
						weights[j] = (byte) (maxWeight - minWeight);
					}

					// Finally.
					INTERNAL_events[i] = new PlayWaveVariationEvent(
						tracks,
						waveBanks,
						weights
					);
				}
				else if (eventType == 4)
				{
					// Unknown values
					reader.ReadBytes(4);
					
					// WaveBank track
					ushort track = reader.ReadUInt16();
					
					// WaveBank index, unconfirmed
					byte waveBank = reader.ReadByte();
					
					// Loop Count, unconfirmed
					byte loopCount = reader.ReadByte();
					
					// Unknown values
					reader.ReadBytes(4);
					
					// Pitch Variation
					short minPitch = reader.ReadInt16();
					short maxPitch = reader.ReadInt16();
					
					// Volume Variation
					float minVolume = XACTCalculator.CalculateVolume(reader.ReadByte());
					float maxVolume = XACTCalculator.CalculateVolume(reader.ReadByte());

					// Unknown values, but familiar table format
					reader.ReadBytes(17);
					
					// Finally.
					INTERNAL_events[i] = new PlayWavePitchVolumeEvent(
						track,
						waveBank,
						loopCount,
						minPitch,
						maxPitch,
						minVolume,
						maxVolume
					);
				}
				else if (eventType == 6)
				{
					// Unknown values
					reader.ReadBytes(13);

					// Volume variation
					float minVolume = XACTCalculator.CalculateVolume(reader.ReadByte());
					float maxVolume = XACTCalculator.CalculateVolume(reader.ReadByte());

					// Unknown values
					reader.ReadBytes(18);

					// Number of WaveBank tracks
					ushort numTracks = reader.ReadUInt16();

					// Unknown value
					reader.ReadUInt16();

					// Unknown values
					reader.ReadBytes(4);

					// Obtain WaveBank track information
					ushort[] tracks = new ushort[numTracks];
					byte[] waveBanks = new byte[numTracks];
					byte[] weights = new byte[numTracks];
					for (ushort j = 0; j < numTracks; j++)
					{
						tracks[j] = reader.ReadUInt16();
						waveBanks[j] = reader.ReadByte();
						byte minWeight = reader.ReadByte();
						byte maxWeight = reader.ReadByte();
						weights[j] = (byte) (maxWeight - minWeight);
					}

					// Finally.
					INTERNAL_events[i] = new PlayWaveVariationVolumeEvent(
						minVolume,
						maxVolume,
						tracks,
						waveBanks,
						weights
					);
				}
				else if (eventType == 8)
				{
					// Unknown value
					reader.ReadByte();
					
					// Loop Count
					byte loopCount = reader.ReadByte();
					
					// Unknown value
					reader.ReadByte();
					
					// WaveBank index
					byte waveBank = reader.ReadByte();
					
					// Unknown value
					reader.ReadUInt16();
					
					// Unknown value
					reader.ReadByte();
					
					// Track index, I think?
					ushort track = reader.ReadUInt16();
					track = 0; // FIXME: Apparently not.
					
					// Unknown Value
					reader.ReadUInt32();
					
					// Unknown value
					reader.ReadUInt32();
					
					INTERNAL_events[i] = new PlayWaveEvent(
						track,
						waveBank,
						loopCount
					);
				}
				else
				{
					// TODO: All XACT Events
					throw new Exception(
						"EVENT TYPE " + eventType + " NOT IMPLEMENTED!"
					);
				}
			}
		}

		public void LoadTracks(AudioEngine audioEngine, List<string> waveBankNames)
		{
			foreach (XACTEvent curEvent in INTERNAL_events)
			{
				if (curEvent.Type == 1)
				{
					((PlayWaveEvent) curEvent).LoadTrack(
						audioEngine,
						waveBankNames
					);
				}
				else if (curEvent.Type == 3)
				{
					((PlayWaveVariationEvent) curEvent).LoadTracks(
						audioEngine,
						waveBankNames
					);
				}
				else if (curEvent.Type == 4)
				{
					((PlayWavePitchVolumeEvent) curEvent).LoadTrack(
						audioEngine,
						waveBankNames
					);
				}
				else if (curEvent.Type == 6)
				{
					((PlayWaveVariationVolumeEvent) curEvent).LoadTracks(
						audioEngine,
						waveBankNames
					);
				}
			}
		}

		public void GenerateInstances(List<SoundEffectInstance> result)
		{
			foreach (XACTEvent curEvent in INTERNAL_events)
			{
				if (curEvent.Type == 1)
				{
					result.Add(((PlayWaveEvent) curEvent).GenerateInstance());
				}
				else if (curEvent.Type == 3)
				{
					result.Add(((PlayWaveVariationEvent) curEvent).GenerateInstance());
				}
				else if (curEvent.Type == 4)
				{
					result.Add(((PlayWavePitchVolumeEvent) curEvent).GenerateInstance());
				}
				else if (curEvent.Type == 6)
				{
					result.Add(((PlayWaveVariationVolumeEvent) curEvent).GenerateInstance());
				}
			}
		}
	}

	internal abstract class XACTEvent
	{
		public uint Type
		{
			get;
			private set;
		}

		public XACTEvent(uint type)
		{
			Type = type;
		}
	}

	internal class PlayWaveEvent : XACTEvent
	{
		private ushort INTERNAL_track;
		private byte INTERNAL_waveBank;
		private byte INTERNAL_loopCount;

		private SoundEffect INTERNAL_wave;

		public PlayWaveEvent(
			ushort track,
			byte waveBank,
			byte loopCount
		) : base(1) {
			INTERNAL_track = track;
			INTERNAL_waveBank = waveBank;
			INTERNAL_loopCount = loopCount;
		}

		public void LoadTrack(AudioEngine audioEngine, List<string> waveBankNames)
		{
			INTERNAL_wave = audioEngine.INTERNAL_getWaveBankTrack(
				waveBankNames[INTERNAL_waveBank],
				INTERNAL_track
			);
		}

		public SoundEffectInstance GenerateInstance()
		{
			SoundEffectInstance result = INTERNAL_wave.CreateInstance();
			// FIXME: Better looping!
			result.IsLooped = (INTERNAL_loopCount == 255);
			return result;
		}
	}

	internal class PlayWaveVariationEvent : XACTEvent
	{
		private ushort[] INTERNAL_tracks;
		private byte[] INTERNAL_waveBanks;

		private byte[] INTERNAL_weights;

		private SoundEffect[] INTERNAL_waves;

		private static Random random = new Random();

		public PlayWaveVariationEvent(
			ushort[] tracks,
			byte[] waveBanks,
			byte[] weights
		) : base(3) {
			INTERNAL_tracks = tracks;
			INTERNAL_waveBanks = waveBanks;
			INTERNAL_weights = weights;
			INTERNAL_waves = new SoundEffect[tracks.Length];
		}

		public void LoadTracks(AudioEngine audioEngine, List<string> waveBankNames)
		{
			for (int i = 0; i < INTERNAL_waves.Length; i++)
			{
				INTERNAL_waves[i] = audioEngine.INTERNAL_getWaveBankTrack(
					waveBankNames[INTERNAL_waveBanks[i]],
					INTERNAL_tracks[i]
				);
			}
		}

		public SoundEffectInstance GenerateInstance()
		{
			double max = 0.0;
			for (int i = 0; i < INTERNAL_weights.Length; i++)
			{
				max += INTERNAL_weights[i];
			}
			double next = random.NextDouble() * max;
			for (int i = INTERNAL_weights.Length - 1; i >= 0; i--)
			{
				if (next > max - INTERNAL_weights[i])
				{
					return INTERNAL_waves[i].CreateInstance();
				}
				max -= INTERNAL_weights[i];
			}
			throw new Exception("PlayWaveVariationEvent... what?!");
		}
	}

	internal class PlayWavePitchVolumeEvent : XACTEvent
	{
		private ushort INTERNAL_track;
		private byte INTERNAL_waveBank;
		private byte INTERNAL_loopCount;
		private short INTERNAL_minPitch;
		private short INTERNAL_maxPitch;
		private float INTERNAL_minVolume;
		private float INTERNAL_maxVolume;

		private SoundEffect INTERNAL_wave;
		
		private static Random random = new Random();

		public PlayWavePitchVolumeEvent(
			ushort track,
			byte waveBank,
			byte loopCount,
			short minPitch,
			short maxPitch,
			float minVolume,
			float maxVolume
		) : base(4) {
			INTERNAL_track = track;
			INTERNAL_waveBank = waveBank;
			INTERNAL_loopCount = loopCount;
			INTERNAL_minPitch = minPitch;
			INTERNAL_maxPitch = maxPitch;
			INTERNAL_minVolume = minVolume;
			INTERNAL_maxVolume = maxVolume;
		}

		public void LoadTrack(AudioEngine audioEngine, List<string> waveBankNames)
		{
			INTERNAL_wave = audioEngine.INTERNAL_getWaveBankTrack(
				waveBankNames[INTERNAL_waveBank],
				INTERNAL_track
			);
		}

		public SoundEffectInstance GenerateInstance()
		{
			SoundEffectInstance result = INTERNAL_wave.CreateInstance();
			result.Pitch = (random.Next(INTERNAL_minPitch, INTERNAL_maxPitch) / 1000.0f);
			// FIXME: Better looping!
			result.IsLooped = (INTERNAL_loopCount == 255);
			return result;
		}
	}

	internal class PlayWaveVariationVolumeEvent : XACTEvent
	{
		private float INTERNAL_minVolume;
		private float INTERNAL_maxVolume;

		private ushort[] INTERNAL_tracks;
		private byte[] INTERNAL_waveBanks;

		private byte[] INTERNAL_weights;

		private SoundEffect[] INTERNAL_waves;

		private static Random random = new Random();

		public PlayWaveVariationVolumeEvent(
			float minVolume,
			float maxVolume,
			ushort[] tracks,
			byte[] waveBanks,
			byte[] weights
		) : base(6) {
			INTERNAL_minVolume = minVolume;
			INTERNAL_maxVolume = maxVolume;
			INTERNAL_tracks = tracks;
			INTERNAL_waveBanks = waveBanks;
			INTERNAL_weights = weights;
			INTERNAL_waves = new SoundEffect[tracks.Length];
		}

		public void LoadTracks(AudioEngine audioEngine, List<string> waveBankNames)
		{
			for (int i = 0; i < INTERNAL_waves.Length; i++)
			{
				INTERNAL_waves[i] = audioEngine.INTERNAL_getWaveBankTrack(
					waveBankNames[INTERNAL_waveBanks[i]],
					INTERNAL_tracks[i]
				);
			}
		}

		public SoundEffectInstance GenerateInstance()
		{
			double max = 0.0;
			for (int i = 0; i < INTERNAL_weights.Length; i++)
			{
				max += INTERNAL_weights[i];
			}
			double next = random.NextDouble() * max;
			for (int i = INTERNAL_weights.Length - 1; i >= 0; i--)
			{
				if (next > max - INTERNAL_weights[i])
				{
					SoundEffectInstance retVal = INTERNAL_waves[i].CreateInstance();
					return retVal;
				}
				max -= INTERNAL_weights[i];
			}
			throw new Exception("PlayWaveVariationVolumeEvent... what?!");
		}
	}
}
