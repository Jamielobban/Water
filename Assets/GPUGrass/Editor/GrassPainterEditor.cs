using UnityEditor;
using UnityEngine;

namespace GPUGrass.Editor
{
    [CustomEditor(typeof(GpuGrass))]
    public sealed class GrassPainterEditor : UnityEditor.Editor
    {
        bool painting, stroke;
        float radius = 3, strength = .5f;
        int type, undoGroup;
        Vector3 lastPoint;
        bool hasPoint;
        GpuGrass Field => (GpuGrass)target;
        void OnEnable() { Undo.undoRedoPerformed += UndoPaint; }
        void OnDisable() { EndStroke(); Undo.undoRedoPerformed -= UndoPaint; }
        void UndoPaint() { if (target) { Field.RefreshPaint(); SceneView.RepaintAll(); } }
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Create Swirling Wind Map")) GrassWindMap.Create(Field);
            if (GUILayout.Button("Copy Wind Settings to Scene Grass / Flowers")) GrassWindMap.Share(Field);
            if (GUILayout.Button("Create Valley Mesh and Use It"))
            {
                var go = new GameObject("Grass Valley Surface");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, Field.gameObject.scene);
                Undo.RegisterCreatedObjectUndo(go, "Create grass valley");
                var valley = go.AddComponent<GrassValley>();
                valley.size = Field.fieldSize; valley.Rebuild();
                Undo.RecordObject(Field, "Assign valley surface");
                Field.surface = valley; Field.RefreshPaint(); EditorUtility.SetDirty(Field);
                foreach (var demo in Resources.FindObjectsOfTypeAll<GrassDemo>())
                    if (demo.grass == Field) { Undo.RecordObject(demo, "Disable flat demo ground"); demo.createGround = false; EditorUtility.SetDirty(demo); }
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Field.flowers ? "Flower Painting" : "Grass Painting", EditorStyles.boldLabel);
            if (!Field.flowers)
            {
                EditorGUILayout.LabelField("Map Presets", EditorStyles.boldLabel);
                if (GUILayout.Button("New Map: Three Grasses + Winding Path"))
                {
                    EndStroke();
                    GrassMapPresets.ApplyPathPreset(Field);
                }
                EditorGUILayout.HelpBox("Creates a new editable copy: short turf on the left, meadow in the middle, golden tall on the right, with a winding bare path across all three. Existing maps are preserved.", MessageType.None);
            }
            if (!Field.flowers && GUILayout.Button("Add Paintable Flower Layer"))
            {
                string path = EditorUtility.SaveFilePanelInProject("Save flower paint map", "Flower Paint", "asset", "Flowers use a separate map so grass remains underneath.");
                if (!string.IsNullOrEmpty(path))
                {
                    var map = CreateInstance<GrassPaintMap>();
                    AssetDatabase.CreateAsset(map, path);
                    var go = new GameObject("Paintable Flowers");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, Field.gameObject.scene);
                    Undo.RegisterCreatedObjectUndo(go, "Add flower layer");
                    var layer = go.AddComponent<GpuGrass>();
                    layer.flowers = true; layer.bladeCount = 12000;
                    layer.fieldSize = Field.fieldSize; layer.lodDistances = Field.lodDistances;
                    layer.wind = Field.wind; layer.paintMap = map;
                    layer.windFlowMap = Field.windFlowMap; layer.windDirection = Field.windDirection;
                    layer.flowInfluence = Field.flowInfluence; layer.directionVariation = Field.directionVariation;
                    layer.windPatternSize = Field.windPatternSize;
                    layer.surface = Field.surface; layer.slopeAlignment = Field.slopeAlignment; layer.maxSlope = Field.maxSlope;
                    EditorUtility.SetDirty(layer);
                    Selection.activeGameObject = go;
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.HelpBox("Paint on the assigned valley surface, or Y = 0 when no surface is assigned. Maps store top-down density and species; shared maps affect all their users. Keep the valley transform at world origin with no rotation or scale.", MessageType.Info);
            if (!Field.paintMap)
            {
                if (GUILayout.Button("Create Empty Paint Map"))
                {
                    string path = EditorUtility.SaveFilePanelInProject("Save grass paint map", "Grass Paint", "asset", "Choose where to save the painted grass.");
                    if (!string.IsNullOrEmpty(path))
                    {
                        var map = CreateInstance<GrassPaintMap>();
                        AssetDatabase.CreateAsset(map, path);
                        Undo.RecordObject(Field, "Assign grass paint map");
                        Field.paintMap = map; EditorUtility.SetDirty(Field); Field.RefreshPaint();
                        painting = true; SceneView.RepaintAll();
                    }
                }
                return;
            }
            painting = GUILayout.Toggle(painting, "Enable Scene Brush", "Button");
            type = GUILayout.Toolbar(type, Field.flowers ? new[] { "Daisy", "Poppy", "Tall Cosmos" } : new[] { "Short Turf", "Meadow", "Golden Tall" });
            radius = EditorGUILayout.Slider("Radius (meters)", radius, .25f, 15);
            strength = EditorGUILayout.Slider("Strength", strength, .01f, 1);
            EditorGUILayout.HelpBox("Left drag: paint selected type. Shift + drag: erase. Alt: orbit. Ctrl/Cmd + Z: undo. Save your scene after assigning a map; paint strokes save automatically.", MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fill Selected Type")) Fill(false);
                if (GUILayout.Button("Clear Grass")) Fill(true);
            }
        }
        Vector4 SelectedWeight() { var weight = Vector4.zero; weight[type] = 1; return weight; }
        void Fill(bool clear)
        {
            Undo.RegisterCompleteObjectUndo(Field.paintMap, clear ? "Clear grass" : "Fill grass");
            Vector4 value = clear ? Vector4.zero : SelectedWeight();
            for (int i = 0; i < Field.paintMap.pixels.Length; i++) Field.paintMap.pixels[i] = value;
            Changed(); AssetDatabase.SaveAssetIfDirty(Field.paintMap);
        }
        void Changed()
        {
            EditorUtility.SetDirty(Field.paintMap);
            // Other fields may share this map.
            foreach (var field in Resources.FindObjectsOfTypeAll<GpuGrass>())
                if (field.paintMap == Field.paintMap) field.RefreshPaint();
            SceneView.RepaintAll();
        }
        void EndStroke()
        {
            if (!stroke) return;
            stroke = false; hasPoint = false;
            Undo.CollapseUndoOperations(undoGroup);
            if (target && Field.paintMap) AssetDatabase.SaveAssetIfDirty(Field.paintMap);
        }
        void OnSceneGUI()
        {
            Event e = Event.current;
            if (e.rawType == EventType.MouseUp) EndStroke();
            if (!painting || !Field.paintMap || Application.isPlaying || e.alt) return;
            int control = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(control);
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 point;
            if (Field.surface)
            {
                if (!Field.surface.GetComponent<MeshCollider>().Raycast(ray, out RaycastHit hit, 10000)) return;
                point = hit.point;
            }
            else
            {
                if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance)) return;
                point = ray.GetPoint(distance);
            }
            float half = Mathf.Max(10, Field.fieldSize) * .5f;
            Handles.color = e.shift ? new Color(1, .3f, .2f, .9f) : new Color(.4f, 1, .2f, .9f);
            if (Field.surface)
            {
                var ring = new Vector3[65];
                var collider = Field.surface.GetComponent<MeshCollider>();
                for (int i = 0; i <= 64; i++)
                {
                    float angle = i * Mathf.PI * 2 / 64;
                    Vector3 p = point + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                    if (collider.Raycast(new Ray(new Vector3(p.x, 1000, p.z), Vector3.down), out RaycastHit ringHit, 2000)) p = ringHit.point;
                    ring[i] = p + Vector3.up * .035f;
                }
                Handles.DrawAAPolyLine(2, ring);
            }
            else Handles.DrawWireDisc(point + Vector3.up * .02f, Vector3.up, radius);
            if (e.type == EventType.MouseMove) SceneView.RepaintAll();
            if ((e.type != EventType.MouseDown && e.type != EventType.MouseDrag) || e.button != 0 || e.control || e.command) return;
            if (Mathf.Abs(point.x) > half || Mathf.Abs(point.z) > half) return;
            if (!stroke)
            {
                Undo.IncrementCurrentGroup(); undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Paint grass");
                Undo.RegisterCompleteObjectUndo(Field.paintMap, "Paint grass");
                stroke = true;
            }
            float spacing = Mathf.Max(radius * .15f, half * 2 / GrassPaintMap.Resolution * .25f);
            if (!hasPoint) { Dab(point, e.shift); lastPoint = point; hasPoint = true; }
            else
            {
                float travel = Vector3.Distance(lastPoint, point);
                int steps = Mathf.FloorToInt(travel / spacing);
                Vector3 start = lastPoint;
                for (int i = 1; i <= steps; i++) Dab(Vector3.Lerp(start, point, i * spacing / travel), e.shift);
                if (steps > 0) lastPoint = Vector3.Lerp(start, point, steps * spacing / travel);
            }
            Changed(); e.Use();
        }
        void Dab(Vector3 point, bool erase)
        {
            int n = GrassPaintMap.Resolution;
            float size = Mathf.Max(10, Field.fieldSize);
            Vector4 desired = erase ? Vector4.zero : SelectedWeight();
            int minX = Mathf.Clamp(Mathf.FloorToInt(((point.x - radius) / size + .5f) * (n - 1)), 0, n - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(((point.x + radius) / size + .5f) * (n - 1)), 0, n - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(((point.z - radius) / size + .5f) * (n - 1)), 0, n - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(((point.z + radius) / size + .5f) * (n - 1)), 0, n - 1);
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    float d = Vector2.Distance(new Vector2((x / (float)(n - 1) - .5f) * size, (y / (float)(n - 1) - .5f) * size), new Vector2(point.x, point.z));
                    float falloff = Mathf.Clamp01(1 - d / radius);
                    int index = y * n + x;
                    Field.paintMap.pixels[index] = Vector4.Lerp(Field.paintMap.pixels[index], desired, strength * falloff * falloff);
                }
        }
    }
}
