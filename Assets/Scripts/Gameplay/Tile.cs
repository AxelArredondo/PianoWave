using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("Movement")]
    public float baseSpeed = 5f;

    [Header("Hit Accuracy Windows")]
    [Tooltip("Distance from hit line for a PERFECT hit")]
    public float perfectWindow = 0.15f;

    [Tooltip("Distance from hit line for a GOOD hit")]
    public float goodWindow = 0.35f;

    private bool hit = false;
    private Transform hitLine;

    [Header("Visuals")]
    public SpriteRenderer tileRenderer;
    public Color[] tileColors;

    [Header("Hit FX")]
    public float hitDestroyDelay = 0.08f;
    public float hitScalePerfect = 1.25f;
    public float hitScaleGood = 1.12f;
    public GameObject hitParticlePrefab;

    void Start()
    {
        // Auto-find the renderer if you forgot to assign it
        if (tileRenderer == null)
            tileRenderer = GetComponentInChildren<SpriteRenderer>();

        if (tileRenderer != null && tileColors != null && tileColors.Length > 0)
        {
            tileRenderer.color = tileColors[Random.Range(0, tileColors.Length)];
        }
        else
        {
            Debug.LogWarning("Tile: tileRenderer missing OR tileColors empty on " + gameObject.name);
        }

        hitLine = GameManager.Instance.hitLine;
    }

    void Update()
    {
        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused)
            return;

        float speedMultiplier = BeatManager.Instance.bpm / 120f;
        transform.Translate(Vector3.down * baseSpeed * speedMultiplier * Time.deltaTime);

        // Auto-miss if tile passes below the good window
        if (transform.position.y < hitLine.position.y - goodWindow && !hit)
        {
            Miss();
        }

        
            
    }

    public void Hit()
    {
        if (hit) return;
        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused) return;

        float distance = Mathf.Abs(transform.position.y - hitLine.position.y);

        if (distance <= perfectWindow)
        {
            hit = true;
            Perfect();
            PlayHitFX(1.25f);   // bigger pop
        }
        else if (distance <= goodWindow)
        {
            hit = true;
            Good();
            PlayHitFX(1.12f);   // smaller pop
        }
        else
        {
            Miss(); // Miss() will handle destroy
        }
    }

    void Perfect()
    {
        GameManager.Instance.RegisterPerfect();
        Debug.Log("PERFECT");
    }

    void Good()
    {
        GameManager.Instance.RegisterGood();
        Debug.Log("GOOD");
    }

    void Miss()
    {
        if (hit) return;
        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused) return;

        hit = true;
        GameManager.Instance.RegisterMiss();
        GameManager.Instance.MissTile();
        Destroy(gameObject);
    }

    void PlayHitFX(float scale)
    {
        // stop tile from being hit/missed again
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // quick “punch” scale
        transform.localScale *= scale;

        // quick flash to white (keeps contrast)
        if (tileRenderer != null)
        {
            Color c = tileRenderer.color;
            tileRenderer.color = Color.white;
            // (optional) you could restore c, but we destroy quickly anyway
        }

        // optional particles
        if (hitParticlePrefab != null)
            Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);

        Destroy(gameObject, hitDestroyDelay);
    }


}
