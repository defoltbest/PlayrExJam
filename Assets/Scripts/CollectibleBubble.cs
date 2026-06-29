using UnityEngine;
using TMPro;

/// <summary>
/// World-space billboard bubble showing "Press E" prompt.
/// Always faces the camera. Attached to a child Canvas of the collectible item.
/// </summary>
public class CollectibleBubble : MonoBehaviour
{
    [SerializeField] private GameObject bubbleRoot;
    [SerializeField] private TextMeshProUGUI label;

    private Transform _cameraTransform;

    void Awake()
    {
        if (bubbleRoot == null)
            bubbleRoot = gameObject;

        bubbleRoot.SetActive(false);
    }

    void Start()
    {
        _cameraTransform = Camera.main?.transform;
    }

    void LateUpdate()
    {
        if (_cameraTransform == null)
        {
            _cameraTransform = Camera.main?.transform;
            return;
        }

        if (!bubbleRoot.activeSelf)
            return;

        // Billboard: face the camera
        var lookPos = transform.position + _cameraTransform.rotation * Vector3.forward;
        transform.LookAt(lookPos, _cameraTransform.rotation * Vector3.up);
    }

    public void Show()
    {
        bubbleRoot.SetActive(true);
    }

    public void Hide()
    {
        bubbleRoot.SetActive(false);
    }
}