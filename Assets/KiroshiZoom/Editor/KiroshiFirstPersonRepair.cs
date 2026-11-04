#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using VRC.SDK3.Avatars.Components;

namespace KiroshiZoom
{
    public static class KiroshiFirstPersonRepair
    {
        [MenuItem("Tools/Kiroshi Zoom/Repair selected avatar for first person")]
        public static void Repair()
        {
            var selected = Selection.activeGameObject;
            var avatar = selected == null ? null : selected.GetComponentInParent<VRCAvatarDescriptor>();
            if (avatar == null) throw new InvalidOperationException("Select the avatar root in Hierarchy first.");
            var animator = avatar.GetComponent<Animator>();
            if (animator == null || !animator.isHuman) throw new InvalidOperationException("Humanoid Animator required.");
            Transform scope = null;
            foreach (var t in avatar.GetComponentsInChildren<Transform>(true))
                if (t.name == "KiroshiZoom")
                {
                    if (scope != null) throw new InvalidOperationException("Multiple scopes found; select an avatar with one scope.");
                    scope = t;
                }
            if (scope == null) throw new InvalidOperationException("Generate a scope first.");
            var camera = scope.Find("ZoomCamera").GetComponent<Camera>();
            var display = scope.Find("Display");
            string folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(camera.targetTexture)).Replace('\\', '/');
            var clips = new AnimationClip[3];
            var names = new[] { "Off", "On", "Amount" };
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i] = AssetDatabase.LoadAssetAtPath<AnimationClip>(folder + "/" + names[i] + ".anim");
                if (clips[i] == null) throw new InvalidOperationException("Original generated animation assets are required.");
            }
            if (avatar.transform.Find("KiroshiZoom") != null && scope.parent != avatar.transform)
                throw new InvalidOperationException("Avatar root already contains another KiroshiZoom object.");
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            string oldPath = AnimationUtility.CalculateTransformPath(scope, avatar.transform);
            Undo.SetTransformParent(scope, avatar.transform, "Move scope outside first-person head hiding");
            Undo.RecordObject(scope, "Set scope eye anchor");
            scope.position = avatar.transform.TransformPoint(avatar.ViewPosition);
            scope.rotation = avatar.transform.rotation;
            var constraint = scope.GetComponent<ParentConstraint>();
            if (constraint == null) constraint = Undo.AddComponent<ParentConstraint>(scope.gameObject);
            Undo.RecordObject(constraint, "Configure head tracking without inherited scale");
            constraint.constraintActive = false;
            while (constraint.sourceCount > 0) constraint.RemoveSource(0);
            constraint.AddSource(new ConstraintSource { sourceTransform = head, weight = 1 });
            constraint.SetTranslationOffset(0, Quaternion.Inverse(head.rotation) * (scope.position - head.position));
            constraint.SetRotationOffset(0, (Quaternion.Inverse(head.rotation) * scope.rotation).eulerAngles);
            constraint.weight = 1; constraint.locked = true; constraint.constraintActive = true;
            string newPath = AnimationUtility.CalculateTransformPath(scope, avatar.transform);
            foreach (var clip in clips)
            {
                Undo.RecordObject(clip, "Retarget scope animations");
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (binding.path != oldPath && !binding.path.StartsWith(oldPath + "/", StringComparison.Ordinal)) continue;
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    var updated = binding; updated.path = newPath + binding.path.Substring(oldPath.Length);
                    AnimationUtility.SetEditorCurve(clip, updated, curve);
                }
                EditorUtility.SetDirty(clip);
            }
            Undo.RecordObject(display, "Center display anchor at eyes");
            display.localPosition = Vector3.zero;
            // Use a unique mesh with generous bounds: the shader projects it full-screen.
            var filter = display.GetComponent<MeshFilter>();
            var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
            mesh.name = "FirstPersonDisplay";
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 20);
            AssetDatabase.CreateAsset(mesh, AssetDatabase.GenerateUniqueAssetPath(folder + "/FirstPersonDisplay.asset"));
            Undo.RecordObject(filter, "Assign display bounds"); filter.sharedMesh = mesh;
            Undo.RecordObject(camera, "Configure world-only capture"); camera.aspect = 1;
            // Explicit world layers; exclude player/UI layers and the display itself.
            camera.cullingMask = (1 << 0) | (1 << 4) | (1 << 11);
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = Color.gray;
            camera.nearClipPlane = 0.1f;
            Undo.RecordObject(scope.gameObject, "Default optics off"); scope.gameObject.SetActive(false);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(avatar);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(avatar.gameObject.scene);
            EditorUtility.DisplayDialog("Repair applied", "Save the scene and Build & Test again. Existing menu and parameter components are retained. First-person alignment still requires an in-game check.", "OK");
        }
    }
}
#endif
