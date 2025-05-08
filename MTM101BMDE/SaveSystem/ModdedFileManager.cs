﻿using HarmonyLib;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MTM101BaldAPI.SaveSystem
{
    public class ModdedFileManager : Singleton<ModdedFileManager>
    {
        public ModdedSaveGame saveData = new ModdedSaveGame();
        public Dictionary<int, PartialModdedSavedGame> saveDatas = new Dictionary<int, PartialModdedSavedGame>();
        public int saveIndex { get; internal set; }
        public string savePath { get; internal set; }
        public List<int> saveIndexes = new List<int>();

        public void CreateSavedGameCoreManager(GameLoader loader)
        {
            UnityEngine.Object.Instantiate<CoreGameManager>(loader.cgmPre);
            ModdedSaveGame savedGameData = saveData;
        }

        public void RegenerateTags()
        {
            if (TagsMatch()) return;
            MTM101BaldiDevAPI.Log.LogInfo("Tags don't match! Reloading save...");
            // force a reload
            saveIndex = 0;
            savePath = "";
            Singleton<PlayerFileManager>.Instance.Load();
        }

        bool TagsMatch()
        {
            foreach (KeyValuePair<string, ModdedSaveGameIOBinary> kvp in ModdedSaveGame.ModdedSaveGameHandlers)
            {
                if (!kvp.Value.TagsReady()) continue;
                string[] tags = kvp.Value.GenerateTags();
                if (!saveData.modTags.ContainsKey(kvp.Key)) return false;
                if (saveData.modTags[kvp.Key].Length != tags.Length) return false;
                for (int i = 0; i < tags.Length; i++)
                {
                    if (!saveData.modTags[kvp.Key].Contains(tags[i])) return false;
                }
            }
            return true;
        }

        public int FindAppropiateSaveGame(string myPath, bool ignoreAlready)
        {
            // compare the currently cached path to the new path, if they aren't the same, then reset the save index
            if (myPath != savePath)
            {
                saveIndex = 0;
            }
            if ((!ignoreAlready) && (saveIndex != 0))
            {
                return saveIndex;
            }
            savePath = myPath;
            saveIndexes.Clear();
            saveDatas.Clear();
            if (File.Exists(Path.Combine(myPath, "availableSlots.txt")))
            {
                saveIndexes.AddRange(File.ReadAllLines(Path.Combine(myPath, "availableSlots.txt")).Select(x => int.Parse(x)));
            }
            else
            {
                if (File.Exists(Path.Combine(myPath, "savedgame0.bbapi")))
                {
                    File.Move(Path.Combine(myPath, "savedgame0.bbapi"), Path.Combine(myPath, "savedgame1.bbapi"));
                    saveIndexes.Add(1);
                    FileStream fs = File.OpenRead(Path.Combine(myPath, "savedgame1.bbapi"));
                    BinaryReader reader = new BinaryReader(fs);
                    saveDatas.Add(1, ModdedSaveGame.PartialLoad(reader));
                    reader.Close();
                    return 1;
                }
                int validIndex = 1;
                saveIndexes.Add(validIndex);
                saveDatas.Add(validIndex, new PartialModdedSavedGame());
                saveIndexes.Sort();
                return validIndex;
            }
            for (int i = 0; i < saveIndexes.Count; i++)
            {
                if (!File.Exists(Path.Combine(myPath, "savedgame" + saveIndexes[i] + ".bbapi"))) continue;
                FileStream fs = File.OpenRead(Path.Combine(myPath, "savedgame" + saveIndexes[i] + ".bbapi"));
                BinaryReader reader = new BinaryReader(fs);
                saveDatas.Add(saveIndexes[i], ModdedSaveGame.PartialLoad(reader));
                reader.Close();
            }
            // a list of kvps that has every file that shares the same mods, might include files with less mods
            // also includes mods with matching tags that are ready for tag comparisons
            KeyValuePair<int, PartialModdedSavedGame>[] containsAllMods = saveDatas.Where(x =>
            {
                for (int i = 0; i < x.Value.mods.Length; i++)
                {
                    string mod = x.Value.mods[i];
                    if (!ModdedSaveGame.ModdedSaveGameHandlers.ContainsKey(mod))
                    {
                        return false;
                    }
                    // only check tags if they are ready
                    if (ModdedSaveGame.ModdedSaveGameHandlers[mod].TagsReady())
                    {
                        string[] tagsToCheck = ModdedSaveGame.ModdedSaveGameHandlers[mod].GenerateTags();
                        // if there is a length mismatch, tags aren't matching!
                        if (x.Value.tags[mod].Length != tagsToCheck.Length)
                        {
                            return false;
                        }
                        for (int j = 0; j < tagsToCheck.Length; j++)
                        {
                            if (!x.Value.tags[mod].Contains(tagsToCheck[j]))
                            {
                                return false;
                            }
                        }
                    }
                }
                return true;
            }).ToArray();
            containsAllMods.Select(x => x.Value).Do(x => x.canBeMoved = true);
            // no need for any more tag checks, as containsAllMods already has tags that match exactly
            KeyValuePair<int, PartialModdedSavedGame>[] containsExactMods = containsAllMods.Where(x =>
            {
                int mods = 0;
                for (int i = 0; i < x.Value.mods.Length; i++)
                {
                    if (ModdedSaveGame.ModdedSaveGameHandlers.ContainsKey(x.Value.mods[i]))
                    {
                        mods++;
                    }
                }
                return mods == ModdedSaveGame.ModdedSaveGameHandlers.Count;
            }).ToArray();
            if (containsExactMods.Length > 1)
            {
                MTM101BaldiDevAPI.Log.LogError("Dirty hacker! Found duplicate files with same mods and tags! Unfortunately, can't do anything about this, but SHAME! SHAMMEEE!");
            }
            saveData.FillBlankModTags();
            if (containsExactMods.Length == 0)
            {
                int validIndex = 1;
                while (saveDatas.ContainsKey(validIndex)) validIndex++;
                saveIndexes.Add(validIndex);
                saveDatas.Add(validIndex, new PartialModdedSavedGame());
                saveIndexes.Sort();
                return validIndex;
            }
            containsExactMods[0].Value.canBeMoved = false;
            return containsExactMods[0].Key;
        }

        public void SaveFileList(string myPath)
        {
            string toPrint = "";
            saveIndexes.Do(x => toPrint += (x + "\n"));
            toPrint = toPrint.Trim();
            File.WriteAllText(Path.Combine(myPath, "availableSlots.txt"), toPrint);
        }

        public void UpdateCurrentPartialSave()
        {
            saveDatas[saveIndex] = new PartialModdedSavedGame(saveData);
        }

        public void DeleteIndexedGame(int index)
        {
            string myPath = ModdedSaveSystem.GetSaveFolder(MTM101BaldiDevAPI.Instance, Singleton<PlayerFileManager>.Instance.fileName);
            File.Delete(Path.Combine(myPath, "savedgame" + index + ".bbapi"));
            saveDatas.Remove(index);
            saveIndexes.Remove(index);
            SaveFileList(myPath);
        }

        public void SaveGameWithIndex(string path, int index)
        {
            FileStream fs = File.OpenWrite(Path.Combine(path, "savedgame" + index + ".bbapi"));
            fs.SetLength(0); // make sure to clear the contents before writing to it!
            BinaryWriter writer = new BinaryWriter(fs);
            saveData.Save(writer);
            writer.Close();
        }

        public void LoadGameWithIndex(string path, int index, bool addMissingTags)
        {
            if (!File.Exists(Path.Combine(path, "savedgame" + index + ".bbapi"))) return;
            FileStream fs = File.OpenRead(Path.Combine(path, "savedgame" + index + ".bbapi"));
            BinaryReader reader = new BinaryReader(fs);
            ModdedSaveLoadStatus status = Singleton<ModdedFileManager>.Instance.saveData.Load(reader, addMissingTags);
            reader.Close();
            switch (status)
            {
                default:
                    break;
                case ModdedSaveLoadStatus.MissingHandlers:
                    MTM101BaldiDevAPI.Log.LogWarning("Failed to load save because one or more mod handlers were missing!");
                    Singleton<ModdedFileManager>.Instance.saveData.saveAvailable = false;
                    break;
                case ModdedSaveLoadStatus.MismatchedTags:
                    MTM101BaldiDevAPI.Log.LogWarning("Failed to load save because the tags were mismatched!");
                    Singleton<ModdedFileManager>.Instance.saveData.saveAvailable = false;
                    break;
                case ModdedSaveLoadStatus.MissingItems:
                    if (ItemMetaStorage.Instance.All().Length == 0) break; //item metadata hasnt loaded yet!
                    MTM101BaldiDevAPI.Log.LogWarning("Failed to load save because one or more items couldn't be found!");
                    Singleton<ModdedFileManager>.Instance.saveData.saveAvailable = false;
                    break;
                case ModdedSaveLoadStatus.NoSave:
                    MTM101BaldiDevAPI.Log.LogInfo("No save data was found.");
                    break;
                case ModdedSaveLoadStatus.Success:
                    MTM101BaldiDevAPI.Log.LogInfo("Modded Savedata was succesfully loaded!");
                    break;
            }
        }

        public void DeleteSavedGame()
        {
            saveData.saveAvailable = false;
            Singleton<PlayerFileManager>.Instance.Save();
        }
    }



    // ******* Patches ******* //

    [HarmonyPatch(typeof(GameLoader))]
    [HarmonyPatch("SetSave")]
    class DisableSave
    {
        static void Prefix(ref bool val)
        {
            val = val & MTM101BaldiDevAPI.SaveGamesEnabled;
        }
    }

    //[HarmonyPatch(typeof(GameLoader))]
    //[HarmonyPatch("LoadSavedGame")]
    //class LoadModdedSavedGame
    //{
    //    static bool Prefix(GameLoader __instance)
    //    {
    //        if (MTM101BaldiDevAPI.SaveGamesHandler != SavedGameDataHandler.Modded) return true;
    //        Singleton<ModdedFileManager>.Instance.CreateSavedGameCoreManager(__instance);
    //        Singleton<CursorManager>.Instance.LockCursor();
    //        __instance.SetMode(0);
    //        Singleton<ModdedFileManager>.Instance.DeleteSavedGame();
    //        ModdedSaveGame.ModdedSaveGameHandlers.Do(x =>
    //        {
    //            x.Value.OnCGMCreated(Singleton<CoreGameManager>.Instance, true);
    //        });
    //        return false;
    //    }
    //}

    [HarmonyPatch(typeof(GameLoader))]
    [HarmonyPatch("Initialize")]
    class LoadStandardGame
    {
        static void Postfix(GameLoader __instance)
        {
            if ((MTM101BaldiDevAPI.SaveGamesHandler != SavedGameDataHandler.Modded) && (!MTM101BaldiDevAPI.SaveGameHasMods)) return;
            ModdedSaveGame.ModdedSaveGameHandlers.Do(x =>
            {
                x.Value.OnCGMCreated(Singleton<CoreGameManager>.Instance, false);
            });
        }
    }

    [HarmonyPatch(typeof(PlayerFileManager))]
    [HarmonyPatch("Start")]
    class AddModdedFM
    {
        static void Prefix(PlayerFileManager __instance)
        {
            __instance.gameObject.AddComponent<ModdedFileManager>();
        }
    }

    [HarmonyPatch(typeof(PlayerFileManager))]
    [HarmonyPatch("ResetSaveData")]
    class ResetModdedData
    {
        static void Postfix()
        {
            if (MTM101BaldiDevAPI.SaveGamesHandler != SavedGameDataHandler.Modded) return;
            ModdedFileManager.Instance.saveData = new ModdedSaveGame();
            ModdedFileManager.Instance.saveData.FillBlankModTags();
            ModdedSaveGame.ModdedSaveGameHandlers.Do(x =>
            {
                x.Value.Reset();
            });
            Singleton<ModdedFileManager>.Instance.savePath = "";
            Singleton<ModdedFileManager>.Instance.saveIndex = 0; //force a reload next time we try to grab data
        }
    }

    //[HarmonyPatch(typeof(CoreGameManager))]
    //[HarmonyPatch("SaveAndQuit")]
    //class SaveAndQuitModdedData
    //{
    //    // override the function completely, if we make sure every reference is referring to ModdedSaveGame, this should leave vanilla games intact.
    //    static bool Prefix(CoreGameManager __instance, ref int ___lives, ref int ___seed, ref bool[,] ___foundTilesToRestore, ref IntVector2 ___savedMapSize, ref List<Vector2> ___markerPositions, ref List<int> ___markerIds, ref int ___attempts)
    //    {
    //        if (MTM101BaldiDevAPI.SaveGamesHandler != SavedGameDataHandler.Modded) return true;
    //        ModdedSaveGame newSave = new ModdedSaveGame();
    //        newSave.saveAvailable = true;
    //        newSave.FillBlankModTags();
    //        Singleton<ModdedFileManager>.Instance.saveData = newSave;
    //        Singleton<PlayerFileManager>.Instance.Save();
    //        __instance.Quit();
    //        return false;
    //    }
    //}
}
