// This file is or was originally a part of the Unofficial Block, Location and Model Fixes mod for Daggerfall Unity by XJDHDR,
// which can be found here: https://github.com/XJDHDR/DFU_UBLaMF
//
// The license for it may be found here:
// https://github.com/XJDHDR/DFU_UBLaMF/blob/master/License-MIT.txt
//

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility;
using UnityEngine;
using UnityEngine.Networking;

namespace UBLAMFMod
{
	internal struct UblamfUpdateCheck
	{
		// ==== Fields ====
		private const string VERSION_NUMBER_FOR_THIS_RELEASE = "2021.09.00";

		// ReSharper disable once InconsistentNaming
		private static bool dfuVersionIs0_14_2orLater;
		private static int dfuVersionMajor;
		private static int dfuVersionMinor;
		private static int dfuVersionRevision;
		private static string downloadedVersionNumber = string.Empty;

		private static Ublamf ublamfManagerInstance;

		private static Delegate newGameStartedOrMainMenuReachedDelegate;
		private static EventInfo onStartFirstVisibleEvent;


		// ==== Internal methods ====
		internal static void _GetDfuVersionNumberAndStartCheckerRoutine(Ublamf MangerInstance)
		{
			try
			{
				ublamfManagerInstance = MangerInstance;

				// Skip this version check if the mod is running inside the editor
				if (Application.isEditor)
					return;

				string[] versionParts = VersionInfo.DaggerfallUnityVersion.Split('.');
				dfuVersionMajor = Convert.ToInt32(versionParts[0]);
				dfuVersionMinor = Convert.ToInt32(versionParts[1]);
				dfuVersionRevision = Convert.ToInt32(versionParts[2]);

				if (!Ublamf._DontCheckForUpdatesSetting)
					ublamfManagerInstance._StartCoroutineHelper(doUpdateCheckInBackground());
			}
			catch (Exception e)
			{
				Ublamf._ModStringBuilder.Clear();
				Ublamf._ModStringBuilder.Append($"UBLaMF: An exception occurred in the {nameof(UblamfUpdateCheck)}.");
				Ublamf._ModStringBuilder.AppendLine($"{nameof(_GetDfuVersionNumberAndStartCheckerRoutine)} method.");
				Ublamf._ModStringBuilder.AppendLine("Arguments:");
				Ublamf._ModStringBuilder.AppendLine($"  Arg1: {nameof(Ublamf)}:{nameof(MangerInstance)} - {MangerInstance}.");
				Ublamf._ModStringBuilder.AppendLine($"{e}");
				Debug.LogError(Ublamf._ModStringBuilder.ToString(), ublamfManagerInstance);
				Ublamf._ModStringBuilder.Clear();
			}
		}


		// ==== Private methods ====
        private static IEnumerator doUpdateCheckInBackground()
        {
	        if (!getLastUpdateCheckTimeFromPersistentDirectoryAndCheckIfItIsTimeForAnotherCheck())
                yield break;

            // Download the text file containing the version number for the latest release.
            UnityWebRequest webRequest = UnityWebRequest.Get("https://raw.githubusercontent.com/XJDHDR/DFU_UBLaMF/master/Latest_version.txt");
            yield return webRequest.SendWebRequest();

	        try
	        {
	            // If UnityWebRequest failed to download the latest version file, just abort the update checker.
	            if (webRequest.error != null || webRequest.isNetworkError || webRequest.isHttpError)
	                yield break;

	            if (!checkModVersionNumberForInstalledCopyAgainstDownloadedLatestVersionNumber(webRequest))
		            yield break;

	            // Create appropriate event registrations so that a messagebox can be displayed at the correct moment.
	            // Is the version of DFU being used equal to 0.14.2 or later.
	            if ((dfuVersionMajor > 0) || (dfuVersionMajor == 0 && dfuVersionMinor > 14) ||
	                (dfuVersionMajor == 0 && dfuVersionMinor == 14 && dfuVersionRevision >= 2))
	            {
	                // If so, use the main menu event that was added in that version.
	                dfuVersionIs0_14_2orLater = true;
	                // TODO: Uncomment this line and remove reflection logic if mod's minimum version ever becomes 0.14.2 or later
	                //DaggerfallStartWindow.OnStartFirstVisible += newGameStartedOrMainMenuReached;
	                bool? addMethodWithReflectionStatus = addMethodToOnStartFirstVisibleEventWithReflection(newGameStartedOrMainMenuReached);
	                if (addMethodWithReflectionStatus == null || addMethodWithReflectionStatus == true)
						yield break;
	            }

	            // Otherwise, register for the New Game and Load Save events that exist in previous versions.
	            SaveLoadManager.OnLoad += saveGameLoaded;
	            StartGameBehaviour.OnNewGame += newGameStartedOrMainMenuReached;
	        }
	        catch (Exception e)
	        {
		        Ublamf._ModStringBuilder.Clear();
		        Ublamf._ModStringBuilder.Append($"UBLaMF: An exception occurred in the {nameof(UblamfUpdateCheck)}.");
		        Ublamf._ModStringBuilder.AppendLine($"{nameof(doUpdateCheckInBackground)} method.");
		        Ublamf._ModStringBuilder.AppendLine($"{e}");
		        Debug.LogError(Ublamf._ModStringBuilder.ToString(), ublamfManagerInstance);
		        Ublamf._ModStringBuilder.Clear();
	        }
        }

