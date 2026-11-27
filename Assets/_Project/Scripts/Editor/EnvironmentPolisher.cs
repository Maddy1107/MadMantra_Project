#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using MadMantra.Audio;
using MadMantra.Environment;
using MadMantra.NPC;

namespace MadMantra.Editor
{
    public static class EnvironmentPolisher
    {
        private const string ModelDir = "Assets/_Project/Models/Environment/";
        private const string MatDir = "Assets/_Project/Materials/";

        [MenuItem("MadMantra/Setup Mixamo NPC")]
        public static void SetupMixamoNPC()
        {
            const string idlePath = "Assets/_Project/Models/NPC/Standing Idle.fbx";
            const string walkPath = "Assets/_Project/Models/NPC/Walking.fbx";
            const string talkPath = "Assets/_Project/Models/NPC/Talking.fbx";
            const string disappointedPath = "Assets/_Project/Models/NPC/Disappointed.fbx";
            const string texFolder = "Assets/_Project/Models/NPC/Textures";
            const string animCtrlPath = "Assets/_Project/Animations/NPC_Animator.controller";
            const string npcPrefabPath = "Assets/_Project/Prefabs/NPC/NPC_Survivor.prefab";

            // 1. Configure Idle FBX as Humanoid + looping + extract embedded textures
            ModelImporter idleImp = AssetImporter.GetAtPath(idlePath) as ModelImporter;
            if (idleImp == null)
            {
                Debug.LogError("[EnvironmentPolisher] Standing Idle.fbx not found in Assets/_Project/Models/NPC/");
                return;
            }

            if (!AssetDatabase.IsValidFolder(texFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Models/NPC", "Textures");
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Animations"))
                AssetDatabase.CreateFolder("Assets/_Project", "Animations");

            idleImp.animationType = ModelImporterAnimationType.Human;
            idleImp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            idleImp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            SetClipLoop(idleImp, true);
            idleImp.ExtractTextures(texFolder);
            idleImp.SaveAndReimport();
            AssetDatabase.Refresh();

            Avatar npcAvatar = AssetDatabase.LoadAllAssetsAtPath(idlePath).OfType<Avatar>().FirstOrDefault();

            // 2. Configure Walking, Talking, and Disappointed FBXs sharing Idle's Avatar
            AnimationClip idleClip = LoadFirstClip(idlePath);
            AnimationClip walkClip = ConfigureClipFbx(walkPath, npcAvatar, true);
            AnimationClip talkClip = ConfigureClipFbx(talkPath, npcAvatar, false);
            AnimationClip disappointedClip = ConfigureClipFbx(disappointedPath, npcAvatar, false);

            // 3. Build clean NPC_Animator.controller (Idle, Walk, Talking, Disappointed)
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(animCtrlPath) != null)
                AssetDatabase.DeleteAsset(animCtrlPath);

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(animCtrlPath);
            ctrl.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Talk", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Disappointed", AnimatorControllerParameterType.Trigger);
            AnimatorControllerLayer[] layers = ctrl.layers;
            layers[0].iKPass = true;
            ctrl.layers = layers;
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

            AnimatorState idleState = sm.AddState("Idle");
            idleState.motion = idleClip;
            sm.defaultState = idleState;

            AnimatorState walkState = sm.AddState("Walk");
            walkState.motion = walkClip;

            AnimatorState talkState = sm.AddState("Talking");
            talkState.motion = talkClip;

            AnimatorState disappointedState = sm.AddState("Disappointed");
            disappointedState.motion = disappointedClip;

            AddBoolTransition(idleState, walkState, "IsWalking", true);
            AddBoolTransition(walkState, idleState, "IsWalking", false);

            AddTriggerTransition(idleState, talkState, "Talk");
            AddTriggerTransition(talkState, talkState, "Talk");

            AnimatorStateTransition talkToIdle = talkState.AddTransition(idleState);
            talkToIdle.hasExitTime = true;
            talkToIdle.exitTime = 0.9f;
            talkToIdle.duration = 0.15f;

            AddTriggerTransition(idleState, disappointedState, "Disappointed");
            AddTriggerTransition(talkState, disappointedState, "Disappointed");

            AnimatorStateTransition disappointedToIdle = disappointedState.AddTransition(idleState);
            disappointedToIdle.hasExitTime = true;
            disappointedToIdle.exitTime = 0.9f;
            disappointedToIdle.duration = 0.15f;

            AddBoolTransition(disappointedState, walkState, "IsWalking", true);

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();

            // 4. Swap placeholder body in NPC_Survivor.prefab with Mixamo character
            GameObject npcFbx = AssetDatabase.LoadAssetAtPath<GameObject>(idlePath);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(npcPrefabPath);
            try
            {
                Transform oldModelRoot = prefabRoot.transform.Find("ModelRoot");
                if (oldModelRoot != null) oldModelRoot.gameObject.SetActive(false);

                Transform existing = prefabRoot.transform.Find("Model_MixamoNPC");
                if (existing != null) Object.DestroyImmediate(existing.gameObject);

                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(npcFbx);
                inst.name = "Model_MixamoNPC";
                inst.transform.SetParent(prefabRoot.transform, false);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one;

                Animator anim = inst.GetComponent<Animator>();
                if (anim == null) anim = inst.AddComponent<Animator>();
                anim.runtimeAnimatorController = ctrl;
                anim.avatar = npcAvatar;
                anim.applyRootMotion = false;

                NPCHeadLook headLook = inst.GetComponent<NPCHeadLook>();
                if (headLook == null) headLook = inst.AddComponent<NPCHeadLook>();

                NPCController npcCtrl = prefabRoot.GetComponent<NPCController>();
                if (npcCtrl != null)
                {
                    SerializedObject so = new SerializedObject(npcCtrl);
                    so.FindProperty("animator").objectReferenceValue = anim;
                    so.FindProperty("headLook").objectReferenceValue = headLook;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, npcPrefabPath);
                Debug.Log("[EnvironmentPolisher] Mixamo NPC configured with Idle, Walk, Talking & Disappointed animations!");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static AnimationClip ConfigureClipFbx(string fbxPath, Avatar avatar, bool loop)
        {
            ModelImporter imp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (imp == null)
            {
                Debug.LogWarning("[EnvironmentPolisher] Animation FBX not found: " + fbxPath);
                return null;
            }
            imp.animationType = ModelImporterAnimationType.Human;
            if (avatar != null)
            {
                imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                imp.sourceAvatar = avatar;
            }
            SetClipLoop(imp, loop);
            imp.SaveAndReimport();
            return LoadFirstClip(fbxPath);
        }

        private static AnimationClip LoadFirstClip(string fbxPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(fbxPath)
                .OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }

        private static void AddBoolTransition(AnimatorState from, AnimatorState to, string param, bool value)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, param);
        }

