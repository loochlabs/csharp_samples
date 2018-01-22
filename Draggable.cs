using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using ClimbGlobals;
using TeamUtility.IO;



[RequireComponent(typeof(Rigidbody2D))]
public class Draggable : MonoBehaviour
{
    #region[Fields]
    public DragType dragtype;
    public Rigidbody2D anchorBody;
    public GameObject upperBody;
    public GameObject lowerBody;
    public GameObject cursorSprite;
    public GameObject uiarrow;

    //sfx
    public AudioClip mouseoverClip;
    public AudioClip mousedownClip;
    public AudioClip mouseReleaseClip;
    public AudioClip manmoveClip;
    public AudioClip mansighClip;
    public AudioClip sfx_impactDebuff;
    
    public enum DragType
    {
        HAND_RIGHT,
        HAND_LEFT,
        FOOT_RIGHT,
        FOOT_LEFT
    }

    //PRIVATE FIELDS
    private PlayerControls player;
    private bool dragActive = false;
    private bool dragOver = false;
    private Vector2 dir = Vector2.zero;
    private Rigidbody2D body;
    private HingeJoint2D myJoint;
    private JointMotor2D myMotor;
    private float myMotorSpeed;
    private Vector2 anchorVelocity;
    private HingeJoint2D motorJoint1;
    private HingeJoint2D motorJoint2;
    private float damageDuration;  //damage from birds
    //part
    private HandControl[] partControls; 
    private Color tintDefault = Color.white;
    private Color tintDrag = new Color(0.45f, 1, 0.45f, 1);
    private Color tintDamage = new Color(1, 0.45f, 0.45f, 1);
    private float DRAG_MOVE_LIMIT = 80000f; 
    private float dir_tolerance = 0.05f;
    private float dragSensitivity = 0.6f;
    private Vector3 arrowScale = new Vector3(1, 1, 1);

    //envinment effects
    private EnvEffect currentEnvEffects;

    //sfx
    private AudioSource audioSource;
    private float mouseVolume = 0.25f;
    private float pitchDefault;
    private float pitchVariation = 0.2f;
    private System.Random rng = new System.Random(); //for pitch mod
    #endregion

    
    //GETTERS & SETTERS
    #region Getters and Setters
    public bool DragActive {
        get { return dragActive; }
    }
    
    public string DragtypeString {
        get { return DragtypeToString(dragtype); }
    }

    public PlayerControls Player {
        get { return player; }
        set { player = value; }
    }

    public bool Damaged {
        get { return player.PermanentDamage || damageDuration > 0; }
    }
    
    #endregion


    private void Start()
    {
        partControls = GetComponentsInChildren<HandControl>(false);
        body = GetComponent<Rigidbody2D>();
        myJoint = GetComponent<HingeJoint2D>();
        myMotorSpeed = myJoint.motor.motorSpeed;
        motorJoint1 = upperBody.GetComponent<HingeJoint2D>();
        motorJoint2 = lowerBody.GetComponent<HingeJoint2D>();
        
        
        foreach(HandControl hc in partControls) {
            hc.Tint(tintDefault);
        }
        cursorSprite.GetComponent<SpriteRenderer>().enabled = false;
        arrowScale = uiarrow.transform.localScale;

        //sfx
        audioSource = GetComponent<AudioSource>();
        pitchDefault = audioSource.pitch;
    }
    

    private void Update()
    {
        //damage to draggables
        if(damageDuration > 0 && !player.PermanentDamage) {
            damageDuration -= Time.deltaTime;
            if(damageDuration <= 0) {
                damageDuration = 0;
                motorJoint1.useMotor = true;
                foreach(HandControl hc in partControls) {
                    hc.Tint(tintDefault);
                    hc.SetTriggers(false);
                }
            }
        }

        //ui
        uiarrow.SetActive(dragActive);

        //Mouse input is handled as OnMouse_ calls
        if (InputAdapter.inputDevice == InputDevice.KeyboardAndMouse) { return; }
        
        //Gamepad input
        if (InputManager.GetButtonDown(DragtypeString, Player.PlayerID))
        {
            Select();
        }
        else if (InputManager.GetButton(DragtypeString, Player.PlayerID) 
            && player.CurrentDraggable == this)
        {
            Vector2 pos = transform.position;
            pos.x += InputManager.GetAxis("Horizontal", Player.PlayerID) * dragSensitivity;
            pos.y += InputManager.GetAxis("Vertical", Player.PlayerID) * dragSensitivity;
            Drag(pos);
        }
        else if (InputManager.GetButtonUp(DragtypeString, Player.PlayerID))
        {
            Release();
        }
        
    }
    