        /// <summary>
        /// Attempts to grab the last update check time from the mod's Persistent Data Directory,
        /// then checks if enough time has elapsed to justify doing another check.
        /// </summary>
        /// <returns>True if enough time has passed. False if it's still too early.</returns>
        private static bool getLastUpdateCheckTimeFromPersistentDirectoryAndCheckIfItIsTimeForAnotherCheck()
        {
	        try
	        {
		        // Retrieve the last update time (UTC) from the PersistentDataDirectory.
		        DateTime lastUpdateTime;
		        string ublamfModInstancePersistentDataDirectory = Ublamf._UblamfModInstance.PersistentDataDirectory;
		        if (Directory.Exists(ublamfModInstancePersistentDataDirectory))
		        {
			        if (File.Exists($"{ublamfModInstancePersistentDataDirectory}/LastUpdateTime.txt"))
			        {
				        byte[] fileContents = File.ReadAllBytes($"{ublamfModInstancePersistentDataDirectory}/LastUpdateTime.txt");
				        lastUpdateTime = DateTime.FromBinary(BitConverter.ToInt64(fileContents, 0));
			        }
			        else
			        {
				        lastUpdateTime = DateTime.MinValue;
			        }
		        }
		        else
		        {
			        Directory.CreateDirectory(ublamfModInstancePersistentDataDirectory);
			        lastUpdateTime = DateTime.MinValue;
		        }

		        // Compare the retrieved last update time against the current time and date.
		        // Return based on whether it's still too soon for another check.
		        TimeSpan timeSinceLastCheck = DateTime.UtcNow - lastUpdateTime;
		        return timeSinceLastCheck.Days >= Ublamf._UpdateIntervalSetting;
	        }
	        catch (Exception e)
	        {
		        Ublamf._ModStringBuilder.Clear();
		        Ublamf._ModStringBuilder.Append($"UBLaMF: An exception occurred in the {nameof(UblamfUpdateCheck)}.");
		        Ublamf._ModStringBuilder.AppendLine($"{nameof(getLastUpdateCheckTimeFromPersistentDirectoryAndCheckIfItIsTimeForAnotherCheck)} method.");
		        Ublamf._ModStringBuilder.AppendLine($"{e}");
		        Debug.LogError(Ublamf._ModStringBuilder.ToString(), ublamfManagerInstance);
		        Ublamf._ModStringBuilder.Clear();
		        return false;
	        }
        }

