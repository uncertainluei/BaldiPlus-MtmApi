using BepInEx;

using System;
using System.Collections.Generic;
using System.Linq;

namespace MTM101BaldAPI.Registers
{
    // flags for basic item behaviors so mods know how to handle them
    [Flags]
    public enum ItemFlags
    {
        /// <summary>
        /// This item has no necessary flags.
        /// </summary>
        None = 0,
        /// <summary>
        /// // This item has multiple uses like the grappling hook.
        /// </summary>
        MultipleUse = 1,
        /// <summary>
        /// This item should not appear in the players inventory and is used instantly upon pickup.
        /// </summary>
        InstantUse = 2,
        /// <summary>
        /// This item doesn't do anything when used, regardless of circumstance. This is for items like the Apple, but not the quarter as it can be used in machines.
        /// </summary>
        NoUses = 4,
        /// <summary>
        /// This item's behavior doesn't instantly destroy itself when used. This is applicable for the BSODA or the Techno Boots.
        /// </summary>
        Persists = 8,
        /// <summary>
        /// This item creates a physical entity in the world, this is applicable for the BSODA but not the Techno Boots.
        /// </summary>
        CreatesEntity = 16,
        /// <summary>
        /// This item has a variant in the tutorial that must be accounted for when handling MultipleUse.
        /// </summary>
        HasTutorialVariant = 32
    }

    public class ItemMetaData : IMetadata<ItemObject>
    {

        public ItemObject[] itemObjects; // for things like the grappling hook, the highest use count should be stored first.

        public ItemObject value => itemObjects.Last();

        public PluginInfo info => _info;
        private PluginInfo _info;

        public int generatorCost => value.cost;
        public string nameKey => value.nameKey;

        public ItemFlags flags;
        public Items id => value.itemType;

        public List<string> tags => _tags;
        List<string> _tags = new List<string>();

        public ItemMetaData(PluginInfo info, ItemObject itmObj)
        {
            itemObjects = new ItemObject[1] { itmObj };
            _info = info;
        }

        public ItemMetaData(PluginInfo info, ItemObject[] itmObjs)
        {
            itemObjects = itmObjs;
            _info = info;
        }
    }

    public class ItemMetaStorage : BasicMetaStorage<ItemMetaData, ItemObject>
    {
        public static ItemMetaStorage Instance => MTM101BaldiDevAPI.itemMetadata;

        public ItemMetaData FindByEnum(Items itm)
        {
            return Find(x =>
            {
                return x.id == itm;
            });
        }

        public ItemMetaData FindByEnumFromMod(Items itm, PluginInfo specificMod)
        {
            return Find(x =>
            {
                return (x.id == itm) && (x.info == specificMod);
            });
        }

        public ItemMetaData[] GetAllWithFlags(ItemFlags flag)
        {
            return FindAll(x =>
            {
                return x.flags.HasFlag(flag);
            }).Distinct().ToArray();
        }

        public ItemMetaData[] GetAllFromMod(PluginInfo mod)
        {
            return FindAll(x =>
            {
                return x.info == mod;
            }).Distinct().ToArray();
        }

        public ItemMetaData[] GetAllWithoutFlags(ItemFlags flag)
        {
            return FindAll(x =>
            {
                return !x.flags.HasFlag(flag);
            }).Distinct().ToArray();
        }
    }
}
