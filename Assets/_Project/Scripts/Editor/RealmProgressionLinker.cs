using System.Collections.Generic;
using System.Linq;
using CultivationGame.Data;
using UnityEditor;
using UnityEngine;

namespace CultivationGame.Editor
{
    public static class RealmProgressionLinker
    {
        [MenuItem("Tools/Cultivation/Link Realm Progression")]
        public static void LinkRealmProgression()
        {
            var guids = AssetDatabase.FindAssets("t:RealmDefinition");
            if (guids == null || guids.Length == 0)
            {
                Debug.LogWarning("Realm linker: No RealmDefinition assets found.");
                return;
            }

            var realms = new List<RealmDefinition>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var realm = AssetDatabase.LoadAssetAtPath<RealmDefinition>(path);
                if (realm != null)
                    realms.Add(realm);
            }

            if (realms.Count == 0)
            {
                Debug.LogWarning("Realm linker: RealmDefinition assets could not be loaded.");
                return;
            }

            // Log duplicated indices so incorrect data is visible before linking.
            var duplicateGroups = realms
                .GroupBy(r => r.realmIndex)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in duplicateGroups)
            {
                string names = string.Join(", ", group.Select(r => r.name));
                Debug.LogWarning($"Realm linker: Duplicate realmIndex {group.Key}: {names}");
            }

            realms.Sort((a, b) => a.realmIndex.CompareTo(b.realmIndex));

            int changed = 0;
            for (int i = 0; i < realms.Count; i++)
            {
                RealmDefinition current = realms[i];
                RealmDefinition next = i < realms.Count - 1 ? realms[i + 1] : null;

                if (current.nextRealm == next)
                    continue;

                Undo.RecordObject(current, "Link Realm Progression");
                current.nextRealm = next;
                EditorUtility.SetDirty(current);
                changed++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int gaps = CountIndexGaps(realms);
            Debug.Log($"Realm linker: linked {realms.Count} realms, updated {changed} assets, detected {duplicateGroups.Count} duplicate index groups, detected {gaps} index gaps.");
        }

        [MenuItem("Tools/Cultivation/Validate Realm Progression")]
        public static void ValidateRealmProgression()
        {
            var guids = AssetDatabase.FindAssets("t:RealmDefinition");
            if (guids == null || guids.Length == 0)
            {
                Debug.LogWarning("Realm validator: No RealmDefinition assets found.");
                return;
            }

            var realms = new List<RealmDefinition>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var realm = AssetDatabase.LoadAssetAtPath<RealmDefinition>(path);
                if (realm != null)
                    realms.Add(realm);
            }

            realms.Sort((a, b) => a.realmIndex.CompareTo(b.realmIndex));

            int brokenLinks = 0;
            for (int i = 0; i < realms.Count; i++)
            {
                RealmDefinition expected = i < realms.Count - 1 ? realms[i + 1] : null;
                if (realms[i].nextRealm != expected)
                {
                    brokenLinks++;
                    string expectedName = expected != null ? expected.realmName : "<none>";
                    string actualName = realms[i].nextRealm != null ? realms[i].nextRealm.realmName : "<none>";
                    Debug.LogWarning($"Realm validator: {realms[i].realmName} (index {realms[i].realmIndex}) links to {actualName}, expected {expectedName}.");
                }
            }

            var duplicateGroups = realms
                .GroupBy(r => r.realmIndex)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in duplicateGroups)
            {
                string names = string.Join(", ", group.Select(r => r.name));
                Debug.LogWarning($"Realm validator: Duplicate realmIndex {group.Key}: {names}");
            }

            int gaps = CountIndexGaps(realms);
            Debug.Log($"Realm validator: checked {realms.Count} realms, found {brokenLinks} broken links, {duplicateGroups.Count} duplicate index groups, {gaps} index gaps.");
        }

        private static int CountIndexGaps(List<RealmDefinition> realms)
        {
            int gaps = 0;
            for (int i = 0; i < realms.Count - 1; i++)
            {
                int expectedNext = realms[i].realmIndex + 1;
                if (realms[i + 1].realmIndex != expectedNext)
                    gaps++;
            }

            return gaps;
        }
    }
}