using UnityEngine;

public class Vformation_new : MonoBehaviour
{
    private const int 角點數 = 4;
    private const int 每段取樣數 = 64;
    private const int 總取樣數 = 角點數 * 每段取樣數;

    public GameObject 黑面琵鷺;
    [Min(1)] public int 數量 = 11;
    [SerializeField, Min(0.01f)] private float 間距 = 2f;
    [SerializeField, Min(0f)] private float 飛行速度 = 6f;
    [SerializeField, Tooltip("播放前勾選，讓隊形中心沿四角內的橢圓飛行。")]
    private bool 使用橢圓航線 = false;
    [SerializeField] private Transform[] 角點 = new Transform[角點數];

    private readonly Vector3[] 取樣點 = new Vector3[角點數];
    private readonly Vector3[] 把手 = new Vector3[角點數];
    private readonly Vector3[] 航線預覽 = new Vector3[總取樣數 + 1];
    private readonly float[] 累積距離 = new float[總取樣數 + 1];
    private Vector3 橢圓中心;
    private Vector3 橢圓軸一;
    private Vector3 橢圓軸二;
    private float 已飛距離;
    private bool 航線已建立;
    private bool 目前使用橢圓;

    private void Start()
    {
        if (黑面琵鷺 == null || transform.IsChildOf(黑面琵鷺.transform))
        {
            Debug.LogError("請在黑面琵鷺欄位指定 Spoonbill_Flight。", this);
            return;
        }

        if (!建立航線())
            return;

        移動隊形();

        Vector3[] 隊形位置 = new Vector3[Mathf.Max(0, 數量)];
        Vector3 隊形中心 = Vector3.zero;
        for (int i = 0; i < 隊形位置.Length; i++)
        {
            int 排數 = (i + 1) / 2;
            float 距離 = 排數 * 間距;
            float 左右 = i % 2 == 0 ? 距離 : -距離;
            隊形位置[i] = new Vector3(-距離, 0f, 左右);
            隊形中心 += 隊形位置[i];
        }

        Vector3 航線偏移 = Vector3.zero;
        if (目前使用橢圓 && 隊形位置.Length > 0)
        {
            航線偏移 = 隊形中心 / 隊形位置.Length;
            // A02 的拍翅模型可能偏離鳥的根物件，將這段偏移也納入中心。
            Animator 拍翅模型 = 黑面琵鷺.GetComponentInChildren<Animator>(true);
            if (拍翅模型 != null)
            {
                Transform 鳥的根物件 = 黑面琵鷺.transform;
                Vector3 模型位置 = 鳥的根物件.InverseTransformPoint(拍翅模型.transform.position);
                航線偏移 += Vector3.Scale(模型位置, 鳥的根物件.localScale);
            }
        }

        foreach (Vector3 位置 in 隊形位置)
        {
            GameObject 鳥 = Instantiate(黑面琵鷺, transform, false);
            鳥.transform.SetLocalPositionAndRotation(位置 - 航線偏移, Quaternion.identity);

            if (鳥.TryGetComponent(out BirdMove 直線飛行))
                直線飛行.enabled = false;

            鳥.SetActive(true);
        }
    }

    private bool 建立航線()
    {
        if (角點 == null || 角點.Length != 角點數 ||
            System.Array.Exists(角點, 點 => 點 == null))
        {
            Debug.LogError("請依四邊形周圍順序填好 4 個角點。", this);
            return false;
        }

        Vector3[] 四邊形 = new Vector3[角點數];
        float 高度 = 角點[0].position.y;
        for (int i = 0; i < 角點數; i++)
        {
            四邊形[i] = 角點[i].position;
            四邊形[i].y = 高度;
        }

        float 第一個轉向 = 0f;
        for (int i = 0; i < 角點數; i++)
        {
            int 下一角 = (i + 1) % 角點數;
            Vector3 邊 = 四邊形[下一角] - 四邊形[i];
            Vector3 下一邊 = 四邊形[(i + 2) % 角點數] - 四邊形[下一角];
            float 轉向 = Vector3.Cross(邊, 下一邊).y;

            if (Mathf.Abs(轉向) < 0.0001f || 轉向 * 第一個轉向 < 0f)
            {
                Debug.LogError("角點需要依順時針或逆時針排列成凸四邊形。", this);
                return false;
            }

            第一個轉向 = 轉向;
        }

        目前使用橢圓 = 使用橢圓航線;
        if (目前使用橢圓)
            設定橢圓(四邊形);
        else
            設定隨機取樣點(四邊形);

        航線預覽[0] = 計算曲線(0, 0f, out _);
        for (int i = 1; i <= 總取樣數; i++)
        {
            int 段 = (i - 1) / 每段取樣數;
            float t = (float)(i - 段 * 每段取樣數) / 每段取樣數;
            航線預覽[i] = 計算曲線(段, t, out _);
            累積距離[i] = 累積距離[i - 1] +
                Vector3.Distance(航線預覽[i - 1], 航線預覽[i]);
        }

        航線已建立 = true;
        return true;
    }

