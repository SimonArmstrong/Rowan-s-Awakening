using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntryPoint : MonoBehaviour {
    public static EntryPoint instance;
    private void Awake()
    {
        instance = this;
    }

}
