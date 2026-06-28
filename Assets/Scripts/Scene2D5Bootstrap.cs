using UnityEngine;

/// <summary>
/// Apartment layout template (1-bedroom + stairwell, cutaway) + isometric camera preset.
/// Does not rebuild automatically — edit House in the scene and save (Ctrl+S).
///
/// Layout (top-down, X = ширина, Z = глубина, фасад открыт на стороне камеры):
///   back  (Z 4..8):  Bedroom | Kitchen | Bathroom
///   front (Z 0..4):  Living  |  Hallway
///   Stairwell (подъезд): X 6..10, Z 0..8, со ступенями (справа).
/// </summary>
public class Scene2D5Bootstrap : MonoBehaviour
{
    const string HouseRootName = "House";

    [Header("Materials")]
    [SerializeField] Material floorMaterialWood;
    [SerializeField] Material floorMaterialTile;
    [SerializeField] Material wallMaterial;

    [Header("Prefabs")]
    [SerializeField] GameObject doorPrefab;

    [Header("Dimensions")]
    [SerializeField] float wallThickness = 0.2f;
    [SerializeField] float doorWidth = 1.2f;

    // Масштаб планировки по X/Z. Площадь растёт как layoutScale^2 (√2 => площадь ×2).
    // Толщина стен, высота стен и размер дверей НЕ масштабируются.
    [Header("Layout")]
    [SerializeField] float layoutScale = 1.4142135f;

    Transform doorsRoot;

    [Header("Isometric camera preset")]
    [SerializeField] Vector3 cameraLookAt = new(1f, 1.5f, 4f);
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

        if (doorPrefab == null)
            doorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Door.prefab");
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
        BuildFloor(hallway, "Floor", 2f, 6f, 0f, 4f, floorMaterialTile);

        var living = CreateGroup(root, "Living");
        BuildFloor(living, "Floor", -8f, 2f, 0f, 4f, floorMaterialWood);

        var bathroom = CreateGroup(root, "Bathroom");
        BuildFloor(bathroom, "Floor", 2f, 6f, 4f, 8f, floorMaterialTile);

        var kitchen = CreateGroup(root, "Kitchen");
        BuildFloor(kitchen, "Floor", -2.5f, 2f, 4f, 8f, floorMaterialTile);

