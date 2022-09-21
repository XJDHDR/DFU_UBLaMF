// ReSharper disable once CommentTypo
// This source code is or was originally a part of the "Unofficial Block, Location and Model Fixes" mod for Daggerfall Unity.
//
// This source code is provided under the following license:
// https://github.com/XJDHDR/DFU_UBLaMF/blob/master/License-MIT.txt

using System;
using System.Collections;
using System.Globalization;
using System.Text;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Windows;

namespace UBLAMFMod
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    public class UBLAMF : MonoBehaviour
    {
        // ==== Fields ====
        private const string VERSION_NUMBER_FOR_THIS_RELEASE = "2021.09.00";

        // ReSharper disable once InconsistentNaming
        private static bool dfuVersionIs0_14_2orLater;
        private static bool dontCheckForUpdates;
        private static int dfuVersionMajor;
        private static int dfuVersionMinor;
        private static int dfuVersionRevision;
        private static int updateInterval;
        private static string downloadedVersionNumber = string.Empty;

        private static Mod mod;
        private static ModSettings settings;


        // ==== DFU and Unity initialisation methods ====
        // ReSharper disable once UnusedMember.Global
        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams InitParams)
        {
            mod = InitParams.Mod;

            settings = mod.GetSettings();
            dontCheckForUpdates = settings.GetValue<bool>("UpdateOptions", "DisableUpdateCheck");
            updateInterval = settings.GetValue<int>("UpdateOptions", "UpdateInterval");

            GameObject go = new GameObject("UBLaMF manager");
            go.AddComponent<UBLAMF>();

            mod.IsReady = true;
        }

        private void Start()
        {
            if (Application.isEditor)
                return;

            string[] versionParts = VersionInfo.DaggerfallUnityVersion.Split('.');
            dfuVersionMajor = Convert.ToInt32(versionParts[0]);
            dfuVersionMinor = Convert.ToInt32(versionParts[1]);
            dfuVersionRevision = Convert.ToInt32(versionParts[2]);

            if (!dontCheckForUpdates)
                StartCoroutine(getLatestVersionNumberFromRepo());
        }


        // ==== Private methods ====
        private static IEnumerator getLatestVersionNumberFromRepo()
        {
            // Retrieve the last update time (UTC) from the PersistentDataDirectory.
            DateTime lastUpdateTime;
            if (Directory.Exists(mod.PersistentDataDirectory))
            {
                if (File.Exists($"{mod.PersistentDataDirectory}/LastUpdateTime.txt"))
                {
                    byte[] fileContents = File.ReadAllBytes($"{mod.PersistentDataDirectory}/LastUpdateTime.txt");
                    lastUpdateTime = DateTime.FromBinary(BitConverter.ToInt64(fileContents, 0));
                }
                else
                {
                    lastUpdateTime = DateTime.MinValue;
                }
            }
            else
            {
                Directory.CreateDirectory(mod.PersistentDataDirectory);
                lastUpdateTime = DateTime.MinValue;
            }

            // Compare the retrieved last update time against the current time and date.
            // Abort if it's still too soon for another check.
            TimeSpan timeSinceLastCheck = DateTime.UtcNow - lastUpdateTime;
            if (timeSinceLastCheck.Days < updateInterval)
                yield break;

            // Download the text file containing the version number for the latest release.
            UnityWebRequest webRequest = UnityWebRequest.Get("https://raw.githubusercontent.com/XJDHDR/DFU_UBLaMF/master/Latest_version.txt");
            yield return webRequest.SendWebRequest();

            // If UnityWebRequest failed to download the latest version file, just abort the update checker.
            if (webRequest.error != null || webRequest.isNetworkError || webRequest.isHttpError)
                yield break;

            // Strip any NULL, newline, and whitespace characters from the downloaded data.
            int downloadedVersionNumberLength = webRequest.downloadHandler.data.Length;
            StringBuilder downloadedVersionNumberStrippedStringBuilder = new StringBuilder(downloadedVersionNumberLength);
            int fullByteArrayIndexCounter = 0;
            while (fullByteArrayIndexCounter < downloadedVersionNumberLength)
            {
                switch(webRequest.downloadHandler.data[fullByteArrayIndexCounter])
                {
                    case 0x00:  // NULL
                    case 0x09:  // TAB
                    case 0x0A:  // LINE FEED
                    case 0x0D:  // CARRIAGE RETURN
                    case 0x20:  // SPACE
                        // Skip to the next byte
                        break;

                    default:
                        downloadedVersionNumberStrippedStringBuilder.Append(Convert.ToChar(webRequest.downloadHandler.data[fullByteArrayIndexCounter]));
                        break;
                }
                ++fullByteArrayIndexCounter;
            }
            downloadedVersionNumber = downloadedVersionNumberStrippedStringBuilder.ToString();

            // Is the version number for this installed copy of the mod equal to the processed version number?
            if (!downloadedVersionNumber.Equals(VERSION_NUMBER_FOR_THIS_RELEASE, StringComparison.Ordinal))
            {
                File.WriteAllBytes($"{mod.PersistentDataDirectory}/LastUpdateTime.txt", BitConverter.GetBytes(DateTime.UtcNow.ToBinary()));
                yield break;
            }

            // Create appropriate event registrations so that a messagebox can be displayed at the correct moment.
            // Is the version of DFU being used equal to 0.14.2 or later.
            if ((dfuVersionMajor > 0) || (dfuVersionMajor == 0 && dfuVersionMinor > 14) ||
                (dfuVersionMajor == 0 && dfuVersionMinor == 14 && dfuVersionRevision > 1))
            {
                // If so, use the main menu event that was added in that version.
                dfuVersionIs0_14_2orLater = true;
                DaggerfallStartWindow.OnStartFirstVisible += newGameStartedOrMainMenuReached;
            }
            else
            {
                // Otherwise, register for the New Game and Load Save events that exist in previous versions.
                SaveLoadManager.OnLoad += saveGameLoaded;
                StartGameBehaviour.OnNewGame += newGameStartedOrMainMenuReached;
            }
        }

        private static void saveGameLoaded(SaveData_v1 SaveData) =>
	        newGameStartedOrMainMenuReached();

        private static void newGameStartedOrMainMenuReached()
        {
            if (dfuVersionIs0_14_2orLater)
            {
                DaggerfallStartWindow.OnStartFirstVisible -= newGameStartedOrMainMenuReached;
            }
            else
            {
                SaveLoadManager.OnLoad -= saveGameLoaded;
                StartGameBehaviour.OnNewGame -= newGameStartedOrMainMenuReached;
            }
            File.WriteAllBytes($"{mod.PersistentDataDirectory}/LastUpdateTime.txt", BitConverter.GetBytes(DateTime.UtcNow.ToBinary()));

            DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow, true)
            {
                ClickAnywhereToClose = true
            };
            DateTime nextUpdateCheck = DateTime.Now + new TimeSpan(updateInterval, 0, 0, 0);
            string[] messageLines =
            {
                "Message from the Unofficial Block, Locations and Model Fixes mod:",
                "",
                "You are using an outdated version.",
                $"The latest version available is {downloadedVersionNumber}, but you are using version {VERSION_NUMBER_FOR_THIS_RELEASE}",
                "It is recommended that you download the latest version from Nexus Mods:",
                "https://www.nexusmods.com/daggerfallunity/mods/100",
                "",
                "Unless you disable update checking or change the waiting period in the mod's settings, ",
                $"the next update check will happen after {nextUpdateCheck.ToString(CultureInfo.CurrentCulture)}.",
                "",
                "Press Esc, Enter, or click any mouse button to close this message."
            };
            messageBox.SetText(messageLines);
            messageBox.Show();
        }
    }
}
