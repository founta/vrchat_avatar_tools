using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.Core;

public class AccessoryAnimMaker : EditorWindow
{

    public GameObject acc, model;
    public Object anim_folder;

    public bool makeOnClip = true;
    public bool makeOffClip = false;

    [MenuItem("Tools/founta/Accessory Animation Maker")]
    public static void ShowWindow() => GetWindow<AccessoryAnimMaker>("Accessory animation clip maker").Show(true);

    void createClip(string heir_path, string property_name, float value, string asset_path)
    {
        AnimationClip clip = new AnimationClip();

        var curve = new AnimationCurve();
        curve.AddKey(0, value);

        clip.SetCurve(heir_path, typeof(GameObject), property_name, curve);

        AssetDatabase.CreateAsset(clip, asset_path + ".anim");
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        acc = EditorGUILayout.ObjectField("Accessory object", acc, typeof(GameObject), true) as GameObject;
        model = EditorGUILayout.ObjectField("Your avatar", model, typeof(GameObject), true) as GameObject;
        anim_folder = EditorGUILayout.ObjectField("Your animation asset folder", anim_folder, typeof(Object), true) as Object;

        makeOnClip = EditorGUILayout.Toggle("Create on animation", makeOnClip);
        makeOffClip = EditorGUILayout.Toggle("Create off animation", makeOffClip);
        EditorGUILayout.EndVertical();

        if (GUILayout.Button("Create!"))
        {
            string obj_path = VRC.Core.ExtensionMethods.GetHierarchyPath(acc.transform, model.transform);
            string enable_str = "m_IsActive";
            string asset_folder_path = AssetDatabase.GetAssetPath(anim_folder);
            //string asset_folder_path = VRC.Core.ExtensionMethods.GetHierarchyPath(anim_folder_asset.name);

            if (makeOffClip)
            {
                createClip(obj_path, enable_str, 0, asset_folder_path + "/" + acc.name + "_off");
                Debug.Log("Created off accessory clip");
            }
            if (makeOnClip)
            {
                createClip(obj_path, enable_str, 1, asset_folder_path + "/" + acc.name + "_on");
                Debug.Log("Created on accessory clip");
            }
        }
    }
};