using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GameUpSDK.Ads;

namespace GameUpSDK.Editor.Setup
{
    public enum AdMobIdEditorPlatform
    {
        Android,
        IOS
    }

    public class AdUnitConfigData
    {
        public bool useMultiIds;
        public string defaultIdAndroid;
        public string defaultIdIOS;

        // Thêm 2 field
        public BannerSize defaultBannerSize;
        public BannerFormatType defaultBannerFormat;
        public CollapsibleBannerPlacement defaultCollapsible;

        public List<AdUnitIdEntry> multiIdsAndroid = new List<AdUnitIdEntry>();
        public List<AdUnitIdEntry> multiIdsIOS = new List<AdUnitIdEntry>();

        public void Load(SerializedProperty configProp)
        {
            if (configProp == null) return;
            useMultiIds = configProp.FindPropertyRelative("useMultiAdUnitIds")?.boolValue ?? false;
            defaultIdAndroid = configProp.FindPropertyRelative("defaultIdAndroid")?.stringValue ?? "";
            defaultIdIOS = configProp.FindPropertyRelative("defaultIdIOS")?.stringValue ?? "";
            
            var colProp = configProp.FindPropertyRelative("defaultCollapsible");
            if (colProp != null) defaultCollapsible = (CollapsibleBannerPlacement)colProp.intValue;
            
            var bsProp = configProp.FindPropertyRelative("defaultBannerSize");
            if (bsProp != null) defaultBannerSize = (BannerSize)bsProp.intValue;
            
            var colBannerFormat = configProp.FindPropertyRelative("defaultBannerFormat");
            if (colBannerFormat != null) defaultBannerFormat = (BannerFormatType)colBannerFormat.intValue;

            SetupTabBase.AssignAdUnitIdListDirect(configProp.FindPropertyRelative("multiIdsAndroid"), multiIdsAndroid);
            SetupTabBase.AssignAdUnitIdListDirect(configProp.FindPropertyRelative("multiIdsIOS"), multiIdsIOS);
        }

        public void Save(SerializedProperty configProp)
        {
            if (configProp == null) return;
            if (configProp.FindPropertyRelative("useMultiAdUnitIds") != null)
                configProp.FindPropertyRelative("useMultiAdUnitIds").boolValue = useMultiIds;
            if (configProp.FindPropertyRelative("defaultIdAndroid") != null)
                configProp.FindPropertyRelative("defaultIdAndroid").stringValue = defaultIdAndroid;
            if (configProp.FindPropertyRelative("defaultIdIOS") != null)
                configProp.FindPropertyRelative("defaultIdIOS").stringValue = defaultIdIOS;

            // SỬA: Đổi .enumValueIndex thành .intValue
            
            var colBannerFormat = configProp.FindPropertyRelative("defaultBannerFormat");
            if (colBannerFormat != null) colBannerFormat.intValue = (int)defaultBannerFormat;
            
            var bsProp = configProp.FindPropertyRelative("defaultBannerSize");
            if (bsProp != null) bsProp.intValue = (int)defaultBannerSize;

            var colProp = configProp.FindPropertyRelative("defaultCollapsible");
            if (colProp != null) colProp.intValue = (int)defaultCollapsible;

            SetupTabBase.SetAdUnitIdListDirect(configProp.FindPropertyRelative("multiIdsAndroid"), multiIdsAndroid);
            SetupTabBase.SetAdUnitIdListDirect(configProp.FindPropertyRelative("multiIdsIOS"), multiIdsIOS);
        }
    }

    public abstract class SetupTabBase
    {
        public abstract string Title { get; }
        public virtual bool IsVisible => true;

        public abstract void Load();
        public abstract void Draw();

        // HÀM DUY NHẤT ĐỂ LƯU VÀO PREFAB TƯƠNG ỨNG
        public abstract void Save();

        // --- UTILITIES LÀM VIỆC VỚI SERIALIZED OBJECT ---
        protected void Assign(SerializedObject so, string prop, ref string target)
        {
            var p = so.FindProperty(prop);
            if (p != null) target = p.stringValue ?? "";
        }

        protected void AssignInt(SerializedObject so, string prop, ref int target)
        {
            var p = so.FindProperty(prop);
            if (p != null) target = p.intValue;
        }

        protected void AssignBool(SerializedObject so, string prop, ref bool target)
        {
            var p = so.FindProperty(prop);
            if (p != null) target = p.boolValue;
        }

