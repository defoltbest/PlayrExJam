using UnityEngine;

/// <summary>
/// Simple 2.5D setup: creates a ground plane, a player cube,
/// attaches PlayerController, and sets up isometric-lite camera.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("Ground")]
    [SerializeField] private Vector3 groundPosition = new Vector3(0f, -0.5f, 0f);
    [SerializeField] private Vector3 groundSize = new Vector3(20f, 1f, 20f);

    [Header("Player")]
    [SerializeField] private Vector3 playerStartPosition = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private Vector3 playerScale = new Vector3(1f, 1f, 1f);

    [Header("Camera")]
    [SerializeField] private Vector3 cameraPosition = new Vector3(0f, 15f, -12f);
    [SerializeField] private float orthographicSize = 8f;

    [ContextMenu("Build Scene")]
    public void BuildScene()
    {
        BuildGround();
        BuildPlayer();
        SetupCamera();
        Debug.Log("[GameBootstrap] Scene built! Click/tap on the ground to move.");
    }

    private void BuildGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = groundPosition;
        ground.transform.localScale = groundSize;

        var renderer = ground.GetComponent<Renderer>();
        renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            color = new Color(0.4f, 0.7f, 0.4f)
        };
    }

    private void BuildPlayer()
    {
        var player = GameObject.CreatePrimitive(PrimitiveType.Cube);
        player.name = "Player";
        player.transform.position = playerStartPosition;
        player.transform.localScale = playerScale;

        var renderer = player.GetComponent<Renderer>();
        renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            color = new Color(0.2f, 0.4f, 0.8f)
        };

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerInventory>();
    }

    private void SetupCamera()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogWarning("Main Camera not found.");
            return;
        }

        camera.transform.position = cameraPosition;
        camera.transform.LookAt(Vector3.zero);
        camera.orthographic = true;
        camera.orthographicSize = orthographicSize;
    }
}
