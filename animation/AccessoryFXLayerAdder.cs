using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using Codice.CM.Common;
using VRC.Core;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC;

using static founta_common;

public class AccessoryFXLayerAdder : EditorWindow
{

  public AnimatorController fx_ctrl;
  public AnimationClip on_anim;
  public VRCExpressionParameters vrc_params;

  public string param_name = "";

  [MenuItem("Tools/founta/Accessory FX Layer Adder")]
  public static void ShowWindow() => GetWindow<AccessoryFXLayerAdder>("Accessory FX layer adder").Show(true);

  void set_vrc_param(string param_name, VRCExpressionParameters.Parameter p)
  {
    p.name = param_name;
    p.valueType = VRCExpressionParameters.ValueType.Bool;
    p.defaultValue = 0;
    p.networkSynced = true;
    p.saved = true;
  }

  void OnGUI()
  {
    EditorGUILayout.BeginVertical();
    on_anim = EditorGUILayout.ObjectField("Accessory on animation", on_anim, typeof(AnimationClip), true) as AnimationClip;
    fx_ctrl = EditorGUILayout.ObjectField("FX controller", fx_ctrl, typeof(AnimatorController), true) as AnimatorController;
    vrc_params = EditorGUILayout.ObjectField("VRC expression parameters", vrc_params, typeof(VRCExpressionParameters), true) as VRCExpressionParameters;
    param_name = EditorGUILayout.TextField("Parameter name", param_name);
    EditorGUILayout.EndVertical();

    if (GUILayout.Button("Add to FX layer!"))
    {
      Undo.RecordObject(vrc_params, "Add VRC parameter for accessory");
      Undo.RecordObject(fx_ctrl, "Modifications to FX controller");

      //Add new parameters
      addExpressionParameter(param_name, ref vrc_params);
      fx_ctrl.AddParameter(new AnimatorControllerParameter
      {
        defaultBool = false,
        name = param_name,
        type = AnimatorControllerParameterType.Bool
      });

      AnimatorControllerLayer layer = init_layer(fx_ctrl, param_name);

      AnimatorState default_state = init_state(fx_ctrl, layer, "idle", pos: new Vector3(250, 100, 0));
      AnimatorState acc_on_state = init_state(fx_ctrl, layer, param_name, pos: new Vector3(500, 100, 0));
      acc_on_state.motion = on_anim;

      AnimatorStateTransition default_to_on = init_transition(fx_ctrl, acc_on_state);
      default_to_on.AddCondition(AnimatorConditionMode.If, 0, param_name);
      default_state.AddTransition(default_to_on);

      //accessory on to exit arrow
      AnimatorStateTransition on_to_exit = init_exit_transition(fx_ctrl, ref acc_on_state);
      on_to_exit.AddCondition(AnimatorConditionMode.IfNot, 0, param_name);
      EditorUtility.SetDirty(on_to_exit);

      EditorUtility.SetDirty(fx_ctrl);

      //save everything
      AssetDatabase.SaveAssets();
    }
  }
};