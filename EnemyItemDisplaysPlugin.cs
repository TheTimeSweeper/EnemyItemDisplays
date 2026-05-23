using BepInEx;
using BepInEx.Configuration;
using Newtonsoft.Json.Utilities;
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
    [BepInPlugin("com.TheTimeSweeper.EnemyItemDisplays", "EnemyItemDisplays", "1.1.0")]
    public class EnemyItemDisplaysPlugin : BaseUnityPlugin
    {
        public static Dictionary<string, string> IDRSFiles = new Dictionary<string, string>();

        public static ConfigEntry<bool> PrintUnused;
        public static ConfigEntry<bool> AllowStubs;

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
                "Export Unused Item Displays",
                false,
                "Exports ALL unused item displays into separate folder.\nIt exports every rule that body has and then adds the ones that are missing with dummy values. Also adds Child Locator entries for any bones not already in child locator.\nExport happens only if the body has IDRS, export happens for each body and some bodies share IDRS, so be mindful of that.");
            
            
            AllowStubs = Config.Bind<bool>(
                "Item Displays",
                "Allow Unused Child Locator Entires",
                false,
                "By default, the mod will skip IDRS with default values and additionalChildren entries in the .json files that do not have any IDRS entries. Enable this so that they are no longer skipped, and thus can be used for new sets.");
            

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

                List<string> usedNames = new List<string>();
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

                        AddToUsedNames(usedNames, karg);

                        HG.ArrayUtils.ArrayAppend(ref bodyIDRS.keyAssetRuleGroups, karg);
                        BookKeep.TotalAddedDisplays++;
                    }

                    additionalChildren = jsonNode["additionalChildren"].AsArray.DeserializeAdditionalChildren();
                    foreach (var child in additionalChildren)
                    {
                        //skip adding additional entries to child locator if none of the entries above used them. 
                        if (!AllowStubs.Value && !usedNames.Contains(child.Name))
                        {
                            continue;
                        }

                        if (child.Name == "ExampeChildName" || child.Name == "ExampleChildName2")
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

                    additionalChildren = GenerateAdditionalChildren(characterModel, additionalChildren);

                    var result = ItemDisplayCheck.PrintUnused(bodyIDRS.keyAssetRuleGroups, additionalChildren, body.name);
                    File.WriteAllText(System.IO.Path.Combine(dirInfo.FullName, body.name + ".json"), result);
                }
                BookKeep.TotalPotentialDisplays += BookKeep.TotalVanillaItems;
                BookKeep.MonstersAdded++;
            }
            BookKeep.Print();
        }

        private static void AddToUsedNames(List<string> usedNames, ItemDisplayRuleSet.KeyAssetRuleGroup karg)
        {
            if(karg.displayRuleGroup.rules == null)
            {
                return;
            }
            for (int i = 0; i < karg.displayRuleGroup.rules.Length; i++)
            {
                if (!usedNames.Contains(karg.displayRuleGroup.rules[i].childName))
                {
                    usedNames.Add(karg.displayRuleGroup.rules[i].childName);
                }
            }
        }

        private static AdditionalChild[] GenerateAdditionalChildren(CharacterModel characterModel, AdditionalChild[] existingArray = null)
        {
            List<AdditionalChild> newAdditionalChildren;
            if (existingArray == null)
            {
                newAdditionalChildren = new List<AdditionalChild>();
            }
            else
            {
                newAdditionalChildren = existingArray.ToList();
            }
            var childLocator = characterModel.transform.GetComponent<ChildLocator>();

            Transform parent = characterModel.transform;

            var ALL = parent.GetComponentsInChildren<Transform>();
            for (int i = 0; i < ALL.Length; i++)
            {
                string name = ALL[i].name;
                if (name.Contains("IK"))
                    continue;

                    //skip if already exsists in childlocator
                if (childLocator)
                {
                    bool same = false;
                    for (int j = 0; j < childLocator.transformPairs.Length; j++)
                    {
                        if (childLocator.transformPairs[j].transform == ALL[i])
                        {
                            same = true;
                            continue;
                        }
                    }
                    if (same)
                    {
                        continue;
                    }
                }

                newAdditionalChildren.Add(new AdditionalChild
                {
                    Name = name,
                    Path = Util.BuildPrefabTransformPath(parent, ALL[i])
                });
            }
            return newAdditionalChildren.ToArray();
        }
    }
}
/* this is for KEB's IDRSHelper
          {childName},
          [{r:localPos.x},{r:localPos.y},{r:localPos.z}],
          [{r:localAngles.x},{r:localAngles.y},{r:localAngles.z}],
          [{r:localScale.x},{r:localScale.y},{r:localScale.z}],
*/