        /// <summary>
        /// Checks the version number for this installed copy of the mod against the latest version number downloaded from my repo.
        /// </summary>
        /// <param name="DownloadedVersionNumberWebRequest">The UnityWebRequest that downloaded the mod's latest version number.</param>
        /// <returns>True if the two version number match. False if there is a mismatch.</returns>
        private static bool checkModVersionNumberForInstalledCopyAgainstDownloadedLatestVersionNumber(UnityWebRequest DownloadedVersionNumberWebRequest)
        {
	        try
	        {
		        // Strip any NULL, newline, and whitespace characters from the downloaded data.
		        int downloadedVersionNumberLength = DownloadedVersionNumberWebRequest.downloadHandler.data.Length;
		        Ublamf._ModStringBuilder.Clear();
		        int fullByteArrayIndexCounter = 0;
		        while (fullByteArrayIndexCounter < downloadedVersionNumberLength)
		        {
			        switch(DownloadedVersionNumberWebRequest.downloadHandler.data[fullByteArrayIndexCounter])
			        {
				        case 0x00:  // NULL
				        case 0x09:  // TAB
				        case 0x0A:  // LINE FEED
				        case 0x0D:  // CARRIAGE RETURN
				        case 0x20:  // SPACE
					        // Skip to the next byte
					        break;

				        default:
					        char currentChar = Convert.ToChar(DownloadedVersionNumberWebRequest.downloadHandler.data[fullByteArrayIndexCounter]);
					        Ublamf._ModStringBuilder.Append(currentChar);
					        break;
			        }
			        ++fullByteArrayIndexCounter;
		        }
		        downloadedVersionNumber = Ublamf._ModStringBuilder.ToString();
		        Ublamf._ModStringBuilder.Clear();

		        // Is the version number for this installed copy of the mod equal to the processed version number?
		        if (downloadedVersionNumber.Equals(VERSION_NUMBER_FOR_THIS_RELEASE, StringComparison.Ordinal))
		        {
			        File.WriteAllBytes($"{Ublamf._UblamfModInstance.PersistentDataDirectory}/LastUpdateTime.txt",
				        BitConverter.GetBytes(DateTime.UtcNow.ToBinary()));
			        return false;
		        }

		        return true;
	        }
	        catch (Exception e)
	        {
		        Ublamf._ModStringBuilder.Clear();
		        Ublamf._ModStringBuilder.Append($"UBLaMF: An exception occurred in the {nameof(UblamfUpdateCheck)}.");
		        Ublamf._ModStringBuilder.AppendLine($"{nameof(checkModVersionNumberForInstalledCopyAgainstDownloadedLatestVersionNumber)} method.");
		        Ublamf._ModStringBuilder.AppendLine("Arguments:");
		        Ublamf._ModStringBuilder.Append($"  Arg1: {nameof(UnityWebRequest)}:{nameof(DownloadedVersionNumberWebRequest)} - ");
		        Ublamf._ModStringBuilder.AppendLine($"{DownloadedVersionNumberWebRequest}.");
		        Ublamf._ModStringBuilder.AppendLine($"{e}");
		        Debug.LogError(Ublamf._ModStringBuilder.ToString(), ublamfManagerInstance);
		        Ublamf._ModStringBuilder.Clear();
		        return false;
	        }
        }

