using RoR2;
using RoR2.ContentManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static EnemyItemDisplays.SimpleJsonExtensions;
using static Rewired.UI.ControlMapper.ControlMapper;

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

        public static string PrintUnused(ItemDisplayRuleSet itemDisplayRuleSet, AdditionalChild[] additionalChildren, string bodyName)
        {
            return PrintUnused(itemDisplayRuleSet.keyAssetRuleGroups.ToList(), additionalChildren, bodyName);
        }

        public static string PrintUnused(IEnumerable<ItemDisplayRuleSet.KeyAssetRuleGroup> ruleSet, AdditionalChild[] additionalChildren, string bodyName)
        {
            Log.Info($"generating item displays for {bodyName}");

            SimpleJSON.JSONObject newJson = new SimpleJSON.JSONObject();

            SimpleJSON.JSONArray keyAssetArray = new SimpleJSON.JSONArray();
            SimpleJSON.JSONArray additionalChildrenArray = new SimpleJSON.JSONArray();

            if (additionalChildren.Length == 0)
            {
                additionalChildren = new AdditionalChild[] {
                    new AdditionalChild { Name = "ExampleChildName", Path = "Example/Child/Path/To/Transform" },
                    new AdditionalChild { Name = "ExampleChildName2", Path = "Example/Child/Path/To/Transform2" } 
                };
            }
            foreach(var child in additionalChildren)
            {
                additionalChildrenArray.SerializeAdditionalChild(child);
            }

            newJson["additionalChildren"] = additionalChildrenArray;

            //grab all keyassets
            if (allDisplayedItems == null)
                GatherAllItems();

            //remove from list keyassets that we already have displays for
            List<Object> missingKeyAssets = new List<Object>(allDisplayedItems);
            string firstCompatibleChild = "";
            foreach (ItemDisplayRuleSet.KeyAssetRuleGroup ruleGroup in ruleSet)
            {
                keyAssetArray.SerializeKARG(ruleGroup);

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
                if (!ItemDisplays.KeyAssetDisplayPrefabs.TryGetValue(keyAsset, out var prefabList))
                {
                    continue;
                }

                ItemDisplayRuleSet.KeyAssetRuleGroup karg = new ItemDisplayRuleSet.KeyAssetRuleGroup()
                {
                    keyAsset = keyAsset,
                    displayRuleGroup = new DisplayRuleGroup()
                    {
                    }
                };

                ItemDisplayRule[] rules = new ItemDisplayRule[prefabList.Count];
                for(int i = 0; i < prefabList.Count; i++)
                {
                    rules[i] = new ItemDisplayRule()
                    {
                        childName = firstCompatibleChild,
                        followerPrefab = ItemDisplays.LoadDisplay(prefabList[i]),
                        localAngles = Vector3.zero,
                        localPos = Vector3.zero,
                        localScale = Vector3.one,
                        ruleType = ItemDisplayRuleType.ParentedPrefab
                    };
                }

                karg.displayRuleGroup.rules = rules;

                keyAssetArray.SerializeKARG(karg);
            }

            newJson["keyAssetRules"] = keyAssetArray;

            return newJson.ToString()
                .Replace("\"additionalChildren\":[", "\"additionalChildren\":\n[\n")
                .Replace("],\"keyAssetRules\":[", "\n],\n\"keyAssetRules\":[\n")
                .Replace("\"\",\"", "\"\",\n\"")
                .Replace("]]]],", "]\n]]],\n")
                .Replace("]]]]]", "]\n]]]\n]")
                .Replace("]],[", "]\n],\n[")
                .Replace("Transform\"],[\"ExampleChildName2", "Transform\"],\n[\"ExampleChildName2")
                //.Replace(",", ",\n")
                //.Replace("{", "{\n")
                //.Replace("}", "\n}")
                //.Replace("[", "[\n")
                //.Replace("]", "\n]")
                ;
        }
    }
}