using UnityEngine;

/// <summary>
/// Side-view dollhouse setup: cross-section house + static orthographic camera
/// (Neighbours from Hell style).
/// </summary>
[ExecuteAlways]
public class Scene2D5Bootstrap : MonoBehaviour
{
    const string HouseRootName = "House";

    [Header("House")]
    [SerializeField] Material floorMaterial;
    [SerializeField] Material wallMaterial;
    [SerializeField] float houseWidth = 14f;
    [SerializeField] float houseDepth = 3f;
    [SerializeField] float floorHeight = 3f;
    [SerializeField] int floorCount = 2;
    [SerializeField] float wallThickness = 0.2f;

    [Header("Camera")]
    [SerializeField] Vector3 cameraPosition = new(0f, 3f, -15f);
    [SerializeField] float orthographicSize = 4.5f;

    Transform houseRoot;

    void OnEnable()
    {
        EnsureMaterials();
        BuildHouse();
        SetupCamera();
    }

    void OnDisable()
    {
        if (!Application.isPlaying)
            ClearHouse();
    }

    void EnsureMaterials()
    {
#if UNITY_EDITOR
        if (floorMaterial == null)
            floorMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RoomFloor.mat");

        if (wallMaterial == null)
            wallMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RoomWall.mat");
#endif
    }

    void BuildHouse()
    {
        ClearHouse();

        houseRoot = new GameObject(HouseRootName).transform;
        houseRoot.SetParent(transform);
        houseRoot.localPosition = Vector3.zero;

        var totalHeight = floorHeight * floorCount;
        var depthCenter = houseDepth * 0.5f - wallThickness * 0.5f;

        CreatePart(
            "BackWall",
            new Vector3(0f, totalHeight * 0.5f, depthCenter),
            new Vector3(houseWidth, totalHeight + wallThickness, wallThickness),
            wallMaterial);

        CreatePart(
            "Wall_Left",
            new Vector3(-houseWidth * 0.5f, totalHeight * 0.5f, houseDepth * 0.5f),
            new Vector3(wallThickness, totalHeight + wallThickness, houseDepth),
            wallMaterial);

        CreatePart(
            "Wall_Right",
            new Vector3(houseWidth * 0.5f, totalHeight * 0.5f, houseDepth * 0.5f),
            new Vector3(wallThickness, totalHeight + wallThickness, houseDepth),
            wallMaterial);

        CreatePart(
            "Ceiling",
            new Vector3(0f, totalHeight + wallThickness * 0.5f, houseDepth * 0.5f),
            new Vector3(houseWidth, wallThickness, houseDepth),
            wallMaterial);

        for (var i = 0; i < floorCount; i++)
        {
            var y = i * floorHeight - wallThickness * 0.5f;
            CreatePart(
                i == 0 ? "Floor_Ground" : $"Floor_{i + 1}",
                new Vector3(0f, y, houseDepth * 0.5f),
                new Vector3(houseWidth, wallThickness, houseDepth),
                floorMaterial);
        }
    }

    void SetupCamera()
    {
        var camera = Camera.main;
        if (camera == null)
            return;

        camera.transform.SetPositionAndRotation(cameraPosition, Quaternion.identity);
        camera.orthographic = true;
        camera.orthographicSize = orthographicSize;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;

        if (camera.TryGetComponent(out StaticCamera2D5 staticCamera))
            staticCamera.ConfigureSideView(cameraPosition, orthographicSize);
        else if (Application.isPlaying)
            camera.gameObject.AddComponent<StaticCamera2D5>().ConfigureSideView(cameraPosition, orthographicSize);
    }

    void CreatePart(string name, Vector3 localPosition, Vector3 size, Material material)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(houseRoot);
        part.transform.localPosition = localPosition;
        part.transform.localScale = size;

        if (material != null)
            part.GetComponent<Renderer>().sharedMaterial = material;
    }

    void ClearHouse()
    {
        var existing = transform.Find(HouseRootName);
        if (existing == null)
            return;

        if (Application.isPlaying)
            Destroy(existing.gameObject);
        else
            DestroyImmediate(existing.gameObject);

        houseRoot = null;
    }
}
