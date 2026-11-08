using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageCubeSpawner : MonoBehaviour
{
    [SerializeField] private ARTrackedImageManager imageManager;

    [FormerlySerializedAs("cubePrefab")]
    [SerializeField] private GameObject redFlockPrefab;

    [SerializeField] private GameObject yellowFlockPrefab;
    [SerializeField] private float surfaceOffset = 0.02f;

    private const string FlockName = "FlockVisual";

    private void OnEnable()
    {
        if (imageManager == null)
            return;

        imageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);

        foreach (var image in imageManager.trackables)
            UpdateFlock(image);
    }

    private void OnDisable()
    {
        if (imageManager != null)
            imageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
    }

    private void OnTrackedImagesChanged(
        ARTrackablesChangedEventArgs<ARTrackedImage> changes)
    {
        foreach (var image in changes.added)
            UpdateFlock(image);

        foreach (var image in changes.updated)
            UpdateFlock(image);
    }

    private GameObject GetFlockPrefab(string imageName)
    {
        switch (imageName)
        {
            case "Red_L_Target":
                return redFlockPrefab;

            case "Yellow_I_Target":
                return yellowFlockPrefab;

            default:
                return null;
        }
    }

    private void UpdateFlock(ARTrackedImage image)
    {
        // 先檢查這張圖卡是否已經有鳥群，避免重複生成。
        var flock = image.transform.Find(FlockName);

        if (flock == null)
        {
            var prefab = GetFlockPrefab(image.referenceImage.name);

            if (prefab == null)
                return;

            // 鳥群成為這張圖卡的子物件，跟隨圖卡的位置和方向。
            var instance = Instantiate(prefab, image.transform);
            instance.name = FlockName;

            flock = instance.transform;
            flock.localPosition = Vector3.up * surfaceOffset;
            flock.localRotation = Quaternion.identity;
        }

        // 追蹤狀態正常時顯示，其他狀態隱藏。
        flock.gameObject.SetActive(
            image.trackingState == TrackingState.Tracking);
    }
}