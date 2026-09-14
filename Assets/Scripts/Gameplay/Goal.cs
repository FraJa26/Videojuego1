using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Goal : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        GameManager.Instance?.WinLevel();
    }
}
