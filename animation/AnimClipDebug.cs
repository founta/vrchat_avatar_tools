using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class AnimationClipDebugger : EditorWindow {

    public AnimationClip srcClip;

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

    [MenuItem("Tools/founta/AnimationClipDebugger")]
    public static void ShowWindow() => GetWindow<AnimationClipDebugger>("Animation clip debugger").Show(true);

    void OnGUI() {
        EditorGUILayout.BeginVertical();
        srcClip = EditorGUILayout.ObjectField("clip", srcClip, typeof(UnityEngine.AnimationClip), true) as AnimationClip;
        EditorGUILayout.BeginVertical();
        if (GUILayout.Button("Debug!"))
        {
            var curves = AnimationUtility.GetAllCurves(srcClip);
            Debug.Log($"debugging clip! {srcClip.events.Count()}");
        }
    }
};