        protected void AssignFloat(SerializedObject so, string prop, ref float target)
        {
            var p = so.FindProperty(prop);
            if (p != null) target = p.floatValue;
        }

        protected void Set(SerializedObject so, string propName, string value)
        {
            var p = so.FindProperty(propName);
            if (p != null) p.stringValue = value ?? "";
        }

        protected void SetInt(SerializedObject so, string propName, int value)
        {
            var p = so.FindProperty(propName);
            if (p != null) p.intValue = value;
        }

        protected void SetBool(SerializedObject so, string propName, bool value)
        {
            var p = so.FindProperty(propName);
            if (p != null) p.boolValue = value;
        }

        protected void SetFloat(SerializedObject so, string propName, float value)
        {
            var p = so.FindProperty(propName);
            if (p != null) p.floatValue = value;
        }

        public static void AssignStringList(SerializedProperty listProp, List<string> target)
        {
            if (target == null || listProp == null || !listProp.isArray) return;
            target.Clear();
            for (int i = 0; i < listProp.arraySize; i++) target.Add(listProp.GetArrayElementAtIndex(i).stringValue);
        }

        public static void SetStringList(SerializedProperty listProp, List<string> source)
        {
            if (listProp == null || !listProp.isArray) return;
            source ??= new List<string>();
            listProp.arraySize = source.Count;
            for (int i = 0; i < source.Count; i++) listProp.GetArrayElementAtIndex(i).stringValue = source[i];
        }

        // --- UTILITIES CHO AD UNIT CONFIG ---
        public static void AssignAdUnitIdListDirect(SerializedProperty listProp, List<AdUnitIdEntry> target)
        {
            if (target == null || listProp == null || !listProp.isArray) return;
            target.Clear();
            for (int i = 0; i < listProp.arraySize; i++)
            {
                var el = listProp.GetArrayElementAtIndex(i);
                target.Add(new AdUnitIdEntry
                {
                    // SỬA CÁC ĐUÔI ENUM THÀNH .intValue
                    AdType = (AdUnitType)(el.FindPropertyRelative("AdType")?.intValue ?? 0),
                    NameId = el.FindPropertyRelative("NameId")?.stringValue ?? "",
                    Id = el.FindPropertyRelative("Id")?.stringValue ?? "",
                    intId = el.FindPropertyRelative("intId")?.intValue ?? 0,

                    BannerSize = (BannerSize)(el.FindPropertyRelative("BannerSize")?.intValue ?? 0),
                    BannerFormat = (BannerFormatType)(el.FindPropertyRelative("BannerFormat")?.intValue ?? 0),
                    CollapsiblePlacement =
                        (CollapsibleBannerPlacement)(el.FindPropertyRelative("CollapsiblePlacement")?.intValue ?? 0)
                });
            }

            NormalizeIntIds(target);
        }

        public static void SetAdUnitIdListDirect(SerializedProperty listProp, List<AdUnitIdEntry> source)
        {
            if (listProp == null || !listProp.isArray) return;
            source ??= new List<AdUnitIdEntry>();
            NormalizeIntIds(source);
            listProp.arraySize = source.Count;
            for (int i = 0; i < source.Count; i++)
            {
                var el = listProp.GetArrayElementAtIndex(i);
                var e = source[i];

                // SỬA CÁC ĐUÔI ENUM THÀNH .intValue
                if (el.FindPropertyRelative("AdType") != null)
                    el.FindPropertyRelative("AdType").intValue = (int)e.AdType;
                if (el.FindPropertyRelative("NameId") != null)
                    el.FindPropertyRelative("NameId").stringValue = e.NameId ?? "";
                if (el.FindPropertyRelative("Id") != null) el.FindPropertyRelative("Id").stringValue = e.Id ?? "";
                if (el.FindPropertyRelative("intId") != null) el.FindPropertyRelative("intId").intValue = e.intId;

                if (el.FindPropertyRelative("BannerFormat") != null)
                    el.FindPropertyRelative("BannerFormat").intValue = (int)e.BannerFormat;
                if (el.FindPropertyRelative("BannerSize") != null)
                    el.FindPropertyRelative("BannerSize").intValue = (int)e.BannerSize;
                if (el.FindPropertyRelative("CollapsiblePlacement") != null)
                    el.FindPropertyRelative("CollapsiblePlacement").intValue = (int)e.CollapsiblePlacement;
            }
        }

        protected static void NormalizeIntIds(List<AdUnitIdEntry> list)
        {
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null)
                    list[i].intId = i + 1;
        }

