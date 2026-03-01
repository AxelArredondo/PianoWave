using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("Movement")]
    public float baseSpeed = 5f;

    [Header("Hit Accuracy Windows (percent of TILE height)")]
    [Tooltip("Perfect window as a percent of the tile height (measured from tile center).")]
    [Range(0.01f, 0.50f)]
    public float perfectPercentOfTileHeight = 0.12f;

    [Tooltip("Good window as a percent of the tile height (measured from the tile edges).")]
    [Range(0.00f, 1f)]
    public float goodPercentOfTileHeight = 0.20f;

    private bool hit = false;
    private Transform hitLine;

    [Header("Visuals")]
    public SpriteRenderer tileRenderer;
    public Color[] tileColors;

    [Header("Hit FX")]
    public float hitDestroyDelay = 0.08f;
    public float hitScalePerfect = 1.35f;
    public float hitScaleGood = 1.12f;

    [Tooltip("Particle prefab plays ONLY on PERFECT")]
    public GameObject hitParticlePrefab;

    [Tooltip("Where particles spawn: TRUE = at hitline (cleaner), FALSE = at tile position")]
    public bool spawnParticlesAtHitLine = true;

    [Tooltip("Fallback lifetime if your particle prefab does not self-destroy (Stop Action = Destroy).")]
    public float particleLifetime = 1.0f;

    void Start()
    {
        if (tileRenderer == null)
            tileRenderer = GetComponentInChildren<SpriteRenderer>();

        if (tileRenderer != null && tileColors != null && tileColors.Length > 0)
            tileRenderer.color = tileColors[Random.Range(0, tileColors.Length)];
        else
            Debug.LogWarning("Tile: tileRenderer missing OR tileColors empty on " + gameObject.name);

        hitLine = GameManager.Instance.hitLine;
    }

    void Update()
    {
        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused)
            return;

        float speedMultiplier = BeatManager.Instance.bpm / 120f;
        transform.Translate(Vector3.down * baseSpeed * speedMultiplier * Time.deltaTime);

        // Auto-miss once the tile is fully past the "good" zone under the hitline
        if (tileRenderer != null && hitLine != null && !hit)
        {
            float hitY = hitLine.position.y;

            float topY = tileRenderer.bounds.max.y;
            float bottomY = tileRenderer.bounds.min.y;
            float tileHeight = Mathf.Max(0.0001f, topY - bottomY);

            float goodWindowWorld = tileHeight * goodPercentOfTileHeight;

            // if the tile is BELOW the hitline by more than the allowed good window, it's a miss
            if (topY < hitY - goodWindowWorld)
                Miss();
        }
    }

    public void Hit()
    {
        if (hit) return;
        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused) return;

        if (tileRenderer == null)
            tileRenderer = GetComponentInChildren<SpriteRenderer>();

        if (tileRenderer == null || hitLine == null)
        {
            Miss();
            return;
        }

        float hitY = hitLine.position.y;

        // Tile vertical edges in world space
        float topY = tileRenderer.bounds.max.y;
        float bottomY = tileRenderer.bounds.min.y;

        float tileHeight = Mathf.Max(0.0001f, topY - bottomY);

        // Convert percents to world units
        float perfectWindowWorld = tileHeight * perfectPercentOfTileHeight;
        float goodWindowWorld = tileHeight * goodPercentOfTileHeight;

        // Distance from hitline to tile center (Perfect check)
        float centerY = tileRenderer.bounds.center.y;
        float distToCenter = Mathf.Abs(centerY - hitY);

        // Distance from hitline to tile (0 if hitline is inside tile)
        float distToTile = 0f;
        if (hitY > topY) distToTile = hitY - topY;
        else if (hitY < bottomY) distToTile = bottomY - hitY;

        if (distToCenter <= perfectWindowWorld)
        {
            hit = true;
            Perfect();
        }
        else if (distToTile <= goodWindowWorld)
        {
            hit = true;
            Good();
        }
        else
        {
            Miss();
        }
    }

    void Perfect()
    {
        GameManager.Instance.RegisterPerfect();
        Debug.Log("PERFECT");

        PlayHitFX(hitScalePerfect);

        // Particles only on PERFECT
        if (hitParticlePrefab != null)
        {
            Vector3 spawnPos = (spawnParticlesAtHitLine && hitLine != null) ? hitLine.position : transform.position;
            GameObject fx = Instantiate(hitParticlePrefab, spawnPos, Quaternion.identity);
            Destroy(fx, particleLifetime);
        }
    }

    void Good()
    {
        GameManager.Instance.RegisterGood();
        Debug.Log("GOOD");

        PlayHitFX(hitScaleGood);
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
        // Stop tile from being hit/missed again
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Quick “punch” scale
        transform.localScale *= scale;

        // Flash to white
        if (tileRenderer != null)
            tileRenderer.color = Color.white;

        Destroy(gameObject, hitDestroyDelay);
    }
}