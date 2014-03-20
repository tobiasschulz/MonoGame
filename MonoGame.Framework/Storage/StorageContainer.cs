#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Assembly Microsoft.Xna.Framework.Storage.dll, v4.0.30319
// C:\Program Files (x86)\Microsoft XNA\XNA Game Studio\v4.0\References\Windows\x86\Microsoft.Xna.Framework.Storage.dll
#endregion
using Microsoft.Xna.Framework;
using System;
using System.IO;

namespace Microsoft.Xna.Framework.Storage
{
	/* Implementation on Windows
	 *
	 * User storage is in the My Documents folder of the user who is currently logged in, in the
	 * SavedGames folder. A subfolder is created for each game according to the titleName passed
	 * to the BeginOpenContainer method.  When no PlayerIndex is specified, content is saved in
	 * the AllPlayers folder. When a PlayerIndex is specified, the content is saved in the Player1,
	 * Player2, Player3, or Player4 folder, depending on which PlayerIndex was passed to
	 * BeginShowSelector.
	 */

	/// <summary>
	/// Contains a logical collection of files used for user-data storage.
	/// </summary>
	/// <remarks>
	/// MSDN documentation contains related conceptual article:
	/// http://msdn.microsoft.com/en-us/library/bb200105.aspx#ID4EDB
	/// </remarks>
	public class StorageContainer : IDisposable
	{
		internal readonly string _storagePath;
		private readonly StorageDevice _device;
		private readonly string _name;

		/// <summary>
		/// Initializes a new instance of the <see cref="Microsoft.Xna.Framework.Storage.StorageContainer"/> class.
		/// </summary>
		/// <param name='device'>The attached storage-device.</param>
		/// <param name='name'> name.</param>
		/// <param name='playerIndex'>The player index of the player to save the data.</param>
		internal StorageContainer(StorageDevice device, string name, PlayerIndex? playerIndex)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException("A title name has to be provided in parameter name.");
			}

			_device = device;
			_name = name;

			// From the examples the root is based on MyDocuments folder
			string saved;
			if (SDL2_GamePlatform.OSVersion.Equals("Windows"))
			{
				saved = Path.Combine(StorageDevice.StorageRoot, "SavedGames");
			}
			else if (	SDL2_GamePlatform.OSVersion.Equals("Mac OS X") ||
					SDL2_GamePlatform.OSVersion.Equals("Linux") )
			{
				// Unix systems are expected to have a dedicated userdata folder.
				saved = StorageDevice.StorageRoot;
			}
			else
			{
				throw new Exception("StorageContainer: SDL2 platform not handled!");
			}
			_storagePath = Path.Combine(saved, name);

			string playerSave = string.Empty;
			if (playerIndex.HasValue) 
			{
				playerSave = Path.Combine(_storagePath, "Player" + (int) playerIndex.Value);
			}

			if (!string.IsNullOrEmpty(playerSave))
			{
				_storagePath = Path.Combine(_storagePath, "Player" + (int)playerIndex);
			}