    /// <summary>
    /// Highlight OnMouseOver and OnMouseExit
    /// </summary>
    /// <param name="enter">Mouse entering/exiting this draggable</param>
    public void Highlight(bool enter) {
        if (Globals.state != Globals.GameState.PLAYING) { return; }
        if (player.PermanentDamage || damageDuration > 0) { return; }

        if (enter) {
            dragOver = true;
            if (!player.dragging)
            {
                cursorSprite.GetComponent<SpriteRenderer>().enabled = true; 
            }
        }
        else {
            dragOver = false;
            cursorSprite.GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    
    /// <summary>
    /// Selection of this draggable OnMouseDown
    /// </summary>
    public void Select() {
        if (Globals.state != Globals.GameState.PLAYING) { return; }
        if (player.PermanentDamage || damageDuration > 0) { return; }

        player.Select(this);

        dragActive = true;
        myMotor = myJoint.motor;
        myMotor.motorSpeed = -myMotorSpeed;
        myJoint.motor = myMotor;

        foreach (HandControl hc in partControls)
        {
            hc.SetTriggers(true);
        }
        cursorSprite.GetComponent<CursorSpriteControl>().Select();

        if (motorJoint1 != null && motorJoint2 != null)
        {
            motorJoint1.useMotor = false;
            motorJoint2.useMotor = false;
        }
        
        //sprite tint
        foreach (HandControl hc in partControls)
        {
            hc.Tint(tintDrag);
        }
        //sfx 
        audioSource.pitch = pitchDefault + (float)(rng.NextDouble() * pitchVariation);
        audioSource.PlayOneShot(mousedownClip, mouseVolume);
        audioSource.PlayOneShot(manmoveClip, mouseVolume);

        //globals
        player.dragging = true;

    }
    
    /// <summary>
    /// OnMouseDown dragging this to desired position
    /// </summary>
    /// <param name="cursorPosition"></param>
    public void Drag(Vector2 cursorPosition) {
        if (Globals.state != Globals.GameState.PLAYING) { return; }
        if (player.PermanentDamage || damageDuration > 0) { return; }
        
        dir.x = cursorPosition.x - transform.position.x;
        dir.y = cursorPosition.y - transform.position.y;
        if (dir.magnitude > 1)
        {
            dir.Normalize();
        }
        
        body.AddForce(dir * DRAG_MOVE_LIMIT * Time.deltaTime);

        //Prevent this draggable from having loose physics during dragging
        if (dir.magnitude < dir_tolerance)
        {
            body.bodyType = RigidbodyType2D.Static;
        }else {
            body.bodyType = RigidbodyType2D.Dynamic;
        }

        //ui arrow
        if(dir.magnitude != 0) {
            uiarrow.transform.rotation = Quaternion.AngleAxis(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg , Vector3.forward);
        }
        arrowScale.x = Mathf.Max(dir.magnitude, 0.5f);
        uiarrow.transform.localScale = arrowScale;

        //global info
        player.Dragging = true;
    }
    
    /// <summary>
    /// Releasing this draggable. Reset properties
    /// </summary>
    /// <param name="userRelease">Was this triggered by the user?</param>
    public void Release(bool userRelease = true) {
        if (Globals.state != Globals.GameState.PLAYING) { return; }
        if (player.PermanentDamage || damageDuration > 0) { return; }
        
        if(player.CurrentDraggable == this) {
            player.CurrentDraggable = null;
            
        }

        //sfx, only play on user action
        if(userRelease) {
            audioSource.Stop(); //clear current audio
            audioSource.pitch = pitchDefault + (float)(rng.NextDouble() * pitchVariation);
            audioSource.PlayOneShot(mouseReleaseClip, mouseVolume);
            audioSource.PlayOneShot(mansighClip, mouseVolume);
        }
            
        body.bodyType = RigidbodyType2D.Dynamic;
        myMotor = myJoint.motor;
        myMotor.motorSpeed = myMotorSpeed;
        myJoint.motor = myMotor;

        //enable collisions again
        foreach (HandControl hc in partControls)
        {
            hc.SetTriggers(false);
            hc.Tint(tintDefault);
        }

        if (motorJoint1 != null && motorJoint2 != null)
        {
            motorJoint1.useMotor = true;
            motorJoint2.useMotor = true;
        }

        dragActive = false;

        if (dragOver)
        {
            cursorSprite.GetComponent<SpriteRenderer>().enabled = true; 
        }

        //globals 
        player.Dragging = false;
    }

    /// <summary>
    /// Damage from hazards in level.
    /// </summary>
    /// <param name="duration">Duration of damage. 0 for permanent damage</param>
    public void Damage(float duration) {
        if(duration == 0) {
            player.PermanentDamage = true;
        }

        foreach (HandControl hc in partControls)
        {
            hc.SetTriggers(true);
            hc.Tint(tintDamage);
        }

        anchorBody = player.torso.GetComponent<Rigidbody2D>();
        anchorBody.isKinematic = false;
        anchorBody.freezeRotation = false;
        anchorBody.velocity = anchorVelocity;
        damageDuration = duration;
        cursorSprite.GetComponent<SpriteRenderer>().enabled = false;
        audioSource.PlayOneShot(sfx_impactDebuff, 0.5f);
        motorJoint1.useMotor = false;

        //globals
        player.Dragging = false;
    }


    
    /// <summary>
    /// ToString method for DragType
    /// </summary>
    /// <param name="dragtype"></param>
    /// <returns></returns>
    public static string DragtypeToString(DragType dragtype)
    {
        switch (dragtype)
        {
            case DragType.HAND_RIGHT:
                return "Right Hand";
            case DragType.HAND_LEFT:
                return "Left Hand";
            case DragType.FOOT_LEFT:
                return "Left Foot";
            case DragType.FOOT_RIGHT:
                return "Right Foot";
        }

        return "<INVALID TYPE>";
    }



    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponentInParent<Draggable>() == null) { return; }
        if (collision.GetComponentInParent<Draggable>().Player.PlayerID == player.PlayerID) { return; }
        if (collision.GetComponentInParent<Draggable>().DragActive) { return; }
        if (Damaged) { return; }

        collision.GetComponentInParent<Draggable>().Damage(1);
    }


