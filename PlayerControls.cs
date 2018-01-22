using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClimbGlobals;
using TeamUtility.IO;
using cakeslice;

public class PlayerControls : MonoBehaviour {


    #region[Fields]
    [HideInInspector]
    public PlayerID playerid;
    [HideInInspector]
    public Transform playerTransform;

    [HideInInspector]
    public bool dragging = false;
    [HideInInspector]
    public bool permanentDamage = false;
    [HideInInspector]
    public Draggable[] draggables;
    public GameObject torso;
    public AudioSource torsoAudio;

    private Draggable currentDraggable;
    private GameObject currentHoldObject;
    private Vector3 curPosition = Vector3.zero;
    private float joystick_movespeed = 0.2f;
    #endregion

    #region[Globals]
    public static Vector3 highestPoint;
    #endregion

    #region[Getters and Setters]

    public PlayerID PlayerID {
        get { return playerid; }
    }
    
    public bool Dragging {
        get { return dragging; }
        set { dragging = value; }
    }

    public Draggable CurrentDraggable {
        get { return currentDraggable; }
        set { currentDraggable = value; }
    }

    public bool PermanentDamage {
        get { return permanentDamage; }
        set { permanentDamage = value; }
    }

    public GameObject Torso {
        get { return torso; }
    }

    #endregion


    /// <summary>
    /// Init for local multiplayer settings
    /// </summary>
    /// <param name="id"></param>
    public void Init(PlayerID id = PlayerID.One)
    {
        playerid = id;
        
        if(id == PlayerID.Two) {
            foreach(Outline o in GetComponentsInChildren<Outline>()){
                o.color = 1;
            }
        }
    }



    private void Start()
    {
        //global set for the sake of draggables
        playerTransform = transform;
        highestPoint = Vector3.zero;
        dragging = false;
        permanentDamage = false;
        
        //checking for highest player point in game
        foreach(Transform t in gameObject.GetComponentsInChildren<Transform>(false)) {
            if (t == transform) continue;
            if(t.position.y > highestPoint.y) { highestPoint = t.position; }
        }

        //Setting draggables for player
        //Needed for local multiplayer controls
        draggables = GetComponentsInChildren<Draggable>();
        foreach (Draggable d in draggables)
        {
            d.Player = this;
        }

        //sorting fix for second player
        if(playerid == PlayerID.Two) {
            foreach(SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>()) {
                sr.sortingOrder += 100;
            }
        }
    }



    
    void Update () {
        switch(Globals.state) {
            case Globals.GameState.PAUSED:

                if(InputManager.GetButtonDown("Pause", playerid))
                {
                    LevelManager.Settings(false);
                }

                break;

            case Globals.GameState.FREEHOLD:
                if (permanentDamage) { break; }
                
                if (currentHoldObject != null) //place freehold
                {

                    switch (InputAdapter.inputDevice)
                    {
                        case InputDevice.Joystick:
                            //get joystick input
                            //add to current position of this freehold
                            curPosition.x = currentHoldObject.transform.position.x + InputManager.GetAxis("Horizontal", playerid) * joystick_movespeed; //@TODO do we need a Time.deltaTime here?
                            curPosition.y = currentHoldObject.transform.position.y + InputManager.GetAxis("Vertical", playerid) * joystick_movespeed;
                            currentHoldObject.transform.position = curPosition;

                            LevelManager.UpdateFreeholdPosition();
                            //listen for "Freehold" input command
                            if (InputManager.GetButtonDown("Freehold", playerid))
                            {
                                if(LevelManager.UnpauseForFreeHold(currentHoldObject)) {
                                    currentHoldObject = null;
                                }
                            }
                            break;
                    }
                }
                break;

            case Globals.GameState.PLAYING:
                if (permanentDamage) { break; }
                //audio control
                torsoAudio.mute = dragging;

                //high point check
                foreach (Transform t in gameObject.GetComponentsInChildren<Transform>(false))
                {
                    if (t.tag != "Hand") continue;
                    if (t.position.y > highestPoint.y)
                    {
                        highestPoint = t.position;
                    }
                }
                
                if (InputAdapter.inputDevice == InputDevice.Joystick)
                {
                    if (InputManager.GetButtonDown("Freehold", playerid))
                    {
                        currentHoldObject = LevelManager
                            .PauseForFreeHold(torso.transform.position, playerid);
                    }
                }

                if (InputManager.GetButtonDown("Pause", playerid))
                {
                    LevelManager.Settings(true);
                }

                //draggables
                if (currentDraggable != null) {
                    torso.GetComponent<Rigidbody2D>().isKinematic = true;
                    torso.GetComponent<Rigidbody2D>().freezeRotation = true;
                    torso.GetComponent<Rigidbody2D>().velocity = Vector2.zero;

                    if(InputAdapter.inputDevice == InputDevice.KeyboardAndMouse) {
                        Cursor.visible = false;
                    }
                }
                else {
                    torso.GetComponent<Rigidbody2D>().isKinematic = false;
                    torso.GetComponent<Rigidbody2D>().freezeRotation = false;

                    if (InputAdapter.inputDevice == InputDevice.KeyboardAndMouse)
                    {
                        Cursor.visible = true;
                    }
                }
                
                break;
        }
    }



    /// <summary>
    /// Permanent damage. Hard fail condition. See individual Draggables for temp damage.
    /// </summary>
    public void Damage() {
        foreach(Draggable d in draggables) {
            d.Damage(0f);
        }
    }


    #region [draggables]

    public void Select(Draggable d)
    {
        foreach (Draggable dr in draggables)
        {
            dr.Release(false);
        }
        currentDraggable = d;
    }
    #endregion

}
