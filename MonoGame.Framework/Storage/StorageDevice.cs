#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

using Microsoft.Xna.Framework;
using System;
using System.IO;

using System.Runtime.Remoting.Messaging;

namespace Microsoft.Xna.Framework.Storage
{

	// The delegate must have the same signature as the method it will call asynchronously.
	public delegate StorageDevice ShowSelectorAsynchronousShow(PlayerIndex player, int sizeInBytes,
		int directoryCount);

	/* The MonoTouch AOT cannot deal with nullable types in a delegate (or
	 * at least not the straightforward implementation), so we define two
	 * delegate types.
	 */
	public delegate StorageDevice ShowSelectorAsynchronousShowNoPlayer(int sizeInBytes,
		int directoryCount);

	// The delegate must have the same signature as the method it will call asynchronously.
	public delegate StorageContainer OpenContainerAsynchronous(string displayName);

	/// <summary>
	/// Exposes a storage device for storing user data.
	/// </summary>
	/// <remarks>
	/// MSDN documentation contains related conceptual article:
	/// http://msdn.microsoft.com/en-us/library/bb200105.aspx
	/// </remarks>
	public sealed class StorageDevice
	{

		PlayerIndex? player;

		int directoryCount;
		private int DirectoryCount { get { return this.directoryCount; } }

		StorageContainer storageContainer;

		/// <summary>
		/// Creates a new <see cref="StorageDevice"/> instance.
		/// </summary>
		/// <param name="player">The playerIndex of the player.</param>
		/// <param name="sizeInBytes">Size of the storage device.</param>
		/// <param name="directoryCount"></param>
		internal StorageDevice(PlayerIndex? player, int sizeInBytes, int directoryCount)
		{
			this.player = player;
			this.directoryCount = directoryCount;
		}

		/// <summary>
		/// Returns the amount of free space.
		/// </summary>
		public long FreeSpace {
			get {
				// I do not know if the DriveInfo is is implemented on Mac or not thus the try catch.
				try {
					return new DriveInfo(GetDevicePath).AvailableFreeSpace;
				}
				catch (Exception) {
					StorageDeviceHelper.Path = StorageRoot;
					return StorageDeviceHelper.FreeSpace;
				}
			}
		}

		/// <summary>
		/// Returns true if device is connected, false otherwise.
		/// </summary>
		public bool IsConnected {
			get {
				// I do not know if the DriveInfo is is implemented on Mac or not thus the try catch.
				try {
					return new DriveInfo(GetDevicePath).IsReady;
				}
				catch (Exception) {
					return true;
				}
			}
		}

		/// <summary>
		/// Returns the total size of device.
		/// </summary>
		public long TotalSpace {
			get {

				// I do not know if the DriveInfo is is implemented on Mac or not thus the try catch.
				try {

					// Not sure if this should be TotalSize or TotalFreeSize.
					return new DriveInfo(GetDevicePath).TotalSize;
				}
				catch (Exception) {
					StorageDeviceHelper.Path = StorageRoot;
					return StorageDeviceHelper.TotalSpace;
				}

			}
		}

		string GetDevicePath
		{
			get {
				/* We may not need to store the StorageContainer in the future
				 * when we get DeviceChanged events working.
				 */
				if (storageContainer == null) 
				{
					return StorageRoot;
				}
				else {
					return storageContainer._storagePath;
				}
			}
		}

		// TODO: Implement DeviceChanged when we having the graphical implementation.

		/// <summary>
		/// Fired when a device is removed or inserted.
		/// </summary>
		public static event EventHandler<EventArgs> DeviceChanged;

		private bool SuppressEventHandlerWarningsUntilEventsAreProperlyImplemented()
		{
			return DeviceChanged != null;
		}

		/// <summary>
		/// Begins the open for a StorageContainer.
		/// </summary>
		/// <returns>The open StorageContainer.</returns>
		/// <param name="displayName">Name of file.</param>
		/// <param name="callback">Method to call on completion.</param>
		/// <param name="state">Request identifier object for callback (can be null).</param>
		public IAsyncResult BeginOpenContainer(string displayName, AsyncCallback callback,
			object state)
		{
			return OpenContainer(displayName, callback, state);

		}

		private IAsyncResult OpenContainer(string displayName, AsyncCallback callback, object state)
		{
			try {
				OpenContainerAsynchronous AsynchronousOpen = new OpenContainerAsynchronous(Open);
				return AsynchronousOpen.BeginInvoke(displayName, callback, state);
			} finally {
			}
		}

		// Private method to handle the creation of the StorageDevice.
		private StorageContainer Open(string displayName)
		{
			storageContainer = new StorageContainer(this, displayName, this.player);
			return storageContainer;
		}

		/// <summary>
		/// Begin process to display the StorageDevice selector UI.
		/// </summary>
		/// <returns>The show selector.</returns>
		/// <param name="callback">Method to invoke when device is selected by player.</param>
		/// <param name="state">Request identifier object for callback (can be null).</param>
		public static IAsyncResult BeginShowSelector(AsyncCallback callback, object state)
		{
			return BeginShowSelector(0, 0, callback, state);
		}

		/// <summary>
		/// Begin process to display the StorageDevice selector UI.
		/// </summary>
		/// <returns>The show selector.</returns>
		/// <param name="player">The PlayerIndex.  Only PlayerIndex.One is valid on Windows.</param>
		/// <param name="callback">Method to invoke when device is selected by player.</param>
		/// <param name="state">Request identifier object for callback (can be null).</param>
		public static IAsyncResult BeginShowSelector(PlayerIndex player, AsyncCallback callback,
			object state)
		{
			return BeginShowSelector(player, 0, 0, callback, state);
		}