        // --- UI DRAWERS ---
        protected void DrawStringListUI(string label, List<string> list)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                list[i] = EditorGUILayout.TextField(list[i]);
                if (GUILayout.Button("-", GUILayout.Width(24)))
                {
                    list.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ Add Device", GUILayout.Width(120))) list.Add("");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        // Đã thêm tham số defaultAdType
        protected void DrawConfigDataUI(string label, AdUnitConfigData configData, AdMobIdEditorPlatform platform,
            AdUnitType defaultAdType)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            configData.useMultiIds = EditorGUILayout.Toggle("Use Multi IDs", configData.useMultiIds);

            if (configData.useMultiIds)
            {
                var list = platform == AdMobIdEditorPlatform.Android
                    ? configData.multiIdsAndroid
                    : configData.multiIdsIOS;
                DrawAdUnitIdListUI(ref list, defaultAdType);
                if (platform == AdMobIdEditorPlatform.Android) configData.multiIdsAndroid = list;
                else configData.multiIdsIOS = list;
            }
            else
            {
                if (platform == AdMobIdEditorPlatform.Android)
                    configData.defaultIdAndroid =
                        EditorGUILayout.TextField("Android Default ID", configData.defaultIdAndroid);
                else configData.defaultIdIOS = EditorGUILayout.TextField("iOS Default ID", configData.defaultIdIOS);

                // Cấu hình Banner Mặc Định (Chỉ hiện ở Tab Banner)
                if (defaultAdType == AdUnitType.Banner)
                {
                    EditorGUILayout.Space();
                    configData.defaultBannerFormat =
                        (BannerFormatType)EditorGUILayout.EnumPopup("Banner Format", configData.defaultBannerFormat);
                    configData.defaultBannerSize =
                        (BannerSize)EditorGUILayout.EnumPopup("Banner Size", configData.defaultBannerSize);
                    configData.defaultCollapsible =
                        (CollapsibleBannerPlacement)EditorGUILayout.EnumPopup("Collapsible",
                            configData.defaultCollapsible);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawAdUnitIdListUI(ref List<AdUnitIdEntry> list, AdUnitType defaultAdType)
        {
            if (list == null) list = new List<AdUnitIdEntry>();
            NormalizeIntIds(list);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("#", EditorStyles.miniLabel, GUILayout.Width(28f));
            GUILayout.Label("Where (Placement)", EditorStyles.miniLabel, GUILayout.Width(200f));
            GUILayout.Label("Ad Unit ID", EditorStyles.miniLabel, GUILayout.MinWidth(160f));
            GUILayout.Label("", GUILayout.Width(24));
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i] ?? (list[i] = new AdUnitIdEntry { AdType = defaultAdType });
                e.AdType = defaultAdType;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(e.intId.ToString(), GUILayout.Width(28f));
                e.NameId = EditorGUILayout.TextField(e.NameId ?? "", GUILayout.Width(200f));
                e.Id = EditorGUILayout.TextField(e.Id ?? "", GUILayout.MinWidth(160f));
                if (GUILayout.Button("-", GUILayout.Width(24)))
                {
                    list.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();

                // UI Nâng cao: Hiện setting riêng của từng banner thụt vào bên trong
                if (e.AdType == AdUnitType.Banner)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(32f);
                    GUILayout.Label("↳ Format:", EditorStyles.miniLabel, GUILayout.Width(45));
                    e.BannerFormat = (BannerFormatType)EditorGUILayout.EnumPopup(e.BannerFormat, GUILayout.Width(100));
                    GUILayout.Label("Size:", EditorStyles.miniLabel, GUILayout.Width(45));
                    e.BannerSize = (BannerSize)EditorGUILayout.EnumPopup(e.BannerSize, GUILayout.Width(100));
                    GUILayout.Label("Collapsible:", EditorStyles.miniLabel, GUILayout.Width(70));
                    e.CollapsiblePlacement =
                        (CollapsibleBannerPlacement)EditorGUILayout.EnumPopup(e.CollapsiblePlacement,
                            GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(4);
                }
            }

            if (GUILayout.Button($"+ Add {defaultAdType} Placement"))
            {
                list.Add(new AdUnitIdEntry { AdType = defaultAdType });
            }

            EditorGUILayout.EndVertical();
        }

        protected void ModifyPrefab(string path, Action<GameObject> modifyAction)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                modifyAction(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                EditorUtility.SetDirty(root);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}