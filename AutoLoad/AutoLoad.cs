﻿using System.IO;
using System.Collections;
using UnityEngine;
using UWE;

namespace Straitjacket.Subnautica.Mods.AutoLoad
{
    internal class AutoLoad
    {
        /// <summary>
        /// Copied from uGUI_MainMenu, altered to work statically
        /// </summary>
        public static void LoadMostRecentSavedGame()
        {
            string[] activeSlotNames = SaveLoadManager.main.GetActiveSlotNames();
            long num = 0L;
            SaveLoadManager.GameInfo gameInfo = null;
            string saveGame = string.Empty;
            int i = 0;
            int num2 = activeSlotNames.Length;
            while (i < num2)
            {
                SaveLoadManager.GameInfo gameInfo2 = SaveLoadManager.main.GetGameInfo(activeSlotNames[i]);
                if (gameInfo2.dateTicks > num)
                {
                    gameInfo = gameInfo2;
                    num = gameInfo2.dateTicks;
                    saveGame = activeSlotNames[i];
                }
                i++;
            }
            if (gameInfo != null)
            {
                CoroutineHost.StartCoroutine(LoadGameAsync(saveGame, gameInfo.changeSet, gameInfo.gameMode));
            }
        }

        private static bool isStartingNewGame = false;
        /// <summary>
        /// Copied from uGUI_MainMenu, altered to work statically
        /// </summary>
        /// <param name="saveGame"></param>
        /// <param name="changeSet"></param>
        /// <param name="gameMode"></param>
        /// <returns></returns>
        public static IEnumerator LoadGameAsync(string saveGame, int changeSet, GameMode gameMode)
        {
            if (isStartingNewGame)
            {
                yield break;
            }
            isStartingNewGame = true;
            FPSInputModule.SelectGroup(null, false);
            uGUI.main.loading.ShowLoadingScreen();
            yield return BatchUpgrade.UpgradeBatches(saveGame, changeSet);
            global::Utils.SetContinueMode(true);
            global::Utils.SetLegacyGameMode(gameMode);
            SaveLoadManager.main.SetCurrentSlot(Path.GetFileName(saveGame));
            VRLoadingOverlay.Show();
            CoroutineTask<SaveLoadManager.LoadResult> task = SaveLoadManager.main.LoadAsync();
            yield return task;
            SaveLoadManager.LoadResult result = task.GetResult();
            if (!result.success)
            {
                yield return new WaitForSecondsRealtime(1f);
                isStartingNewGame = false;
                uGUI.main.loading.End(false);
                string descriptionText = Language.main.GetFormat<string>("LoadFailed", result.errorMessage);
                if (result.error == SaveLoadManager.Error.OutOfSpace)
                {
                    descriptionText = Language.main.Get("LoadFailedSpace");
                }
                uGUI.main.confirmation.Show(descriptionText, delegate (bool confirmed)
                {
                    OnErrorConfirmed(confirmed, saveGame, changeSet, gameMode);
                });
            }
            else
            {
                FPSInputModule.SelectGroup(null, false);
                uGUI.main.loading.BeginAsyncSceneLoad("Main");
            }
            isStartingNewGame = false;
            yield break;
        }

        /// <summary>
        /// Copied from uGUI_MainMenu, altered to work statically
        /// </summary>
        /// <param name="confirmed"></param>
        /// <param name="saveGame"></param>
        /// <param name="changeSet"></param>
        /// <param name="gameMode"></param>
        private static void OnErrorConfirmed(bool confirmed, string saveGame, int changeSet, GameMode gameMode)
        {
            if (confirmed)
            {
                CoroutineHost.StartCoroutine(LoadGameAsync(saveGame, changeSet, gameMode));
                return;
            }
            FPSInputModule.SelectGroup(null, false);
        }

        public static StartScreen StartScreen;
        /// <summary>
        /// Extremely trimmed down version of StartScreen.Load(). Waits for the active save slots to be loaded
        /// and parsed, then loads the most recent save. If there are no saves, falls back to the original.
        /// </summary>
        /// <returns></returns>
        public static IEnumerator StartScreen_Load()
        {
            yield return SaveLoadManager.main.earlySlotLoading;
            if (SaveLoadManager.main.GetActiveSlotNames().Length == 0)
            {
                StartScreen.OnGuiInitialized();
            }
            else
            {
                LoadMostRecentSavedGame();
            }
        }
    }
}