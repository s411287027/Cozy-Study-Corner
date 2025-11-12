using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    [Header("角色設定")]
    public GameObject playerPrefab;
    [HideInInspector] public GameObject playerInstance;

    [Header("初始生成點")]
    public Transform spawnPoint;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;

            SpawnPlayer();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void SpawnPlayer()
    {
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;

        playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        DontDestroyOnLoad(playerInstance);

        // 確保 Rigidbody2D 存在
        Rigidbody2D rb = playerInstance.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogWarning("⚠ PlayerPrefab 缺少 Rigidbody2D，自動新增。");
            rb = playerInstance.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // 初始化 player_move targetPosition
        var moveScript = playerInstance.GetComponent<player_move>();
        if (moveScript != null)
        {
            moveScript.SetPositionInstant(spawnPos);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(HandleSceneLoad());
    }

    IEnumerator HandleSceneLoad()
    {
        // 等待一幀，確保新場景物件生成完成
        yield return null;

        if (playerInstance != null)
        {
            Transform newSpawn = GameObject.Find("PlayerSpawnPoint")?.transform;
            Vector3 spawnPos = newSpawn != null ? newSpawn.position : playerInstance.transform.position;

            // 直接設置位置，並同步 targetPosition
            player_move pm = FindObjectOfType<player_move>();
            if (pm != null)
            {
                Debug.Log("11");
                pm.SetPositionInstant(spawnPos);
            }
            else
            {
                Debug.Log("22");
                pm.transform.position = spawnPos;
            }
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
