using UnityEngine;
using TMPro;

public class HitPopup : MonoBehaviour
{
    public float floatSpeed = 40f;
    public float lifetime = 0.8f;

    private TextMeshProUGUI text;
    private Color startColor;

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        startColor = text.color;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Translate(Vector3.up * floatSpeed * Time.deltaTime);

        Color c = text.color;
        c.a -= Time.deltaTime / lifetime;
        text.color = c;
    }

    public void Setup(string message, Material mat, float size)
    {
        text.text = message;
        text.fontSize = size;

        // Force visible
        Color c = text.color;
        c.a = 1f;
        text.color = c;

        // Apply material
        text.fontMaterial = mat;
    }

}
