using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;
using TMPro;

public class PlayerBehaviour : MonoBehaviour
{
    #region Editor Fields
    [SerializeField] private GameObject _interactButton = null;
    //camera should be default playerinputspace so that the player always moves in the direction of the camera
    [SerializeField] private Transform _playerInputSpace = default;
    [SerializeField] public Transform ShieldSocket;
    #endregion

    #region Fields
    private Plane _cursorMovementPlane;
    private Health _playerHealth;
    //Reset Data
    private float _dstWalked = 0.0f;
    private int _dmgDoneWithShieldThrow = 0;

    private int _defaultThrowStrength = 550;
    private float _defaultMoveSpeed = 5.0f;
    //Shield Variables
    private int _throwStrength;
    private Shield[] _shieldsList;
    //Movement
    private Rigidbody _body;
    private Vector3 _velocity;
    private Vector3 _desiredLookatPoint = Vector3.zero;
    private float _moveSpeed;
    #endregion

    #region Properties
    public bool _isPlayerDead { get; private set; } = false;
    public Shield EquippedShield { get; set; }
    #endregion

    #region Functions
    void Awake()
    {
        LoadPlayerData();
        _body = GetComponent<Rigidbody>();
        _playerHealth = GetComponent<Health>();
        _cursorMovementPlane = new Plane(new Vector3(0.0f, 1.0f, 0.0f), transform.position);

    }

    void Start()
    {
        _playerInputSpace = FindObjectOfType<Camera>().transform;
        _interactButton = GameObject.Find("InteractButton");
        _shieldsList = FindObjectsOfType<Shield>();
    }

    #region PlayerData
    //Loading and saving player data
    private void SavePlayerData()
    {
        //Save data
        PlayerData playerData = new PlayerData();        
        playerData.DstWalked = _dstWalked;
        playerData.DmgDoneWithShieldThrow = _dmgDoneWithShieldThrow;
        //Write to file
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/gamesave.save");
        bf.Serialize(file, playerData);
        file.Close();
    }

    private void LoadPlayerData()
    {
        if (SceneManager.GetActiveScene().name != "TutorialLevel")
        {
            if (File.Exists(Application.persistentDataPath + "/gamesave.save"))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(Application.persistentDataPath + "/gamesave.save", FileMode.Open);
                PlayerData playerData = (PlayerData) bf.Deserialize(file);
                file.Close();

                //Calculate throwStrength && movement speed
                _throwStrength = playerData.CalculateNewThrowStrength();
                _moveSpeed = playerData.CalculateNewMovementSpeed();
                Debug.Log("Game Loaded");
            }
            else
            {
                Debug.Log("No game saved!");
            }
        }
        else
        {
            _throwStrength = _defaultThrowStrength;
            _moveSpeed = _defaultMoveSpeed;
        }
    }
    #endregion

    void Update()
    {
        if (_playerHealth._currentHealth <= 0)
        {
            SavePlayerData();
            _isPlayerDead = true;
        }
        //Pause game on escape press
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Break();
        }
        HandleMovementInput();

        _desiredLookatPoint.y = 0; // keep only the horizontal direction
        transform.rotation = Quaternion.LookRotation(_desiredLookatPoint);

        HandleShieldEquip();
        if (IsShieldEquipped())
        {
            HandleShieldThrow();
        }
        else
        {
            if (EquippedShield != null)
            {
                EquippedShield = null;
            }
        }

        //Update player position for use in shaders
        Shader.SetGlobalVector("_PlayerPos", transform.position);
    }

    private void FixedUpdate()
    {
        _body.velocity = _velocity;
        _dstWalked += Vector3.Distance(transform.position, transform.position + _body.velocity);
    }

    #region ShieldInteractions
    private void HandleShieldEquip()
    {
        float closestShieldDistance = Single.MaxValue;
        Shield closestShield = EquippedShield;
        float dstToPlayer = 0.0f;
        //Find the shield closest to the player
        foreach (Shield shield in _shieldsList)
        {
            dstToPlayer = Vector3.Distance(transform.position, shield.transform.position);
            if (dstToPlayer < closestShieldDistance && shield != EquippedShield && dstToPlayer < 5.0f)
            {
                closestShieldDistance = dstToPlayer;
                closestShield = shield;
            }
        }

        if (closestShield != EquippedShield)
        {
            _interactButton.SetActive(true);
            _interactButton.transform.position = closestShield.transform.position;
            _interactButton.transform.Translate(0.0f, 2.0f, 0.0f);
            //Swap out currently equipped shield with the new shield close to the player
            if (Input.GetButtonDown("Equip"))
            {
                if (IsShieldEquipped())
                {
                    EquippedShield.Swap(closestShield);
                }
                else
                {
                    closestShield.Equip();
                }
                EquippedShield = closestShield;
                _interactButton.SetActive(false);
            }
        }
        else if(dstToPlayer > 3.0f)
        {
            _interactButton.SetActive(false);
        }
    }

    public bool IsShieldEquipped()
    {
        return (EquippedShield != null);
    }
    public void AddShieldDamageDone(int damage)
    {
        _dmgDoneWithShieldThrow += damage; 
    }
    private void HandleShieldThrow()
    {
        if (IsShieldEquipped())
        {
            if (Input.GetButtonDown("ThrowShield"))
            {
                EquippedShield.ThrowShield(_throwStrength);
            }
        }
    }
    #endregion

    #region PlayerMovement
    void HandleMovementInput()
    {
        //Movement
        Vector2 playerInput;
        playerInput.x = Input.GetAxisRaw("MovementHorizontal");
        playerInput.y = Input.GetAxisRaw("MovementVertical");

        _velocity = new Vector3(playerInput.x, 0f, playerInput.y) * _moveSpeed;

        if (_playerInputSpace)
        {
            Vector3 forward = _playerInputSpace.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = _playerInputSpace.right;
            right.y = 0f;
            right.Normalize();
            _velocity = (forward * playerInput.y + right * playerInput.x) * _moveSpeed;
        }
        Debug.Log(_velocity);


        //Rotation
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;

        Vector3 positionOfMouseInWorld = transform.position;
        if (Physics.Raycast(mouseRay, out hitInfo, 10000.0f, LayerMask.GetMask("Ground")))
        {
            positionOfMouseInWorld = hitInfo.point;
        }
        else
        {
            _cursorMovementPlane.Raycast(mouseRay, out float distance);
            positionOfMouseInWorld = mouseRay.GetPoint(distance);
        }

        _desiredLookatPoint = positionOfMouseInWorld - transform.position;
    }

    private void HandleRotation()
    {
        transform.LookAt(_desiredLookatPoint, Vector3.up);
    }
    #endregion
    #endregion
}
