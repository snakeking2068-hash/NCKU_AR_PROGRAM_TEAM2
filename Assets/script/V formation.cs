using UnityEngine;

public class VFormation : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject 黑面琵鷺; //有public 才可以設定欄位
    public int 數量 = 10;

    void Start()
    {
        for (int i = 0; i < 數量; i++)
        {

            if (i == 0)
            {
                GameObject bird = Instantiate(黑面琵鷺, new Vector3(0, 0, 0), 黑面琵鷺.transform.rotation);
            }
            else
            {
                int row = (i + 1) / 2;
                int X_vector = row * -1;
                if (i % 2 == 0)
                {
                    int V_Vector = row * 1;
                    GameObject bird = Instantiate(黑面琵鷺, new Vector3(X_vector, 0, V_Vector), 黑面琵鷺.transform.rotation); //黑面琵鷺這個來源物件目前的旋轉值
                }
                else
                {
                    int V_Vector = row * -1;
                    GameObject bird = Instantiate(黑面琵鷺, new Vector3(X_vector, 0, V_Vector), 黑面琵鷺.transform.rotation);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    { 
    
    }

}

