using RoR2.ContentManagement;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace EnemyItemDisplays
{
    public class BookKeep
    {
        public static int TotalAddedDisplays = 0;
        public static int TotalPotentialDisplays = 0;
        public static int MonstersAdded = 0;

        private static int? totalVanillaItems = null;
        public static int TotalVanillaItems
        {
            get
            {
                return ItemDisplays.KeyAssetDisplayPrefabs.Keys.Count;
            }
        }

        private static int? totalEnemies = null;
        public static int TotalEnemies
        {
            get
            {
                if (totalEnemies == null)
                {
                    totalEnemies = 0;

                    for (int i = 0; i < ContentManager.allLoadedContentPacks.Length; i++)
                    {
                        ReadOnlyContentPack contentPack = ContentManager.allLoadedContentPacks[i];
                        if (contentPack.identifier == "RoR2.BaseContent" || contentPack.identifier == "RoR2.DLC1")
                        {
                            totalEnemies += contentPack.bodyPrefabs.Length - contentPack.survivorDefs.Length;// + contentPack.equipmentDefs.Length;
                            foreach (var body in contentPack.bodyPrefabs)
                            {
                                Log.Message(body.name);
                            }
                        }
                    }
                }

                return totalVanillaItems.Value;
            }
        }

        public static void Print()
        {
            Log.Message($"Added iteme displays to {MonstersAdded} enemies. Total enemy item displays: {TotalAddedDisplays}/{TotalPotentialDisplays}");
        }
    }
}