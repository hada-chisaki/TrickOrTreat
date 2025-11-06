using UnityEngine;

public class EnemyDestroyer : MonoBehaviour
{
    private GameManager gameManager;
    void Start()
    {
        // GameManagerを取得
        gameManager = GameObject.FindObjectOfType<GameManager>();
    }

    public void DestroyGhost()
    {
        Destroy(this.gameObject);
    }



    void OnDestroy()
    {
        // シーン遷移時のnullチェック
        if (gameManager != null && gameObject.scene.isLoaded)
        {
            gameManager.OnEnemyDestroyed(gameObject);
        }
    }
}
