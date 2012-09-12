using System;
using System.Collections.Generic;

#if IPHONE || WINDOWS || LINUX
using System.Diagnostics;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK;
#elif MONOMAC
using MonoMac.OpenAL;
#endif

namespace Microsoft.Xna.Framework.Audio
{
	internal sealed class OpenALSoundController : IDisposable
	{
        const int PreallocatedBuffers = 16;
        const int PreallocatedSources = 16;

        class BufferAllocation
        {
            public int BufferId;
            public int SourceCount;
        }

	    static OpenALSoundController instance = new OpenALSoundController();
	    public static OpenALSoundController Instance
	    {
            get { return instance; }
	    }

        readonly AudioContext context;

        readonly Stack<int> freeSources;
        readonly HashSet<int> filteredSources;
        readonly List<SoundEffectInstance> activeSoundEffects;

        readonly Stack<int> freeBuffers;
	    readonly Dictionary<SoundEffect, BufferAllocation> allocatedBuffers;

	    readonly int filterId;

        int totalSources, totalBuffers;
	    float lowpassGainHf = 1;

        private OpenALSoundController()
        {
            context = new AudioContext();

            filterId = ALHelper.Efx.GenFilter();
            ALHelper.Efx.Filter(filterId, EfxFilteri.FilterType, (int)EfxFilterType.Lowpass);
            ALHelper.Efx.Filter(filterId, EfxFilterf.LowpassGain, 1);
            ALHelper.Efx.Filter(filterId, EfxFilterf.LowpassGainHF, 1);
            ALHelper.Check();

            AL.DistanceModel(ALDistanceModel.InverseDistanceClamped);
            ALHelper.Check();

            freeBuffers = new Stack<int>(PreallocatedBuffers);
            ExpandBuffers();

            allocatedBuffers = new Dictionary<SoundEffect, BufferAllocation>(PreallocatedBuffers);

            filteredSources = new HashSet<int>();
            activeSoundEffects = new List<SoundEffectInstance>();
            freeSources = new Stack<int>(PreallocatedSources);
            ExpandSources();
        }

        public int RegisterSfxInstance(SoundEffectInstance instance)
        {
            activeSoundEffects.Add(instance);
            return TakeSourceFor(instance.SoundEffect, !instance.SoundEffect.Name.Contains("Ui"));
        }

        public void Update()
        {
            for (int i = activeSoundEffects.Count - 1; i >= 0; i--)
                if (activeSoundEffects[i].RefreshState())
                {
                    activeSoundEffects[i].Dispose();
                    activeSoundEffects.RemoveAt(i);
                }
        }

        int TakeSourceFor(SoundEffect soundEffect, bool filter = false)
        {
            if (freeSources.Count == 0) ExpandSources();
            var sourceId = freeSources.Pop();
            if (filter && ALHelper.Efx.IsInitialized)
            {
                ALHelper.Efx.Filter(filterId, EfxFilterf.LowpassGainHF, MathHelper.Clamp(lowpassGainHf, 0, 1));
                ALHelper.Efx.BindFilterToSource(sourceId, filterId);
                filteredSources.Add(sourceId);
            }

            BufferAllocation allocation;
            if (!allocatedBuffers.TryGetValue(soundEffect, out allocation))
            {
                if (freeBuffers.Count == 0) ExpandBuffers();
                allocatedBuffers.Add(soundEffect, allocation = new BufferAllocation { BufferId = freeBuffers.Pop() });
                AL.BufferData(allocation.BufferId, soundEffect.Format, soundEffect._data, soundEffect.Size, soundEffect.Rate);
                ALHelper.Check();
            }
            allocation.SourceCount++;

            AL.BindBufferToSource(sourceId, allocation.BufferId);
            ALHelper.Check();

            return sourceId;
        }

        public void ReturnSourceFor(SoundEffect soundEffect, int sourceId)
        {
            BufferAllocation allocation;
            if (!allocatedBuffers.TryGetValue(soundEffect, out allocation))
                throw new InvalidOperationException(soundEffect.Name + " not found");

            AL.Source(sourceId, ALSourcei.Buffer, 0);
            ALHelper.Check();

            allocation.SourceCount--;
            if (allocation.SourceCount <= 0)
            {
                allocatedBuffers.Remove(soundEffect);
                freeBuffers.Push(allocation.BufferId);
            }

            freeSources.Push(sourceId);

            if (ALHelper.Efx.IsInitialized && filteredSources.Remove(sourceId))
            {
                ALHelper.Efx.BindFilterToSource(sourceId, 0);
                ALHelper.Check();
            }

            TidySources();
            TidyBuffers();
        }

