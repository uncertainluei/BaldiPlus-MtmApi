using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MTM101BaldAPI.Registers;
using System.Linq;
using UnityEngine;

namespace MTM101BaldAPI.SaveSystem
{
    public class PartialModdedSavedGame
    {
        public int seed;
        public string[] mods;
        public bool hasFile;
        public bool canBeMoved;
        public Dictionary<string, string[]> tags;

        public PartialModdedSavedGame(int seed, string[] mods, Dictionary<string, string[]> tags)
        {
            this.seed = seed;
            this.mods = mods;
            hasFile = true;
            canBeMoved = false;
            this.tags = tags;
        }
        
        public PartialModdedSavedGame(ModdedSaveGame saveGame)
        {
            mods = ModdedSaveGame.ModdedSaveGameHandlers.Keys.ToArray();
            tags = new Dictionary<string, string[]>();
            foreach (KeyValuePair<string, string[]> kvp in saveGame.modTags)
            {
                tags.Add(kvp.Key, kvp.Value);
            }
            hasFile = saveGame.saveAvailable;
            canBeMoved = false;
        }

        public PartialModdedSavedGame(string[] mods, Dictionary<string, string[]> tags)
        {
            seed = 0;
            this.mods = mods;
            this.tags = tags;
            hasFile = false;
            canBeMoved = false;
        }

        public PartialModdedSavedGame()
        {
            this.seed = 0;
            this.mods = new string[0];
            this.hasFile = false;
            canBeMoved = false;
            tags = new Dictionary<string, string[]>();
        }
    }


    /// <summary>
    /// Stores the mod GUID and name of the ItemObject to allow it to be referenced later.
    /// </summary>
    [Serializable]
    public struct ModdedItemIdentifier
    {
        public byte version;
        public string itemPluginGUID;
        public string itemObjectName; // the name of the ItemObject

        public void Write(BinaryWriter writer)
        {
            writer.Write(version);
            writer.Write(itemPluginGUID);
            writer.Write(itemObjectName);
        }

        public ItemObject LocateObject()
        {
            ModdedItemIdentifier thiz = this;
            ItemMetaData[] datas = MTM101BaldiDevAPI.itemMetadata.FindAll(x => x.info.Metadata.GUID == thiz.itemPluginGUID);
            for (int i = 0; i < datas.Length; i++)
            {
                ItemObject[] objectsMatchingName = datas[i].itemObjects.Where(x => x.name == thiz.itemObjectName).ToArray();
                if (objectsMatchingName.Length != 0) return objectsMatchingName.Last();
            }
            return null;
        }

        public ModdedItemIdentifier(ItemObject objct)
        {
            ItemMetaData meta = objct.GetMeta() ?? throw new NullReferenceException("Object: " + objct.name + " doesn't have meta! Can't create ModdedItemIdentifier!");
            version = 0;
            itemPluginGUID = meta.info.Metadata.GUID;
            itemObjectName = objct.name;
        }

        public static ModdedItemIdentifier Read(BinaryReader reader)
        {
            ModdedItemIdentifier dent = new ModdedItemIdentifier
            {
                version = reader.ReadByte(),
                itemPluginGUID = reader.ReadString(),
                itemObjectName = reader.ReadString()
            };
            return dent;
        }
    }

    public enum ModdedSaveLoadStatus
    {
        Success,
        NoSave,
        MissingHandlers,
        MissingItems,
        MismatchedTags
    }

    public enum SceneIndexMethod : byte
    {
        Metadata, // this is the one that should almost always be used under every circumstance.
        Name // this is for modded SceneObjects that haven't had metadata added yet.
    }

    public struct SceneObjectIdentifier
    {
        public SceneIndexMethod method;
        public string value;

        public SceneObjectIdentifier(SceneIndexMethod method, string value)
        {
            this.value = value;
            this.method = method;
        }