    private void 設定隨機取樣點(Vector3[] 四邊形)
    {
        for (int i = 0; i < 角點數; i++)
        {
            Vector3 邊 = 四邊形[(i + 1) % 角點數] - 四邊形[i];
            float 比例 = Random.Range(0.25f, 0.75f);
            取樣點[i] = 四邊形[i] + 邊 * 比例;
            // 把手沿著同一條邊，長度取到較近端點距離的一半。
            把手[i] = 邊 * (0.5f * Mathf.Min(比例, 1f - 比例));
        }
    }

    private void 設定橢圓(Vector3[] 四邊形)
    {
        橢圓中心 = (四邊形[0] + 四邊形[1] + 四邊形[2] + 四邊形[3]) / 角點數;
        橢圓軸一 = (四邊形[0] + 四邊形[1]) * 0.5f - 橢圓中心;
        橢圓軸二 = (四邊形[1] + 四邊形[2]) * 0.5f - 橢圓中心;

        // 四邊形若有斜邊，就按最接近的邊界等比縮放橢圓。
        float 縮放 = 1f;
        for (int i = 0; i < 角點數; i++)
        {
            Vector3 邊 = 四邊形[(i + 1) % 角點數] - 四邊形[i];
            Vector3 法線 = Vector3.Cross(Vector3.up, 邊);
            float 邊界 = Mathf.Abs(Vector3.Dot(法線, 橢圓中心 - 四邊形[i]));
            float 投影一 = Vector3.Dot(法線, 橢圓軸一);
            float 投影二 = Vector3.Dot(法線, 橢圓軸二);
            float 橢圓延伸 = Mathf.Sqrt(投影一 * 投影一 + 投影二 * 投影二);
            縮放 = Mathf.Min(縮放, 邊界 / 橢圓延伸);
        }

        橢圓軸一 *= 縮放;
        橢圓軸二 *= 縮放;
    }

    private Vector3 計算曲線(int 段, float t, out Vector3 切線)
    {
        if (目前使用橢圓)
        {
            float 弧度 = Mathf.Repeat(段 + t, 角點數) * (2f * Mathf.PI / 角點數);
            float 餘弦 = Mathf.Cos(弧度);
            float 正弦 = Mathf.Sin(弧度);
            切線 = -橢圓軸一 * 正弦 + 橢圓軸二 * 餘弦;
            return 橢圓中心 + 橢圓軸一 * 餘弦 + 橢圓軸二 * 正弦;
        }

        int 下一段 = (段 + 1) % 角點數;
        Vector3 p0 = 取樣點[段];
        Vector3 p1 = p0 + 把手[段];
        Vector3 p3 = 取樣點[下一段];
        Vector3 p2 = p3 - 把手[下一段];

        // 逐層內插，求出三次 Bézier 上的位置和切線方向。
        Vector3 a = Vector3.Lerp(p0, p1, t);
        Vector3 b = Vector3.Lerp(p1, p2, t);
        Vector3 c = Vector3.Lerp(p2, p3, t);
        Vector3 前 = Vector3.Lerp(a, b, t);
        Vector3 後 = Vector3.Lerp(b, c, t);
        切線 = 後 - 前;
        return Vector3.Lerp(前, 後, t);
    }

    private void Update()
    {
        if (!航線已建立)
            return;

        已飛距離 = Mathf.Repeat(
            已飛距離 + 飛行速度 * Time.deltaTime, 累積距離[總取樣數]);
        移動隊形();
    }

    private void 移動隊形()
    {
        // 先依飛過的距離找樣本，再換算回曲線參數 t。
        int 索引 = System.Array.BinarySearch(累積距離, 已飛距離);
        int 後樣本 = Mathf.Clamp(索引 >= 0 ? 索引 : ~索引, 1, 總取樣數);
        int 前樣本 = 後樣本 - 1;
        float 比例 = Mathf.InverseLerp(累積距離[前樣本], 累積距離[後樣本], 已飛距離);
        int 段 = 前樣本 / 每段取樣數;
        float t = (前樣本 - 段 * 每段取樣數 + 比例) / 每段取樣數;

        Vector3 位置 = 計算曲線(段, t, out Vector3 切線);
        float 角度 = Mathf.Atan2(-切線.z, 切線.x) * Mathf.Rad2Deg;
        transform.SetPositionAndRotation(位置, Quaternion.Euler(0f, 角度, 0f));
    }

    private void OnDrawGizmosSelected()
    {
        if (!航線已建立)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 1; i <= 總取樣數; i++)
            Gizmos.DrawLine(航線預覽[i - 1], 航線預覽[i]);

        if (目前使用橢圓)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 0.3f);
            return;
        }

        Gizmos.color = Color.yellow;
        foreach (Vector3 點 in 取樣點)
            Gizmos.DrawSphere(點, 0.3f);
    }
}
