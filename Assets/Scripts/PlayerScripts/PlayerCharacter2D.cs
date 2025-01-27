﻿using System;
using UnityEngine;

public class PlayerCharacter2D : MonoBehaviour
{
    [SerializeField] private float m_MaxSpeed = 10f;                    // The fastest the player can travel in the x axis.
    [SerializeField] private float m_JumpForce = 600f;                  // Amount of force added when the player jumps.
    [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;  // Amount of maxSpeed applied to crouching movement. 1 = 100%
    [SerializeField] private bool m_AirControl = true;                 // Whether or not a player can steer while jumping;
    [SerializeField] private LayerMask m_WhatIsGround;                  // A mask determining what is ground to the character
    [SerializeField] private AudioClip m_AudioJump;
    [SerializeField] private AudioClip m_AudioLand;
    [SerializeField] private AudioClip m_AudioDeath;
    
    private Transform m_GroundCheck;    // A position marking where to check if the player is grounded.
    const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
    private bool m_Grounded;            // Whether or not the player is grounded.
    private Transform m_CeilingCheck;   // A position marking where to check for ceilings
    const float k_CeilingRadius = .01f; // Radius of the overlap circle to determine if the player can stand up
    private Animator m_Anim;            // Reference to the player's animator component.
    private Rigidbody2D m_Rigidbody2D;
    private bool m_FacingRight = true;  // For determining which way the player is currently facing.
    private float minX = -50f;
    private float maxX = 50f;
    private float normalSpeed = 10f;
    private float slowDown = 5f;
    private float m_health = MAX_HEALTH;
    private float m_extraTime = 0;
    private AudioSource m_AudioSource;
    private bool m_PlayingFootsteps;
    private bool m_PlayingDeath;
    private GrabbableObject m_GrabbedObject = null;
    private bool m_InSpaceship = false;

    public const float MAX_HEALTH = 100f;
    private const float GRABBED_OBJ_OFFSET_R = -0.3f;
    private const float GRABBED_OBJ_OFFSET_L = 0.4f;
    private const float GRABBED_OBJ_Y_POS = -0.5f;

    public float health
    {
        get { return m_health; }
        set { m_health = Mathf.Clamp(value, 0, MAX_HEALTH); }
    }

    public float extraTime {
        get { return m_extraTime; }
        set { m_extraTime = value; }
    }

    public bool inSpaceship
    {
        get { return m_InSpaceship; }
    }

    private void Start()
    {
        // Setting up references.
        m_GroundCheck = transform.Find("GroundCheck");
        m_CeilingCheck = transform.Find("CeilingCheck");
        m_Anim = GetComponent<Animator>();
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_AudioSource = GetComponent<AudioSource>();

        m_Rigidbody2D.gravityScale = PersistentTerrainSettings.settings.gravityEffect;
        m_JumpForce = PersistentPlayerSettings.settings.jumpForce;
        float sideLength = PersistentTerrainSettings.settings.sideLength;
        minX = sideLength / 2.0f - 1.0f;
        maxX = sideLength / 2.0f - 1.0f ;
        minX = -minX;

        m_health = PersistentPlayerSettings.settings.health;

        m_PlayingDeath = false;

        //Reload saved value of extra time if we are loading from a saved game
        if (PersistentLevelSettings.settings.loadFromSave)
            m_extraTime = PersistentPlayerSettings.settings.extraTime;
    }
    

    private void FixedUpdate()
    {
        // Limit x position 
        Vector3 currentPosition = m_Rigidbody2D.position;
        currentPosition.x = Mathf.Clamp(m_Rigidbody2D.position.x, minX, maxX);
        m_Rigidbody2D.position = currentPosition;

        m_Grounded = false;
        
        // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
        // This can be done using layers instead but Sample Assets will not overwrite your project settings.



        Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
        for (int i = 0; i < colliders.Length; i++)
        {
            // TODO?: Add platforms that don't awkwardly stop player from moving when approached from the side.
            if (colliders[i].CompareTag("Platform") && m_Rigidbody2D.velocity.y > 0)
               m_Grounded = false;
            if (colliders[i].gameObject != gameObject && !colliders[i].isTrigger)
                m_Grounded = true;
        }
        m_Anim.SetBool("Ground", m_Grounded);
        
        // Set the vertical animation
        m_Anim.SetFloat("vSpeed", m_Rigidbody2D.velocity.y);

    }

    void Update()
    {
        // Update saved extra time
        PersistentPlayerSettings.settings.extraTime = m_extraTime;

        // Hotkeys for inventory items
        if (Input.GetKeyUp(KeyCode.H))
            InventoryScript.inventory.MaybeUseHealthItem();
        if (Input.GetKeyUp(KeyCode.T))
            InventoryScript.inventory.MaybeUseTimeItem();

        // Upon death, play death sound
        if (health <= 0 && !m_PlayingDeath) {
            m_AudioSource.Stop();
            m_AudioSource.volume = 1;
            m_AudioSource.PlayOneShot(m_AudioDeath);
            m_PlayingDeath = true;
        }
    }
    
    
    public void Move(float move, bool crouch, bool jump)
    {
        //Play footstep sounds if player is moving across the ground
        if (Mathf.Abs(move) >= 0.1 && m_Grounded && !m_PlayingFootsteps) {
            m_AudioSource.volume = 0.01f;
            m_AudioSource.Play();
            m_PlayingFootsteps = true;
        }
        else if ((Mathf.Abs(move) < 0.1 || !m_Grounded) && m_PlayingFootsteps) {
            m_AudioSource.Stop();
            m_AudioSource.volume = 1;
            m_PlayingFootsteps = false;
        }

        // If crouching, check to see if the character can stand up
        if (!crouch && m_Anim.GetBool("Crouch"))
        {
            // If the character has a ceiling preventing them from standing up, keep them crouching
            if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
            {
                crouch = true;
            }
        }
        
        // Set whether or not the character is crouching in the animator
        m_Anim.SetBool("Crouch", crouch);
        
        //only control the player if grounded or airControl is turned on
        if (m_Grounded || m_AirControl)
        {
            // Reduce the speed if crouching by the crouchSpeed multiplier
            move = (crouch ? move*m_CrouchSpeed : move);
            
            // The Speed animator parameter is set to the absolute value of the horizontal input.
            m_Anim.SetFloat("Speed", Mathf.Abs(move));
            
            // Move the character
            m_Rigidbody2D.velocity = new Vector2(move*m_MaxSpeed, m_Rigidbody2D.velocity.y);
            
            // If the input is moving the player right and the player is facing left...
            if (move > 0 && !m_FacingRight)
            {
                // ... flip the player.
                Flip();
            }
            // Otherwise if the input is moving the player left and the player is facing right...
            else if (move < 0 && m_FacingRight)
            {
                // ... flip the player.
                Flip();
            }
        }
        // If the player should jump...
        if (m_Grounded && jump && m_Anim.GetBool("Ground"))
        {
            // Add a vertical force to the player.
            m_Grounded = false;
            m_Anim.SetBool("Ground", false);
            m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
            m_AudioSource.volume = 1;
            m_AudioSource.PlayOneShot(m_AudioJump);
        }
        // update player position tracking
        PersistentPlayerSettings.settings.playerPos = m_Rigidbody2D.transform.position;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag ("Collectible")) {
            other.gameObject.GetComponent<Collectible>().OnCollect();
            m_AudioSource.volume = 1;
            m_AudioSource.PlayOneShot(other.gameObject.GetComponent<Collectible>().pickupSound);
            Destroy (other.gameObject);
        } 
        if (other.CompareTag("Slow")) {
            m_MaxSpeed = normalSpeed - slowDown;
        }
        if (other.CompareTag ("TriggerBounds")) {
            //Jumped off the ledge
            gameObject.SetActive(false);
            Canvas lossScreen = GameObject.Find ("LossScreen").GetComponent<Canvas> ();
            lossScreen.enabled = true;
        }
        if (other.CompareTag("Spaceship")) {
            OnSpaceshipEnter();
        }
    }
    
