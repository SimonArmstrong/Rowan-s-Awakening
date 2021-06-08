using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance; void Awake() { instance = this; DontDestroyOnLoad(gameObject); }
    public GameObject soundObject;

    public void PlaySound(AudioClip audio, float volume = (1.0f), float pitchRange = (0.1f))
    {
        AudioSource _as = soundObject.GetComponent<AudioSource>();
        _as.volume = volume;
        _as.clip = audio;
        _as.pitch = Random.Range(1.0f - (pitchRange * 0.5f), 1.0f + (pitchRange * 0.5f));
        Instantiate(soundObject);
    }

    public void PlaySound(SoundFont audio, float volume = (1.0f), float pitchRange = (0.1f))
    {
        AudioSource _as = soundObject.GetComponent<AudioSource>();
        _as.volume = volume;
        _as.clip = audio.GetRandom();
        _as.pitch = Random.Range(1.0f - (pitchRange * 0.5f), 1.0f + (pitchRange * 0.5f));
        Instantiate(soundObject);
    }

    public void Play3DSound(AudioClip audio, Vector3 position, float volume = (1.0f), float pitchRange = (0.1f))
    {
        AudioSource _as = soundObject.GetComponent<AudioSource>();
        _as.volume = volume;
        _as.clip = audio;
        _as.pitch = Random.Range(1.0f - (pitchRange * 0.5f), 1.0f + (pitchRange * 0.5f));
        Instantiate(soundObject, position, Quaternion.identity);
    }

    public void Play3DSound(SoundFont audio, Vector3 position, float volume = (1.0f), float pitchRange = (0.1f))
    {
        AudioSource _as = soundObject.GetComponent<AudioSource>();
        _as.volume = volume;
        _as.clip = audio.GetRandom();
        _as.pitch = Random.Range(1.0f - (pitchRange * 0.5f), 1.0f + (pitchRange * 0.5f));
        Instantiate(soundObject, position, Quaternion.identity);
    }
}
