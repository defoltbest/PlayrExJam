using UnityEngine;

/// <summary>
/// Apartment layout template (1-bedroom + stairwell, cutaway) + isometric camera preset.
/// Does not rebuild automatically — edit House in the scene and save (Ctrl+S).
///
/// Layout (top-down, X = ширина, Z = глубина, фасад открыт на стороне камеры):
///   back  (Z 4..8):  Bathroom | Kitchen | Bedroom
///   front (Z 0..4):  Hallway  |     Living
///   Stairwell (подъезд): X -10..-6, Z 0..8, со ступенями.
/// </summary>
public class Scene2D5Bootstrap : MonoBehaviour
{
    const string HouseRootName = "House";

    [Header("Materials")]
    [SerializeField] Material floorMaterialWood;
    [SerializeField] Material floorMaterialTile;
    [SerializeField] Material wallMaterial;
    [SerializeField] Material doorMaterial;

    [Header("Dimensions")]
    [SerializeField] float floorHeight = 3f;
    [SerializeField] float wallThickness = 0.2f;
    [SerializeField] float doorWidth = 1.2f;
    [SerializeField] float doorThickness = 0.1f;

    Transform doorsRoot;

    [Header("Isometric camera preset")]
    [SerializeField] Vector3 cameraLookAt = new(-1f, 1.5f, 4f);
    [SerializeField] float isometricPitch = 35f;
    [SerializeField] float isometricYaw = -45f;
    [SerializeField] float cameraDistance = 28f;
    [SerializeField] float orthographicSize = 11f;

    [ContextMenu("Rebuild Apartment From Template")]
    public void RebuildHouseFromTemplate()
    {
        EnsureMaterials();
        var root = GetOrCreateHouseRoot();
        ClearHouseChildren(root);
        BuildApartment(root);
        EnsureDoorClicker();
    }

    void EnsureDoorClicker()
    {
        var camera = Camera.main;
        if (camera == null)
            return;

        if (!camera.TryGetComponent(out DoorClickController _))
            camera.gameObject.AddComponent<DoorClickController>();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(camera.gameObject);
#endif
    }

    [ContextMenu("Apply Isometric Camera Preset")]
    public void ApplyCameraPreset()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogWarning("Main Camera not found.");
            return;
        }

        var rotation = Quaternion.Euler(isometricPitch, isometricYaw, 0f);
        var offset = rotation * Vector3.back * cameraDistance;
        camera.transform.SetPositionAndRotation(cameraLookAt + offset, rotation);
        camera.orthographic = true;
        camera.orthographicSize = orthographicSize;

        if (!camera.TryGetComponent(out StaticCamera2D5 staticCamera))
            staticCamera = camera.gameObject.AddComponent<StaticCamera2D5>();

        staticCamera.SyncFromTransform();
        EnsureDoorClicker();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(camera.gameObject);