    private void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag("Slow")) {
            m_MaxSpeed = normalSpeed;
        }
        if (other.CompareTag("Spaceship")) {
            OnSpaceshipExit();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Poison")) {
            health -= PersistentLevelSettings.settings.poisonAmount;
        }

        //GameObject other = coll.gameObject;
        //if the other object is a grabbable object, handle grab logic here.
        if (other.GetComponent<GrabbableObject>() != null) {
            //check to see if player wants to grab this object (we can only grab one at a time)
            if (Input.GetButton("Grab") && (m_GrabbedObject == null/* || m_GrabbedObject == other.GetComponent<GrabbableObject>()*/)) {
                m_GrabbedObject = other.GetComponent<GrabbableObject>();
                float xOffset = m_FacingRight ? GRABBED_OBJ_OFFSET_R : GRABBED_OBJ_OFFSET_L;
                m_GrabbedObject.OnGrab(gameObject, xOffset, GRABBED_OBJ_Y_POS);
            }
            else if (Input.GetButtonUp("Grab") && m_GrabbedObject != null) { //otherwise, reset the Grabbable Object to its normal state
                float xOffset = m_FacingRight ? GRABBED_OBJ_OFFSET_R : GRABBED_OBJ_OFFSET_L;
                m_GrabbedObject.EndGrab(gameObject, xOffset, GRABBED_OBJ_Y_POS);
                m_GrabbedObject = null;
            }
        }
    }

    private void OnCollisionStay2D(Collision2D coll)
    {
        
    }

    public void UseItem(string type) {

    }

    public void AddTime(int value) {
        m_extraTime += value;
    }

    public void StopAudio()
    {
        if (m_PlayingFootsteps) {
            m_AudioSource.Stop();
            m_PlayingFootsteps = false;
        }
    }

    private void Flip()
    {
        // Switch the way the player is labelled as facing.
        m_FacingRight = !m_FacingRight;
        
        // Multiply the player's x local scale by -1.
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    /// <summary>
    /// Occurs when the player returns to the spaceship
    /// </summary>
    private void OnSpaceshipEnter()
    {
        m_InSpaceship = true;

        //Deposit the special item in the spaceship if the player has it.
        if (InventoryScript.inventory.HasItemOfType("SpecialItem")) {
            Collectible specialItem = InventoryScript.inventory.GetItemOfType("SpecialItem");
            InventoryScript.inventory.RemoveItemFromPossibleInventory(specialItem);
        }
    }

    /// <summary>
    /// Occurs when the player leaves the spaceship
    /// </summary>
    private void OnSpaceshipExit()
    {
        m_InSpaceship = false;
    }
}

