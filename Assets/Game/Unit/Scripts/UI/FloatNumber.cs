using UnityEngine;
using TMPro;

public class FloatNumber : MonoBehaviour
{
    public float floatSpeed = 1f;
    public float lifeTime = 1f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.up * floatSpeed * Time.deltaTime);
    }

    public void SetNumber(int number)
    {
        GetComponent<TextMeshProUGUI>().text = "+" + number;
    }
}