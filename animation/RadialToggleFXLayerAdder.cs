using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.Core;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC;

using static founta_common;

public class RadialToggleFXLayerAdder : EditorWindow
{

  public AnimatorController fx_ctrl;
  public AnimationClip on_anim;
  public VRCExpressionParameters vrc_params;
  public List<AnimationClip> animations = new List<AnimationClip>();
  public int num_anims = 1;
  public string param_name = "";

  [MenuItem("Tools/founta/Radial Toggle FX Layer Adder")]
  public static void ShowWindow() => GetWindow<RadialToggleFXLayerAdder>("Radial Toggle FX layer adder").Show(true);

  void set_vrc_int_param(string param_name, VRCExpressionParameters.Parameter p)
  {
    p.name = param_name;
    p.valueType = VRCExpressionParameters.ValueType.Int;
    p.defaultValue = 0;
    p.networkSynced = true;
    p.saved = true;
  }

  void OnGUI()
  {
    EditorGUILayout.BeginVertical();
    fx_ctrl = EditorGUILayout.ObjectField("FX controller", fx_ctrl, typeof(AnimatorController), true) as AnimatorController;
    vrc_params = EditorGUILayout.ObjectField("VRC expression parameters", vrc_params, typeof(VRCExpressionParameters), true) as VRCExpressionParameters;
    param_name = EditorGUILayout.TextField("Parameter name", param_name);

    num_anims = EditorGUILayout.IntField("Number of animations", num_anims);
    if (num_anims > 255)
      num_anims = 255;
    if (num_anims <= 0)
      num_anims = 1;

    while (animations.Count < num_anims)
    {
      animations.Add(null);
    }

    for (int i = 0; i < num_anims; ++i)
    {
      animations[i] = EditorGUILayout.ObjectField($"anim {i+1}", animations[i], typeof(AnimationClip), true) as AnimationClip;
    }
    EditorGUILayout.EndVertical();


    if (GUILayout.Button("Add to FX layer!"))
    {
      Undo.RecordObject(vrc_params, "Add VRC parameter for animations");
      Undo.RecordObject(fx_ctrl, "Modifications to FX controller");

      //Add new parameters
      addExpressionParameter(param_name, ref vrc_params, VRCExpressionParameters.ValueType.Float);
      fx_ctrl.AddParameter(new AnimatorControllerParameter
      {
        defaultBool = false,
        name = param_name,
        type = AnimatorControllerParameterType.Float
      });

      AnimatorControllerLayer layer = init_layer(fx_ctrl, param_name);

      AnimatorState default_state = init_state(fx_ctrl, layer, "idle", pos: new Vector3(250, 100, 0));

      // now make all of the animation states and transitions
      float step = 1.0f / (num_anims + 1);
      float start_y = 100 - 100f * ((float)num_anims) / 2;
      for (int i = 0; i < num_anims; ++i)
      {
        AnimatorState s = init_state(fx_ctrl, layer, animations[i].name, pos: new Vector3(500, start_y + 100*i, 0));
        s.motion = animations[i];

        AnimatorStateTransition default_to_s = init_transition(fx_ctrl, s);
        default_to_s.AddCondition(AnimatorConditionMode.Greater, (i + 1) * step, param_name);
        default_to_s.AddCondition(AnimatorConditionMode.Less, (i + 2) * step, param_name);
        default_state.AddTransition(default_to_s);

        AnimatorStateTransition s_to_exit_1 = init_exit_transition(fx_ctrl, ref s);
        s_to_exit_1.AddCondition(AnimatorConditionMode.Less, (i + 1) * step, param_name);

        AnimatorStateTransition s_to_exit_2 = init_exit_transition(fx_ctrl, ref s);
        s_to_exit_2.AddCondition(AnimatorConditionMode.Greater, (i + 2) * step, param_name);
      }

      EditorUtility.SetDirty(fx_ctrl);

      //save everything
      AssetDatabase.SaveAssets();
    }
  }
};