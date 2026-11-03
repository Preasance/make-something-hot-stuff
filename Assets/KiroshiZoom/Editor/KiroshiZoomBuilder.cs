#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace KiroshiZoom
{
    // Editor-only generator. Uploaded avatars contain no custom runtime scripts.
    public sealed class KiroshiZoomBuilder : EditorWindow
    {
        private VRCAvatarDescriptor avatar;
        [MenuItem("Tools/Kiroshi Zoom/Create desktop zoom")]
        private static void Open() => GetWindow<KiroshiZoomBuilder>("Kiroshi Zoom");

        private void OnGUI()
        {
            avatar = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", avatar, typeof(VRCAvatarDescriptor), true);
            EditorGUILayout.HelpBox("Creates a head-mounted desktop scope and separate FX/menu assets. Existing avatar controllers and menus are not replaced. Follow README integration steps.", MessageType.Info);
            using (new EditorGUI.DisabledScope(avatar == null))
                if (GUILayout.Button("Generate assets and scope")) Build();
        }

        private void Build()
        {
            var animator = avatar.GetComponent<Animator>();
            var head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            var shader = Shader.Find("KiroshiZoom/Display");
            if (head == null || shader == null)
                throw new InvalidOperationException("A humanoid avatar with a Head bone and the KiroshiZoom shader is required.");
            if (head.Find("KiroshiZoom") != null)
                throw new InvalidOperationException("KiroshiZoom already exists on this head. Remove the previous generated scope before rebuilding.");

            string folder = "Assets/KiroshiZoom/Generated-" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/KiroshiZoom", folder.Substring(folder.LastIndexOf('/') + 1));
            var texture = new RenderTexture(1024, 1024, 24) { name = "ZoomTexture", antiAliasing = 1 };
            AssetDatabase.CreateAsset(texture, folder + "/ZoomTexture.renderTexture");
            var material = new Material(shader) { mainTexture = texture };
            AssetDatabase.CreateAsset(material, folder + "/Display.mat");

            var root = new GameObject("KiroshiZoom");
            Undo.RegisterCreatedObjectUndo(root, "Create Kiroshi Zoom");
            root.transform.SetParent(head, false);
            // Descriptor ViewPosition is in avatar-root space; convert to head space.
            root.transform.position = avatar.transform.TransformPoint(avatar.ViewPosition);
            root.transform.rotation = avatar.transform.rotation;
            var cameraObject = new GameObject("ZoomCamera");
            cameraObject.transform.SetParent(root.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.targetTexture = texture;
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            camera.fieldOfView = 30;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 1000;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.cullingMask = ~((1 << 5) | (1 << 12) | (1 << 18));
            var display = GameObject.CreatePrimitive(PrimitiveType.Quad);
            display.name = "Display";
            display.layer = 5; // Camera excludes UI to prevent feedback.
            DestroyImmediate(display.GetComponent<Collider>());
            display.transform.SetParent(root.transform, false);
            display.transform.localPosition = new Vector3(0, 0, 0.45f);
            display.transform.localScale = new Vector3(0.32f, 0.32f, 1);
            display.GetComponent<MeshRenderer>().sharedMaterial = material;

            string rootPath = AnimationUtility.CalculateTransformPath(root.transform, avatar.transform);
            var off = new AnimationClip { name = "ZoomOff" };
            var on = new AnimationClip { name = "ZoomOn" };
            off.SetCurve(rootPath, typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 1, 0));
            on.SetCurve(rootPath, typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 1, 1));
            AssetDatabase.CreateAsset(off, folder + "/Off.anim");
            AssetDatabase.CreateAsset(on, folder + "/On.anim");
            var zoom = new AnimationClip { name = "ZoomAmount" };
            // Perceived magnification is relative to a 60-degree reference, not the player's actual FOV.
            zoom.SetCurve(rootPath + "/ZoomCamera", typeof(Camera), "field of view",
                AnimationCurve.Linear(0, 30, 1, 6));
            AssetDatabase.CreateAsset(zoom, folder + "/Amount.anim");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(folder + "/ZoomFX.controller");
            controller.AddParameter("Kiroshi/Enabled", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Kiroshi/Amount", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsLocal", AnimatorControllerParameterType.Bool);
            var machine = controller.layers[0].stateMachine;
            var disabled = machine.AddState("Off"); disabled.motion = off; disabled.writeDefaultValues = false;
            var enabled = machine.AddState("On"); enabled.motion = on; enabled.writeDefaultValues = false;
            machine.defaultState = disabled;
            var enter = disabled.AddTransition(enabled); enter.hasExitTime = false; enter.duration = 0;
            enter.AddCondition(AnimatorConditionMode.If, 0, "Kiroshi/Enabled");
            enter.AddCondition(AnimatorConditionMode.If, 0, "IsLocal");
            foreach (var parameter in new[] { "Kiroshi/Enabled", "IsLocal" })
            {
                var exit = enabled.AddTransition(disabled); exit.hasExitTime = false; exit.duration = 0;
                exit.AddCondition(AnimatorConditionMode.IfNot, 0, parameter);
            }
            controller.AddLayer("Kiroshi Amount");
            var layers = controller.layers; layers[1].defaultWeight = 1; controller.layers = layers;
            var amount = controller.layers[1].stateMachine.AddState("Amount");
            amount.motion = zoom; amount.writeDefaultValues = false;
            amount.timeParameter = "Kiroshi/Amount"; amount.timeParameterActive = true;
            controller.layers[1].stateMachine.defaultState = amount;

            var parameters = CreateInstance<VRCExpressionParameters>();
            parameters.parameters = new[] {
                new VRCExpressionParameters.Parameter { name = "Kiroshi/Enabled", valueType = VRCExpressionParameters.ValueType.Bool, networkSynced = false, saved = false },
                new VRCExpressionParameters.Parameter { name = "Kiroshi/Amount", valueType = VRCExpressionParameters.ValueType.Float, networkSynced = false, saved = true }
            };
            AssetDatabase.CreateAsset(parameters, folder + "/Parameters.asset");
            var menu = CreateInstance<VRCExpressionsMenu>();
            menu.controls.Add(new VRCExpressionsMenu.Control { name = "Optics", type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = "Kiroshi/Enabled" }, value = 1 });
            menu.controls.Add(new VRCExpressionsMenu.Control { name = "Zoom", type = VRCExpressionsMenu.Control.ControlType.RadialPuppet,
                subParameters = new[] { new VRCExpressionsMenu.Control.Parameter { name = "Kiroshi/Amount" } } });
            AssetDatabase.CreateAsset(menu, folder + "/Menu.asset");
            root.SetActive(false);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root;
            EditorUtility.DisplayDialog("Generated", "Assets saved in " + folder + ". Follow README to merge FX layers, parameters and menu before upload.", "OK");
        }
    }
}
#endif
