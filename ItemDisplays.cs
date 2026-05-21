using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace EnemyItemDisplays
{
    internal static class ItemDisplays
    {
        public static Dictionary<string, GameObject> itemDisplayPrefabs = new Dictionary<string, GameObject>();
        public static Dictionary<Object, List<string>> KeyAssetDisplayPrefabs = new Dictionary<Object, List<string>>();

        public static void PopulateDisplays()
        {
            PopulateDisplaysFromBody("MercBody");
        }

        private static void PopulateDisplaysFromBody(string bodyName)
        {
            ItemDisplayRuleSet itemDisplayRuleSet = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterBodies/" + bodyName)?.GetComponent<ModelLocator>()?.modelTransform.GetComponent<CharacterModel>()?.itemDisplayRuleSet;
            if (itemDisplayRuleSet == null)
            {
                Log.Error("couldn't load ItemDisplayRuleSet from " + bodyName + ". Check if name was entered correctly");
                return;
            }

            ItemDisplayRuleSet.KeyAssetRuleGroup[] itemRuleGroups = itemDisplayRuleSet.keyAssetRuleGroups;

            for (int i = 0; i < itemRuleGroups.Length; i++)
            {
                ItemDisplayRule[] rules = itemRuleGroups[i].displayRuleGroup.rules;

                bool alreadySeenThisKey = KeyAssetDisplayPrefabs.ContainsKey(itemRuleGroups[i].keyAsset);

                for (int j = 0; j < rules.Length; j++)
                {
                    GameObject followerPrefab = rules[j].followerPrefab;
                    if (followerPrefab)
                    {
                        string key = followerPrefab.name?.ToLowerInvariant();
                        if (!itemDisplayPrefabs.ContainsKey(key))
                        {
                            itemDisplayPrefabs[key] = followerPrefab;
                        }

                        if (!alreadySeenThisKey)
                        {
                            if (!KeyAssetDisplayPrefabs.ContainsKey(itemRuleGroups[i].keyAsset))
                            {
                                KeyAssetDisplayPrefabs[itemRuleGroups[i].keyAsset] = new List<string>();
                            }
                            KeyAssetDisplayPrefabs[itemRuleGroups[i].keyAsset].Add(followerPrefab.name);
                        }
                    }
                }
            }
        }

        public static GameObject LoadDisplay(string name)
        {
            if (itemDisplayPrefabs.ContainsKey(name.ToLowerInvariant()))
            {
                if (itemDisplayPrefabs[name.ToLowerInvariant()])
                {

                    GameObject display = itemDisplayPrefabs[name.ToLowerInvariant()];

                    return display;
                }
            }
            Log.Error("item display " + name + " returned null");
            return null;
        }

        #region add rule helpers


        #endregion add rule helpers
    }
}