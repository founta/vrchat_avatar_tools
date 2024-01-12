using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using static VRC.SDKBase.RPC;

static public class founta_common
{

  public static List<GameObject> GetAllChildren(GameObject root)
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

  public static void set_vrc_param(string param_name, VRCExpressionParameters.Parameter p, VRCExpressionParameters.ValueType type)
  {
    p.name = param_name;
    p.valueType = type;
    p.defaultValue = 0;
    p.networkSynced = true;
    p.saved = true;
  }

  public static AnimatorControllerLayer init_layer(AnimatorController fx_ctrl, string name)
  {
    fx_ctrl.AddLayer(name);
    AnimatorControllerLayer layer = fx_ctrl.layers[fx_ctrl.layers.Length - 1];

    //set layer weight
    AnimatorControllerLayer[] layers = fx_ctrl.layers;
    layers[fx_ctrl.layers.Length - 1].defaultWeight = 1;
    fx_ctrl.layers = layers;

    EditorUtility.SetDirty(layer.stateMachine);
    return layer;
  }

  public static AnimatorState init_state(AnimatorController fx_ctrl, AnimatorControllerLayer layer, string name, string anim_path = "", Vector3? pos = null)
  {
    AnimatorState s;
    if (pos != null)
    {
      s = layer.stateMachine.AddState(name, pos.Value);
    }
    else
    {
      s = layer.stateMachine.AddState(name);
    }

    if (anim_path.Length != 0)
      s.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(anim_path);

    EditorUtility.SetDirty(s);
    return s;
  }

  private static void configure_transition(ref AnimatorStateTransition t, AnimatorController fx_ctrl, float duration, float exit_time, bool add_asset = true)
  {
    t.duration = duration;
    t.exitTime = exit_time;
    t.hasExitTime = exit_time > 1e-6f;

    if (add_asset)
    {
      AssetDatabase.AddObjectToAsset(t, fx_ctrl);
      t.hideFlags = HideFlags.HideInHierarchy;
      EditorUtility.SetDirty(t);
    }
  }

  public static AnimatorStateTransition init_transition(AnimatorController fx_ctrl, AnimatorState destination, float duration = 0, float exit_time = 0)
  {
    AnimatorStateTransition t = new AnimatorStateTransition();
    t.destinationState = destination;

    configure_transition(ref t, fx_ctrl, duration, exit_time);

    return t;
  }

  public static AnimatorStateTransition init_exit_transition(AnimatorController fx_ctrl, ref AnimatorState source, float duration = 0, float exit_time = 0)
  {
    AnimatorStateTransition t = source.AddExitTransition();

    configure_transition(ref t, fx_ctrl, duration, exit_time, false);

    return t;
  }

  public static void addExpressionParameter(string name, ref VRCExpressionParameters para, VRCExpressionParameters.ValueType type = VRCExpressionParameters.ValueType.Bool)
  {
    bool set = false;
    foreach (VRCExpressionParameters.Parameter p in para.parameters)
    {
      if (p.name == "")
      {
        set_vrc_param(name, p, type);
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
      set_vrc_param(name, new_vrc_param, type);
      new_params.Add(new_vrc_param);
      para.parameters = new_params.ToArray();

      EditorUtility.SetDirty(para); //need to do this otherwise expression parameters don't save after re-opening unity
    }
  }
}
