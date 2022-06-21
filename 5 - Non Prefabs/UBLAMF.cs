// This source code is or was originally a part of the "Unofficial Block, Location and Model Fixes" mod for Daggerfall Unity.
//
// This source code is provided under the following license:
// https://github.com/XJDHDR/DFU_UBLaMF/blob/master/License-MIT.txt

using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using UnityEngine.Networking;
using UnityEngine.Windows;
using UnityEngine.WSA;

// ReSharper disable once CheckNamespace
namespace UBLAMFMod
{
    public class UBLAMF : MonoBehaviour
    {
        private static bool dontCheckForUpdates;
        private static int updateInterval;
        private static string downloadedVersionNumber = string.Empty;
        
        private static Mod mod;
        private static ModSettings settings;


        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            
            settings = mod.GetSettings();
            dontCheckForUpdates = settings.GetValue<bool>("UpdateOptions", "DisableUpdateCheck");
            updateInterval = settings.GetValue<int>("UpdateOptions", "UpdateInterval");

            var go = new GameObject("UBLaMF manager");
            go.AddComponent<UBLAMF>();

            mod.IsReady = true;
        }

        private void Start()
        {
#if !UNITY_EDITOR || true
            if (!dontCheckForUpdates)
                StartCoroutine(GetLatestVersionNumberFromRepo());
#endif
        }

        private static IEnumerator GetLatestVersionNumberFromRepo()
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
            byte[] downloadedVersionNumberStripped = new byte[downloadedVersionNumberLength];
            int fullByteArrayIndexCounter = 0;
            int strippedByteArrayIndexCounter = 0;
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
                        downloadedVersionNumberStripped[strippedByteArrayIndexCounter++] = webRequest.downloadHandler.data[fullByteArrayIndexCounter];
                        break;
                }
                ++fullByteArrayIndexCounter;
            }
            
            // Is the version number for this installed copy of the mod equal to the processed version number?
            //                         2      0     2     1     .     0     9     .     0     0
            byte[] thisModVersion = { 0x32, 0x30, 0x32, 0x31, 0x2E, 0x30, 0x39, 0x2E, 0x30, 0x30 };
            if (downloadedVersionNumberStripped.SequenceEqual(thisModVersion))
            {
                Debug.LogWarning("Versions equal");
                File.WriteAllBytes($"{mod.PersistentDataDirectory}/LastUpdateTime.txt", BitConverter.GetBytes(DateTime.UtcNow.ToBinary()));
                yield break;
            }

            // Populate the version number field for the messagebox, and register for the New Game and Load Save events.
            downloadedVersionNumber = Encoding.ASCII.GetString(downloadedVersionNumberStripped);
            SaveLoadManager.OnLoad += SaveGameLoaded;
            StartGameBehaviour.OnNewGame += NewGameStarted;
        }

        private static void SaveGameLoaded(SaveData_v1 saveData)
        {
            NewGameStarted();
        }

        private static void NewGameStarted()
        {
            SaveLoadManager.OnLoad -= SaveGameLoaded;
            StartGameBehaviour.OnNewGame -= NewGameStarted;
            File.WriteAllBytes($"{mod.PersistentDataDirectory}/LastUpdateTime.txt", BitConverter.GetBytes(DateTime.UtcNow.ToBinary()));
            
            DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow, true)
            {
                ClickAnywhereToClose = true
            };
            DateTime nextUpdateCheck = DateTime.Now + new TimeSpan(updateInterval, 0, 0, 0);
            string[] messageLines =
            {
                "Message from Unofficial Block, Locations and Model Fixes mod:",
                "",
                $"You are using an outdated version. The latest version available is {downloadedVersionNumber}, but you are using version 2021.09.00",
                "It is recommended that you update to the latest version, which can be downloaded from Nexus Mods:",
                "https://www.nexusmods.com/daggerfallunity/mods/100",
                "",
                "Unless you disable update checking or change the waiting period in the mod's settings, ",
                $"the next update check will happen after {nextUpdateCheck.ToString(CultureInfo.CurrentCulture)}.",
                "",
                "Press any key to close this message."
            };
            messageBox.SetText(messageLines);
            messageBox.Show();
        }
    }
}
