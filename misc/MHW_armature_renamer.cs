using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MHWArmatureRenamer : EditorWindow {

    public GameObject armorArmature;
    public bool arm = true, leg = false, chest = false, waist = false;
    public Dictionary<int,string> armRenameMap = new Dictionary<int,string> {
        {1, "Right shoulder"},
        {2, "Right arm" },
        {3, "Right elbow" },
        {4, "Right wrist"},
        {6, "Thumb1_R"},
        {7, "Thumb2_R"},
        {8, "Thumb3_R"},
        {9, "IndexFinger1_R"},
        {10, "IndexFinger2_R"},
        {11, "IndexFinger3_R"},
        {12, "MiddleFinger1_R"},
        {13, "MiddleFinger2_R"},
        {14, "MiddleFinger3_R"},
        {16, "RingFinger1_R"},
        {17, "RingFinger2_R"},
        {18, "RingFinger3_R"},
        {19, "LittleFinger1_R"},
        {20, "LittleFinger2_R"},
        {21, "LittleFinger3_R"},
        {32, "Thumb1_L"},
        {33, "Thumb2_L"},
        {34, "Thumb3_L"},
        {35, "IndexFinger1_L"},
        {36, "IndexFinger2_L"},
        {37, "IndexFinger3_L"},
        {38, "MiddleFinger1_L"},
        {39, "MiddleFinger2_L"},
        {40, "MiddleFinger3_L"},
        {42, "RingFinger1_L"},
        {43, "RingFinger2_L"},
        {44, "RingFinger3_L"},
        {45, "LittleFinger1_L"},
        {46, "LittleFinger2_L"},
        {47, "LittleFinger3_L"},
        {26, "Left shoulder"},
        {28, "Left arm"},
        {29, "Left elbow"},
        {30, "Left wrist"}
    };

    public Dictionary<int, string> legRenameMap = new Dictionary<int, string>
    {
        {0, "Hips"},
        {1, "Right leg" },
        {2, "Right knee" },
        {3, "Right ankle"},
        {4, "Right toe" },
        {7, "Right butt" },
        {8, "Left leg" },
        {9, "Left knee" },
        {10, "Left ankle"},
        {11, "Left toe" },
        {14, "Left butt" }
    };

    public Dictionary<int, string> chestRenameMap = new Dictionary<int, string>
    {
        {1, "Hips" },
        {2, "Spine" },
        {3, "Chest" },
        {4, "Neck" },
        {7, "Left shoulder" },
        {8, "Left pauldron"},
        {9, "Left arm" },
        {10, "Left elbow" },
        {11, "Left wrist" },
        {34, "UpperArmTwist_L" },
        {36, "Right shoulder" },
        {60, "Right pauldron"},
        {37, "Right arm" },
        {38, "Right elbow" },
        {58, "UpperArmTwist_R" }
    };

    public Dictionary<int, string> waistRenameMap = new Dictionary<int, string>
    {
        {0, "Hips" },
        {13, "ScoutflyCage" },
        {15, "KnifeRibbon" },
        {17, "WaistArmorL" },
        {21, "WaistArmorR" },
        {25, "WaistFlap1"},
        {27, "WaistFlap2" },
        {29, "WaistFlap3" },
        {31, "WaistFlap4" }
    };

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

    [MenuItem("Tools/founta/MHWArmatureRenamer")]
    public static void ShowWindow() => GetWindow<MHWArmatureRenamer>("MHW Armature Renamer").Show(true);

    void OnGUI() {
        EditorGUILayout.BeginVertical();
        armorArmature = EditorGUILayout.ObjectField("Armor Armature", armorArmature, typeof(GameObject), true) as GameObject;
        bool prev_arm = arm, prev_leg = leg, prev_chest = chest, prev_waist = waist;
        arm = EditorGUILayout.Toggle("Is gauntlets?", arm);
        leg = EditorGUILayout.Toggle("Is leg armor?", leg);
        chest = EditorGUILayout.Toggle("Is body armor?", chest);
        waist = EditorGUILayout.Toggle("Is waist armor?", waist);
        if (leg != prev_leg)
        {
            arm = false;
            chest = false;
            waist = false;
        }
        if (arm != prev_arm)
        {
            leg = false;
            chest = false;
            waist = false;
        }
        if (chest != prev_chest)
        {
            leg = false;
            arm = false;
            waist = false;
        }
        if (waist != prev_waist)
        {
            leg = false;
            arm = false;
            chest = false;
        }
        EditorGUILayout.EndVertical();

        if(GUILayout.Button("Rename!"))
        {
            if (armorArmature == null)
            {
                ShowNotification(new GUIContent("No armor armature selected!"));
            }

            List<GameObject> allArmature = GetAllChildren(armorArmature);

            Dictionary<int, string> renameMap;
            if (arm)
            {
                renameMap = armRenameMap;
            }
            else if (chest)
            {
                renameMap = chestRenameMap;
            }
            else if (waist)
            {
                renameMap = waistRenameMap;
            }
            else //leg
            {
                renameMap = legRenameMap;
            }

            foreach (GameObject bone in allArmature)
            {
                foreach (KeyValuePair<int, string> pair in renameMap)
                {
                    string mhBoneName = $"Bone.{pair.Key:D3}";
                    if (bone.name.Contains(mhBoneName))
                    {
                        bone.name = bone.name.Replace(mhBoneName, pair.Value);
                    }
                }
            }
        }
    }
};