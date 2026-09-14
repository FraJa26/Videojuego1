using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    [SerializeField] private int value = 1;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        GameManager.Instance?.AddCoin(value);
        AudioManager.Instance?.PlayCoin();
        CoinBurstEffect.SpawnAt(transform.position);

        Destroy(gameObject);
    }
}
