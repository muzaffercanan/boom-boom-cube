using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _sfxSource;

    [Header("Settings")]
    [Range(0f, 1f)] [SerializeField] private float _musicVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 1f;

    public float SFXVolume => _sfxVolume;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioSources()
    {
        if (_musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            _musicSource = musicObj.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
        }

        if (_sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            _sfxSource = sfxObj.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
        }

        UpdateVolumes();
    }

    private void UpdateVolumes()
    {
        if (_musicSource) _musicSource.volume = _musicVolume;
        if (_sfxSource) _sfxSource.volume = _sfxVolume;
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;

        if (_musicSource.isPlaying && _musicSource.clip == clip) return;

        _musicSource.clip = clip;
        _musicSource.Play();
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip, volumeScale * _sfxVolume);
    }

    public void SetMusicVolume(float volume)
    {
        _musicVolume = Mathf.Clamp01(volume);
        if (_musicSource) _musicSource.volume = _musicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
    }
    private void OnValidate()
    {
        UpdateVolumes();
    }
}
