using System;
using System.IO;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Audio
{
	internal class XactClip
	{
		float volume;
		
		abstract class ClipEvent {
			public XactClip clip;
			
			public abstract void Update();
			public abstract void Play();
			public abstract void PlayPositional(AudioListener listener, AudioEmitter emitter);
			public abstract void UpdatePosition(AudioListener listener, AudioEmitter emitter);
			public abstract void Stop();
			public abstract void Pause();
			public abstract void Resume();
			public abstract bool Playing { get; }
			public abstract float Volume { get; set; }
			public abstract bool IsPaused { get; }
			public abstract bool IsLooped { get; set; }
		}
		
		class EventPlayWave : ClipEvent {
			public SoundEffect wave;
			private List<SoundEffectInstance> instancePool = new List<SoundEffectInstance>();
			public override void Update() {
				for (int i = 0; i < instancePool.Count; i++)
				{
					if (instancePool[i].State == SoundState.Stopped)
					{
						instancePool[i].Dispose();
						instancePool.RemoveAt(i);
						i--;
					}
				}
			}
			public override void Play() {
				SoundEffectInstance newInstance = wave.CreateInstance();
				newInstance.Volume = Volume;
				newInstance.IsLooped = IsLooped;
				newInstance.Play();
				instancePool.Add(newInstance);
			}
			public override void PlayPositional(AudioListener listener, AudioEmitter emitter) {
				SoundEffectInstance newInstance = wave.CreateInstance();
				newInstance.Volume = Volume;
				newInstance.IsLooped = IsLooped;
				newInstance.Apply3D(listener, emitter);
				newInstance.Play();
				instancePool.Add(newInstance);
			}
			public override void UpdatePosition(AudioListener listener, AudioEmitter emitter) {
				// FIXME: How the hell are you meant to update a Cue position?! -flibit
				instancePool[instancePool.Count - 1].Apply3D(listener, emitter);
			}
			public override void Stop() {
				foreach (SoundEffectInstance sfi in instancePool)
				{
					sfi.Stop();
					sfi.Dispose();
				}
				instancePool.Clear();
			}
			public override void Pause() {
				foreach (SoundEffectInstance sfi in instancePool)
				{
					sfi.Pause();
				}
			}
			public override void Resume()
			{
				foreach (SoundEffectInstance sfi in instancePool)
				{
					sfi.Resume();
				}
			}
			public override bool Playing {
				get
				{
					foreach (SoundEffectInstance sfi in instancePool)
					{
						if (sfi.State != SoundState.Stopped)
						{
							return true;
						}
					}
					return false;
				}
			}
			public override bool IsPaused
			{
				get
				{
					foreach (SoundEffectInstance sfi in instancePool)
					{
						if (sfi.State == SoundState.Paused)
						{
							return true;
						}
					}
					return false;
				}
			}
			private float INTERNAL_volume = 1.0f;
			public override float Volume {
				get
				{
					return INTERNAL_volume;
				}
				set
				{
					INTERNAL_volume = value;
					foreach (SoundEffectInstance sfi in instancePool)
					{
						sfi.Volume = INTERNAL_volume;
					}
				}
			}
			private bool INTERNAL_looped = false;
			public override bool IsLooped {
				get
				{
					return INTERNAL_looped;
				}
				set
				{
					INTERNAL_looped = value;
					foreach (SoundEffectInstance sfi in instancePool)
					{
						sfi.IsLooped = INTERNAL_looped;
					}
				}
			}
		}
		
		ClipEvent[] events;
		
		public XactClip (SoundBank soundBank, BinaryReader clipReader, uint clipOffset)
		{
			long oldPosition = clipReader.BaseStream.Position;
			clipReader.BaseStream.Seek (clipOffset, SeekOrigin.Begin);
			
			byte numEvents = clipReader.ReadByte();
			events = new ClipEvent[numEvents];
			
			for (int i=0; i<numEvents; i++) {
				uint eventInfo = clipReader.ReadUInt32();
				
				uint eventId = eventInfo & 0x1F;
				switch (eventId) {
				case 1:
				case 4:
					EventPlayWave evnt = new EventPlayWave();
					
					
					clipReader.ReadUInt32 (); //unkn
					uint trackIndex = clipReader.ReadUInt16 ();
					byte waveBankIndex = clipReader.ReadByte ();
					
					
					var loopCount = clipReader.ReadByte ();
				    // if loopCount == 255 its an infinite loop
					// otherwise it loops n times..
				    // unknown
					clipReader.ReadUInt16 ();
					clipReader.ReadUInt16 ();
					
					evnt.wave = soundBank.GetWave(waveBankIndex, trackIndex);
					evnt.IsLooped = loopCount == 255;
					
					events[i] = evnt;
					break;
				default:
					throw new NotImplementedException("eventInfo & 0x1F = " + eventId);
				}
				
				events[i].clip = this;
			}
			
			
			clipReader.BaseStream.Seek (oldPosition, SeekOrigin.Begin);
		}

		public XactClip(SoundEffect effect)
		{
			EventPlayWave evt = new EventPlayWave();
			evt.wave = effect;
			events = new ClipEvent[1];
			events[0] = evt;
			events[0].clip = this;
		}

		public void Update() {
			foreach (ClipEvent evt in events)
			{
				evt.Update();
			}
		}
		
		public void Play() {
			//TODO: run events
			events[0].Play ();
		}

		public void Resume()
		{
			events[0].Resume();
		}
		
		public void Stop() {
			events[0].Stop ();
		}
		
		public void Pause() {
			events[0].Pause();
		}
		
		public bool Playing {
			get {
				return events[0].Playing;
			}
		}
		
		public float Volume {
			get {
				return volume;
			}
			set {
				volume = value;
				events[0].Volume = value;
			}
		}

		// Needed for positional audio
		internal void PlayPositional(AudioListener listener, AudioEmitter emitter) {
			// TODO: run events
			events[0].PlayPositional(listener, emitter);
		}

		internal void UpdatePosition(AudioListener listener, AudioEmitter emitter) {
			// TODO: run events
			events[0].UpdatePosition(listener, emitter);
		}

		public bool IsPaused { 
			get { 
				return events[0].IsPaused; 
			} 
		}
	}
}

