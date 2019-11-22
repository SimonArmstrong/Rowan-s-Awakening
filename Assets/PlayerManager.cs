using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour {
    public static PlayerManager instance;
    public Player player;
    public Camera playerCamera;

    public EntryPoint entryPoint;

    #region MonoCalls
    private void Awake()
    {
        instance = this;
    }
    private void Start() {
        player.Init();
    }
    private void Update() {
        player.Tick();
    }
    private void FixedUpdate() {
        player.FixedTick();
    }
    #endregion
}
