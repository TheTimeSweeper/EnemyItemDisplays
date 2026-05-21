using BepInEx;
using BepInEx.Configuration;
using RoR2;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[assembly: HG.Reflection.SearchableAttribute.OptInAttribute]

namespace EnemyItemDisplays
{
    [BepInPlugin("com.TheTimeSweeper.SillyEnemyItemDisplays", "SillyEnemyItemDisplays", "0.2.0")]
    public class EnemyItemDisplaysPlugin : BaseUnityPlugin
    {
        public static Dictionary<string, string> IDRSFiles = new Dictionary<string, string>();

        public static ConfigEntry<bool> PrintUnused;

        void Awake()
        {
            Log.Init(Logger);

            PrintUnused = Config.Bind<bool>("Item Displays", "Print Unused Item Displays", false, "Prints unused item displays for bodies that have at least some IDRS.");

            var rulesPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(base.Info.Location), "Rules");
            var allFiles = Directory.GetFiles(rulesPath, "*.json", SearchOption.AllDirectories);

            foreach (var file in allFiles)
            {
                IDRSFiles.Add(System.IO.Path.GetFileNameWithoutExtension(file), file);
            }
        }

        [SystemInitializer(new Type[] { typeof(BodyCatalog), typeof(ItemCatalog) })]
        private static void Init()
        {
            ItemDisplays.PopulateDisplays();

            foreach (var body in BodyCatalog.allBodyPrefabs)
            {
                if (!IDRSFiles.TryGetValue(body.name, out var filePath))
                {
                    continue;
                }

                var jsonNode = SimpleJSON.JSON.Parse(File.ReadAllText(filePath));

                var modelLocator = body.GetComponent<ModelLocator>();
                if (!modelLocator) continue;

                if (!modelLocator.modelTransform) continue;

                var characterModel = modelLocator.modelTransform.GetComponent<CharacterModel>();
                if (!characterModel) continue;

                var bodyIDRS = characterModel.itemDisplayRuleSet;
                if (!bodyIDRS) continue;

                var childLocator = characterModel.GetComponent<ChildLocator>();
                if (!childLocator) continue;

                var additionalChildren = jsonNode["additionalChildren"].AsArray.DeserializeAdditionalChildren();
                foreach (var child in additionalChildren)
                {
                    Transform newTransform = characterModel.transform.Find(child.Path);
                    if (!newTransform)
                    {
                        Log.Warning($"Error adding ChildLocator entry: Couldn't find transform for {child.Path} on body {body}.");
                        continue;
                    }

                    HG.ArrayUtils.ArrayAppend(ref childLocator.transformPairs, new ChildLocator.NameTransformPair
                    {
                        name = child.Name,
                        transform = newTransform
                    });
                }

                foreach (JSONNode item in jsonNode["keyAssetRules"].AsArray)
                {
                    var karg = item.AsArray.DeserializeKARG();
                    if (bodyIDRS.keyAssetRuleGroups.Where(keyAsset => keyAsset.keyAsset == karg.keyAsset).Any())
                    {
                        Log.Info($"Skipping IDR for object {karg.keyAsset} ({karg.keyAssetAddress}) for body {body.name} as body's IDRS already has an entry for it.");
                        continue;
                    }
                    HG.ArrayUtils.ArrayAppend(ref bodyIDRS.keyAssetRuleGroups, karg);
                    BookKeep.TotalAddedDisplays++;
                }
                if (PrintUnused.Value)
                {
                    ItemDisplayCheck.PrintUnused(bodyIDRS.keyAssetRuleGroups, body.name);
                }
                BookKeep.TotalPotentialDisplays += BookKeep.TotalVanillaItems;
                BookKeep.MonstersAdded++;
            }
            BookKeep.Print();
        }
    }
    // this is for KEB's IDRSHelper
    //[
    // {itemName},
    //  [
    //   [
    //    {objectName},
    //    "",
    //    {childName},
    //    [{r:localPos.x},{r:localPos.y},{r:localPos.z}],
    //    [{r:localAngles.x},{r:localAngles.y},{r:localAngles.z}],
    //    [{r:localScale.x},{r:localScale.y},{r:localScale.z}],
    //   ]
    //  ]
    //],
}

