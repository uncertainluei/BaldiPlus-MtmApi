using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MTM101BaldAPI
{

    public static class CustomLevelObjectExtensions
    {
        /// <summary>
        /// Get all the level objects referenced by the specified SceneObject.
        /// </summary>
        /// <param name="sceneObj"></param>
        /// <returns></returns>
        public static CustomLevelObject[] GetCustomLevelObjects(this SceneObject sceneObj)
        {
            return new CustomLevelObject[] { (CustomLevelObject)sceneObj.levelObject };
        }

        public static CustomLevelObject GetCustomLevelObject(this SceneObject sceneObj)
        {
            return (CustomLevelObject)sceneObj.levelObject;
        }
    }


    /// <summary>
    /// A custom version of the LevelObject class, currently doesn't contain much else but it serves as a good base to make extending level generator functionality easy in the future.
    /// </summary>
    public class CustomLevelObject : LevelObject
    {

        private Dictionary<string, Dictionary<string, object>> customModDatas = new Dictionary<string, Dictionary<string, object>>();

        public object GetCustomModValue(string modUUID, string key)
        {
            if (!customModDatas.ContainsKey(modUUID)) return null;
            if (!customModDatas[modUUID].ContainsKey(key)) return null;
            return customModDatas[modUUID][key];
        }

        public object GetCustomModValue(PluginInfo pluginInfo, string key)
        {
            return GetCustomModValue(pluginInfo.Metadata.GUID, key);
        }

        /// <summary>
        /// Makes a clone of this CustomLevelObject, preserving the custom mod data.
        /// </summary>
        /// <returns></returns>
        public CustomLevelObject MakeClone()
        {
            CustomLevelObject obj = CustomLevelObject.Instantiate(this);
            foreach (KeyValuePair<string, Dictionary<string, object>> kvp in customModDatas)
            {
                foreach (KeyValuePair<string, object> internalKvp in kvp.Value)
                {
                    obj.SetCustomModValue(kvp.Key, internalKvp.Key, internalKvp.Value);
                }
            }
            return obj;
        }

        /// <summary>
        /// Adds the specified key/value to the CustomLevelObject, allowing for storing extra, mod specific generator settings.
        /// </summary>
        /// <param name="pluginInfo"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetCustomModValue(PluginInfo pluginInfo, string key, object value)
        {
            string modUUID = pluginInfo.Metadata.GUID;
            if (!customModDatas.ContainsKey(modUUID))
            {
                customModDatas.Add(modUUID, new Dictionary<string, object>());
            }
            if (customModDatas[modUUID].ContainsKey(key))
            {
                customModDatas[modUUID][key] = value;
                return;
            }
            customModDatas[modUUID].Add(key, value);
        }

        public void SetCustomModValue(string modUUID, string key, object value)
        {
            if (!customModDatas.ContainsKey(modUUID))
            {
                customModDatas.Add(modUUID, new Dictionary<string, object>());
            }
            if (customModDatas[modUUID].ContainsKey(key))
            {
                customModDatas[modUUID][key] = value;
                return;
            }
            customModDatas[modUUID].Add(key, value);
        }
    }
}