        var bedroom = CreateGroup(root, "Bedroom");
        BuildFloor(bedroom, "Floor", -8f, -2.5f, 4f, 8f, floorMaterialWood);
    }

    // Высота полноразмерной (внешней) стены и низкой (внутренней) стены.
    const float TallWall = 3f;
    const float LowWall = 1f;

    void BuildOuterWalls(Transform root)
    {
        var shell = CreateGroup(root, "Walls_Outer");

        // Полноразмерный периметр (высота 3, стоит на полу).
        BuildWallX(shell, "BackWall", 8f, -8f, 10f, TallWall, TallWall * 0.5f);
        BuildWallZ(shell, "RightWall", 10f, 0f, 8f, TallWall, TallWall * 0.5f);
        BuildWallZ(shell, "LeftWall", -7.92f, 0f, 8f, TallWall, TallWall * 0.5f);

        // Заполнители приямка/фасада (высота 3, опущены под уровень пола).
        BuildWallX(shell, "FrontPitWall", 0.03f, -8f, 10f, TallWall, -1f);
        BuildWallZ(shell, "RightPitWall", 10f, 0f, 8f, TallWall, -1.1f);

        // Низкая стена между подъездом и квартирой с дверью в прихожую.
        BuildWallZ(shell, "Wall_Entrance", 6f, 0f, 8f, LowWall, 0f, 2f);
    }

    void BuildPartitions(Transform root)
    {
        var inner = CreateGroup(root, "Walls_Inner");

        // Внутренние перегородки — низкие (высота 1, по центру уровня пола), с дверями.
        // Прихожая | Гостиная
        BuildWallZ(inner, "Wall_Hall_Living", 2f, 0f, 4f, LowWall, 0f, 2f);
        // Прихожая | Ванная
        BuildWallX(inner, "Wall_Hall_Bath", 4f, 2f, 6f, LowWall, 0f, 4f);
        // Гостиная | Кухня
        BuildWallX(inner, "Wall_Living_Kitchen", 4f, -2.5f, 2f, LowWall, 0f, -0.25f);
        // Гостиная | Спальня
        BuildWallX(inner, "Wall_Living_Bedroom", 4f, -8f, -2.5f, LowWall, 0f, -5.25f);
        // Ванная | Кухня
        BuildWallZ(inner, "Wall_Bath_Kitchen", 2f, 4f, 8f, LowWall, 0f, 6f);
        // Кухня | Спальня
        BuildWallZ(inner, "Wall_Kitchen_Bedroom", -2.5f, 4f, 8f, LowWall, 0f, 6f);
    }

    void BuildStairwell(Transform root)
    {
        var stairwell = CreateGroup(root, "Stairwell");

        // Площадка подъезда на уровне квартиры (передняя часть). X/Z масштабируются, толщина пола — нет.
        CreatePart(stairwell, "Floor",
            new Vector3(8f * layoutScale, -wallThickness * 0.5f, 2.27f * layoutScale),
            new Vector3(4f * layoutScale, wallThickness, 4.51f * layoutScale),
            floorMaterialTile);

        // Опущенная лестница: ступени стоят на дне приямка (pitFloorY) и поднимаются к уровню квартиры.
        // Высота ступеней (riser/pit) НЕ масштабируется, X/Z — масштабируются.
        const int steps = 6;
        const float stepDepth = 0.55f;
        const float startZ = 7.2f;
        const float pitFloorY = -2.74f;
        const float riser = 0.5f;
        var stepWidth = 3.73f;
        var stepX = 8f;

        for (var i = 0; i < steps; i++)
        {
            var height = riser * (i + 1);
            CreatePart(stairwell, $"Step_{i + 1}",
                new Vector3(stepX * layoutScale, pitFloorY + height * 0.5f, (startZ - i * stepDepth) * layoutScale),
                new Vector3(stepWidth * layoutScale, height, stepDepth * layoutScale),
                wallMaterial);
        }
    }

    // Стена вдоль оси X (фиксированный Z). X/Z масштабируются, толщина/высота — нет. Необязательный проём по X.
    void BuildWallX(Transform parent, string name, float z, float xMin, float xMax, float height, float yCenter, float? doorAt = null)
    {
        z *= layoutScale;
        xMin *= layoutScale;
        xMax *= layoutScale;

        if (doorAt == null)
        {
            CreatePart(parent, name,
                new Vector3((xMin + xMax) * 0.5f, yCenter, z),
                new Vector3(xMax - xMin, height, wallThickness),
                wallMaterial);
            return;
        }

        var d = doorAt.Value * layoutScale;
        var a1 = d - doorWidth * 0.5f;
        var b0 = d + doorWidth * 0.5f;
        if (a1 - xMin > 0.01f)
            CreatePart(parent, name + "_A",
                new Vector3((xMin + a1) * 0.5f, yCenter, z),
                new Vector3(a1 - xMin, height, wallThickness),
                wallMaterial);
        if (xMax - b0 > 0.01f)
            CreatePart(parent, name + "_B",
                new Vector3((b0 + xMax) * 0.5f, yCenter, z),
                new Vector3(xMax - b0, height, wallThickness),
                wallMaterial);

        // Префаб двери ориентирован под стену вдоль Z; поворот на 90° по Y разворачивает его под стену вдоль X.
        CreateDoor(name + "_Door", new Vector3(a1, 0f, z), Quaternion.Euler(0f, 90f, 0f));
    }

    // Стена вдоль оси Z (фиксированный X). X/Z масштабируются, толщина/высота — нет. Необязательный проём по Z.
    void BuildWallZ(Transform parent, string name, float x, float zMin, float zMax, float height, float yCenter, float? doorAt = null)
    {
        x *= layoutScale;
        zMin *= layoutScale;
        zMax *= layoutScale;

        if (doorAt == null)
        {
            CreatePart(parent, name,
                new Vector3(x, yCenter, (zMin + zMax) * 0.5f),
                new Vector3(wallThickness, height, zMax - zMin),
                wallMaterial);
            return;
        }

        var d = doorAt.Value * layoutScale;
        var a1 = d - doorWidth * 0.5f;
        var b0 = d + doorWidth * 0.5f;
        if (a1 - zMin > 0.01f)
            CreatePart(parent, name + "_A",
                new Vector3(x, yCenter, (zMin + a1) * 0.5f),
                new Vector3(wallThickness, height, a1 - zMin),
                wallMaterial);
        if (zMax - b0 > 0.01f)
            CreatePart(parent, name + "_B",
                new Vector3(x, yCenter, (b0 + zMax) * 0.5f),
                new Vector3(wallThickness, height, zMax - b0),
                wallMaterial);

        // Префаб двери уже ориентирован под стену вдоль Z (петля у края проёма, полотно вдоль +Z).
        CreateDoor(name + "_Door", new Vector3(x, 0f, a1), Quaternion.identity);
    }

    // Дверь — экземпляр Door.prefab (петля-корень с Door + полотно Panel).
    // Корень ставим у края проёма; поворот задаёт ориентацию (стена вдоль Z — identity, вдоль X — 90° по Y).
    void CreateDoor(string name, Vector3 hingeLocalPos, Quaternion localRotation)
    {
        if (doorPrefab == null)
        {
            Debug.LogWarning("Door.prefab не назначен — двери не созданы.");
            return;
        }

        GameObject instance = null;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(doorPrefab, doorsRoot);
#endif
        if (instance == null)
            instance = Instantiate(doorPrefab, doorsRoot);

        instance.name = name;
        instance.transform.localPosition = hingeLocalPos;
        instance.transform.localRotation = localRotation;
        instance.transform.localScale = Vector3.one;
    }

    // Пол комнаты. X/Z масштабируются (площадь растёт как layoutScale^2), толщина пола — нет.
    void BuildFloor(Transform parent, string name, float xMin, float xMax, float zMin, float zMax, Material material)
    {
        xMin *= layoutScale;
        xMax *= layoutScale;
        zMin *= layoutScale;
        zMax *= layoutScale;

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