        /// <summary>
        /// Uses Reflection to dynamically get a reference to the OnStartFirstVisible event in the DaggerfallStartWindow,
        /// then register an event handler to it.
        /// This allows the mod to still be compatible with versions of DFU below 0.14.2.
        /// </summary>
        /// <param name="MethodToUseAsEventHandler">The method that will be used as the event handler.</param>
        /// <returns>
        /// True if the event handler was successfully created. False if the OnStartFirstVisible event was not found. Null if an exception occurred.
        /// </returns>
        private static bool? addMethodToOnStartFirstVisibleEventWithReflection(Action MethodToUseAsEventHandler)
        {
	        try
	        {
		        onStartFirstVisibleEvent = typeof(DaggerfallStartWindow).GetEvent("OnStartFirstVisible",
			        BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

		        if (onStartFirstVisibleEvent == null)
			        return false;

		        newGameStartedOrMainMenuReachedDelegate = Delegate.CreateDelegate(onStartFirstVisibleEvent.EventHandlerType, MethodToUseAsEventHandler.Method);
		        onStartFirstVisibleEvent.AddEventHandler(null, newGameStartedOrMainMenuReachedDelegate);
		        return true;
	        }
	        catch (Exception e)
	        {
		        Ublamf._ModStringBuilder.Clear();
		        Ublamf._ModStringBuilder.Append($"UBLaMF: An exception occurred in the {nameof(UblamfUpdateCheck)}.");
		        Ublamf._ModStringBuilder.AppendLine($"{nameof(addMethodToOnStartFirstVisibleEventWithReflection)} method.");
		        Ublamf._ModStringBuilder.AppendLine("Arguments:");
		        Ublamf._ModStringBuilder.AppendLine($"  Arg1: {nameof(Action)}:{nameof(MethodToUseAsEventHandler)} - {MethodToUseAsEventHandler}.");
		        Ublamf._ModStringBuilder.AppendLine($"{e}");
		        Debug.LogError(Ublamf._ModStringBuilder.ToString(), ublamfManagerInstance);
		        Ublamf._ModStringBuilder.Clear();
		        return null;
	        }
        }

        private static void saveGameLoaded(SaveData_v1 SaveData) =>
	        newGameStartedOrMainMenuReached();

        private static void newGameStartedOrMainMenuReached()
        {
	        try
	        {
	            if (dfuVersionIs0_14_2orLater)
	            {
		            // TODO: Uncomment this line and remove reflection logic if mod's minimum version ever becomes 0.14.2 or later
	                //DaggerfallStartWindow.OnStartFirstVisible -= newGameStartedOrMainMenuReached;
	                onStartFirstVisibleEvent.RemoveEventHandler(null, newGameStartedOrMainMenuReachedDelegate);
	            }
	            else
	            {
	                SaveLoadManager.OnLoad -= saveGameLoaded;
	                StartGameBehaviour.OnNewGame -= newGameStartedOrMainMenuReached;
	            }
	            File.WriteAllBytes($"{Ublamf._UblamfModInstance.PersistentDataDirectory}/LastUpdateTime.txt", BitConverter.GetBytes(DateTime.UtcNow.ToBinary()));

	            DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow, true)
	            {
	                ClickAnywhereToClose = true
	            };
	            DateTime nextUpdateCheck = DateTime.Now + new TimeSpan(Ublamf._UpdateIntervalSetting, 0, 0, 0);

	            Ublamf._ModStringBuilder.Clear();
	            Ublamf._ModStringBuilder.AppendLine("Message from the Unofficial Block, Locations and Model Fixes mod:");
	            Ublamf._ModStringBuilder.AppendLine();
	            Ublamf._ModStringBuilder.AppendLine("You are using an outdated version.");
	            Ublamf._ModStringBuilder.Append($"The latest version available is {downloadedVersionNumber},");
	            Ublamf._ModStringBuilder.AppendLine($" but you are using version {VERSION_NUMBER_FOR_THIS_RELEASE}");
	            Ublamf._ModStringBuilder.AppendLine("It is recommended that you download the latest version from Nexus Mods:");
	            Ublamf._ModStringBuilder.AppendLine("https://www.nexusmods.com/daggerfallunity/mods/100");
	            Ublamf._ModStringBuilder.AppendLine("");
	            Ublamf._ModStringBuilder.AppendLine("Unless you disable update checking or change the waiting period in the mod's settings, ");
	            Ublamf._ModStringBuilder.AppendLine($"the next update check will happen after {nextUpdateCheck.ToString(CultureInfo.CurrentCulture)}.");
	            Ublamf._ModStringBuilder.AppendLine();
	            Ublamf._ModStringBuilder.AppendLine("Press Esc, Enter, or click any mouse button to close this message.");
	            messageBox.SetText(Ublamf._ModStringBuilder.ToString());
	            Ublamf._ModStringBuilder.Clear();
	            messageBox.Show();
	        }
	        catch (Exception e)
	        {
		        Ublamf._ModStringBuilder.Clear();
		        Ublamf._ModStringBuilder.Append($"UBLaMF: An exception occurred in the {nameof(UblamfUpdateCheck)}.");
		        Ublamf._ModStringBuilder.AppendLine($"{nameof(newGameStartedOrMainMenuReached)} method.");
		        Ublamf._ModStringBuilder.AppendLine($"{e}");
		        Debug.LogError(Ublamf._ModStringBuilder.ToString(), ublamfManagerInstance);
		        Ublamf._ModStringBuilder.Clear();
	        }
        }
	}
}