#endif
    }

    void EnsureMaterials()
    {
#if UNITY_EDITOR
        if (floorMaterialWood == null)
            floorMaterialWood = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RoomFloor.mat");

        if (floorMaterialTile == null)
            floorMaterialTile = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RoomFloorTile.mat");

        if (wallMaterial == null)
            wallMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RoomWall.mat");

        if (doorMaterial == null)
            doorMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RoomDoor.mat");
#endif
    }

    Transform GetOrCreateHouseRoot()
    {
        var existing = transform.Find(HouseRootName);
        if (existing != null)
            return existing;

        var houseObject = new GameObject(HouseRootName);
        houseObject.transform.SetParent(transform);
        houseObject.transform.localPosition = Vector3.zero;
        houseObject.transform.localRotation = Quaternion.identity;
        houseObject.transform.localScale = Vector3.one;
        return houseObject.transform;
    }

    void BuildApartment(Transform root)
    {
        doorsRoot = CreateGroup(root, "Doors");
        BuildFloors(root);
        BuildOuterWalls(root);
        BuildPartitions(root);
        BuildStairwell(root);
    }

    void BuildFloors(Transform root)
    {
        var hallway = CreateGroup(root, "Hallway");
        BuildFloor(hallway, "Floor", -6f, -2f, 0f, 4f, floorMaterialTile);

        var living = CreateGroup(root, "Living");
        BuildFloor(living, "Floor", -2f, 8f, 0f, 4f, floorMaterialWood);

        var bathroom = CreateGroup(root, "Bathroom");
        BuildFloor(bathroom, "Floor", -6f, -2f, 4f, 8f, floorMaterialTile);

        var kitchen = CreateGroup(root, "Kitchen");
        BuildFloor(kitchen, "Floor", -2f, 2.5f, 4f, 8f, floorMaterialTile);

        var bedroom = CreateGroup(root, "Bedroom");
        BuildFloor(bedroom, "Floor", 2.5f, 8f, 4f, 8f, floorMaterialWood);
    }

    void BuildOuterWalls(Transform root)
    {
        var shell = CreateGroup(root, "Walls_Outer");

        // Задняя стена (дальняя от камеры), общая для квартиры и подъезда.
        BuildWallX(shell, "BackWall", 8f, -10f, 8f);

        // Левая внешняя стена (дальняя), вдоль подъезда.
        BuildWallZ(shell, "LeftWall", -10f, 0f, 8f);

        // Стена между подъездом и квартирой с дверью в прихожую.
        BuildWallZ(shell, "Wall_Entrance", -6f, 0f, 8f, 2f);
    }

    void BuildPartitions(Transform root)
    {
        var inner = CreateGroup(root, "Walls_Inner");

        // Прихожая | Гостиная
        BuildWallZ(inner, "Wall_Hall_Living", -2f, 0f, 4f, 2f);
        // Прихожая | Ванная
        BuildWallX(inner, "Wall_Hall_Bath", 4f, -6f, -2f, -4f);
        // Гостиная | Кухня
        BuildWallX(inner, "Wall_Living_Kitchen", 4f, -2f, 2.5f, 0.25f);
        // Гостиная | Спальня
        BuildWallX(inner, "Wall_Living_Bedroom", 4f, 2.5f, 8f, 5.25f);
        // Ванная | Кухня
        BuildWallZ(inner, "Wall_Bath_Kitchen", -2f, 4f, 8f, 6f);
        // Кухня | Спальня
        BuildWallZ(inner, "Wall_Kitchen_Bedroom", 2.5f, 4f, 8f, 6f);
    }

    void BuildStairwell(Transform root)
    {
        var stairwell = CreateGroup(root, "Stairwell");

        // Площадка подъезда на уровне квартиры (передняя часть).
        CreatePart(stairwell, "Floor",
            new Vector3(-8f, -wallThickness * 0.5f, 2.27f),
            new Vector3(4f, wallThickness, 4.51f),
            floorMaterialTile);

        // Опущенная лестница: ступени стоят на дне приямка (pitFloorY) и поднимаются к уровню квартиры.
        const int steps = 6;
        const float stepDepth = 0.55f;
        const float startZ = 7.2f;
        const float pitFloorY = -2.74f;
        const float riser = 0.5f;
        var stepWidth = 5f;
        var stepX = -8f;

        for (var i = 0; i < steps; i++)
        {
            var height = riser * (i + 1);
            CreatePart(stairwell, $"Step_{i + 1}",
                new Vector3(stepX, pitFloorY + height * 0.5f, startZ - i * stepDepth),
                new Vector3(stepWidth, height, stepDepth),
                wallMaterial);
        }
    }

    // Стена вдоль оси X (фиксированный Z). Необязательный дверной проём по X.
    void BuildWallX(Transform parent, string name, float z, float xMin, float xMax, float? doorAt = null)
    {
        var wy = floorHeight * 0.5f;
        if (doorAt == null)
        {
            CreatePart(parent, name,
                new Vector3((xMin + xMax) * 0.5f, wy, z),
                new Vector3(xMax - xMin, floorHeight, wallThickness),
                wallMaterial);
            return;
        }

        var d = doorAt.Value;
        var a1 = d - doorWidth * 0.5f;
        var b0 = d + doorWidth * 0.5f;
        if (a1 - xMin > 0.01f)
            CreatePart(parent, name + "_A",
                new Vector3((xMin + a1) * 0.5f, wy, z),
                new Vector3(a1 - xMin, floorHeight, wallThickness),
                wallMaterial);
        if (xMax - b0 > 0.01f)
            CreatePart(parent, name + "_B",
                new Vector3((b0 + xMax) * 0.5f, wy, z),
                new Vector3(xMax - b0, floorHeight, wallThickness),
                wallMaterial);

        // Петля (корень с Door) у края проёма; полотно смещено вдоль +X.
        CreateDoor(name + "_Door",
            new Vector3(a1, 0f, z),
            new Vector3(doorWidth, floorHeight, doorThickness),
            new Vector3(doorWidth * 0.5f, floorHeight * 0.5f, 0f));
    }

    // Стена вдоль оси Z (фиксированный X). Необязательный дверной проём по Z.
    void BuildWallZ(Transform parent, string name, float x, float zMin, float zMax, float? doorAt = null)
    {
        var wy = floorHeight * 0.5f;
        if (doorAt == null)
        {
            CreatePart(parent, name,
                new Vector3(x, wy, (zMin + zMax) * 0.5f),
                new Vector3(wallThickness, floorHeight, zMax - zMin),
                wallMaterial);
            return;
        }

        var d = doorAt.Value;
        var a1 = d - doorWidth * 0.5f;
        var b0 = d + doorWidth * 0.5f;
        if (a1 - zMin > 0.01f)
            CreatePart(parent, name + "_A",
                new Vector3(x, wy, (zMin + a1) * 0.5f),
                new Vector3(wallThickness, floorHeight, a1 - zMin),
                wallMaterial);
        if (zMax - b0 > 0.01f)
            CreatePart(parent, name + "_B",
                new Vector3(x, wy, (b0 + zMax) * 0.5f),
                new Vector3(wallThickness, floorHeight, zMax - b0),
                wallMaterial);

        // Петля (корень с Door) у края проёма; полотно смещено вдоль +Z.
        CreateDoor(name + "_Door",
            new Vector3(x, 0f, a1),
            new Vector3(doorThickness, floorHeight, doorWidth),
            new Vector3(0f, floorHeight * 0.5f, doorWidth * 0.5f));
    }

    // Дверь: корневой объект-петля (с Door) у края проёма + дочернее полотно со смещением.
    // Door вращает только корень, поэтому перемещение корня сохраняет точку вращения и высоту.
    void CreateDoor(string name, Vector3 hingeLocalPos, Vector3 panelSize, Vector3 panelOffset)
    {
        var hinge = new GameObject(name);
        hinge.transform.SetParent(doorsRoot);
        hinge.transform.localPosition = hingeLocalPos;
        hinge.transform.localRotation = Quaternion.identity;
        hinge.transform.localScale = Vector3.one;
        hinge.AddComponent<Door>();

        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "Panel";
        panel.transform.SetParent(hinge.transform);
        panel.transform.localPosition = panelOffset;
        panel.transform.localScale = panelSize;
        if (doorMaterial != null)
            panel.GetComponent<Renderer>().sharedMaterial = doorMaterial;
    }

    void BuildFloor(Transform parent, string name, float xMin, float xMax, float zMin, float zMax, Material material)
    {
        CreatePart(parent, name,
            new Vector3((xMin + xMax) * 0.5f, -wallThickness * 0.5f, (zMin + zMax) * 0.5f),
            new Vector3(xMax - xMin, wallThickness, zMax - zMin),
            material);
    }

    static Transform CreateGroup(Transform parent, string name)
    {
        var group = new GameObject(name).transform;
        group.SetParent(parent);
        group.localPosition = Vector3.zero;
        group.localRotation = Quaternion.identity;
        group.localScale = Vector3.one;
        return group;
    }

    static void CreatePart(Transform root, string name, Vector3 localPosition, Vector3 size, Material material)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(root);
        part.transform.localPosition = localPosition;
        part.transform.localScale = size;

        if (material != null)
            part.GetComponent<Renderer>().sharedMaterial = material;
    }

    static void ClearHouseChildren(Transform root)
    {
        for (var i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }
}