        public int[] TakeBuffers(int count)
        {
            if (freeBuffers.Count < count) ExpandBuffers();

            var buffersIds = new int[count];
            for (int i = 0; i < count; i++)
                buffersIds[i] = freeBuffers.Pop();

            return buffersIds;
        }

        public int TakeSource()
        {
            if (freeSources.Count == 0) ExpandSources();
            var sourceId = freeSources.Pop();

            if (ALHelper.Efx.IsInitialized)
            {
                filteredSources.Add(sourceId);
                ALHelper.Efx.Filter(filterId, EfxFilterf.LowpassGainHF, MathHelper.Clamp(lowpassGainHf, 0, 1));
                ALHelper.Efx.BindFilterToSource(sourceId, filterId);
            }

            return sourceId;
        }

        public void ReturnBuffers(int[] bufferIds)
        {
            foreach (var b in bufferIds)
                freeBuffers.Push(b);

            TidyBuffers();
        }

        public void ReturnSource(int sourceId)
        {
            if (ALHelper.Efx.IsInitialized)
            {
                ALHelper.Efx.BindFilterToSource(sourceId, 0);
                ALHelper.Check();
                filteredSources.Remove(sourceId);
            }

            freeSources.Push(sourceId);

            AL.Source(sourceId, ALSourcei.Buffer, 0);
            ALHelper.Check();

            TidySources();
        }

        void TidySources()
        {
            bool tidiedUp = false;
            while (freeSources.Count > 2 * PreallocatedSources)
            {
                AL.DeleteSource(freeSources.Pop());
                ALHelper.Check();
                totalSources--;
                tidiedUp = true;
            }
            if (tidiedUp)
                Trace.WriteLine("[OpenAL] Tidied sources down to " + totalSources);
        }
        void TidyBuffers()
        {
            bool tidiedUp = false;
            while (freeBuffers.Count > 2 * PreallocatedBuffers)
            {
                AL.DeleteBuffer(freeBuffers.Pop());
                ALHelper.Check();
                totalBuffers--;
                tidiedUp = true;
            }
            if (tidiedUp)
                Trace.WriteLine("[OpenAL] Tidied buffers down to " + totalBuffers);
        }

        void ExpandBuffers()
        {
            totalBuffers += PreallocatedBuffers;
            Trace.WriteLine("[OpenAL] Expanding buffers to " + totalBuffers);

            var newBuffers = AL.GenBuffers(PreallocatedBuffers);
            ALHelper.Check();

            if (ALHelper.XRam.IsInitialized)
            {
                ALHelper.XRam.SetBufferMode(newBuffers.Length, ref newBuffers[0], XRamExtension.XRamStorage.Hardware);
                ALHelper.Check();
            }

            foreach (var b in newBuffers)
                freeBuffers.Push(b);
        }

        void ExpandSources()
        {
            totalSources += PreallocatedSources;
            Trace.WriteLine("[OpenAL] Expanding sources to " + totalSources);

            var newSources = AL.GenSources(PreallocatedSources);
            ALHelper.Check();

            foreach (var s in newSources)
                freeSources.Push(s);
        }

	    public float LowPassHFGain
	    {
            set
            {
                if (ALHelper.Efx.IsInitialized)
                {
                    foreach (var s in filteredSources)
                    {
                        ALHelper.Efx.Filter(filterId, EfxFilterf.LowpassGainHF, MathHelper.Clamp(value, 0, 1));
                        ALHelper.Efx.BindFilterToSource(s, filterId);
                    }
                    ALHelper.Check();

                    lowpassGainHf = value;
                }
            }
	    }

	    public void Dispose()
	    {
            if (ALHelper.Efx.IsInitialized)
                ALHelper.Efx.DeleteFilter(filterId);

            while (freeSources.Count > 0) AL.DeleteSource(freeSources.Pop());
            while (freeBuffers.Count > 0) AL.DeleteBuffer(freeBuffers.Pop());

            context.Dispose();
            instance = null;
	    }
	}
}