        private static void AddTriggerTransition(AnimatorState from, AnimatorState to, string trigger)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0.15f;
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void SetClipLoop(ModelImporter imp, bool loop)
        {
            ModelImporterClipAnimation[] clips = imp.clipAnimations;
            if (clips == null || clips.Length == 0) clips = imp.defaultClipAnimations;
            for (int i = 0; i < clips.Length; i++) clips[i].loopTime = loop;
            imp.clipAnimations = clips;
        }

        [MenuItem("MadMantra/Setup Audio Manager")]
        public static void SetupAudioManager()
        {
            const string audioDir = "Assets/_Project/Audio/";
            GameObject go = GameObject.Find("AudioManager");
            if (go != null) Object.DestroyImmediate(go);

            go = new GameObject("AudioManager");
            AudioSource ambient = go.AddComponent<AudioSource>();
            ambient.playOnAwake = false;
            ambient.spatialBlend = 0f;

            AudioSource sfx = go.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;

            AudioManager mgr = go.AddComponent<AudioManager>();
            SerializedObject so = new SerializedObject(mgr);
            so.FindProperty("ambientSource").objectReferenceValue = ambient;
            so.FindProperty("sfxSource").objectReferenceValue = sfx;
            so.FindProperty("ambientHum").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "spaceEngineLow_000.ogg");
            so.FindProperty("lightFlicker").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "forceField_000.ogg");
            so.FindProperty("footstep").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "impactMetal_000.ogg");
            so.FindProperty("buttonClick").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "switch_002.ogg");
            so.FindProperty("puzzleSolved").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "confirmation_002.ogg");
            so.FindProperty("puzzleFail").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "error_004.ogg");
            so.FindProperty("keyPickup").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "glass_001.ogg");
            so.FindProperty("doorLocked").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "impactMetal_002.ogg");
            so.FindProperty("doorUnlock").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "doorClose_001.ogg");
            so.FindProperty("doorOpen").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "doorOpen_000.ogg");
            so.FindProperty("typewriterBlip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tick_001.ogg");
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[EnvironmentPolisher] AudioManager added to scene with all 11 SFX clips wired! Press Ctrl+S to save.");
        }

        [MenuItem("MadMantra/Setup Door Unlock Key")]
        public static void SetupDoorUnlockKey()
        {
            const string doorPrefabPath = "Assets/_Project/Prefabs/Interactables/SecurityDoor.prefab";
            const string cabinetPrefabPath = "Assets/_Project/Prefabs/Interactables/Cabinet.prefab";
            Material keyMat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("4525e712f9254ce419ddd394ca797cb5"));
            Material lockMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "M_DoorFrame.mat");

            // 1. Add LockCylinder + UnlockKey to SecurityDoor.prefab
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(doorPrefabPath);
            try
            {
                Transform hinge = prefabRoot.transform.Find("Hinge");
                if (hinge == null) { Debug.LogError("[EnvironmentPolisher] Hinge not found in SecurityDoor.prefab"); return; }

                Transform oldLock = hinge.Find("LockCylinder");
                if (oldLock != null) Object.DestroyImmediate(oldLock.gameObject);
                Transform oldKey = hinge.Find("UnlockKey");
                if (oldKey != null) Object.DestroyImmediate(oldKey.gameObject);

                GameObject lockCyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lockCyl.name = "LockCylinder";
                lockCyl.transform.SetParent(hinge, false);
                lockCyl.transform.localPosition = new Vector3(1.02f, 1.05f, -0.085f);
                lockCyl.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                lockCyl.transform.localScale = new Vector3(0.075f, 0.018f, 0.075f);
                Object.DestroyImmediate(lockCyl.GetComponent<Collider>());
                if (lockMat != null) lockCyl.GetComponent<Renderer>().sharedMaterial = lockMat;

                GameObject keyRoot = new GameObject("UnlockKey");
                keyRoot.transform.SetParent(hinge, false);
                keyRoot.transform.localPosition = new Vector3(1.02f, 1.05f, -0.105f);
                keyRoot.transform.localRotation = Quaternion.identity;
                BuildKeyVisual(keyRoot.transform, keyMat);
                keyRoot.SetActive(false);

                Transform oldFocus = prefabRoot.transform.Find("UnlockFocusPoint");
                if (oldFocus != null) Object.DestroyImmediate(oldFocus.gameObject);
                GameObject focusGo = new GameObject("UnlockFocusPoint");
                focusGo.transform.SetParent(prefabRoot.transform, false);
                focusGo.transform.localPosition = new Vector3(0.44f, 1.08f, -0.58f);
                focusGo.transform.localRotation = Quaternion.Euler(3f, 0f, 0f);

                Door door = hinge.GetComponent<Door>();
                if (door != null)
                {
                    SerializedObject so = new SerializedObject(door);
                    so.FindProperty("lockMode").enumValueIndex = (int)LockMode.Keycard;
                    so.FindProperty("canClose").boolValue = true;
                    so.FindProperty("unlockKeyVisual").objectReferenceValue = keyRoot.transform;
                    so.FindProperty("unlockFocusPoint").objectReferenceValue = focusGo.transform;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, doorPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            // 2. Apply the exact same key model to SecurityKey inside Cabinet.prefab
            GameObject cabRoot = PrefabUtility.LoadPrefabContents(cabinetPrefabPath);
            try
            {
                Transform keyPickup = cabRoot.transform.Find("SecurityKey");
                if (keyPickup != null)
                {
                    // Hide old keycard box renderer (keep its collider for easy pickup raycast)
                    foreach (Transform child in keyPickup)
                    {
                        if (child.name == "KeyModel") Object.DestroyImmediate(child.gameObject);
                        else
                        {
                            Renderer r = child.GetComponent<Renderer>();
                            if (r != null) r.enabled = false;
                        }
                    }

                    GameObject keyModel = new GameObject("KeyModel");
                    keyModel.transform.SetParent(keyPickup, false);
                    keyModel.transform.localPosition = new Vector3(0f, 0f, 0.03f);
                    keyModel.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    keyModel.transform.localScale = Vector3.one * 1.35f;
                    BuildKeyVisual(keyModel.transform, keyMat);
                }

                PrefabUtility.SaveAsPrefabAsset(cabRoot, cabinetPrefabPath);
                Debug.Log("[EnvironmentPolisher] Applied matching Key model to both SecurityDoor.prefab and Cabinet.prefab!");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(cabRoot);
            }
        }

        private static void BuildKeyVisual(Transform parent, Material keyMat)
        {
            CreateBox("KeyBow", parent, new Vector3(0f, 0f, -0.075f), new Vector3(0.018f, 0.075f, 0.06f), keyMat);
            CreateBox("KeyBlade", parent, new Vector3(0f, 0f, 0.005f), new Vector3(0.012f, 0.022f, 0.12f), keyMat);
            CreateBox("KeyTooth", parent, new Vector3(0f, -0.018f, 0.042f), new Vector3(0.012f, 0.02f, 0.022f), keyMat);
        }

        [MenuItem("MadMantra/Apply 3D Hand to Player")]
        public static void ApplyHandToPlayer()
        {
            const string handObjPath = "Assets/_Project/Models/Hand/Cybernetic_Arm.obj";
            const string handTexPath = "Assets/_Project/Models/Hand/textures/Color_f897eebf-ed7a-498e-9800-52e4c6442e88_0.jpeg";
            const string handNormalPath = "Assets/_Project/Models/Hand/textures/NormalGL_f897eebf-ed7a-498e-9800-52e4c6442e88_2.png";
            const string playerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

            GameObject handModel = AssetDatabase.LoadAssetAtPath<GameObject>(handObjPath);
            if (handModel == null)
            {
                Debug.LogError("[EnvironmentPolisher] Cybernetic_Arm.obj not found at: " + handObjPath);
                return;
            }

            // Ensure normal map is imported as NormalMap
            TextureImporter normImp = AssetImporter.GetAtPath(handNormalPath) as TextureImporter;
            if (normImp != null && normImp.textureType != TextureImporterType.NormalMap)
            {
                normImp.textureType = TextureImporterType.NormalMap;
                normImp.SaveAndReimport();
            }

            // Create / update Cybernetic Arm PBR material
            string matPath = MatDir + "M_CyberneticArm.mat";
            Material handMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (handMat == null)
            {
                handMat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(handMat, matPath);
            }
            handMat.SetFloat("_Metallic", 0.65f);
            handMat.SetFloat("_Glossiness", 0.55f);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(handTexPath);
            if (tex != null) handMat.mainTexture = tex;
            Texture2D norm = AssetDatabase.LoadAssetAtPath<Texture2D>(handNormalPath);
            if (norm != null) { handMat.EnableKeyword("_NORMALMAP"); handMat.SetTexture("_BumpMap", norm); }
            EditorUtility.SetDirty(handMat);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(playerPrefabPath);
            try
            {
                Transform holder = prefabRoot.transform.Find("CameraMount/PlayerHandPlaceholder");
                if (holder == null)
                {
                    Debug.LogError("[EnvironmentPolisher] PlayerHandPlaceholder not found in Player.prefab");
                    return;
                }

                // Right-hand FPV placement (wrist/forearm tucked neatly into bottom-right corner)
                holder.localPosition = new Vector3(0.28f, -0.18f, 0.44f);
                holder.localRotation = Quaternion.identity;

                // Clean previous hand models and hide placeholder cubes
                for (int i = holder.childCount - 1; i >= 0; i--)
                {
                    Transform child = holder.GetChild(i);
                    if (child.name == "Model_GlovedHand" || child.name == "Model_RightHand" || child.name == "Model_LeftHand" || child.name == "Model_CyberneticArm")
                        Object.DestroyImmediate(child.gameObject);
                    else
                        child.gameObject.SetActive(false);
                }

                // Arm is vertical along Y (-0.5..+0.5): +75X tilts forward, +155Z turns palm down/inward
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(handModel);
                inst.name = "Model_CyberneticArm";
                inst.transform.SetParent(holder, false);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.Euler(75f, -10f, 155f);
                inst.transform.localScale = Vector3.one * 0.62f;
                ApplyMaterial(inst, handMat);

                foreach (Collider c in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

                // Update PlayerHand offsets for single right-hand Cybernetic Arm
                MadMantra.Player.PlayerHand ph = holder.GetComponent<MadMantra.Player.PlayerHand>();
                if (ph != null)
                {
                    SerializedObject so = new SerializedObject(ph);
                    so.FindProperty("reachOffset").vector3Value = new Vector3(-0.06f, 0.05f, 0.16f);
                    so.FindProperty("pressTipOffset").vector3Value = new Vector3(0.04f, -0.04f, -0.24f);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, playerPrefabPath);
                Debug.Log("[EnvironmentPolisher] Applied Cybernetic Arm PBR model to Player.prefab!");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [MenuItem("MadMantra/Apply 3D Models & Room Polish")]
        public static void ApplyModelsAndPolish()
        {
            Material kenneyMat = GetOrCreateKenneyMaterial();
            Material matFrame = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "M_DoorFrame.mat");
            Material matTubeWarm = GetOrCreateEmissive("M_Lamp_Warm", new Color(1.0f, 0.92f, 0.78f), 1.6f);
            Material matTubeCool = GetOrCreateEmissive("M_Lamp_Cool", new Color(0.7f, 0.9f, 1.0f), 1.6f);
            Material matConduit = GetOrCreateEmissive("M_Conduit_Glow", new Color(0.15f, 0.75f, 0.9f), 0.8f);

            // 1. Rebuild clean Room Dressing root (idempotent)
            GameObject existing = GameObject.Find("--- ROOM DRESSING ---");
            if (existing != null) Object.DestroyImmediate(existing);
            GameObject root = new GameObject("--- ROOM DRESSING ---");

            // Ceiling fluorescent fixtures right above Room 1 & Room 2 lights
            BuildCeilingFixture("Lamp_Room1_Center", root.transform, new Vector3(0f, 2.94f, 0f), matFrame, matTubeWarm);
            BuildCeilingFixture("Lamp_Room1_Back", root.transform, new Vector3(0f, 2.94f, -2.5f), matFrame, matTubeWarm);
            BuildCeilingFixture("Lamp_Room2_Center", root.transform, new Vector3(0f, 2.94f, 8f), matFrame, matTubeCool);

            // Power conduit visually linking ControlPanel (-3.5, 0, 0) -> KeyCabinet (-4.78, 1.45, 0.2)
            CreateBox("Conduit_Floor", root.transform, new Vector3(-4.15f, 0.03f, 0.15f), new Vector3(1.3f, 0.05f, 0.05f), matConduit);
            CreateBox("Conduit_WallUp", root.transform, new Vector3(-4.76f, 0.72f, 0.15f), new Vector3(0.05f, 1.4f, 0.05f), matConduit);

            // Room 1: Sci-fi containers in back-right corner (SE)
            PlaceModel("container-tall.fbx", "Container_Tall", root.transform, new Vector3(3.8f, 0f, -3.7f), new Vector3(0f, -35f, 0f), 1.35f, kenneyMat, true);
            PlaceModel("container.fbx", "Container_Mid", root.transform, new Vector3(3.9f, 0f, -2.1f), new Vector3(0f, -15f, 0f), 1.35f, kenneyMat, true);
            PlaceModel("container-flat.fbx", "Container_Flat", root.transform, new Vector3(2.6f, 0f, -3.9f), new Vector3(0f, 12f, 0f), 1.35f, kenneyMat, true);

            // Room 1: Sci-fi wall displays & computer props
            PlaceModel("display-wall-wide.fbx", "Display_East", root.transform, new Vector3(4.74f, 1.3f, 0.5f), new Vector3(0f, -90f, 0f), 1.5f, kenneyMat, false);
            PlaceModel("display-wall.fbx", "Display_West_Back", root.transform, new Vector3(-4.74f, 1.3f, -2.6f), new Vector3(0f, 90f, 0f), 1.4f, kenneyMat, false);
            PlaceModel("wall-switch.fbx", "WallSwitch_Door", root.transform, new Vector3(1.1f, 1.15f, 4.78f), new Vector3(0f, 180f, 0f), 1.4f, kenneyMat, false);
            PlaceModel("computer-system.fbx", "ServerRack_NE", root.transform, new Vector3(3.9f, 0f, 3.8f), new Vector3(0f, -135f, 0f), 1.4f, kenneyMat, true);

            // Room 1: 3D pipes running lengthwise (Z) along upper East & West walls
            for (float z = -3.6f; z <= 3.6f; z += 1.8f)
            {
                PlaceModel("pipe-ring-colored.fbx", $"Pipe_West_{z:F1}", root.transform, new Vector3(-4.68f, 2.55f, z), new Vector3(90f, 0f, 0f), 1.8f, kenneyMat, false);
                PlaceModel("pipe-ring-colored.fbx", $"Pipe_East_{z:F1}", root.transform, new Vector3(4.68f, 2.55f, z), new Vector3(90f, 0f, 0f), 1.8f, kenneyMat, false);
            }

            // Room 1: Corner structural pillars
            PlaceModel("wall-pillar.fbx", "Pillar_NW", root.transform, new Vector3(-4.55f, 0f, 4.55f), new Vector3(0f, 45f, 0f), 1.5f, kenneyMat, false);
            PlaceModel("wall-pillar.fbx", "Pillar_NE", root.transform, new Vector3(4.55f, 0f, 4.55f), new Vector3(0f, -45f, 0f), 1.5f, kenneyMat, false);
            PlaceModel("wall-pillar.fbx", "Pillar_SW", root.transform, new Vector3(-4.55f, 0f, -4.55f), new Vector3(0f, 135f, 0f), 1.5f, kenneyMat, false);
            PlaceModel("wall-pillar.fbx", "Pillar_SE", root.transform, new Vector3(4.55f, 0f, -4.55f), new Vector3(0f, -135f, 0f), 1.5f, kenneyMat, false);

            // Room 2: Sci-fi command bay furniture around NPC (at 0, 0, 8)
            PlaceModel("table-display-planet.fbx", "Room2_HoloTable", root.transform, new Vector3(-2.4f, 0f, 8.2f), new Vector3(0f, 90f, 0f), 1.4f, kenneyMat, true);
            PlaceModel("chair-armrest-headrest.fbx", "Room2_Chair", root.transform, new Vector3(-1.4f, 0f, 8.2f), new Vector3(0f, -90f, 0f), 1.3f, kenneyMat, true);
            PlaceModel("computer-wide.fbx", "Room2_Console", root.transform, new Vector3(2.8f, 0f, 8.5f), new Vector3(0f, -90f, 0f), 1.4f, kenneyMat, true);
            PlaceModel("display-wall-wide.fbx", "Room2_Display_East", root.transform, new Vector3(3.75f, 1.35f, 7.6f), new Vector3(0f, -90f, 0f), 1.5f, kenneyMat, false);
            PlaceModel("container-wide.fbx", "Room2_Crate", root.transform, new Vector3(2.9f, 0f, 6.2f), new Vector3(0f, 180f, 0f), 1.3f, kenneyMat, true);

            // 2. Upgrade SecurityDoor_Room1 visual mesh inside Hinge (keeps Door.cs + BoxCollider untouched)
            UpgradeDoorVisual("SecurityDoor_Room1", "door-single-closed.fbx", kenneyMat);

            // 3. Keep solid brushed-metal DeskStand on ControlPanel_Station (remove floating base overlap)
            RestoreControlPanelStand("ControlPanel_Station");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[EnvironmentPolisher] Applied Kenney Space Station 3D models & room dressing! Press Ctrl+S to save.");
        }

        private static void UpgradeDoorVisual(string doorName, string fbxName, Material mat)
        {
            GameObject doorGo = GameObject.Find(doorName);
            if (doorGo == null) return;
            Transform hinge = doorGo.transform.Find("Hinge");
            Transform panel = doorGo.transform.Find("Hinge/DoorPanel");
            if (hinge == null || panel == null) return;

            Transform old1 = panel.Find("Model_DoorVisual");
            if (old1 != null) Object.DestroyImmediate(old1.gameObject);
            Transform old2 = hinge.Find("Model_DoorVisual");
            if (old2 != null) Object.DestroyImmediate(old2.gameObject);

            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + fbxName);
            if (fbx == null) return;

            // Parent directly under Hinge (1:1:1 scale) and auto-fit exact DoorPanel bounds
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            inst.name = "Model_DoorVisual";
            inst.transform.SetParent(hinge, false);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
            ApplyMaterial(inst, mat);

            Bounds target = new Bounds(panel.position, Vector3.zero);
            BoxCollider col = panel.GetComponent<BoxCollider>();
            Vector3 targetSize = col != null
                ? Vector3.Scale(col.size, panel.lossyScale)
                : panel.lossyScale;

            Bounds b = GetCombinedBounds(inst);
            if (b.size.x > 0.001f && b.size.y > 0.001f && b.size.z > 0.001f)
            {
                inst.transform.localScale = new Vector3(
                    targetSize.x / b.size.x,
                    targetSize.y / b.size.y,
                    Mathf.Max(targetSize.z, 0.14f) / b.size.z
                );
                Bounds fitted = GetCombinedBounds(inst);
                inst.transform.position += (panel.position - fitted.center);
            }

            // Keep BoxCollider on DoorPanel for raycast/collision, hide plain cube renderer
            MeshRenderer mr = panel.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        private static Bounds GetCombinedBounds(GameObject go)
        {
            Renderer[] rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }

        private static void RestoreControlPanelStand(string panelName)
        {
            GameObject panelGo = GameObject.Find(panelName);
            if (panelGo == null) return;
            Transform oldVisual = panelGo.transform.Find("Model_ConsoleBase");
            if (oldVisual != null) Object.DestroyImmediate(oldVisual.gameObject);
            Transform stand = panelGo.transform.Find("DeskStand");
            if (stand != null)
            {
                MeshRenderer mr = stand.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = true;
            }
        }

        private static GameObject PlaceModel(string fbxName, string instanceName, Transform parent, Vector3 pos, Vector3 euler, float scale, Material mat, bool addCollider)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + fbxName);
            if (fbx == null)
            {
                Debug.LogWarning($"[EnvironmentPolisher] Model not found: {ModelDir + fbxName}");
                return null;
            }

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            go.name = instanceName;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = Vector3.one * scale;
            ApplyMaterial(go, mat);

            if (addCollider)
            {
                foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.GetComponent<Collider>() == null) mf.gameObject.AddComponent<BoxCollider>();
                }
            }
            return go;
        }

        private static void ApplyMaterial(GameObject go, Material mat)
        {
            if (mat == null) return;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
        }

        private static void BuildCeilingFixture(string name, Transform parent, Vector3 pos, Material housingMat, Material tubeMat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            CreateBox("Housing", go.transform, new Vector3(0f, 0.03f, 0f), new Vector3(1.2f, 0.06f, 0.24f), housingMat);
            CreateBox("Tube", go.transform, new Vector3(0f, -0.02f, 0f), new Vector3(1.1f, 0.04f, 0.14f), tubeMat);
        }

        private static void CreateBox(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        private static Material GetOrCreateKenneyMaterial()
        {
            string path = MatDir + "M_SpaceStation.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                mat.SetFloat("_Metallic", 0.35f);
                mat.SetFloat("_Glossiness", 0.45f);
                AssetDatabase.CreateAsset(mat, path);
            }
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ModelDir + "Textures/colormap.png");
            if (tex != null) mat.mainTexture = tex;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateEmissive(string name, Color color, float intensity)
        {
            string path = MatDir + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * intensity);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }
    }
}
#endif