    #region [env effects]

    //Various environmental effects. Implmentation/Space for other mechanics. 
    [Flags]
    public enum EnvEffect
    {
        None = 0,
        Balloon = 1
    }

    public bool HasEffect(EnvEffect effect)
    {
        return (currentEnvEffects & effect) == effect;
    }


    public void RemoveEffect(EnvEffect effect) {
        if(HasEffect(effect)) {
            currentEnvEffects &= ~effect;
        }
    }

    public void AddEffect(EnvEffect effect) {
        if(!HasEffect(effect))
        {
            currentEnvEffects |= effect;
        }
    }
    
    #endregion
    
    #region [mouse controls]
    //Wrappers for MOUSE CONTROLLER MANAGEMENT
    //Gamepad still maintains priority

    private void OnMouseOver()
    {
        if (InputAdapter.inputDevice != InputDevice.KeyboardAndMouse) { return; }
        Highlight(true);
    }

    private void OnMouseExit()
    {
        if (InputAdapter.inputDevice != InputDevice.KeyboardAndMouse) { return; }
        Highlight(false);
    }


    private void OnMouseDown()
    {
        if (InputAdapter.inputDevice != InputDevice.KeyboardAndMouse) { return; }
        Select();
    }


    private void OnMouseDrag()
    {
        if (InputAdapter.inputDevice != InputDevice.KeyboardAndMouse) { return; }
        Drag(Camera.main.ScreenToWorldPoint(InputManager.mousePosition));
    }

    private void OnMouseUp()
    {
        if (InputAdapter.inputDevice != InputDevice.KeyboardAndMouse) { return; }
        Release();
    }
    #endregion

}

