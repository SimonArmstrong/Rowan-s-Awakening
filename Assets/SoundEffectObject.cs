using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEffectObject : MonoBehaviour {
    AudioSource _as;

    public void Start(){
        _as = GetComponent<AudioSource>();
    }

    public void Update(){
        if(!_as.isPlaying) Destroy(gameObject);
    }
}
