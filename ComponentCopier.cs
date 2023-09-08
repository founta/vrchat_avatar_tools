using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ComponentCopier : EditorWindow {

    public GameObject srcArmature, dstArmature;
    public string ignorePrefix="";

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

    [MenuItem("Tools/founta/ComponentCopier")]
    public static void ShowWindow() => GetWindow<ComponentCopier>("Component Copier").Show(true);

    void OnGUI() {
        EditorGUILayout.BeginVertical();
        dstArmature = EditorGUILayout.ObjectField("Destination root", dstArmature, typeof(GameObject), true) as GameObject;
        srcArmature = EditorGUILayout.ObjectField("Source root", srcArmature, typeof(GameObject), true) as GameObject;
        ignorePrefix = EditorGUILayout.TextField("Prefix to ignore in source", ignorePrefix);
        EditorGUILayout.BeginVertical();

        if(GUILayout.Button("Move!"))
        {
            if (dstArmature == null || srcArmature == null)
            {
                ShowNotification(new GUIContent("Either no destination or no source selected!"));
            }

            List<GameObject> allDstChildren = GetAllChildren(dstArmature);
            List<GameObject> allSrcChildren = GetAllChildren(srcArmature);

            foreach (GameObject srco in allSrcChildren)
            {
                string srch_name = srco.name.Replace(ignorePrefix,"");
                foreach (GameObject dsto in allDstChildren)
                {
                    if (dsto.name.ToLower() == srch_name.ToLower())
                    {
                        foreach (Component c in srco.GetComponents<Component>())
                        {
                            var comp_type = c.GetType();
                            bool skip = false;
                            foreach (Component dstc in dsto.GetComponents<Component>())
                            {
                                if (comp_type == dstc.GetType())
                                {
                                    skip = true;
                                    break;
                                }
                            }
                            if (skip)
                                continue;
                            //make a new component with the same type as the source
                            var new_c = dsto.AddComponent(comp_type);

                            EditorUtility.CopySerialized(c, new_c);
                        }
                        break;
                    }
                }
            }
        }
    }
};