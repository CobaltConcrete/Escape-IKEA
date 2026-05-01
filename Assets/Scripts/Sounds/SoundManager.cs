using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [System.Serializable]
    public class NamedSound
    {
        public string key;
        public AudioClip clip;
        [Range(0f, 2f)] public float volume = 1f;
        public bool useUISource = false;

        [Header("Optional Random Pitch")]
        [Range(0f, 0.5f)] public float pitchRandomRange = 0f;
    }

    [Header("Sound Library")]
    [SerializeField] private NamedSound[] soundLibrary;

    [Header("Mixer Optional")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource ambientSource;
    private Dictionary<string, AudioSource> loopSources = new Dictionary<string, AudioSource>();

    [Header("Default Volumes")]
    [Range(0f, 1f)][SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float musicVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float uiVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float ambientVolume = 1f;

    [Header("Fade Settings")]
    [SerializeField] private float defaultMusicFadeDuration = 1f;

    [Header("Cooldowns")]
    [SerializeField] private float playerHurtSoundCooldown = 0.12f;

    private Dictionary<string, NamedSound> soundMap;
    private Coroutine musicFadeCoroutine;
    private Coroutine ambientLoadCoroutine;
    private float lastPlayerHurtSoundTime = -999f;

    private const string MASTER_PREF_KEY = "Audio_MasterVolume";
    private const string MUSIC_PREF_KEY = "Audio_MusicVolume";
    private const string SFX_PREF_KEY = "Audio_SFXVolume";
    private const string UI_PREF_KEY = "Audio_UIVolume";
    private const string AMBIENT_PREF_KEY = "Audio_AmbientVolume";

    public const string BUTTON_CLICK = "ButtonClick";
    public const string PLAYER_HURT = "PlayerHurt";
    public const string PLAYER_DEATH = "PlayerDeath";
    public const string ITEM_PICKUP = "PickupItem";
    public const string INVENTORY_OPEN = "OpenInventory";
    public const string INVENTORY_CLOSE = "CloseInventory";
    public const string GENERAL_AMBIENT = "GeneralAmbient";

    public const string DISTORTED_SHOPPER_LOOP = "DistortedShopper";
    public const string DISTORTED_SHOPPER_DEATH = "DistortedShopperDeath";
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
        BuildSoundMap();
        LoadVolumes();
        ApplyVolumes();
    }

    private void Start()
    {
        AudioListener listener = FindObjectOfType<AudioListener>();
        if (listener == null)
            Debug.LogError("SoundManager: No AudioListener found in scene.");

        if (HasSound(GENERAL_AMBIENT))
            PlayAmbientSound(GENERAL_AMBIENT, true);
    }

    private void BuildSoundMap()
    {
        soundMap = new Dictionary<string, NamedSound>();

        if (soundLibrary == null)
            return;

        foreach (NamedSound sound in soundLibrary)
        {
            if (sound == null || string.IsNullOrWhiteSpace(sound.key) || sound.clip == null)
                continue;

            soundMap[sound.key.Trim()] = sound;
        }
    }

    private NamedSound GetNamedSound(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        if (soundMap == null)
            BuildSoundMap();

        string cleanKey = key.Trim();

        if (!soundMap.TryGetValue(cleanKey, out NamedSound sound))
        {
            Debug.LogWarning($"SoundManager: Sound key not found: {cleanKey}");
            return null;
        }

        return sound;
    }
    public AudioClip GetClip(string key)
    {
        NamedSound sound = GetNamedSound(key);
        return sound != null ? sound.clip : null;
    }

    public float GetSoundVolume(string key)
    {
        NamedSound sound = GetNamedSound(key);
        return sound != null ? sound.volume : 1f;
    }

    public bool HasSound(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (soundMap == null)
            BuildSoundMap();

        return soundMap.ContainsKey(key.Trim());
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = CreateChildSource("MusicSource");
            musicSource.loop = true;
        }

        if (uiSource == null)
        {
            uiSource = CreateChildSource("UISource");
            uiSource.loop = false;
        }

        if (sfxSource == null)
        {
            sfxSource = CreateChildSource("SFXSource");
            sfxSource.loop = false;
        }

        if (ambientSource == null)
        {
            ambientSource = CreateChildSource("AmbientSource");
            ambientSource.loop = true;
        }
    }

    private AudioSource CreateChildSource(string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform);
        child.transform.localPosition = Vector3.zero;

        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private void LoadVolumes()
    {
        masterVolume = PlayerPrefs.GetFloat(MASTER_PREF_KEY, masterVolume);
        musicVolume = PlayerPrefs.GetFloat(MUSIC_PREF_KEY, musicVolume);
        sfxVolume = PlayerPrefs.GetFloat(SFX_PREF_KEY, sfxVolume);
        uiVolume = PlayerPrefs.GetFloat(UI_PREF_KEY, uiVolume);
        ambientVolume = PlayerPrefs.GetFloat(AMBIENT_PREF_KEY, ambientVolume);
    }

    private void SaveVolumes()
    {
        PlayerPrefs.SetFloat(MASTER_PREF_KEY, masterVolume);
        PlayerPrefs.SetFloat(MUSIC_PREF_KEY, musicVolume);
        PlayerPrefs.SetFloat(SFX_PREF_KEY, sfxVolume);
        PlayerPrefs.SetFloat(UI_PREF_KEY, uiVolume);
        PlayerPrefs.SetFloat(AMBIENT_PREF_KEY, ambientVolume);
        PlayerPrefs.Save();
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = masterVolume * musicVolume;

        if (uiSource != null)
            uiSource.volume = masterVolume * uiVolume;

        if (sfxSource != null)
            sfxSource.volume = masterVolume * sfxVolume;

        if (ambientSource != null)
            ambientSource.volume = masterVolume * ambientVolume;
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyVolumes();
        SaveVolumes();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        ApplyVolumes();
        SaveVolumes();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyVolumes();
        SaveVolumes();
    }

    public void SetUIVolume(float value)
    {
        uiVolume = Mathf.Clamp01(value);
        ApplyVolumes();
        SaveVolumes();
    }

    public void SetAmbientVolume(float value)
    {
        ambientVolume = Mathf.Clamp01(value);
        ApplyVolumes();
        SaveVolumes();
    }

    public float GetMasterVolume() => masterVolume;
    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
    public float GetUIVolume() => uiVolume;
    public float GetAmbientVolume() => ambientVolume;

    public void PlaySound(string key)
    {
        PlaySound(key, 1f);
    }

    public void PlaySound(string key, float volumeMultiplier)
    {
        NamedSound sound = GetNamedSound(key);
        if (sound == null)
            return;

        float finalVolume = sound.volume * volumeMultiplier;

        if (sound.useUISource)
            PlayUI(sound.clip, finalVolume);
        else
            PlaySFX(sound.clip, finalVolume);
    }

    public void PlaySoundAtPosition(string key, Vector3 position)
    {
        PlaySoundAtPosition(key, position, 1f);
    }

    public void PlaySoundAtPosition(
    string key,
    Vector3 position,
    float volumeMultiplier = 1f,
    float minDistance = 1.5f,
    float maxDistance = 12f
)
    {
        NamedSound sound = GetNamedSound(key);
        if (sound == null || sound.clip == null)
            return;

        GameObject audioObj = new GameObject("OneShotAudio_" + key);
        audioObj.transform.position = position;

        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.clip = sound.clip;
        source.volume = masterVolume * sfxVolume * sound.volume * volumeMultiplier;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.playOnAwake = false;

        if (sound.pitchRandomRange > 0f)
        {
            source.pitch = Random.Range(
                1f - sound.pitchRandomRange,
                1f + sound.pitchRandomRange
            );
        }

        source.Play();
        Destroy(audioObj, sound.clip.length / Mathf.Max(0.01f, source.pitch) + 0.1f);
    }

    public void PlayMusicSound(string key, bool loop = true)
    {
        NamedSound sound = GetNamedSound(key);
        if (sound == null)
            return;

        PlayMusic(sound.clip, loop);
    }

    public void PlayMusicSoundWithFade(string key, float fadeDuration = -1f, bool loop = true)
    {
        NamedSound sound = GetNamedSound(key);
        if (sound == null)
            return;

        PlayMusicWithFade(sound.clip, fadeDuration, loop);
    }

    public void PlayAmbientSound(string key, bool loop = true)
    {
        NamedSound sound = GetNamedSound(key);
        if (sound == null)
            return;

        PlayAmbient(sound.clip, loop);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null || musicSource == null)
            return;

        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = null;
        }

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = masterVolume * musicVolume;
        musicSource.Play();
    }

    public void PlayMusicWithFade(AudioClip clip, float fadeDuration = -1f, bool loop = true)
    {
        if (clip == null || musicSource == null)
            return;

        if (fadeDuration < 0f)
            fadeDuration = defaultMusicFadeDuration;

        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);

        musicFadeCoroutine = StartCoroutine(FadeToNewMusicCoroutine(clip, fadeDuration, loop));
    }

    public void StopMusic()
    {
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = null;
        }

        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }
    }

    public void StopMusicWithFade(float fadeDuration = -1f)
    {
        if (musicSource == null || !musicSource.isPlaying)
            return;

        if (fadeDuration < 0f)
            fadeDuration = defaultMusicFadeDuration;

        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);

        musicFadeCoroutine = StartCoroutine(FadeOutMusicCoroutine(fadeDuration));
    }

    private IEnumerator FadeToNewMusicCoroutine(AudioClip newClip, float duration, bool loop)
    {
        float targetVolume = masterVolume * musicVolume;

        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
                yield return null;
            }

            musicSource.Stop();
        }

        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.volume = 0f;
        musicSource.Play();

        float fadeInTimer = 0f;
        while (fadeInTimer < duration)
        {
            fadeInTimer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, fadeInTimer / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
        musicFadeCoroutine = null;
    }

    private IEnumerator FadeOutMusicCoroutine(float duration)
    {
        float startVolume = musicSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = null;
        musicSource.volume = masterVolume * musicVolume;
        musicFadeCoroutine = null;
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, volumeScale);
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null)
            return;

        AudioSource.PlayClipAtPoint(clip, position, masterVolume * sfxVolume * volume);
    }

    public void PlayUI(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || uiSource == null)
            return;

        uiSource.PlayOneShot(clip, volumeScale);
    }

    public void PlayAmbient(AudioClip clip, bool loop = true)
    {
        if (clip == null || ambientSource == null)
            return;

        if (ambientLoadCoroutine != null)
            StopCoroutine(ambientLoadCoroutine);

        ambientLoadCoroutine = StartCoroutine(PlayAmbientWhenReady(clip, loop));
    }

    private IEnumerator PlayAmbientWhenReady(AudioClip clip, bool loop)
    {
        if (clip == null || ambientSource == null)
            yield break;

        if (clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();

        float timeout = 5f;
        float timer = 0f;

        while (clip.loadState == AudioDataLoadState.Loading)
        {
            timer += Time.unscaledDeltaTime;
            if (timer >= timeout)
            {
                Debug.LogError("SoundManager: Timed out while loading ambient clip: " + clip.name);
                yield break;
            }

            yield return null;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogError("SoundManager: Failed to load ambient clip: " + clip.name);
            yield break;
        }

        ambientSource.Stop();
        ambientSource.clip = clip;
        ambientSource.loop = loop;
        ambientSource.volume = masterVolume * ambientVolume;
        ambientSource.Play();

        ambientLoadCoroutine = null;
    }

    public void StopAmbient()
    {
        if (ambientLoadCoroutine != null)
        {
            StopCoroutine(ambientLoadCoroutine);
            ambientLoadCoroutine = null;
        }

        if (ambientSource == null)
            return;

        ambientSource.Stop();
        ambientSource.clip = null;
    }

    public void PlayButtonClick()
    {
        PlaySound(BUTTON_CLICK, 1.4f);
    }

    public void PlayPlayerHurt()
    {
        if (Time.time < lastPlayerHurtSoundTime + playerHurtSoundCooldown)
            return;

        PlaySound(PLAYER_HURT);
        lastPlayerHurtSoundTime = Time.time;
    }

    public void PlayPlayerDeath()
    {
        PlaySound(PLAYER_DEATH);
    }
    public void PlayPlayerLose()
    {
        PlaySound(PLAYER_DEATH);
    }

    public void PlayInventoryOpen()
    {
        PlaySound(INVENTORY_OPEN);
    }

    public void PlayInventoryClose()
    {
        PlaySound(INVENTORY_CLOSE);
    }

    public void PlayItemPickup()
    {
        PlaySound(ITEM_PICKUP);
    }

    public void PlayPlayerAttack()
    {
        PlaySound("PlayerAttack");
    }

    public void PlayEnemyHit(Vector3 position)
    {
        PlaySoundAtPosition("HitOneEnemy", position);
    }

    public void PlayLightsOn()
    {
        PlaySound("LightsOn");
    }

    public void PlayLightsOff()
    {
        PlaySound("LightsOff");
    }
    public void StopAllAudio()
    {
        StopMusic();
        StopAmbient();

        if (sfxSource != null)
            sfxSource.Stop();

        if (uiSource != null)
            uiSource.Stop();
        foreach (var kv in loopSources)
        {
            if (kv.Value != null)
                Destroy(kv.Value.gameObject);
        }
        loopSources.Clear();
    }
    public void PauseAllAudio()
    {
        AudioListener.pause = true;
    }

    public void ResumeAllAudio()
    {
        AudioListener.pause = false;
    }

    public void PlayLoop(string key)
    {
        if (loopSources.ContainsKey(key))
            return;

        NamedSound sound = GetNamedSound(key);
        if (sound == null || sound.clip == null)
            return;

        GameObject obj = new GameObject("Loop_" + key);
        obj.transform.SetParent(transform);

        AudioSource source = obj.AddComponent<AudioSource>();
        source.clip = sound.clip;
        source.loop = true;
        source.volume = masterVolume * sfxVolume * sound.volume;
        source.spatialBlend = 0f;
        source.playOnAwake = false;

        source.Play();

        loopSources[key] = source;
    }
    public void StopLoop(string key)
    {
        if (!loopSources.TryGetValue(key, out AudioSource source))
            return;

        if (source != null)
            Destroy(source.gameObject);

        loopSources.Remove(key);
    }
}