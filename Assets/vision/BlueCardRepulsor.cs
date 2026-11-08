using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// 掛在 XR Origin，讓 Blue_T_Target 提供紅、黃鳥群共用的驅離來源。
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class BlueCardRepulsor : MonoBehaviour
{
    [Header("藍卡追蹤")]
    [SerializeField] private ARTrackedImageManager imageManager = null;
    [SerializeField] private string blueTargetName = "Blue_T_Target";

    [Header("驅離設定")]
    [SerializeField, Min(0.01f)] private float influenceRadius = 0.15f;
    [SerializeField, Min(0f)] private float repulsionStrength = 8f;

    [Header("電腦預覽（只在 Unity Editor 生效）")]
    [SerializeField] private bool editorPreview = false;
    [SerializeField] private Transform previewTarget = null;

    private static BlueCardRepulsor activeInstance;
    private ARTrackedImageManager subscribedManager;
    private ARTrackedImage blueImage;
    private bool applicationPaused;

    private void Reset()
    {
        imageManager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        if (activeInstance != null && activeInstance != this &&
            activeInstance.isActiveAndEnabled)
        {
            Debug.LogWarning("場景中只需一個 BlueCardRepulsor。", this);
            enabled = false;
            return;
        }

        activeInstance = this;

        if (imageManager == null)
            imageManager = GetComponent<ARTrackedImageManager>();

        if (imageManager == null)
        {
            Debug.LogWarning("請在 BlueCardRepulsor 指定 Image Manager。", this);
            return;
        }

        subscribedManager = imageManager;
        subscribedManager.trackablesChanged.AddListener(OnImagesChanged);

        foreach (ARTrackedImage image in subscribedManager.trackables)
            ObserveImage(image);
    }

    private void OnDisable()
    {
        if (subscribedManager != null)
            subscribedManager.trackablesChanged.RemoveListener(OnImagesChanged);

        subscribedManager = null;
        blueImage = null;

        if (activeInstance == this)
            activeInstance = null;
    }

    private void OnApplicationPause(bool paused)
    {
        applicationPaused = paused;
    }

    private void OnImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> changes)
    {
        foreach (ARTrackedImage image in changes.added)
            ObserveImage(image);

        foreach (ARTrackedImage image in changes.updated)
            ObserveImage(image);

        foreach (var removed in changes.removed)
        {
            if (blueImage != null && removed.Key.Equals(blueImage.trackableId))
                blueImage = null;
        }
    }

    private void ObserveImage(ARTrackedImage image)
    {
        if (image != null && image.referenceImage.name == blueTargetName)
            blueImage = image;
    }

    /// <summary>每個鳥群每幀只讀取一次；直接查詢目前的 Tracking 狀態與位置。</summary>
    public static bool TryGetActiveField(
        out Vector3 center,
        out float radius,
        out float strength)
    {
        center = Vector3.zero;
        radius = 0f;
        strength = 0f;

        BlueCardRepulsor source = activeInstance;

        if (source == null || !source.isActiveAndEnabled || source.applicationPaused)
            return false;

#if UNITY_EDITOR
        if (source.editorPreview && source.previewTarget != null &&
            source.previewTarget.gameObject.activeInHierarchy)
        {
            center = source.previewTarget.position;
            radius = Mathf.Max(0.01f, source.influenceRadius);
            strength = Mathf.Max(0f, source.repulsionStrength);
            return true;
        }
#endif

        if (source.subscribedManager == null ||
            !source.subscribedManager.isActiveAndEnabled ||
            source.blueImage == null ||
            !source.blueImage.gameObject.activeInHierarchy ||
            source.blueImage.trackingState != TrackingState.Tracking)
            return false;

        center = source.blueImage.transform.position;
        radius = Mathf.Max(0.01f, source.influenceRadius);
        strength = Mathf.Max(0f, source.repulsionStrength);
        return true;
    }

    /// <summary>
    /// 使用世界距離算出沿鳥群平面逃離的轉向，不會把藍卡位置環繞到場域另一側。
    /// </summary>
    public static Vector3 CalculateAvoidance(
        Vector3 birdWorldPosition,
        Vector3 flockPlaneNormal,
        Vector3 currentWorldHeading,
        Vector3 fieldCenter,
        float radius,
        float strength)
    {
        if (radius <= 0f || strength <= 0f)
            return Vector3.zero;

        Vector3 offset = birdWorldPosition - fieldCenter;
        float distanceSquared = offset.sqrMagnitude;

        if (distanceSquared >= radius * radius)
            return Vector3.zero;

        Vector3 away = Vector3.ProjectOnPlane(offset, flockPlaneNormal);

        // 正好重疊時沿目前前進方向離開，避免零向量造成無法轉向。
        if (away.sqrMagnitude < 0.00000001f)
            away = Vector3.ProjectOnPlane(currentWorldHeading, flockPlaneNormal);

        if (away.sqrMagnitude < 0.00000001f)
            return Vector3.zero;

        float falloff = 1f - Mathf.Sqrt(distanceSquared) / radius;
        return away.normalized * strength * falloff;
    }

    private void OnDrawGizmosSelected()
    {
        if (activeInstance != this)
            return;

        Vector3 center;
        float radius;
        float strength;

        if (!TryGetActiveField(out center, out radius, out strength))
            return;

        Gizmos.color = new Color(0.1f, 0.5f, 1f, 0.9f);
        Gizmos.DrawWireSphere(center, radius);
    }

    private void OnValidate()
    {
        influenceRadius = Mathf.Max(0.01f, influenceRadius);
        repulsionStrength = Mathf.Max(0f, repulsionStrength);
    }
}
