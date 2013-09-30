using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Xna.Framework.Audio
{
	internal class XactSound
	{
		internal uint category;

		internal byte[] rpcEffects;
		internal Dictionary<string, float> rpcVariables = new Dictionary<string, float>();

		bool complexSound;
		XactClip[] soundClips;

		float staticVolume;
		
		public XactSound (SoundBank soundBank, BinaryReader soundReader, uint soundOffset)
		{
			long oldPosition = soundReader.BaseStream.Position;
			soundReader.BaseStream.Seek (soundOffset, SeekOrigin.Begin);
			
			byte flags = soundReader.ReadByte ();
			complexSound = (flags & 1) != 0;
			
			category = soundReader.ReadUInt16 ();
			uint volume = soundReader.ReadByte (); // FIXME: Maybe wrong?
			uint pitch = soundReader.ReadUInt16 (); // FIXME: Maybe wrong?
			soundReader.ReadByte (); //unkn
			uint entryLength = soundReader.ReadUInt16 ();
			
			uint numClips = 0;
			if (complexSound) {
				numClips = (uint)soundReader.ReadByte ();
			} else {
				uint trackIndex = soundReader.ReadUInt16 ();
				byte waveBankIndex = soundReader.ReadByte ();
				soundClips = new XactClip[1];
				soundClips[0] = new XactClip(soundBank.GetWave(waveBankIndex, trackIndex));
			}
			
			if ( (flags & 0x1E) != 0 ) {
				uint extraDataLen = soundReader.ReadUInt16 ();

				if ((flags & 0x10) != 0) { // FIXME: Verify this!
					throw new NotImplementedException("XACT DSP Preset tables!");
				} else if ((flags == 0x02) || (flags == 0x03)) { // FIXME: Verify this!

					// The number of RPC presets that affect this sound.
					uint numRPCPresets = soundReader.ReadByte();

					rpcEffects = new byte[numRPCPresets];

					for (uint i = 0; i < numRPCPresets; i++) {
						byte rpcTable = soundReader.ReadByte();

						// !!! FIXME: Anyone know how these bytes work? -flibit

						// System.Console.WriteLine(rpcTable);

						// !!! HACK: Screw it, I need these working. -flibit

						// Codename lolno has these RPC entries...
						// All affect Volume, based on the Distance variable.
						// 1 1 0 0 0 1 1 0 --- 198 - Attenuation
						// 1 1 1 1 1 0 0 0 --- 248 - Attenuation_high
						// 0 0 1 0 0 0 0 1 --- 033 - Attenuation_low

						if (rpcTable == 198 || rpcTable == 218) {
							rpcEffects[i] = 0;
						} else if (rpcTable == 248) {
							rpcEffects[i] = 1;
						} else if (rpcTable == 033) {
							rpcEffects[i] = 2;
						} else {
							throw new NotImplementedException("Check the XACT RPC parsing!");
						}
					}

					// Create the variable table
					for (int i = 0; i < rpcEffects.Length; i++)
					{
						rpcVariables.Add(
							soundBank.audioengine.variables[soundBank.audioengine.rpcCurves[rpcEffects[i]].variable].name,
							soundBank.audioengine.variables[soundBank.audioengine.rpcCurves[rpcEffects[i]].variable].initValue
						);
					}

					// Seek to the end of this block.
					soundReader.BaseStream.Seek(extraDataLen - 3 - numRPCPresets, SeekOrigin.Current);
				} else {
					// Screw it, just skip the block.
					soundReader.BaseStream.Seek(extraDataLen - 2, SeekOrigin.Current);
				}
			}
			
			if (complexSound) {
				soundClips = new XactClip[numClips];
				for (int i=0; i<numClips; i++) {
					soundReader.ReadByte (); //unkn
					uint clipOffset = soundReader.ReadUInt32 ();
					soundReader.ReadUInt32 (); //unkn
					
					soundClips[i] = new XactClip(soundBank, soundReader, clipOffset);
				}
			}

			// FIXME: This is totally arbitrary. I dunno the exact ratio here.
			staticVolume = volume / 256.0f;
			Volume = 1.0f;

			soundReader.BaseStream.Seek (oldPosition, SeekOrigin.Begin);
		}
		
//		public XactSound (Sound sound) {
//			complexSound = false;
//			wave = sound;
//		}
		public XactSound (SoundEffect sound) {
			soundClips = new XactClip[1];
			soundClips[0] = new XactClip(sound);
		}		

		public void Update()
		{
			foreach (XactClip clip in soundClips)
			{
				clip.Update();
			}
		}

		public void Play() {
				foreach (XactClip clip in soundClips) {
					clip.Play();
				}
		}

		internal void PlayPositional(AudioListener listener, AudioEmitter emitter) {
				foreach (XactClip clip in soundClips) {
					clip.PlayPositional(listener, emitter);
				}
		}

		internal void UpdatePosition(AudioListener listener, AudioEmitter emitter)
		{
				foreach (XactClip clip in soundClips) {
					clip.UpdatePosition(listener, emitter);
				}
			
		}
		
		public void Stop() {
				foreach (XactClip clip in soundClips) {
					clip.Stop();
				}
		}
		
		public void Pause() {
				foreach (XactClip clip in soundClips) {
					clip.Pause();
				}
		}
                
		public void Resume() {
				foreach (XactClip clip in soundClips) {
					clip.Resume();
				}
		}
		
		public float Volume {
			get {
					return soundClips[0].Volume;
			}
			set {
					foreach (XactClip clip in soundClips) {
						clip.Volume = value * staticVolume;
					}
			}
		}
		
		public bool Playing {
			get {
					foreach (XactClip clip in soundClips) {
						if (clip.Playing) return true;
					}
					return false;
			}
		}

		public bool IsPaused
		{
			get
			{
					foreach (XactClip clip in soundClips) {
						if (clip.IsPaused) return true;
					}
					return false;
			}
		}
		
	}
}

