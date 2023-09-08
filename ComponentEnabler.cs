using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ComponentEnabler : EditorWindow {

    public GameObject rootObj;
    public string searchSubstring="";
    public bool toEnable = true;

    static List<GameObject> GetAllChildren(GameObject root)
    {
        List<GameObject> objs_to_return = new List<GameObject>();
        List<GameObject> objs_to_search = new List<GameObject>();
        objs_to_search.Add(root);

        //get a list of all armature components
        while (objs_to_search.Count > 0)
        {
            List<int> removal_idxs = new List<int>();
            for (int i = 0; i < objs_to_search.Count; ++i)
            {
                GameObject o = objs_to_search[i];
                objs_to_return.Add(o);
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
        return objs_to_return;
    }

    [MenuItem("Tools/founta/ComponentEnabler")]
    public static void ShowWindow() => GetWindow<ComponentEnabler>("Component Enabler").Show(true);

    void OnGUI() {
        EditorGUILayout.BeginVertical();
        rootObj = EditorGUILayout.ObjectField("Root object", rootObj, typeof(GameObject), true) as GameObject;
        searchSubstring = EditorGUILayout.TextField("Substring to search for", searchSubstring);
        toEnable = EditorGUILayout.Toggle("Enable objects found?", toEnable);
        EditorGUILayout.BeginVertical();

        if(GUILayout.Button("Enable/Disable!"))
        {
            if (rootObj == null || searchSubstring == "")
            {
                ShowNotification(new GUIContent("Either no root object or no substring selected!"));
            }

            List<GameObject> allRootChildren = GetAllChildren(rootObj);

            foreach (GameObject o in allRootChildren)
            {
                if (o.name.ToLower().Contains(searchSubstring.ToLower()))
                {
                    o.SetActive(toEnable);
                }
            }
        }
    }
};