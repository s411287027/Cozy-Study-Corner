using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneMusicManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneMusicGroup
    {
        public string description;    // (選用) 寫註解用
        public string[] sceneNames;   // 場景名稱
        public string[] pageNames;    // 頁面名稱
        public AudioClip[] musicClips;
    }

    public SceneMusicGroup[] sceneMusicsGroups;
    public float fadeDuration = 1.0f;

    private AudioSource audioSource;
    private int currentTrack = 0;
    private SceneMusicGroup audioPlayingGroup = null;
    private bool isFading = false;

    public static SceneMusicManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // ⭐ 1. 在自殺之前，把「最新場景設定好的資料」強制覆蓋給「活著的那個舊 Manager」
            Instance.sceneMusicsGroups = this.sceneMusicsGroups;

            // ⭐ 2. 如果淡入淡出時間有改，順便也更新過去
            Instance.fadeDuration = this.fadeDuration;

            // 3. 任務完成，安心上路
            Destroy(gameObject);
            return;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // ⭐ 修正 1：加入 Start 方法，處理遊戲剛開始的音樂
    void Start()
    {
        // 如果遊戲剛開始沒有任何音樂在播，就嘗試播放當前場景的音樂
        if (audioPlayingGroup == null)
        {
            TrySwitchMusic(SceneManager.GetActiveScene().name);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TrySwitchMusic(scene.name);
    }

    public void SwitchMusic(string keyName)
    {
        TrySwitchMusic(keyName);
    }

    private void TrySwitchMusic(string nameKey)
    {
        SceneMusicGroup targetGroup = GetMusicGroup(nameKey);

        if (targetGroup == null || targetGroup == audioPlayingGroup) return;

        StopAllCoroutines();
        StartCoroutine(FadeToSceneMusic(targetGroup));
    }

    IEnumerator FadeToSceneMusic(SceneMusicGroup newGroup)
    {
        // ⭐ 防呆機制：確保新群組真的有音樂檔案
        if (newGroup.musicClips == null || newGroup.musicClips.Length == 0 || newGroup.musicClips[0] == null)
        {
            Debug.LogWarning($"[SceneMusicManager] 群組 {newGroup.description} 沒有設定音樂 AudioClip！");
            yield break;
        }

        isFading = true;

        // 1. 淡出舊音樂
        float startVolume = audioSource.volume;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }
        audioSource.Stop();

        // 2. 準備新音樂
        audioPlayingGroup = newGroup;
        currentTrack = 0;

        audioSource.clip = newGroup.musicClips[0];
        audioSource.Play();

        // 3. 淡入新音樂
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }

        isFading = false;
    }

    void Update()
    {
        // 自動播放下一首
        if (!audioSource.isPlaying && !isFading && audioPlayingGroup != null && audioPlayingGroup.musicClips.Length > 0)
        {
            // ⭐ 修正 2：增加防呆，避免播放到空的格子 (null)
            int nextTrack = (currentTrack + 1) % audioPlayingGroup.musicClips.Length;

            // 如果下一首是空的，就跳過
            if (audioPlayingGroup.musicClips[nextTrack] != null)
            {
                currentTrack = nextTrack;
                audioSource.clip = audioPlayingGroup.musicClips[currentTrack];
                audioSource.Play();
            }
        }
    }

    SceneMusicGroup GetMusicGroup(string nameKey)
    {
        foreach (var group in sceneMusicsGroups)
        {
            if (group.sceneNames != null && System.Array.Exists(group.sceneNames, s => s == nameKey))
                return group;
            if (group.pageNames != null && System.Array.Exists(group.pageNames, p => p == nameKey))
                return group;
        }
        return null;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}