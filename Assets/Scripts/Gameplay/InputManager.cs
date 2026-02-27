using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    void Update()
    {
        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused)
            return;

        if (Pointer.current == null) return;

        if (Pointer.current.press.wasPressedThisFrame)
        {
            HandleTap(Pointer.current.position.ReadValue());
        }
    }

    void HandleTap(Vector2 screenPosition)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, 0f)
        );

        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider != null)
        {
            Tile tile = hit.collider.GetComponent<Tile>();
            if (tile != null)
            {
                tile.Hit();
            }
        }
    }
}
