using UnityEngine;

public class FlockController : MonoBehaviour
{
    [Header("生成與移動")]
    [SerializeField] private GameObject boidPrefab;
    [SerializeField, Min(1)] private int boidCount = 50;
    [SerializeField, Min(0.01f)] private float areaWidth = 0.4f;
    [SerializeField, Min(0.01f)] private float areaDepth = 0.2f;
    [SerializeField, Min(0f)] private float moveSpeed = 0.05f;

    [Header("群聚規則")]
    [SerializeField, Min(0.001f)] private float neighborRadius = 0.08f;
    [SerializeField, Min(0.001f)] private float separationDistance = 0.025f;
    [SerializeField, Min(0f)] private float separationWeight = 1.8f;
    [SerializeField, Min(0f)] private float alignmentWeight = 1f;
    [SerializeField, Min(0f)] private float cohesionWeight = 0.5f;
    [SerializeField, Min(0f)] private float turnSpeed = 180f;

    [Header("藍卡避讓")]
    [SerializeField] private bool avoidBlueCard = true;
    [SerializeField, Min(0f)] private float blueAvoidanceMultiplier = 1f;

    private Transform[] boids;
    private Vector3[] positions;
    private Vector3[] directions;
    private Quaternion[] nextRotations;

    private void Start()
    {
        if (boidPrefab == null)
        {
            Debug.LogWarning("請先指定 Boid Prefab。", this);
            return;
        }

        int count = Mathf.Max(1, boidCount);

        boids = new Transform[count];
        positions = new Vector3[count];
        directions = new Vector3[count];
        nextRotations = new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            Transform boid = Instantiate(boidPrefab, transform).transform;
            boid.name = $"Boid_{i + 1}";
            boids[i] = boid;

            positions[i] = new Vector3(
                Random.Range(-areaWidth / 2f, areaWidth / 2f),
                0f,
                Random.Range(-areaDepth / 2f, areaDepth / 2f)
            );

            Quaternion rotation = Quaternion.Euler(
                0f, Random.Range(0f, 360f), 0f
            );

            directions[i] = rotation * Vector3.forward;

            boid.localPosition = positions[i];
            boid.localRotation = rotation;
            boid.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (boids == null)
            return;

        float deltaTime = Time.deltaTime;
        float neighborRadiusSquared = neighborRadius * neighborRadius;
        float separationSquared = separationDistance * separationDistance;

        Vector3 blueCenter = Vector3.zero;
        float blueRadius = 0f;
        float blueStrength = 0f;
        bool blueFieldActive = avoidBlueCard && BlueCardRepulsor.TryGetActiveField(
            out blueCenter, out blueRadius, out blueStrength
        );

        // 先計算所有 Boid 下一個轉向。
        for (int i = 0; i < boids.Length; i++)
        {
            Vector3 separation = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 cohesion = Vector3.zero;

            for (int j = 0; j < boids.Length; j++)
            {
                if (i == j)
                    continue;

                // 鳥群自己的鄰居計算仍維持原本的環繞邊界。
                Vector3 offset = WrapXZ(positions[j] - positions[i]);
                float distanceSquared = offset.sqrMagnitude;

                if (distanceSquared < 0.00000001f ||
                    distanceSquared > neighborRadiusSquared)
                    continue;

                alignment += directions[j];
                cohesion += offset;

                if (distanceSquared < separationSquared)
                    separation -= offset / distanceSquared;
            }

            Vector3 desiredDirection =
                separation.normalized * separationWeight +
                alignment.normalized * alignmentWeight +
                cohesion.normalized * cohesionWeight;

            if (blueFieldActive && blueAvoidanceMultiplier > 0f)
            {
                // 兩張圖卡有不同的本地座標，要先用世界位置計算與藍卡的距離。
                Vector3 birdWorldPosition = transform.TransformPoint(positions[i]);
                Vector3 worldHeading = transform.TransformDirection(directions[i]);
                Vector3 worldAvoidance = BlueCardRepulsor.CalculateAvoidance(
                    birdWorldPosition,
                    transform.up,
                    worldHeading,
                    blueCenter,
                    blueRadius,
                    blueStrength
                );

                Vector3 localAvoidance = transform.InverseTransformDirection(worldAvoidance);
                localAvoidance.y = 0f;
                desiredDirection += localAvoidance * blueAvoidanceMultiplier;
            }

            if (desiredDirection.sqrMagnitude < 0.00000001f)
                desiredDirection = directions[i];

            Quaternion targetRotation = Quaternion.LookRotation(
                desiredDirection, Vector3.up
            );

            nextRotations[i] = Quaternion.RotateTowards(
                boids[i].localRotation,
                targetRotation,
                turnSpeed * deltaTime
            );
        }

        // 全部計算完後，再一起更新方向與位置。
        for (int i = 0; i < boids.Length; i++)
        {
            directions[i] = nextRotations[i] * Vector3.forward;
            positions[i] += directions[i] * moveSpeed * deltaTime;
            positions[i] = WrapXZ(positions[i]);
            boids[i].localPosition = positions[i];
            boids[i].localRotation = nextRotations[i];
        }
    }

    private Vector3 WrapXZ(Vector3 position)
    {
        float width = Mathf.Max(0.01f, areaWidth);
        float depth = Mathf.Max(0.01f, areaDepth);

        position.x = Mathf.Repeat(position.x + width / 2f, width) - width / 2f;
        position.z = Mathf.Repeat(position.z + depth / 2f, depth) - depth / 2f;
        return position;
    }
}
