using UnityEngine;

public class BirdMove  : MonoBehaviour
{
    public float 速度 = 1;

    public void Update()
    {
        this.transform.position +=
            transform.right * 速度 * Time.deltaTime;
    }
}