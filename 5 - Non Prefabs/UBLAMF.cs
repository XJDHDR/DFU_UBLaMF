// This source code is or was originally a part of the "Unofficial Block, Location and Model Fixes" mod for Daggerfall Unity.
//
// This source code is provided under the following license:
// https://github.com/XJDHDR/DFU_UBLaMF/blob/master/License-MIT.txt

using System;
using System.Collections;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using UnityEngine.Networking;

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
            UnityWebRequest webRequest = UnityWebRequest.Get("https://raw.githubusercontent.com/XJDHDR/DFU_UBLaMF/master/Helpers%20and%20Cheat%20Sheets/Faction%20codes.url");
            yield return webRequest.SendWebRequest();

            // If UnityWebRequest failed to download the latest version file, just abort the update checker.
            if (webRequest.error != null || webRequest.isNetworkError || webRequest.isHttpError)
                yield break;
            
            int downloadedVersionNumberLength = webRequest.downloadHandler.text.Length;
            char[] downloadedVersionNumberStripped = new char[downloadedVersionNumberLength];
            int stringIndexCounter = 0;
            int strippedStringIndexCounter = 0;
            while (stringIndexCounter < downloadedVersionNumberLength)
            {
                char currentChar = webRequest.downloadHandler.text[stringIndexCounter];
                switch(currentChar)
                {
                    case ' ':
                    case '\n':
                    case '\r':
                    case '\t':
                        // Ignore then
                        break;

                    default:
                        downloadedVersionNumberStripped[strippedStringIndexCounter++] = currentChar;
                        break;
                }
                ++stringIndexCounter;
            }
            downloadedVersionNumber = new string(downloadedVersionNumberStripped, 0, strippedStringIndexCounter);

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
            DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow, true)
            {
                ClickAnywhereToClose = true
            };
            messageBox.SetText(downloadedVersionNumber);
            messageBox.Show();
        }
    }
}
