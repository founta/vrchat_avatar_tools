using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class ArmatureNameChanger : EditorWindow {

    public UnityEngine.GameObject rootArmature;
    public string armPrefix;
    public bool replacePeriod = false;

    [MenuItem("Tools/founta/ArmatureNameChanger")]
    public static void ShowWindow() => GetWindow<ArmatureNameChanger>("Armature Name Changer").Show(true);

    void OnGUI() {
        EditorGUILayout.BeginVertical();
        rootArmature = EditorGUILayout.ObjectField("Root Armature", rootArmature, typeof(UnityEngine.GameObject), true) as GameObject;
        armPrefix = EditorGUILayout.TextField("Prefix", armPrefix);
        replacePeriod = EditorGUILayout.Toggle("Replace periods", replacePeriod);
        EditorGUILayout.EndVertical();

        if(GUILayout.Button("Replace!"))
        {
            if (rootArmature == null || armPrefix == null)
                ShowNotification(new GUIContent("No prefix or no armature selected!"));
            
            List<GameObject> objs_to_rename = new List<GameObject>();
            List<GameObject> objs_to_search = new List<GameObject>();
            objs_to_search.Add(rootArmature);

            //get a list of all armature components
            while (objs_to_search.Count > 0)
            {
                List<int> removal_idxs = new List<int>();
                for (int i = 0; i < objs_to_search.Count; ++i)
                {
                    GameObject o = objs_to_search[i];
                    objs_to_rename.Add(o);
                    for (int j = 0; j < o.transform.childCount; ++j)
                    {
                        objs_to_search.Add(o.transform.GetChild(j).gameObject);
                    }
                    removal_idxs.Add(i);
                }
                removal_idxs.Reverse();
                foreach (int i in removal_idxs)
                {
                    objs_to_search.RemoveAt(i);
                }
            }
            foreach (GameObject o in objs_to_rename)
            {
                o.name = armPrefix + o.name;
                if (replacePeriod)
                    o.name = o.name.Replace(".", "_");
            }
        }

    }
}
