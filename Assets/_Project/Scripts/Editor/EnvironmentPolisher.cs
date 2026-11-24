#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MadMantra.Editor
{
    public static class EnvironmentPolisher
    {
        private const string ModelDir = "Assets/_Project/Models/";
        private const string MatDir = "Assets/_Project/Materials/";

        [MenuItem("MadMantra/Apply 3D Hand to Player")]
        public static void ApplyHandToPlayer()
        {
            const string handFbxPath = "Assets/_Project/Models/first-person-arm-hand-fingerless-gloves/source/hand final.fbx";
            const string handTexPath = "Assets/_Project/Models/first-person-arm-hand-fingerless-gloves/textures/texture_basecolor.png";
            const string playerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

            GameObject handFbx = AssetDatabase.LoadAssetAtPath<GameObject>(handFbxPath);
            if (handFbx == null)
            {
                Debug.LogError("[EnvironmentPolisher] Hand FBX not found at: " + handFbxPath);
                return;
            }

            // Create / update gloved hand material
            string matPath = MatDir + "M_PlayerHand_Gloved.mat";
            Material handMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (handMat == null)
            {
                handMat = new Material(Shader.Find("Standard"));
                handMat.SetFloat("_Metallic", 0.05f);
                handMat.SetFloat("_Glossiness", 0.25f);
                AssetDatabase.CreateAsset(handMat, matPath);
            }
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(handTexPath);
            if (tex != null) handMat.mainTexture = tex;
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

                // Hide the 4 placeholder cubes (Forearm, Palm, Thumb, IndexFinger)
                foreach (Transform child in holder)
                {
                    if (child.name == "Model_GlovedHand") Object.DestroyImmediate(child.gameObject);
                    else child.gameObject.SetActive(false);
                }

                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(handFbx);
                inst.name = "Model_GlovedHand";
                inst.transform.SetParent(holder, false);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one;
                ApplyMaterial(inst, handMat);

                // Remove any colliders so hand never blocks raycasts
                foreach (Collider c in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

                // Auto-fit longest dimension to ~0.58m and angle forward with cuff tucked off-screen
                Bounds b = GetCombinedBounds(inst);
                float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (maxDim > 0.001f)
                {
                    float s = 0.58f / maxDim;
                    inst.transform.localScale = Vector3.one * s;
                    inst.transform.localRotation = Quaternion.Euler(6f, 42f, -8f);
                    Bounds fitted = GetCombinedBounds(inst);
                    inst.transform.position += (holder.position - fitted.center);
                    inst.transform.localPosition += new Vector3(0.06f, -0.04f, -0.14f);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, playerPrefabPath);
                Debug.Log("[EnvironmentPolisher] Applied gloved 3D hand to Player.prefab! Adjust Model_GlovedHand localRotation in Player.prefab if needed.");
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
