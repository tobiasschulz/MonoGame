using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

#if IOS || WINDOWS || LINUX
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
#elif MONOMAC
using MonoMac.OpenAL;
#endif

namespace Microsoft.Xna.Framework.Audio
{
    internal sealed class OpenALSoundController : IDisposable
    {
        const int PreallocatedBuffers = 24;
        const int PreallocatedSources = 16;
        const float BufferTimeout = 10; // in seconds

        class BufferAllocation
        {
            public int BufferId;
            public int SourceCount;
            public float SinceUnused;
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

        //readonly object bufferDataMutex = new object();

        int totalSources, totalBuffers;
        float lowpassGainHf = 1;

        static void Log(string message)
        {
            try
            {
                using (var stream = File.Open("Debug Log.txt", FileMode.Append))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.WriteLine("({0}) [{1}] {2} : {3}",
                            DateTime.Now.ToString("HH:mm:ss.fff"), "OpenAL", "INFORMATION", message);
                    }
                }
            }
            catch (Exception ex)
            {
                // NOT THAT BIG A DEAL GUYS
            }
        }

        [DllImport("kernel32", SetLastError = true)]
        static extern bool FreeLibrary(IntPtr hModule);

        public static void UnloadModule(string moduleName)
        {
            var module = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
                .SingleOrDefault(m => m.ModuleName == moduleName);

            if (module != null)
                FreeLibrary(module.BaseAddress);
        }

        private static bool IsFileLocked(Exception exception)
        {
            int errorCode = Marshal.GetHRForException(exception) & ((1 << 16) - 1);
            return errorCode == 32 || errorCode == 33;
        }
        protected static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        private OpenALSoundController()
        {
            if (File.Exists("openal32.dll"))
            try
            {
                File.SetAttributes("openal32.dll", FileAttributes.Normal);
                File.Delete("openal32.dll");
            }
            catch (Exception ex)
            {
                // no big deal, we either already have the right openal32.dll file, or we're about to create it
                Log("Exception while deleting, ignored : " + ex);
            }

            if (!File.Exists("openal32.dll"))
            {
                // choose the right DLL!
                if (IntPtr.Size == 8) File.Copy("soft_oal_64.dll", "openal32.dll");
                else File.Copy("soft_oal_32.dll", "openal32.dll");
            }

            try
            {
                context = new AudioContext();
            }
            catch (TypeInitializationException)
            {
                // Unload OpenAL DLL
                UnloadModule("openal32.dll");

                // Badly advertised, but we're using the wrong DLL... try the other one. (and hope that it works!)
                if (File.Exists("openal32.dll"))
                {
                    try
                    {
                        Log("Attempting to delete openal32.dll because of failed initialization");
                        File.SetAttributes("openal32.dll", FileAttributes.Normal);

                        // try to wait 1 second for the lock to clear
                        int millisecondsWaited = 0;
                        while (millisecondsWaited < 1000 && IsFileLocked(new FileInfo("openal32.dll")))
                        {
                            Thread.Sleep(50);
                            millisecondsWaited += 50;
                        }

                        File.Delete("openal32.dll");
                        Log("Deleted!");
                    }
                    catch (Exception ex)
                    {
                        Log("Exception while deleting : " + ex);
                        if (IsFileLocked(ex))
                            Log("File was locked.");
                        throw;
                    }
                }

                if (IntPtr.Size != 8) File.Copy("soft_oal_64.dll", "openal32.dll");
                else File.Copy("soft_oal_32.dll", "openal32.dll");

                context = new AudioContext();
            }

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
            staleAllocations = new List<KeyValuePair<SoundEffect, BufferAllocation>>();

            filteredSources = new HashSet<int>();
            activeSoundEffects = new List<SoundEffectInstance>();
            freeSources = new Stack<int>(PreallocatedSources);
            ExpandSources();
        }

        public int RegisterSfxInstance(SoundEffectInstance instance, bool forceNoFilter = false)
        {
            activeSoundEffects.Add(instance);
            var doFilter = !forceNoFilter &&
                           !instance.SoundEffect.Name.Contains("Ui") && !instance.SoundEffect.Name.Contains("Warp") &&
                           !instance.SoundEffect.Name.Contains("Zoom");
            return TakeSourceFor(instance.SoundEffect, doFilter);
        }

        readonly List<KeyValuePair<SoundEffect, BufferAllocation>> staleAllocations;
        public void Update(GameTime gameTime)
        {
            for (int i = activeSoundEffects.Count - 1; i >= 0; i--)
            {
                var sfx = activeSoundEffects[i];
                if (sfx.RefreshState())
                {
                    if (!sfx.IsDisposed) sfx.Dispose();
                    activeSoundEffects.RemoveAt(i);
                }
            }

            var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            foreach (var kvp in allocatedBuffers)
                if (kvp.Value.SourceCount == 0)
                {
                    kvp.Value.SinceUnused += elapsedSeconds;
                    if (kvp.Value.SinceUnused >= BufferTimeout)
                        staleAllocations.Add(kvp);
                }

            foreach (var kvp in staleAllocations)
            {
                //Trace.WriteLine("[OpenAL] Deleting buffer for " + kvp.Key.Name);
                allocatedBuffers.Remove(kvp.Key);
                freeBuffers.Push(kvp.Value.BufferId);
            }

            TidySources();
            TidyBuffers();

            staleAllocations.Clear();
        }

