using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneMusicManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneMusic
    {
        public string sceneName;          // 場景名稱
        public AudioClip[] musicClips;    // 該場景的音樂列表
    }

    public SceneMusic[] sceneMusics;      // 各場景的音樂設定
    public float fadeDuration = 2f;       // 淡入淡出時間（秒）

    private AudioSource audioSource;
    private int currentTrack = 0;
    private string currentSceneName = "";
    private bool isFading = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject); // 場景切換時不銷毀自己
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
        PlaySceneMusic(currentSceneName);
    }

    void Update()
    {
        if (!audioSource.isPlaying && !isFading && audioSource.clip != null)
        {
            PlayNextTrack();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string newScene = scene.name;
        if (newScene != currentSceneName)
        {
            StartCoroutine(FadeToSceneMusic(newScene));
        }
    }

    IEnumerator FadeToSceneMusic(string newScene)
    {
        isFading = true;

        // 淡出舊音樂
        float startVolume = audioSource.volume;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }
        audioSource.volume = 0f;
        audioSource.Stop();

        // 播放新場景音樂
        currentSceneName = newScene;
        PlaySceneMusic(currentSceneName);

        // 淡入新音樂
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }
        audioSource.volume = 1f;
        isFading = false;
    }

    void PlaySceneMusic(string sceneName)
    {
        SceneMusic musicSet = System.Array.Find(sceneMusics, s => s.sceneName == sceneName);

        if (musicSet != null && musicSet.musicClips.Length > 0)
        {
            currentTrack = 0;
            audioSource.clip = musicSet.musicClips[currentTrack];
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"[SceneMusicManager] 此場景沒有設定音樂: {sceneName}");
            audioSource.Stop();
        }
    }

    void PlayNextTrack()
    {
        SceneMusic musicSet = System.Array.Find(sceneMusics, s => s.sceneName == currentSceneName);
        if (musicSet == null || musicSet.musicClips.Length == 0) return;

        currentTrack = (currentTrack + 1) % musicSet.musicClips.Length;
        audioSource.clip = musicSet.musicClips[currentTrack];
        audioSource.Play();
    }
}
