using RoR2;
using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;
using static RoR2.ItemDisplayRuleSet;

namespace EnemyItemDisplays
{
    public static class SimpleJsonExtensions
    {
        public struct AdditionalChild
        {
            public AdditionalChild(string name, string path)
            {
                Name = name;
                Path = path;
            }

            public string Name;
            public string Path;
        }

        public static AdditionalChild[] DeserializeAdditionalChildren(this JSONArray node)
        {
            List<AdditionalChild> list = new List<AdditionalChild>();

            foreach (JSONArray child in node)
            {
                list.Add(new AdditionalChild(child[0], child[1]));
            }

            return list.ToArray();
        }

        public static KeyAssetRuleGroup DeserializeKARG(this JSONArray node)
        {
            var keyAssetRuleGroup = new KeyAssetRuleGroup();
            string keyAssetName = node[0];

            UnityEngine.Object keyAsset = null;

            var itemIndex = ItemCatalog.FindItemIndex(keyAssetName);
            if (itemIndex != ItemIndex.None)
            {
                keyAsset = ItemCatalog.GetItemDef(itemIndex);
            }

            var equipmentIndex = EquipmentCatalog.FindEquipmentIndex(keyAssetName);
            if (equipmentIndex != EquipmentIndex.None)
            {
                keyAsset = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
            }

            if (!keyAsset)
            {
                Log.Info($"Couldn't create Item Display Rule for {keyAssetName}, wasn't able to find EquipmentDef\\ItemDef");
                return default;
            }

            keyAssetRuleGroup.keyAsset = keyAsset;

            DisplayRuleGroup displayRuleGroup = new DisplayRuleGroup();

            JSONArray rules = node[1].AsArray;
            foreach (JSONArray rule in rules)
            {
                var idr = new ItemDisplayRule();
                idr.ruleType = (ItemDisplayRuleType)rule[0].AsInt;
                idr.followerPrefabAddress = new UnityEngine.AddressableAssets.AssetReferenceGameObject(rule[1]);
                idr.followerPrefab = ItemDisplays.LoadDisplay(rule[0]);
                idr.childName = rule[2];
                idr.localPos = rule[3].AsArray.ReadVector3();
                idr.localAngles = rule[4].AsArray.ReadVector3();
                idr.localScale = rule[5].AsArray.ReadVector3();
                displayRuleGroup.AddDisplayRule(idr);
            }

            keyAssetRuleGroup.displayRuleGroup = displayRuleGroup;

            return keyAssetRuleGroup;
        }

        public static void SerializeKARG(this JSONNode node, KeyAssetRuleGroup ruleGroup)
        {
            node.Add(ruleGroup.keyAsset.name);
            JSONArray rules = new JSONArray();
            foreach (var rule in ruleGroup.displayRuleGroup.rules)
            {
                JSONArray ruleJson = new JSONArray
                {
                    rule.followerPrefab != null ? rule.followerPrefab.name : "",
                    rule.followerPrefabAddress != null && rule.followerPrefabAddress.IsValid() ? rule.followerPrefabAddress.AssetGUID : "",
                    rule.childName,
                    new JSONArray().WriteVector3(rule.localPos),
                    new JSONArray().WriteVector3(rule.localAngles),
                    new JSONArray().WriteVector3(rule.localScale),
                };
                rules.Add(ruleJson);
            }
            node.Add(rules);
        }

        // Vector2 and Vector3 conversions are taken from official repo:
        // https://github.com/Bunny83/SimpleJSON/blob/master/SimpleJSONUnity.cs

        #region Vector2
        public static Vector2 ReadVector2(this JSONNode node, Vector2 aDefault)
        {
            if (node.IsObject)
                return new Vector2(node["x"].AsFloat, node["y"].AsFloat);
            if (node.IsArray)
                return new Vector2(node[0].AsFloat, node[1].AsFloat);
            return aDefault;
        }
        public static Vector2 ReadVector2(this JSONNode node, string aXName, string aYName)
        {
            if (node.IsObject)
            {
                return new Vector2(node[aXName].AsFloat, node[aYName].AsFloat);
            }
            return Vector2.zero;
        }

        public static Vector2 ReadVector2(this JSONNode node)
        {
            return node.ReadVector2(Vector2.zero);
        }
        public static JSONNode WriteVector2(this JSONNode node, Vector2 aVec, string aXName = "x", string aYName = "y")
        {
            if (node.IsObject)
            {
                node[aXName].AsFloat = aVec.x;
                node[aYName].AsFloat = aVec.y;
            }
            else if (node.IsArray)
            {
                (node as JSONArray).inline = true;
                node[0].AsFloat = aVec.x;
                node[1].AsFloat = aVec.y;
            }
            return node;
        }
        #endregion Vector2

        #region Vector3
        public static Vector3 ReadVector3(this JSONNode node, Vector3 aDefault)
        {
            if (node.IsObject)
                return new Vector3(node["x"].AsFloat, node["y"].AsFloat, node["z"].AsFloat);
            if (node.IsArray)
                return new Vector3(node[0].AsFloat, node[1].AsFloat, node[2].AsFloat);
            return aDefault;
        }
        public static Vector3 ReadVector3(this JSONNode node, string aXName, string aYName, string aZName)
        {
            if (node.IsObject)
                return new Vector3(node[aXName].AsFloat, node[aYName].AsFloat, node[aZName].AsFloat);
            return Vector3.zero;
        }
        public static Vector3 ReadVector3(this JSONNode node)
        {
            return node.ReadVector3(Vector3.zero);
        }
        public static JSONNode WriteVector3(this JSONNode node, Vector3 aVec, string aXName = "x", string aYName = "y", string aZName = "z")
        {
            if (node.IsObject)
            {
                node[aXName].AsFloat = aVec.x;
                node[aYName].AsFloat = aVec.y;
                node[aZName].AsFloat = aVec.z;
            }
            else if (node.IsArray)
            {
                (node as JSONArray).inline = true;
                node[0].AsFloat = aVec.x;
                node[1].AsFloat = aVec.y;
                node[2].AsFloat = aVec.z;
            }
            return node;
        }
        #endregion Vector3
    }
}
