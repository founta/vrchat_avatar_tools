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
            //Add new parameter to fx controller
            fx_ctrl.AddParameter(param_name, AnimatorControllerParameterType.Bool);
            AnimatorControllerLayer layer = new AnimatorControllerLayer();
            layer.stateMachine = new AnimatorStateMachine();
            layer.stateMachine.name = param_name;

            layer.defaultWeight = 1;
            layer.name = param_name;

            //create states
            AnimatorState default_state = layer.stateMachine.AddState("Default");
            AnimatorState acc_on_state = layer.stateMachine.AddState(param_name + "_on");
            acc_on_state.motion = on_anim;

            //create transistions
            //the first state you add is the default state!
            //layer.stateMachine.AddEntryTransition(default_state); //default state (entry transition arrow)

            //default state to accessory on arrow
            AnimatorStateTransition default_to_on = new AnimatorStateTransition();
            default_to_on.duration = 0;
            default_to_on.exitTime = 0;
            default_to_on.hasExitTime = false;
            default_to_on.destinationState = acc_on_state;
            default_to_on.AddCondition(AnimatorConditionMode.If, 0, param_name);
            default_state.AddTransition(default_to_on);

            //accessory on to exit arrow
            AnimatorStateTransition on_to_exit = acc_on_state.AddExitTransition();
            on_to_exit.duration = 0;
            on_to_exit.exitTime = 0;
            on_to_exit.hasExitTime = false;
            on_to_exit.AddCondition(AnimatorConditionMode.IfNot, 0, param_name);

            fx_ctrl.AddLayer(layer);

            //now save all of the new objects into the asset so they recover on reloading the project ;)
            AssetDatabase.AddObjectToAsset(default_state, fx_ctrl);
            default_state.hideFlags = HideFlags.HideInHierarchy;

            AssetDatabase.AddObjectToAsset(acc_on_state, fx_ctrl);
            acc_on_state.hideFlags = HideFlags.HideInHierarchy;

            AssetDatabase.AddObjectToAsset(layer.stateMachine, fx_ctrl);
            layer.stateMachine.hideFlags = HideFlags.HideInHierarchy;

            AssetDatabase.AddObjectToAsset(default_to_on, fx_ctrl);
            default_to_on.hideFlags = HideFlags.HideInHierarchy;

            AssetDatabase.AddObjectToAsset(on_to_exit, fx_ctrl);
            on_to_exit.hideFlags = HideFlags.HideInHierarchy;

            ///now add the parameter to the VR chat expressions parameter
            //first check if there's an empty spot in the parameters array and use that if so
            bool set = false;
            foreach (VRCExpressionParameters.Parameter p in vrc_params.parameters)
            {
                if (p.name == "")
                {
                    set_vrc_param(param_name, p);
                    vrc_params.MarkDirty();
                    set = true;
                    break;
                }
            }
            //if no free space, expand the parameter array and add a new one
            if (!set)
            {
                List<VRCExpressionParameters.Parameter> mut_params = new List<VRCExpressionParameters.Parameter>(vrc_params.parameters);
                VRCExpressionParameters.Parameter new_vrc_param = new VRCExpressionParameters.Parameter();
                set_vrc_param(param_name, new_vrc_param);
                mut_params.Add(new_vrc_param);
                vrc_params.parameters = mut_params.ToArray();
                vrc_params.MarkDirty();
            }
            EditorUtility.SetDirty(vrc_params);
            //save everything
            AssetDatabase.SaveAssets();
        }
    }
};