using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneMusicManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneMusicGroup
    {
        public string[] sceneNames;   // 哪些場景共享這組音樂
        public AudioClip[] musicClips;
    }

    public SceneMusicGroup[] sceneMusicsGroups;
    public float fadeDuration = 2f;

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
            Destroy(gameObject);
            return;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;

        SceneManager.sceneLoaded += OnSceneLoaded;

        // 直接初始化並播放當前場景音樂
        SceneMusicGroup initialGroup = GetMusicGroup(SceneManager.GetActiveScene().name);
        if (initialGroup != null)
        {
            PlayMusicGroup(initialGroup, immediate: true);
        }
    }

    void Update()
    {
        if (!audioSource.isPlaying && !isFading && audioPlayingGroup != null && audioPlayingGroup.musicClips.Length > 0)
        {
            PlayNextTrack();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneMusicGroup newGroup = GetMusicGroup(scene.name);

        // 如果是不同組才 fade
        if (newGroup != null && newGroup != audioPlayingGroup)
        {
            StartCoroutine(FadeToSceneMusic(newGroup));
        }
        // 同組場景：不做任何事，保持音樂播放
    }

    IEnumerator FadeToSceneMusic(SceneMusicGroup newGroup)
    {
        if (newGroup == null || newGroup.musicClips.Length == 0) yield break;

        isFading = true;

        // 淡出
        float startVolume = audioSource.volume;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }

        audioSource.Stop();

        // 播放新組第一首曲目
        currentTrack = 0;
        audioSource.clip = newGroup.musicClips[currentTrack];
        audioSource.Play();

        audioPlayingGroup = newGroup;

        // 淡入
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }

        isFading = false;
    }

    void PlayMusicGroup(SceneMusicGroup group, bool immediate = false)
    {
        if (group == null || group.musicClips.Length == 0) return;

        currentTrack = 0;
        audioSource.clip = group.musicClips[currentTrack];
        audioSource.Play();

        if (immediate)
            audioSource.volume = 1f;

        audioPlayingGroup = group;
    }

    void PlayNextTrack()
    {
        if (audioPlayingGroup == null || audioPlayingGroup.musicClips.Length == 0) return;

        currentTrack = (currentTrack + 1) % audioPlayingGroup.musicClips.Length;
        audioSource.clip = audioPlayingGroup.musicClips[currentTrack];
        audioSource.Play();
    }

    SceneMusicGroup GetMusicGroup(string sceneName)
    {
        return System.Array.Find(sceneMusicsGroups, g => System.Array.Exists(g.sceneNames, s => s == sceneName));
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
