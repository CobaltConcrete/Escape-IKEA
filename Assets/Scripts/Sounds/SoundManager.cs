using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Mixer (Optional)")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource ambientSource;

    [Header("Common Clips")]
    [SerializeField] private AudioClip[] buttonClickClips;
    [SerializeField] private AudioClip playerHurtClip;
    [SerializeField] private AudioClip playerDeathClip;
    [SerializeField] private AudioClip itemPickupClip;
    [SerializeField] private AudioClip inventoryOpenClip;
    [SerializeField] private AudioClip inventoryCloseClip;
    [SerializeField] private AudioClip generalAmbientClip;

    [Header("Default Volumes")]
    [Range(0f, 1f)][SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float musicVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float uiVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float ambientVolume = 1f;

    [Header("Fade Settings")]
    [SerializeField] private float defaultMusicFadeDuration = 1f;

    [Header("Player Hurt")]
    [SerializeField] private float playerHurtSoundCooldown = 0.12f;

    private Coroutine musicFadeCoroutine;
    private Coroutine ambientLoadCoroutine;
    private float lastPlayerHurtSoundTime = -999f;

    private const string MASTER_PREF_KEY = "Audio_MasterVolume";
    private const string MUSIC_PREF_KEY = "Audio_MusicVolume";
    private const string SFX_PREF_KEY = "Audio_SFXVolume";
    private const string UI_PREF_KEY = "Audio_UIVolume";
    private const string AMBIENT_PREF_KEY = "Audio_AmbientVolume";

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
        LoadVolumes();
        ApplyVolumes();
    }

    private void Start()
    {
        AudioListener listener = FindObjectOfType<AudioListener>();
        if (listener == null)
        {
            Debug.LogError("SoundManager: No AudioListener found in scene.");
        }

        if (generalAmbientClip != null)
        {
            if (ambientLoadCoroutine != null)
            {
                StopCoroutine(ambientLoadCoroutine);
            }

            ambientLoadCoroutine = StartCoroutine(PlayAmbientWhenReady(generalAmbientClip, true));
        }
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = CreateChildSource("MusicSource");
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (uiSource == null)
        {
            uiSource = CreateChildSource("UISource");
            uiSource.loop = false;
            uiSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = CreateChildSource("SFXSource");
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        if (ambientSource == null)
        {
            ambientSource = CreateChildSource("AmbientSource");
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
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
        {
            StopCoroutine(musicFadeCoroutine);
        }

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
        {
            StopCoroutine(musicFadeCoroutine);
        }

        musicFadeCoroutine = StartCoroutine(FadeOutMusicCoroutine(fadeDuration));
    }

    private IEnumerator FadeToNewMusicCoroutine(AudioClip newClip, float duration, bool loop)
    {
        float originalTargetVolume = masterVolume * musicVolume;

        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
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
            float t = fadeInTimer / duration;
            musicSource.volume = Mathf.Lerp(0f, originalTargetVolume, t);
            yield return null;
        }

        musicSource.volume = originalTargetVolume;
        musicFadeCoroutine = null;
    }

    private IEnumerator FadeOutMusicCoroutine(float duration)
    {
        float startVolume = musicSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
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

    public void PlaySFX(AudioClip[] clips, float volumeScale = 1f)
    {
        if (clips == null || clips.Length == 0)
            return;

        AudioClip chosenClip = clips[Random.Range(0, clips.Length)];
        PlaySFX(chosenClip, volumeScale);
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null)
            return;

        AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    public void PlaySFXAtPosition(AudioClip[] clips, Vector3 position, float volume = 1f)
    {
        if (clips == null || clips.Length == 0)
            return;

        AudioClip chosenClip = clips[Random.Range(0, clips.Length)];
        PlaySFXAtPosition(chosenClip, position, volume);
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
        {
            StopCoroutine(ambientLoadCoroutine);
        }

        ambientLoadCoroutine = StartCoroutine(PlayAmbientWhenReady(clip, loop));
    }

    private IEnumerator PlayAmbientWhenReady(AudioClip clip, bool loop)
    {
        if (clip == null || ambientSource == null)
            yield break;

        if (clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }

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
            int extraFrames = 10;
            while (extraFrames > 0 && clip.loadState != AudioDataLoadState.Loaded)
            {
                extraFrames--;
                yield return null;
            }
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogError("SoundManager: Failed to load ambient clip: " + clip.name + " | Final state = " + clip.loadState);
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
        if (buttonClickClips == null || buttonClickClips.Length == 0)
            return;

        AudioClip clip = buttonClickClips[Random.Range(0, buttonClickClips.Length)];

        if (uiSource == null) return;

        float originalPitch = uiSource.pitch;
        uiSource.pitch = Random.Range(0.95f, 1.05f);

        uiSource.PlayOneShot(clip, 1.4f);

        uiSource.pitch = originalPitch;
    }

    public void PlayPlayerHurt()
    {
        if (Time.time < lastPlayerHurtSoundTime + playerHurtSoundCooldown)
            return;

        PlaySFX(playerHurtClip, 0.4f);
        lastPlayerHurtSoundTime = Time.time;
    }

    public void PlayPlayerDeath()
    {
        PlaySFX(playerDeathClip, 0.7f);
    }

    public void PlayInventoryOpen()
    {
        PlayUI(inventoryOpenClip, 1.2f);
    }

    public void PlayInventoryClose()
    {
        PlayUI(inventoryCloseClip, 1.2f);
    }
    public void PlayItemPickup()
    {
        PlaySFX(itemPickupClip, 1.1f);
    }
}