// ReSharper disable once CommentTypo
// This file is or was originally a part of the Unofficial Block, Location and Model Fixes mod for Daggerfall Unity by XJDHDR,
// which can be found here: https://github.com/XJDHDR/DFU_UBLaMF
//
// The license for it may be found here:
// https://github.com/XJDHDR/DFU_UBLaMF/blob/master/License-MIT.txt
//

using System.Collections;
using System.Text;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using UnityEngine;

namespace UBLAMFMod
{
    public class Ublamf : MonoBehaviour
    {
	    // ==== Properties ====
	    internal static bool _DontCheckForUpdatesSetting { get; private set; }
	    internal static int _UpdateIntervalSetting { get; private set; }

	    internal static Mod _UblamfModInstance { get; private set; }
	    internal static ModSettings _UblamfModSettings { get; private set; }
	    internal static StringBuilder _ModStringBuilder { get; } = new StringBuilder();


        // ==== DFU and Unity initialisation methods ====
        // ReSharper disable once UnusedMember.Global
        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams InitParams)
        {
            _UblamfModInstance = InitParams.Mod;

            _UblamfModSettings = _UblamfModInstance.GetSettings();
            _DontCheckForUpdatesSetting = _UblamfModSettings.GetValue<bool>("UpdateOptions", "DisableUpdateCheck");
            _UpdateIntervalSetting = _UblamfModSettings.GetValue<int>("UpdateOptions", "UpdateInterval");

            GameObject go = new GameObject("UBLaMF manager");
            go.AddComponent<Ublamf>();

            _UblamfModInstance.IsReady = true;
        }

        private void Start() =>
	        UblamfUpdateCheck._GetDfuVersionNumberAndStartCheckerRoutine(this);


        // ==== Internal methods ====
        /// <summary>
        /// This helper method is used to start Coroutines from classes and structs that don't extend MonoBehaviour
        /// </summary>
        /// <param name="Routine">The method that will be run inside the Coroutine.</param>
        internal void _StartCoroutineHelper(IEnumerator Routine) =>
	        StartCoroutine(Routine);
    }
}
