using UnityEngine;

public class BirdSpawner : MonoBehaviour
{
    
    public GameObject 黑面琵鷺; //有public 才可以設定欄位
    public int 數量 = 10;
    public int 生成間隔 = 2;
    public int 速度 = 2;


    void Start() //在生命週期中這段函示只會被執行一次
    {
        for (int i = 0; i < 數量; i++)
        {  
            Vector3 newBird=
                transform.position+new Vector3(0,0,i * 生成間隔);

            GameObject bird =Instantiate(黑面琵鷺, newBird, Quaternion.identity); //使用GameObject 後續才可以操作
}
    }
    void Update()
    {
        transform.position +=
    transform.right * 速度 * Time.deltaTime;
    }
}
