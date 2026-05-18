using UnityEngine;
using UnityEditor;
using System.IO;

public class PlaneMeshGenerator : EditorWindow
{
    int resolution = 10;
    float size = 10f;
    string meshName = "GeneratedPlane";
    string savePath = "Assets/Models";
    bool placeInScene = true;
    Material material = null;

    [MenuItem("Tools/Plane Mesh Generator")]
    public static void Open()
    {
        GetWindow<PlaneMeshGenerator>("Plane Mesh Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Plane Mesh Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        resolution = Mathf.Max(1, EditorGUILayout.IntField("Resolution (N)", resolution));
        size = Mathf.Max(0.01f, EditorGUILayout.FloatField("World Size", size));

        EditorGUILayout.Space();
        meshName = EditorGUILayout.TextField("Mesh Name", meshName);
        savePath = EditorGUILayout.TextField("Save Path", savePath);

        EditorGUILayout.Space();
        placeInScene = EditorGUILayout.Toggle("Place in Scene", placeInScene);
        if (placeInScene)
            material = (Material)EditorGUILayout.ObjectField("Material", material, typeof(Material), false);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            $"Grid: {resolution}x{resolution} quads\n" +
            $"Vertices: {(resolution + 1) * (resolution + 1)}\n" +
            $"Triangles: {resolution * resolution * 2}",
            MessageType.Info);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate"))
            Generate();
    }

    void Generate()
    {
        Mesh mesh = BuildMesh();

        // Save mesh asset
        if (!AssetDatabase.IsValidFolder(savePath))
            Directory.CreateDirectory(Application.dataPath + savePath.Substring("Assets".Length));

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{savePath}/{meshName}.asset");
        AssetDatabase.CreateAsset(mesh, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[PlaneMeshGenerator] Saved mesh to {assetPath}");

        if (placeInScene)
        {
            GameObject go = new GameObject(meshName);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;
            Undo.RegisterCreatedObjectUndo(go, "Place Plane Mesh");
            Selection.activeGameObject = go;
        }

        EditorGUIUtility.PingObject(mesh);
    }

    Mesh BuildMesh()
    {
        int n = resolution;
        int vertCount = (n + 1) * (n + 1);

        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        Vector3[] normals = new Vector3[vertCount];
        int[] triangles = new int[n * n * 6];

        float step = size / n;

        for (int z = 0; z <= n; z++)
        {
            for (int x = 0; x <= n; x++)
            {
                int i = z * (n + 1) + x;
                float fx = x * step - size * 0.5f;
                float fz = z * step - size * 0.5f;
                vertices[i] = new Vector3(fx, 0f, fz);
                uvs[i] = new Vector2((float)x / n, (float)z / n);
                normals[i] = Vector3.up;
            }
        }

        int tri = 0;
        for (int z = 0; z < n; z++)
        {
            for (int x = 0; x < n; x++)
            {
                int bl = z * (n + 1) + x;
                int br = bl + 1;
                int tl = bl + (n + 1);
                int tr = tl + 1;

                triangles[tri++] = bl;
                triangles[tri++] = tl;
                triangles[tri++] = tr;

                triangles[tri++] = bl;
                triangles[tri++] = tr;
                triangles[tri++] = br;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = meshName;

        if (vertCount > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        return mesh;
    }
}
