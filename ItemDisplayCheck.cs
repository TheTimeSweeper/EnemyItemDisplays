using RoR2;
using RoR2.ContentManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnemyItemDisplays
{
    internal static class ItemDisplayCheck
    {
        public static List<Object> allDisplayedItems = null;

        private static void GatherAllItems()
        {
            allDisplayedItems = new List<Object>(ItemDisplays.KeyAssetDisplayPrefabs.Keys);

            allDisplayedItems.Sort((item1, item2) =>
            {
                //sort itemdefs by tier
                if (item1 is ItemDef && item2 is ItemDef)
                {
                    return (item1 as ItemDef).tier.CompareTo((item2 as ItemDef).tier);
                }
                //sort equipmentedefs last
                if (item1 is ItemDef && item2 is EquipmentDef)
                {
                    return 1;
                }
                if (item1 is EquipmentDef && item2 is ItemDef)
                {
                    return -1;
                }

                return 0;
            });


            //filter out equipmentdefs and only keep item tiers we're worried about
            //evolution only gives monsters tier 1 2 and 3 items. I suppose with mods they can get other tiers but we'll keep the scope low for now
            for (int i = allDisplayedItems.Count - 1; i >= 0; i--)
            {
                ItemDef item = allDisplayedItems[i] as ItemDef;

                if (!item)
                {
                    allDisplayedItems.Remove(allDisplayedItems[i]);
                    continue;
                }

                if(item.tier == ItemTier.NoTier)
                {
                    allDisplayedItems.Remove(item);
                }
            }

        }

        public static void PrintUnused(ItemDisplayRuleSet itemDisplayRuleSet, string bodyName)
        {
            PrintUnused(itemDisplayRuleSet.keyAssetRuleGroups.ToList(), bodyName);
        }

        public static void PrintUnused(IEnumerable<ItemDisplayRuleSet.KeyAssetRuleGroup> ruleSet, string bodyName)
        {
            string missingDisplays = $"generating item displays for {bodyName}";

            //grab all keyassets
            if (allDisplayedItems == null)
                GatherAllItems();

            //remove from list keyassets that we already have displays for
            List<Object> missingKeyAssets = new List<Object>(allDisplayedItems);
            string firstCompatibleChild = "";
            foreach (ItemDisplayRuleSet.KeyAssetRuleGroup ruleGroup in ruleSet)
            {
                if (ruleGroup.displayRuleGroup.rules == null) continue;
                if (ruleGroup.displayRuleGroup.rules.Length == 0)
                    continue;

                missingKeyAssets.Remove(ruleGroup.keyAsset);
                if (string.IsNullOrEmpty(firstCompatibleChild))
                {
                    firstCompatibleChild = ruleGroup.displayRuleGroup.rules[0].childName;
                }
            }

            //print all display rules
            foreach (Object keyAsset in missingKeyAssets)
            {
                string thing = $"";
                if (ItemDisplays.KeyAssetDisplayPrefabs.ContainsKey(keyAsset))
                {
                    //if we have a displayprefab for it (Populated in ItemDisplays.PopulateDisplays),
                    //generate a rule formatted to the code in this project
                    thing += SpitOutNewRule(keyAsset, firstCompatibleChild, ItemDisplays.KeyAssetDisplayPrefabs[keyAsset]);
                }
                else
                {
                    Log.Error($"COULD NOT FIND DISPLAY PREFABS FOR KEYASSET {keyAsset}");
                }
                missingDisplays += thing;
            }
            //add them all to a giant string and print it out, formatted
            Log.Message(missingDisplays);
        }

        private static string SpitOutNewRule(Object asset, string firstCompatibleChild, List<string> prefabNames)
        {
            string contentType = asset is ItemDef ? "Items" : "Equipment";

            if (prefabNames.Count == 0)
                return $"\n[NO PREFABS FOUND FOR THE KEYASSET {asset}";

            if (prefabNames.Count == 1)
            {
                return string.Format(
                    "  [\r\n   \"{0}\",\r\n   [\r\n    [\r\n     \"{1}\",\r\n     \"{2}\",\r\n     \"{3}\",\r\n     [2,2,2],\r\n     [0,0,0],\r\n     [1,1,1]\r\n    ]\r\n   ]\r\n  ],\r\n",
                    asset.name,
                    prefabNames[0],
                    "", // TODO: asset guid will most likely become a thing in DLC3
                    firstCompatibleChild
                    );
            }

            string newRule = string.Format("  [\r\n   \"{0}\",\r\n   [\r\n", asset.name);
            List<string> displays = new List<string>();
            for (int i = 0; i < prefabNames.Count; i++)
            {
                displays.Add(string.Format(
                    "    [\r\n     \"{0}\",\r\n     \"{1}\",\r\n     \"{2}\",\r\n     [2,2,2],\r\n     [0,0,0],\r\n     [1,1,1]\r\n    ]",
                    prefabNames[0],
                    "", // TODO: asset guid will most likely become a thing in DLC3
                    firstCompatibleChild));
            }
            newRule += string.Join(",\r\n", displays);
            newRule += "\r\n   ]\r\n  ],\r\n";
            return newRule;
        }
    }
}