        public static SceneObjectIdentifier Read(BinaryReader reader)
        {
            return new SceneObjectIdentifier((SceneIndexMethod)reader.ReadByte(), reader.ReadString());
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)method);
            writer.Write(value);
        }

        public SceneObject GetSceneObject()
        {
            string v = value;
            switch (method)
            {
                case SceneIndexMethod.Metadata:
                    string[] split = v.Split('\0');
                    return MTM101BaldiDevAPI.sceneMeta.Find(x => x.info.Metadata.GUID == split[0] && x.value.name == split[1]).value;
                case SceneIndexMethod.Name:
                    MTM101BaldiDevAPI.Log.LogWarning("Attempted to find SceneObject via name in a SceneObjectIdentifier! (" + v + ")");
                    return Resources.FindObjectsOfTypeAll<SceneObject>().First(x => x.name == v);
            }
            return null;
        }
    }

    /// <summary>
    /// The root class for modded save games, storing the base game data, but restructured in a way that is more mod friendly.
    /// </summary>
    public class ModdedSaveGame
    {
        public Dictionary<string, string[]> modTags = new Dictionary<string, string[]>();
        //private SceneObjectIdentifier _level;
        //public SceneObject level
        //{
        //    get
        //    {
        //        if (!saveAvailable) return null;
        //        return _level.GetSceneObject();
        //    }
        //    set
        //    {
        //        SceneObjectMetadata meta = value.GetMeta();
        //        if (meta != null)
        //        {
        //            _level = new SceneObjectIdentifier(SceneIndexMethod.Metadata, meta.info.Metadata.GUID + "\0" + meta.value.name);
        //            return;
        //        }
        //        MTM101BaldiDevAPI.Log.LogWarning("Had to resort to fallback for: " + value.name + "!");
        //        _level = new SceneObjectIdentifier(SceneIndexMethod.Name, value.name);
        //    }
        //}

        public const int version = 6;
        public bool saveAvailable = false;
        internal static Dictionary<string, ModdedSaveGameIOBinary> ModdedSaveGameHandlers = new Dictionary<string, ModdedSaveGameIOBinary>();


        public void FillBlankModTags()
        {
            ModdedSaveGameHandlers.Do(x =>
            {
                if (!modTags.ContainsKey(x.Key))
                {
                    modTags.Add(x.Key, x.Value.GenerateTags());
                }
            });
        }

        public static void AddSaveHandler(ModdedSaveGameIOBinary handler)
        {
            if (handler.pluginInfo == null) throw new ArgumentNullException("You need to create a class that inherits from the ModdedSaveGameIOBinary class!");
            MTM101BaldiDevAPI.saveHandler = SavedGameDataHandler.Modded;
            ModdedSaveGameHandlers.Add(handler.pluginInfo.Metadata.GUID, handler);
        }

        public static void AddSaveHandler(PluginInfo info)
        {
            AddSaveHandler(new ModdedSaveGameIODummy(info));
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(MTM101BaldiDevAPI.VersionNumber);
            writer.Write(saveAvailable);
            writer.Write(version);
            writer.Write(ModdedSaveGameHandlers.Count);
            foreach (KeyValuePair<string, ModdedSaveGameIOBinary> kvp in ModdedSaveGameHandlers)
            {
                writer.Write(kvp.Key);
                string[] tags = kvp.Value.GenerateTags();
                writer.Write(tags.Length);
                for (int i = 0; i < tags.Length; i++)
                {
                    writer.Write(tags[i]);
                }
            }
            if (!saveAvailable) return;
            foreach (KeyValuePair<string, ModdedSaveGameIOBinary> kvp in ModdedSaveGameHandlers)
            {
                kvp.Value.Save(writer);
            }
        }

        public static PartialModdedSavedGame PartialLoad(BinaryReader reader)
        {
            reader.ReadString();
            bool saveAvailable = reader.ReadBoolean();
            if (!saveAvailable)
            {
                if (reader.BaseStream.Position >= reader.BaseStream.Length) //we must be in an older version, return early.
                {
                    return new PartialModdedSavedGame();
                }
            }
            int version = reader.ReadInt32();
            int modCount = reader.ReadInt32();
            List<string> modHandlers = new List<string>();
            Dictionary<string, string[]> modTags = new Dictionary<string, string[]>();
            for (int i = 0; i < modCount; i++)
            {
                string modHandler = reader.ReadString();
                modHandlers.Add(modHandler);
                if (version >= 3)
                {
                    int tagsToRead = reader.ReadInt32();
                    List<string> tags = new List<string>();
                    for (int j = 0; j < tagsToRead; j++)
                    {
                        tags.Add(reader.ReadString());
                    }
                    modTags.Add(modHandler, tags.ToArray());
                }
                else
                {
                    modTags.Add(modHandler, new string[0]);
                }
            }
            if (!saveAvailable)
            {
                return new PartialModdedSavedGame(modHandlers.ToArray(), modTags);
            }
            if (version >= 4)
            {
                SceneObjectIdentifier.Read(reader);
            }
            else
            {
                reader.ReadInt32();
            }
            int seed = reader.ReadInt32();
            return new PartialModdedSavedGame(seed, modHandlers.ToArray(), modTags);
        }

        public ModdedSaveLoadStatus Load(BinaryReader reader, bool addMissingTags)
        {
            modTags.Clear();
            bool tagsMatch = true;
            reader.ReadString();
            saveAvailable = reader.ReadBoolean();
            if (!saveAvailable)
            {
                if (reader.BaseStream.Position >= reader.BaseStream.Length) //we must be in an older version
                {
                    FillBlankModTags();
                    return ModdedSaveLoadStatus.NoSave;
                }
            }
            int version = reader.ReadInt32();
            int modCount = reader.ReadInt32();
            List<string> modHandlers = new List<string>();
            for (int i = 0; i < modCount; i++)
            {
                string modHandler = reader.ReadString();
                modHandlers.Add(modHandler);
                if (version >= 3)
                {
                    int tagsToRead = reader.ReadInt32();
                    List<string> tags = new List<string>();
                    for (int j = 0; j < tagsToRead; j++)
                    {
                        tags.Add(reader.ReadString());
                    }
                    modTags.Add(modHandler, tags.ToArray());
                    if (ModdedSaveGameHandlers.ContainsKey(modHandler))
                    {
                        if (ModdedSaveGameHandlers[modHandler].TagsReady())
                        {
                            string[] generatedTags = ModdedSaveGameHandlers[modHandler].GenerateTags();
                            // if the lengths dont match obviously the rest of the tags won't
                            if (generatedTags.Length != tagsToRead)
                            {
                                tagsMatch = false;
                                continue;
                            }
                            for (int j = 0; j < generatedTags.Length; j++)
                            {
                                if (!tags.Contains(generatedTags[j]))
                                {
                                    tagsMatch = false;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        tagsMatch = false;
                    }
                }
                else
                {
                    modTags.Add(modHandler, new string[0]);
                }
            }
            if (!saveAvailable) return ModdedSaveLoadStatus.NoSave;
            
            // seperate the verification from the actual reading part so we dont partially load mod stuff.
            // we can gurantee that we can reset this class but nothing about the others.
            if (!tagsMatch) return ModdedSaveLoadStatus.MismatchedTags;
            for (int i = 0; i < modHandlers.Count; i++)
            {
                if (!ModdedSaveGameHandlers.ContainsKey(modHandlers[i])) return ModdedSaveLoadStatus.MissingHandlers;
            }
            for (int i = 0; i < modHandlers.Count; i++)
            {
                ModdedSaveGameHandlers[modHandlers[i]].Load(reader);
            }
            if (addMissingTags)
            {
                foreach (KeyValuePair<string, ModdedSaveGameIOBinary> kvp in ModdedSaveGameHandlers)
                {
                    if (modTags.ContainsKey(kvp.Key)) continue;
                    modTags.Add(kvp.Key, kvp.Value.GenerateTags());
                }
            }
            return ModdedSaveLoadStatus.Success;
        }
    }
}
