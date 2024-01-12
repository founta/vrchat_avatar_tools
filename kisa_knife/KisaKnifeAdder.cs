using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Avatars.Components;
using System;

using static founta_common;

public class KisaKnifeAdder : EditorWindow
{

  public VRCAvatarDescriptor avatar = null;
  public Dictionary<string, string> boneDestinations = new Dictionary<string, string> {
    {"legstrap_joined", "UpperLeg_R"},
    {"sheath", "UpperLeg_R" },
    {"legknifepos", "UpperLeg_R" },
    {"knifecontact", "UpperLeg_R"},
    {"knifecontact_Left", "UpperLeg_R"},
    {"knifehandpos_left", "Hand_L"},
    {"knifehandpos", "Hand_R"},
  };

  [MenuItem("Tools/founta/KisaKnifeAdder")]
  public static void ShowWindow() => GetWindow<KisaKnifeAdder>("KisaKnifeAdder").Show(true);

  void OnGUI()
  {
    EditorGUILayout.BeginVertical();
    avatar = EditorGUILayout.ObjectField("Avatar", avatar, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;
    EditorGUILayout.EndVertical();

    if (GUILayout.Button("Add knife!"))
    {
      if (avatar == null)
      {
        ShowNotification(new GUIContent("No avatar selected!"));
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

      //find the fx layer in the avatar descriptor
      AnimatorController fx_ctrl = null;
      VRCAvatarDescriptor.CustomAnimLayer[] all_anim_layers = avatar.baseAnimationLayers;
      foreach (VRCAvatarDescriptor.CustomAnimLayer l in all_anim_layers)
      {
        if (l.type == VRCAvatarDescriptor.AnimLayerType.FX)
        {
          if (l.isDefault)
          {
            ShowNotification(new GUIContent("Selected avatar has no FX controller!"));
            return;
          }
          fx_ctrl = l.animatorController as AnimatorController;
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
      bool added = false;
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

        EditorUtility.SetDirty(fx_ctrl);

        added = true;
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

        EditorUtility.SetDirty(fx_ctrl);

        added = true;
      }

      //save all changes
      AssetDatabase.SaveAssets();

      if (added)
      {
        ShowNotification(new GUIContent("Knife added to avatar!"));
      }
      else
      {
        ShowNotification(new GUIContent("Knife had already beed added; nothing to do!"));
      }
    }
  }
};