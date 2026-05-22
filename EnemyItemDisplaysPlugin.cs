using BepInEx;
using BepInEx.Configuration;
using RoR2;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static EnemyItemDisplays.SimpleJsonExtensions;

[assembly: HG.Reflection.SearchableAttribute.OptInAttribute]

namespace EnemyItemDisplays
{
    [BepInPlugin("com.TheTimeSweeper.SillyEnemyItemDisplays", "SillyEnemyItemDisplays", "0.2.0")]
    public class EnemyItemDisplaysPlugin : BaseUnityPlugin
    {
        public static Dictionary<string, string> IDRSFiles = new Dictionary<string, string>();

        public static ConfigEntry<bool> PrintUnused;

        public static PluginInfo PluginInfo;

        public const string RULES_FOLDER = "Rules";

        public const string EXPORT_FOLDER = "Export";

        void fuck() {
            Awake();
            Init();
        }

        void Awake()
        {
            Log.Init(Logger);

            PluginInfo = base.Info;

            PrintUnused = Config.Bind<bool>(
                "Item Displays", 
                "Export unused item displays",
                false, 
                "Exports ALL unused item displays into separate folder. It exports every rule that body has and then adds the ones that are missing with dummy values. Export happens only if the body has IDRS, export happens for each body and some bodies share IDRS, so be mindful of that.");

            var rulesPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(PluginInfo.Location), RULES_FOLDER);
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
                var modelLocator = body.GetComponent<ModelLocator>();
                if (!modelLocator) continue;

                if (!modelLocator.modelTransform) continue;

                var characterModel = modelLocator.modelTransform.GetComponent<CharacterModel>();
                if (!characterModel) continue;

                var bodyIDRS = characterModel.itemDisplayRuleSet;
                if (!bodyIDRS) continue;

                AdditionalChild[] additionalChildren = Array.Empty<AdditionalChild>();

                if (IDRSFiles.TryGetValue(body.name, out var filePath))
                {
                    if (!System.IO.File.Exists(filePath))
                    {
                        continue;
                    }
                    Log.Debug($"Getting all text for {body.name} at {filePath}");
                    var jsonNode = SimpleJSON.JSON.Parse(File.ReadAllText(filePath));

                    var childLocator = characterModel.GetComponent<ChildLocator>();
                    if (!childLocator) continue;

                    additionalChildren = jsonNode["additionalChildren"].AsArray.DeserializeAdditionalChildren();
                    foreach (var child in additionalChildren)
                    {
                        if (child.Name == "ExampeChildName")
                        {
                            continue;
                        }

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
                        if(karg.Equals(default))
                        {
                            continue;
                        }
                        if (bodyIDRS.keyAssetRuleGroups.Where(keyAsset => keyAsset.keyAsset == karg.keyAsset).Any())
                        {
                            Log.Info($"Skipping IDR for object {karg.keyAsset} ({karg.keyAssetAddress}) for body {body.name} as body's IDRS already has an entry for it.");
                            continue;
                        }
                        HG.ArrayUtils.ArrayAppend(ref bodyIDRS.keyAssetRuleGroups, karg);
                        BookKeep.TotalAddedDisplays++;
                    }
                }

                if (PrintUnused.Value)
                {
                    var dirInfo = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(PluginInfo.Location), EXPORT_FOLDER));
                    foreach(var contentPack in RoR2.ContentManagement.ContentManager.allLoadedContentPacks)
                    {
                        if (contentPack.bodyPrefabs.Contains(body))
                        {
                            dirInfo = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(dirInfo.FullName, string.Join("_", contentPack.identifier.Split(System.IO.Path.GetInvalidFileNameChars()))));
                            break;
                        }
                    }

                    var result = ItemDisplayCheck.PrintUnused(bodyIDRS.keyAssetRuleGroups, additionalChildren, body.name);
                    File.WriteAllText(System.IO.Path.Combine(dirInfo.FullName, body.name + ".json"), result);
                }
                BookKeep.TotalPotentialDisplays += BookKeep.TotalVanillaItems;
                BookKeep.MonstersAdded++;
            }
            BookKeep.Print();
        }
    }
}
/* this is for KEB's IDRSHelper
          {childName},
          [{r:localPos.x},{r:localPos.y},{r:localPos.z}],
          [{r:localAngles.x},{r:localAngles.y},{r:localAngles.z}],
          [{r:localScale.x},{r:localScale.y},{r:localScale.z}],
*/