			// Create the "device", if need be.
			if (!Directory.Exists(_storagePath))
			{
				Directory.CreateDirectory(_storagePath);
			}
		}

		/// <summary>
		/// Returns display name of the title.
		/// </summary>
		public string DisplayName {
			get { return _name; }
		}

		/// <summary>
		/// Gets a bool value indicating whether the instance has been disposed.
		/// </summary>
		public bool IsDisposed { 
			get;
			private set;
		}

		/// <summary>
		/// Returns the <see cref="StorageDevice"/> that holds logical files for the container.
		/// </summary>
		public StorageDevice StorageDevice {
			get {return _device; }
		}

		// TODO: Implement the Disposing function.  Find sample first.

		/// <summary>
		/// Fired when <see cref="Dispose"/> is called or object if finalized or collected by the
		/// garbage collector.
		/// </summary>
		public event EventHandler<EventArgs> Disposing;

		private bool SuppressEventHandlerWarningsUntilEventsAreProperlyImplemented()
		{
			return Disposing != null;
		}

		/// <summary>
		/// Creates a new directory in the storage-container.
		/// </summary>
		/// <param name="directory">Relative path of the directory to be created.</param>
		public void CreateDirectory(string directory)
		{
			if (string.IsNullOrEmpty(directory))
			{
				throw new ArgumentNullException("Parameter directory must contain a value.");
			}

			// Relative, so combine with our path.
			string dirPath = Path.Combine(_storagePath, directory);

			// Now let's try to create it.
			Directory.CreateDirectory(dirPath);
		}

		/// <summary>
		/// Creates a file in the storage-container.
		/// </summary>
		/// <param name="file">Relative path of the file to be created.</param>
		/// <returns>Returns <see cref="Stream"/> for the created file.</returns>
		public Stream CreateFile(string file)
		{
			if (string.IsNullOrEmpty(file))
			{
				throw new ArgumentNullException("Parameter file must contain a value.");
			}

			// Relative, so combine with our path.
			string filePath= Path.Combine(_storagePath, file);

			// Return a new file with read/write access.
			return File.Create(filePath);
		}

		/// <summary>
		/// Deletes specified directory for the storage-container.
		/// </summary>
		/// <param name="directory">The relative path of the directory to be deleted.</param>
		public void DeleteDirectory(string directory)
		{
			if (string.IsNullOrEmpty(directory))
			{
				throw new ArgumentNullException("Parameter directory must contain a value.");
			}

			// Relative, so combine with our path.
			string dirPath = Path.Combine(_storagePath, directory);

			// Now let's try to delete it.
			Directory.Delete(dirPath);
		}

		/// <summary>
		/// Deletes a file from the storage-container.
		/// </summary>
		/// <param name="file">The relative path of the file to be deleted.</param>
		public void DeleteFile(string file)
		{
			if (string.IsNullOrEmpty(file))
			{
				throw new ArgumentNullException("Parameter file must contain a value.");
			}

			// Relative, so combine with our path.
			string filePath= Path.Combine(_storagePath, file);

			// Now let's try to delete it.
			File.Delete(filePath);
		}


		/// <summary>
		/// Returns true if specified path exists in the storage-container, false otherwise.
		/// </summary>
		/// <param name="directory">The relative path of directory to query for.</param>
		/// <returns>True if queried directory exists, false otherwise.</returns>
		public bool DirectoryExists(string directory)
		{
			if (string.IsNullOrEmpty(directory))
			{
				throw new ArgumentNullException("Parameter directory must contain a value.");
			}

			// Relative, so combine with our path.
			string dirPath = Path.Combine(_storagePath, directory);

			return Directory.Exists(dirPath);
		}

		/// <summary>
		/// Disposes un-managed objects referenced by this object.
		/// </summary>
		public void Dispose()
		{

			// Fill this in when we figure out what we should be disposing.
			IsDisposed = true;
		}

		/// <summary>
		/// Returns true if the specified file exists in the storage-container, false otherwise.
		/// </summary>
		/// <param name="file">The relative path of file to query for.</param>
		/// <returns>True if queried file exists, false otherwise.</returns>
		public bool FileExists(string file)
		{
			if (string.IsNullOrEmpty(file))
			{
				throw new ArgumentNullException("Parameter file must contain a value.");
			}

			// Relative, so combine with our path.
			string filePath= Path.Combine(_storagePath, file);

			// Return a new file with read/write access.
			return File.Exists(filePath);
		}

		/// <summary>
		/// Returns list of directory names in the storage-container.
		/// </summary>
		/// <returns>List of directory names.</returns>
		public string[] GetDirectoryNames()
		{
			return Directory.GetDirectories(_storagePath);
		}

		/*
		/// <summary>
		/// Returns list of directory names with given search pattern.
		/// </summary>
		/// <param name="searchPattern">
		/// A search pattern that supports single-character ("?") and multicharacter ("*") wildcards.
		/// </param>
		/// <returns>List of matched directory names.</returns>
		public string[] GetDirectoryNames(string searchPattern)
		{
			throw new NotImplementedException();
		}
		*/

		/// <summary>
		/// Returns list of file names in the storage-container.
		/// </summary>
		/// <returns>List of file names.</returns>
		public string[] GetFileNames()
		{
			return Directory.GetFiles(_storagePath);
		}

		/// <summary>
		/// Returns list of file names with given search pattern.
		/// </summary>
		/// <param name="searchPattern">A search pattern that supports single-character ("?") and multicharacter ("*") wildcards.</param>
		/// <returns>List of matched file names.</returns>
		public string[] GetFileNames(string searchPattern)
		{
			if (string.IsNullOrEmpty(searchPattern))
			{
				throw new ArgumentNullException("Parameter searchPattern must contain a value.");
			}

			return Directory.GetFiles(_storagePath, searchPattern);
		}


		/// <summary>
		/// Opens a file contained in storage-container.
		/// </summary>
		/// <param name="file">Relative path of the file.</param>
		/// <param name="fileMode"><see cref="FileMode"/> that specifies how the file is opened.</param>
		/// <returns><see cref="Stream"/> object for the opened file.</returns>
		public Stream OpenFile(string file, FileMode fileMode)
		{
			return OpenFile(file, fileMode, FileAccess.ReadWrite, FileShare.ReadWrite);
		}

		/// <summary>
		/// Opens a file contained in storage-container.
		/// </summary>
		/// <param name="file">Relative path of the file.</param>
		/// <param name="fileMode"><see cref="FileMode"/> that specifies how the file is opened.</param>
		/// <param name="fileAccess"><see cref="FileAccess"/> that specifies access mode.</param>
		/// <returns><see cref="Stream"/> object for the opened file.</returns>
		public Stream OpenFile(string file, FileMode fileMode, FileAccess fileAccess)
		{
			return OpenFile(file, fileMode, fileAccess, FileShare.ReadWrite);
		}

		/// <summary>
		/// Opens a file contained in storage-container.
		/// </summary>
		/// <param name="file">Relative path of the file.</param>
		/// <param name="fileMode"><see cref="FileMode"/> that specifies how the file is opened.</param>
		/// <param name="fileAccess"><see cref="FileAccess"/> that specifies access mode.</param>
		/// <param name="fileShare">A bitwise combination of <see cref="FileShare"/>
		/// enumeration values that specifies access modes for other stream objects.</param>
		/// <returns><see cref="Stream"/> object for the opened file.</returns>
		public Stream OpenFile(string file, FileMode fileMode, FileAccess fileAccess,
			FileShare fileShare)
		{
			if (string.IsNullOrEmpty(file))
			{
				throw new ArgumentNullException("Parameter file must contain a value.");
			}

			// Relative, so combine with our path.
			string filePath= Path.Combine(_storagePath, file);

			return File.Open(filePath, fileMode, fileAccess, fileShare);
		}
	}
}