		/// <summary>
		/// Begin process to display the StorageDevice selector UI.
		/// </summary>
		/// <returns>The show selector.</returns>
		/// <param name="sizeInBytes">Size (in bytes) of data to write.</param>
		/// <param name="directoryCount">Number of directories to write.</param>
		/// <param name="callback">Method to invoke when device is selected by player.</param>
		/// <param name="state">Request identifier object for callback (can be null).</param>
		public static IAsyncResult BeginShowSelector(int sizeInBytes, int directoryCount,
			AsyncCallback callback, object state)
		{
			ShowSelectorAsynchronousShowNoPlayer del = new ShowSelectorAsynchronousShowNoPlayer(Show);

			return del.BeginInvoke(sizeInBytes, directoryCount, callback, state);
		}

		/// <summary>
		/// Begin process to display the StorageDevice selector UI.
		/// </summary>
		/// <returns>The show selector.</returns>
		/// <param name="player">The PlayerIndex.  Only PlayerIndex.One is valid on Windows.</param>
		/// <param name="sizeInBytes">Size (in bytes) of data to write.</param>
		/// <param name="directoryCount">Number of directories to write.</param>
		/// <param name="callback">Method to invoke when device is selected by player.</param>
		/// <param name="state">Request identifier object for callback (can be null).</param>
		public static IAsyncResult BeginShowSelector(PlayerIndex player, int sizeInBytes,
			int directoryCount, AsyncCallback callback, object state)
		{
			ShowSelectorAsynchronousShow del = new ShowSelectorAsynchronousShow(Show);
			return del.BeginInvoke(player, sizeInBytes, directoryCount, callback, state);
		}

		// Private method to handle the creation of the StorageDevice.
		private static StorageDevice Show(PlayerIndex player, int sizeInBytes, int directoryCount)
		{
			return new StorageDevice(player, sizeInBytes, directoryCount);
		}

		private static StorageDevice Show(int sizeInBytes, int directoryCount)
		{
			return new StorageDevice(null, sizeInBytes, directoryCount);
		}

		/*
		// Parameters:
		//   titleName:
		//     The name of the storage container to delete.
		public void DeleteContainer(string titleName)
		{
			throw new NotImplementedException();
		}
		*/

		/// <summary>
		/// Ends the open container process.
		/// </summary>
		/// <returns>The open StorageContainer.</returns>
		/// <param name="result">Result of BeginOpenContainer.</param>
		public StorageContainer EndOpenContainer(IAsyncResult result)
		{
			StorageContainer returnValue = null;
			try {
				// Retrieve the delegate.
				AsyncResult asyncResult = result as AsyncResult;
				if (asyncResult != null)
				{
					OpenContainerAsynchronous asyncDelegate = asyncResult.AsyncDelegate
						as OpenContainerAsynchronous;

					// Wait for the WaitHandle to become signaled.
					result.AsyncWaitHandle.WaitOne();

					// Call EndInvoke to retrieve the results.
					if (asyncDelegate != null) 
					{
						returnValue = asyncDelegate.EndInvoke(result);
					}
				}
			}
			finally
			{
				// Close the wait handle.
				result.AsyncWaitHandle.Dispose();
			}

			return returnValue;

		}

		/// <summary>
		/// Ends the show selector user interface display.
		/// </summary>
		/// <returns>The storage device.</returns>
		/// <param name="result">The result of BeginShowSelector.</param>
		public static StorageDevice EndShowSelector(IAsyncResult result)
		{

			if (!result.IsCompleted) 
			{
				// Wait for the WaitHandle to become signaled.
				try {
					result.AsyncWaitHandle.WaitOne();
				} finally {
				}
			}
			// Retrieve the delegate.
			AsyncResult asyncResult = (AsyncResult) result;

			Object del = (AsyncResult) asyncResult.AsyncDelegate;

			if (del is ShowSelectorAsynchronousShow)
			{
				return (del as ShowSelectorAsynchronousShow).EndInvoke (result);
			}
			else if (del is ShowSelectorAsynchronousShowNoPlayer)
			{
				return (del as ShowSelectorAsynchronousShowNoPlayer).EndInvoke (result);
			}
			else
			{
				throw new ArgumentException("result");
			}
		}

		internal static string StorageRoot
		{
			get {
				if (SDL2_GamePlatform.OSVersion.Equals("Windows"))
				{
					return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				}
				if (SDL2_GamePlatform.OSVersion.Equals("Mac OS X"))
				{
					string osConfigDir = Environment.GetEnvironmentVariable("HOME");
					if (String.IsNullOrEmpty(osConfigDir))
					{
						return "."; // Oh well.
					}
					osConfigDir += "/Library/Application Support";
					return osConfigDir;
				}
				if (SDL2_GamePlatform.OSVersion.Equals("Linux"))
				{
					// Assuming a non-OSX Unix platform will follow the XDG. Which it should.
					string osConfigDir = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
					if (String.IsNullOrEmpty(osConfigDir))
					{
						osConfigDir = Environment.GetEnvironmentVariable("HOME");
						if (String.IsNullOrEmpty(osConfigDir))
						{
							return ".";	// Oh well.
						}
						osConfigDir += "/.local/share";
					}
					return osConfigDir;
				}
				throw new Exception("StorageDevice: SDL2 platform not handled!");
			}
		}
	}
}
