using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Audio/SoundFont")]
public class SoundFont : ScriptableObject {
    public List<AudioClip> clips = new List<AudioClip>();

    public AudioClip GetRandom(){
        return clips[Random.Range(0, clips.Count)];
    }
}
