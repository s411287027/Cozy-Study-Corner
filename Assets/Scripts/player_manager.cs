using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    [Header("角色設定")]
    public GameObject playerPrefab;
    private GameObject playerInstance;

    [Header("初始生成點")]
    public Transform spawnPoint;

    private bool isClassroomScene = false;
    private Rigidbody2D rb;

    void Awake()
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
        rb = playerInstance.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogWarning("⚠ PlayerPrefab 缺少 Rigidbody2D，自動新增。");
            rb = playerInstance.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        isClassroomScene = SceneManager.GetActiveScene().name == "classroom";
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(HandleSceneLoad(scene.name));
    }

    IEnumerator HandleSceneLoad(string sceneName)
    {
        // 等待一幀，確保新場景內物件都生成完
        yield return null;

        // 找新的生成點
        Transform newSpawn = GameObject.Find("PlayerSpawnPoint")?.transform;
        if (newSpawn != null && playerInstance != null)
        {
            playerInstance.transform.position = newSpawn.position;
        }
        else
        {
            Debug.LogWarning($"⚠ 找不到 PlayerSpawnPoint（Scene: {sceneName}）");
        }

        // 更新場景旗標
        isClassroomScene = sceneName == "classroom";
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
