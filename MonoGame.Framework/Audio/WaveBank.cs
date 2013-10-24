using System;
using System.IO;

namespace Microsoft.Xna.Framework.Audio
{
	// http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.audio.wavebank.aspx
	public class WaveBank : IDisposable
	{
		// We keep this in order to Dispose ourselves later.
		private AudioEngine INTERNAL_baseEngine;
		private string INTERNAL_name;

		private SoundEffect[] INTERNAL_sounds;

		public bool IsDisposed
		{
			get;
			private set;
		}

		public event EventHandler<EventArgs> Disposing;

		public WaveBank(
			AudioEngine audioEngine,
			string nonStreamingWaveBankFilename
		) {
			if (audioEngine == null)
			{
				throw new ArgumentNullException("audioEngine");
			}
			if (String.IsNullOrEmpty(nonStreamingWaveBankFilename))
			{
				throw new ArgumentNullException("nonStreamingWaveBankFilename");
			}

			INTERNAL_baseEngine = audioEngine;

#if ANDROID
			using (MemoryStream stream = new MemoryStream())
			{
				using (Stream s = Game.Activity.Assets.Open(nonStreamingWaveBankFilename))
				{
					s.CopyTo(stream);
				}
				stream.Position = 0;
#else
			using (Stream stream = TitleContainer.OpenStream(nonStreamingWaveBankFilename))
			{
#endif
				using (BinaryReader reader = new BinaryReader(stream))
				{
					// Check the file header. Should be 'WBND'
					if (reader.ReadUInt32() != 0x444E4257)
					{
						throw new ArgumentException("WBND format not recognized!");
					}

					// Check the content version. Assuming XNA4 Refresh.
					if (reader.ReadUInt32() != AudioEngine.ContentVersion)
					{
						throw new ArgumentException("WBND Content version!");
					}

					// Check the tool version. Assuming XNA4 Refresh.
					if (reader.ReadUInt32() != 44)
					{
						throw new ArgumentException("WBND Tool version!");
					}

					// Obtain WaveBank chunk offsets/lengths
					uint[] offsets = new uint[5];
					uint[] lengths = new uint[5];
					for (int i = 0; i < 5; i++)
					{
						offsets[i] = reader.ReadUInt32();
						lengths[i] = reader.ReadUInt32();
					}

					// Seek to the first offset, obtain WaveBank info
					reader.BaseStream.Seek(offsets[0], SeekOrigin.Begin);

					// Unknown value
					reader.ReadUInt16();

					// WaveBank Flags
					ushort wavebankFlags = reader.ReadUInt16();
					// bool containsEntryNames =	(wavebankFlags & 0x00010000) != 0;
					bool compact =			(wavebankFlags & 0x00020000) != 0;
					// bool syncDisabled =		(wavebankFlags & 0x00040000) != 0;
					// bool containsSeekTables =	(wavebankFlags & 0x00080000) != 0;

					// WaveBank Entry Count
					uint numEntries = reader.ReadUInt32();

					// WaveBank Name
					INTERNAL_name = System.Text.Encoding.UTF8.GetString(
						reader.ReadBytes(64), 0, 64
					).Replace("\0", "");

					// WaveBank entry information
					uint metadataElementSize = reader.ReadUInt32();
					reader.ReadUInt32(); // nameElementSize
					uint alignment = reader.ReadUInt32();

					// Determine the generic play region offset
					uint playRegionOffset = offsets[4];
					if (playRegionOffset == 0)
					{
						playRegionOffset = offsets[1] + (numEntries * metadataElementSize);
					}

					// Entry format. Read early for Compact data
					uint entryFormat = 0;
					if (compact)
					{
						entryFormat = reader.ReadUInt32();
					}

					// Read in the wavedata
					INTERNAL_sounds = new SoundEffect[numEntries];
					uint curOffset = offsets[1];
					for (int curEntry = 0; curEntry < numEntries; curEntry++)
					{
						// Seek to the current entry
						reader.BaseStream.Seek(curOffset, SeekOrigin.Begin);

						// Entry Information
						uint entryPlayOffset = 0;
						uint entryPlayLength = 0;
						uint entryLoopOffset = 0;
						uint entryLoopLength = 0;

						// Obtain Entry Information
						if (compact)
						{
							uint entryLength = reader.ReadUInt32();

							entryPlayOffset =
								(entryLength & ((1 << 21) - 1)) *
								alignment;
							entryPlayLength =
								(entryLength >> 21) & ((1 << 11) - 1);

							// FIXME: Deviation Length
							reader.BaseStream.Seek(
								curOffset + metadataElementSize,
								SeekOrigin.Begin
							);

							if (curEntry == (numEntries - 1))
							{
								// Last track, last length.
								entryLength = lengths[4];
							}
							else
							{
								entryLength = (
									(
										reader.ReadUInt32() &
										((1 << 21) - 1)
									) * alignment
								);
							}
							entryPlayLength = entryLength - entryPlayOffset;
						}
						else
						{
							if (metadataElementSize >= 4)
								reader.ReadUInt32(); // Flags/Duration, unused
							if (metadataElementSize >= 8)
								entryFormat = reader.ReadUInt32();
							if (metadataElementSize >= 12)
								entryPlayOffset = reader.ReadUInt32();
							if (metadataElementSize >= 16)
								entryPlayLength = reader.ReadUInt32();
							if (metadataElementSize >= 20)
								entryLoopOffset = reader.ReadUInt32();
							if (metadataElementSize >= 24)
								entryLoopLength = reader.ReadUInt32();
							else
							{
								// FIXME: This is a bit hacky.
								if (entryPlayLength != 0)
								{
									entryPlayLength = lengths[4];
								}
							}
						}

						// Update seek offsets
						curOffset += metadataElementSize;
						entryPlayOffset += playRegionOffset;

						// Parse Format for Wavedata information
						uint entryCodec =	(entryFormat >> 0)		& ((1 << 2) - 1);
						uint entryChannels =	(entryFormat >> 2)		& ((1 << 3) - 1);
						uint entryFrequency =	(entryFormat >> (2 + 3))	& ((1 << 18) - 1);
						uint entryAlignment =	(entryFormat >> (2 + 3 + 18))	& ((1 << 8) - 1);

						// Read Wavedata
						reader.BaseStream.Seek(entryPlayOffset, SeekOrigin.Begin);
						byte[] entryData = reader.ReadBytes((int) entryPlayLength);

						// Load SoundEffect based on codec
						if (entryCodec == 0x0) // PCM
						{
							INTERNAL_sounds[curEntry] = new SoundEffect(
								entryData,
								(int) entryFrequency,
								(AudioChannels) entryChannels,
								(int) entryLoopOffset,
								(int) entryLoopLength
							);
						}
						else if (entryCodec == 0x2) // ADPCM
						{
							// TODO: MSADPCM loop data!
							INTERNAL_sounds[curEntry] = new SoundEffect(
								entryData,
								(int) entryFrequency,
								(AudioChannels) entryChannels,
								(int) entryAlignment + 22
							);
						}
						else if (entryCodec == 0x3) // WMA
						{
							// TODO: WMA Codec
							throw new NotSupportedException();
						}
						else // Includes 0x1, XMA
						{
							throw new NotSupportedException();
						}
					}

					// Add this WaveBank to the AudioEngine Dictionary
					audioEngine.INTERNAL_addWaveBank(INTERNAL_name, this);
				}
			}

			// Finally.
			IsDisposed = false;
		}

		public WaveBank(
			AudioEngine audioEngine,
			string streamingWaveBankFilename,
			int offset,
			short packetsize
		) : this(audioEngine, streamingWaveBankFilename) {
			if (audioEngine == null)
			{
				throw new ArgumentNullException("audioEngine");
			}
			if (String.IsNullOrEmpty(streamingWaveBankFilename))
			{
				throw new ArgumentNullException("streamingWaveBankFilename");
			}
			// HACK: We're attempting to load this as non-streaming!
			if (offset != 0)
			{
				throw new NotSupportedException("Is your MonoGame title on a DVD?!");
			}
		}

		~WaveBank()
		{
			Dispose(true);
		}

		public void Dispose()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (Disposing != null)
				{
					Disposing.Invoke(this, null);
				}
				foreach (SoundEffect se in INTERNAL_sounds)
				{
					se.Dispose();
				}
				INTERNAL_baseEngine.INTERNAL_removeWaveBank(INTERNAL_name);
				INTERNAL_sounds = null;
				IsDisposed = true;
			}
		}

		internal SoundEffect INTERNAL_getTrack(ushort track)
		{
			return INTERNAL_sounds[track];
		}
	}
}
