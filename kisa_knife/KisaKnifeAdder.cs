using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Avatars.Components;
using VRC;
using System;

public class KisaKnifeAdder : EditorWindow
{

  public VRCAvatarDescriptor avatar = null;
  public AnimatorController fx_ctrl = null;
  public Dictionary<string, string> boneDestinations = new Dictionary<string, string> {
    {"legstrap_joined", "UpperLeg_R"},
    {"sheath", "UpperLeg_R" },
    {"legknifepos", "UpperLeg_R" },
    {"knifecontact", "UpperLeg_R"},
    {"knifecontact_Left", "UpperLeg_R"},
    {"knifehandpos_left", "Hand_L"},
    {"knifehandpos", "Hand_R"},
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

  static void set_vrc_param(string param_name, VRCExpressionParameters.Parameter p)
  {
    p.name = param_name;
    p.valueType = VRCExpressionParameters.ValueType.Bool;
    p.defaultValue = 0;
    p.networkSynced = true;
    p.saved = true;
  }

  static AnimatorControllerLayer init_layer(AnimatorController fx_ctrl, string name)
  {
    fx_ctrl.AddLayer(name);
    AnimatorControllerLayer layer = fx_ctrl.layers[fx_ctrl.layers.Length - 1];

    EditorUtility.SetDirty(layer.stateMachine);

    //AssetDatabase.AddObjectToAsset(layer.stateMachine, fx_ctrl);

    return layer;
  }

  static AnimatorState init_state(AnimatorController fx_ctrl, AnimatorControllerLayer layer, string name, string anim_path, Vector3 pos)
  {
    AnimatorState s = layer.stateMachine.AddState(name, pos);
    s.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(anim_path);

    //AssetDatabase.AddObjectToAsset(s, fx_ctrl);
    EditorUtility.SetDirty(s);

    return s;
  }

  static AnimatorStateTransition init_transition(AnimatorController fx_ctrl, AnimatorState destination, float duration=0, float exit_time=0)
  {
    AnimatorStateTransition t = new AnimatorStateTransition();
    t.duration = duration;
    t.exitTime = exit_time;
    t.hasExitTime = exit_time > 1e-6f;
    t.destinationState = destination;

    AssetDatabase.AddObjectToAsset(t, fx_ctrl);
    t.hideFlags = HideFlags.HideInHierarchy;
    EditorUtility.SetDirty(t);

    return t;
  }

  static void addExpressionParameter(string name, ref VRCExpressionParameters para)
  {
    bool set = false;
    foreach (VRCExpressionParameters.Parameter p in para.parameters)
    {
      if (p.name == "")
      {
        set_vrc_param(name, p);
        set = true;
        break;
      }
      if (p.name == name)
      {
        //then we have already added it in the past
        return;
      }
    }
    //if no free space, expand the parameter array and add a new one
    if (!set)
    {
      List<VRCExpressionParameters.Parameter> new_params = new List<VRCExpressionParameters.Parameter>(para.parameters);
      VRCExpressionParameters.Parameter new_vrc_param = new VRCExpressionParameters.Parameter();
      set_vrc_param(name, new_vrc_param);
      new_params.Add(new_vrc_param);
      para.parameters = new_params.ToArray();

      EditorUtility.SetDirty(para); //need to do this otherwise expression parameters don't save after re-opening unity
    }
  }

  [MenuItem("Tools/founta/KisaKnifeAdder")]
  public static void ShowWindow() => GetWindow<KisaKnifeAdder>("KisaKnifeAdder").Show(true);

  void OnGUI()
  {
    EditorGUILayout.BeginVertical();
    avatar = EditorGUILayout.ObjectField("Avatar", avatar, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;
    fx_ctrl = EditorGUILayout.ObjectField("FX controller", fx_ctrl, typeof(AnimatorController), true) as AnimatorController;
    EditorGUILayout.EndVertical();

    if (GUILayout.Button("Add knife!"))
    {
      if (avatar == null)
      {
        ShowNotification(new GUIContent("No avatar selected!"));
        return;
      }
      if (fx_ctrl == null)
      {
        ShowNotification(new GUIContent("No FX controller selected!"));
        return;
      }

      //first add knife to the avatar
      GameObject knife = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/kisa/knife+legstrap/knfe on velle donemaybe.prefab"), avatar.transform) as GameObject;
      Undo.RegisterCreatedObjectUndo(knife, "add velle knife");
      PrefabUtility.UnpackPrefabInstance(knife, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction); //unpack prefab

      //move knife and sheath armature to required positions
      GameObject armature = avatar.gameObject.transform.Find("Armature").gameObject;
      List<GameObject> allArmature = GetAllChildren(armature);
      foreach (GameObject bone in allArmature)
      {
        foreach (KeyValuePair<string, string> pair in boneDestinations)
        {
          if (pair.Value == bone.name)
          {
            //find the armature component in the prefab
            Transform knife_arm_component = knife.transform.Find(pair.Key);

            //move the armature to the correct location on the avatar
            Undo.SetTransformParent(knife_arm_component, bone.transform, $"knife armature: knife {pair.Key} -> velle {pair.Value}");
          }
        }
      }

      //create animation parameters
      List<String> knife_param_names = new List<string> { "Grab", "KnifeSwap", "GrabLeft" };
      List<String> knife_layer_names = new List<string> { "Knife_legstrap", "Knife_sheath_open" };
      ref VRCExpressionParameters vrc_params = ref avatar.expressionParameters;
      Undo.RecordObject(fx_ctrl, "FX layer changes");
      Undo.RecordObject(vrc_params, "Expression parameter changes");
      foreach (string name in knife_param_names)
      {
        bool found = false;
        foreach (AnimatorControllerParameter param in fx_ctrl.parameters)
        {
          if (param.name == name)
          {
            found = true;
            break;
          }
        }
        if (!found) //then add it to the FX layer parameters as well as the VRC parameters
        {
          fx_ctrl.AddParameter(new AnimatorControllerParameter
          {
            defaultBool = false,
            name = name,
            type = AnimatorControllerParameterType.Bool
          });
          addExpressionParameter(name, ref vrc_params);
        }
      }

      //now create animation layers in the FX controller
      List<string> layers_to_add = new List<string>();
      foreach (string layer_name in knife_layer_names)
      {
        bool found = false;
        foreach (AnimatorControllerLayer l in fx_ctrl.layers)
        {
          if (l.name == layer_name)
          {
            found = true;
            break;
          }
        }
        if (!found)
        {
          layers_to_add.Add(layer_name);
        }
      }

      //add legstrap layer
      if (layers_to_add.Contains("Knife_legstrap"))
      {
        AnimatorControllerLayer layer = init_layer(fx_ctrl, "Knife_legstrap");

        //create states
        AnimatorState knifehip = init_state(fx_ctrl, layer, "KnifeHip", "Assets/kisa/knife+legstrap/KnifeHip.anim", new Vector3(250, 100, 0));
        AnimatorState knifehandleft = init_state(fx_ctrl, layer, "knifeHandLeft", "Assets/kisa/knife+legstrap/knifeHandLeft.anim", new Vector3(400, 175, 0));
        AnimatorState knifehand = init_state(fx_ctrl, layer, "KnifeHand", "Assets/kisa/knife+legstrap/KnifeHand.anim", new Vector3(400, 25, 0));

        //make transitions

        //hip <-> right hand
        AnimatorStateTransition hip_to_hand = init_transition(fx_ctrl, knifehand, 0.01f);
        hip_to_hand.AddCondition(AnimatorConditionMode.If, 0, "Grab");
        hip_to_hand.AddCondition(AnimatorConditionMode.Equals, 1, "GestureRight");
        knifehip.AddTransition(hip_to_hand);

        AnimatorStateTransition hand_to_hip = init_transition(fx_ctrl, knifehip, 0.01f);
        hand_to_hip.AddCondition(AnimatorConditionMode.If, 0, "Grab");
        hand_to_hip.AddCondition(AnimatorConditionMode.NotEqual, 1, "GestureRight");
        knifehand.AddTransition(hand_to_hip);

        //hip <-> left hand
        AnimatorStateTransition hip_to_hand_left = init_transition(fx_ctrl, knifehandleft, 0.01f);
        hip_to_hand_left.AddCondition(AnimatorConditionMode.If, 0, "Grab");
        hip_to_hand_left.AddCondition(AnimatorConditionMode.Equals, 1, "GestureLeft");
        knifehip.AddTransition(hip_to_hand_left);

        AnimatorStateTransition hand_to_hip_left = init_transition(fx_ctrl, knifehip, 0.01f);
        hand_to_hip_left.AddCondition(AnimatorConditionMode.If, 0, "Grab");
        hand_to_hip_left.AddCondition(AnimatorConditionMode.NotEqual, 1, "GestureLeft");
        knifehandleft.AddTransition(hand_to_hip);

        //hand swap
        AnimatorStateTransition right_to_left = init_transition(fx_ctrl, knifehandleft, 0.01f);
        right_to_left.AddCondition(AnimatorConditionMode.If, 0, "KnifeSwap");
        right_to_left.AddCondition(AnimatorConditionMode.Equals, 2, "GestureRight");
        right_to_left.AddCondition(AnimatorConditionMode.Equals, 1, "GestureLeft");
        knifehand.AddTransition(right_to_left);

        AnimatorStateTransition left_to_right = init_transition(fx_ctrl, knifehand, 0.01f);
        left_to_right.AddCondition(AnimatorConditionMode.If, 0, "KnifeSwap");
        left_to_right.AddCondition(AnimatorConditionMode.Equals, 2, "GestureLeft");
        left_to_right.AddCondition(AnimatorConditionMode.Equals, 1, "GestureRight");
        knifehandleft.AddTransition(left_to_right);

        //set layer weight
        AnimatorControllerLayer[] layers = fx_ctrl.layers;
        layers[fx_ctrl.layers.Length - 1].defaultWeight = 1;
        fx_ctrl.layers = layers;

        EditorUtility.SetDirty(fx_ctrl);
      }
      //add knife sheath layer
      if (layers_to_add.Contains("Knife_sheath_open"))
      {
        AnimatorControllerLayer layer = init_layer(fx_ctrl, "Knife_sheath_open");

        //create states
        AnimatorState sheathclose = init_state(fx_ctrl, layer, "knifeSheathClose", "Assets/kisa/knife+legstrap/knifeSheathClose.anim", new Vector3(250, 100, 0));
        AnimatorState sheathopen = init_state(fx_ctrl, layer, "knifeSheathOpen", "Assets/kisa/knife+legstrap/knifeSheathOpen.anim", new Vector3(500, 100, 0));

        //make transitions

        //hip <-> right hand
        AnimatorStateTransition close_to_open = init_transition(fx_ctrl, sheathopen, 0.25f, 0.75f);
        close_to_open.AddCondition(AnimatorConditionMode.If, 0, "Grab");
        sheathclose.AddTransition(close_to_open);

        AnimatorStateTransition open_to_close = init_transition(fx_ctrl, sheathclose, 0.25f, 0.75f);
        open_to_close.AddCondition(AnimatorConditionMode.If, 0, "Grab");
        sheathopen.AddTransition(open_to_close);

        //set layer weight
        AnimatorControllerLayer[] layers = fx_ctrl.layers;
        layers[fx_ctrl.layers.Length - 1].defaultWeight = 1;
        fx_ctrl.layers = layers;

        EditorUtility.SetDirty(fx_ctrl);
      }

      //save all changes
      AssetDatabase.SaveAssets();
    }
  }
};