        public void RegisterSoundEffect(SoundEffect soundEffect)
        {
            if (allocatedBuffers.ContainsKey(soundEffect)) return;

            if (freeBuffers.Count == 0) ExpandBuffers();
            Trace.WriteLine("[OpenAL] Pre-allocating buffer for " + soundEffect.Name);
            BufferAllocation allocation;
            allocatedBuffers.Add(soundEffect, allocation = new BufferAllocation { BufferId = freeBuffers.Pop(), SinceUnused = -1 });
            //lock (bufferDataMutex)
            AL.BufferData(allocation.BufferId, soundEffect.Format, soundEffect._data, soundEffect.Size, soundEffect.Rate);
            ALHelper.Check();
        }
        public void DestroySoundEffect(SoundEffect soundEffect)
        {
            BufferAllocation allocation;
            if (!allocatedBuffers.TryGetValue(soundEffect, out allocation))
                return;

            bool foundActive = false;
            for (int i = activeSoundEffects.Count - 1; i >= 0; i--)
            {
                var sfx = activeSoundEffects[i];
                if (sfx.SoundEffect == soundEffect)
                {
                    if (!sfx.IsDisposed)
                    {
                        foundActive = true;
                        sfx.Stop();
                        sfx.Dispose();
                    }
                    activeSoundEffects.RemoveAt(i);
                }
            }

            if (foundActive)
                Trace.WriteLine("[OpenAL] Delete active sources & buffer for " + soundEffect.Name);

            Debug.Assert(allocation.SourceCount == 0);

            allocatedBuffers.Remove(soundEffect);
            freeBuffers.Push(allocation.BufferId);
        }

        int TakeSourceFor(SoundEffect soundEffect, bool filter = false)
        {
            if (freeSources.Count == 0) ExpandSources();
            var sourceId = freeSources.Pop();
            if (filter && ALHelper.Efx.IsInitialized)
            {
                ALHelper.Efx.Filter(filterId, EfxFilterf.LowpassGainHF, MathHelper.Clamp(lowpassGainHf, 0, 1));
                ALHelper.Efx.BindFilterToSource(sourceId, filterId);
                lock (filteredSources)
                filteredSources.Add(sourceId);
            }

            BufferAllocation allocation;
            if (!allocatedBuffers.TryGetValue(soundEffect, out allocation))
            {
                if (freeBuffers.Count == 0) ExpandBuffers();
                //Trace.WriteLine("[OpenAL] Allocating buffer for " + soundEffect.Name);
                allocatedBuffers.Add(soundEffect, allocation = new BufferAllocation { BufferId = freeBuffers.Pop() });
                //lock (bufferDataMutex)
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

            allocation.SourceCount--;
            if (allocation.SourceCount == 0) allocation.SinceUnused = 0;
            Debug.Assert(allocation.SourceCount >= 0);

            ReturnSource(sourceId);
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
                lock (filteredSources)
                filteredSources.Add(sourceId);
                ALHelper.Efx.Filter(filterId, EfxFilterf.LowpassGainHF, MathHelper.Clamp(lowpassGainHf, 0, 1));
                ALHelper.Efx.BindFilterToSource(sourceId, filterId);
            }

            return sourceId;
        }

        public void SetSourceFiltered(int sourceId, bool filtered)
        {
            if (!ALHelper.Efx.IsInitialized) return;

            lock (filteredSources)
            {
                if (!filtered && filteredSources.Remove(sourceId))
                {
                    ALHelper.Efx.Filter(filterId, EfxFilterf.LowpassGainHF, 1);
                    ALHelper.Efx.BindFilterToSource(sourceId, 0);
                }
                else if (filtered && !filteredSources.Contains(sourceId))
                {
                    filteredSources.Add(sourceId);
                    ALHelper.Efx.Filter(filterId, EfxFilterf.LowpassGainHF, MathHelper.Clamp(lowpassGainHf, 0, 1));
                    ALHelper.Efx.BindFilterToSource(sourceId, filterId);
                }
            }
        }

        public void ReturnBuffers(int[] bufferIds)
        {
            foreach (var b in bufferIds)
                freeBuffers.Push(b);
        }

        public void ReturnSource(int sourceId)
        {
            ResetSource(sourceId);
        }

        void ResetSource(int sourceId)
        {
            AL.Source(sourceId, ALSourceb.Looping, false);
            AL.Source(sourceId, ALSource3f.Position, 0, 0.0f, 0.1f);
            AL.Source(sourceId, ALSourcef.Pitch, 1);
            AL.Source(sourceId, ALSourcef.Gain, 1);
            AL.Source(sourceId, ALSourcei.Buffer, 0);

            lock (filteredSources)
            if (ALHelper.Efx.IsInitialized && filteredSources.Remove(sourceId))
                ALHelper.Efx.BindFilterToSource(sourceId, 0);

            ALHelper.Check();

            freeSources.Push(sourceId);
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
                    lock (filteredSources)
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

        void TidySources()
        {
            bool tidiedUp = false;
            if (freeSources.Count > 2 * PreallocatedSources)
            {
                AL.DeleteSource(freeSources.Pop());
                ALHelper.Check();
                totalSources--;
                tidiedUp = true;
            }
            //if (tidiedUp)
            //    Trace.WriteLine("[OpenAL] Tidied sources down to " + totalSources);
        }
        void TidyBuffers()
        {
            bool tidiedUp = false;
            if (freeBuffers.Count > 2 * PreallocatedBuffers)
            {
                AL.DeleteBuffer(freeBuffers.Pop());
                ALHelper.Check();
                totalBuffers--;
                tidiedUp = true;
            }
            //if (tidiedUp)
            //    Trace.WriteLine("[OpenAL] Tidied buffers down to " + totalBuffers);
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

