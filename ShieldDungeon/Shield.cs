using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : MonoBehaviour
{
    #region Enums
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
    }
    public enum BulletEffect
    {
        Piercing,
        Split,
        Explosion,
        NoEffect
    }
    #endregion

    #region Fields
    private int _amountofPoints;
    private int _bulletEffectLvl = 0;
    private GameObject _player;
    private PlayerBehaviour _playerScript;
    #endregion

    #region Properties
    public Rarity GetRarity { get; private set; }
    public BulletEffect GetEffect { get; private set; } = BulletEffect.NoEffect;
    #endregion

    #region Functions
    private void Awake()
    {
        switch (GetRarity)
        {
            case Rarity.Common:
                _amountofPoints = 1;
                gameObject.GetComponentInChildren<Renderer>().material.color = Color.white;
                break;
            case Rarity.Uncommon:
                _amountofPoints = 2;
                gameObject.GetComponentInChildren<Renderer>().material.color = Color.green;
                break;
            case Rarity.Rare:
                _amountofPoints = 3;
                gameObject.GetComponentInChildren<Renderer>().material.color = Color.blue;
                break;
        }

        while (_amountofPoints > 0)
        {
            AddBulletEffect();
        }
        _player = GameObject.Find("Player(Clone)");
        
        if (GetComponent<Rigidbody>())
        {
            Destroy(GetComponent<Rigidbody>());
        }
    }

    private void Start()
    {
        _playerScript = _player.GetComponent<PlayerBehaviour>();
    }

    public void Create(Rarity rarity, Color color)
    {
        GetRarity = rarity;

        switch (rarity)
        {
            case Rarity.Common:
                _amountofPoints = 0;
                break;
            case Rarity.Uncommon:
                _amountofPoints = 1;
                break;
            case Rarity.Rare:
                _amountofPoints = 2;
                break;
        }

        while (_amountofPoints > 0)
        {
            AddBulletEffect();
        }

        gameObject.GetComponentInChildren<Renderer>().material.color = color;
    }

    private void AddBulletEffect()
    {
        if (GetEffect == BulletEffect.NoEffect)
        {
            int amountOfEffects = System.Enum.GetValues(typeof(BulletEffect)).Length;
            GetEffect = (BulletEffect)Random.Range(0, amountOfEffects);
        }

        _bulletEffectLvl++;
        _amountofPoints--;
    }

    public void Swap(Shield otherShield)
    {
        Vector3 shieldPos = otherShield.transform.position;
        Quaternion shieldRotation = otherShield.transform.rotation;

        otherShield.transform.rotation = transform.rotation;
        transform.rotation = shieldRotation;

        otherShield.transform.position = transform.position;
        transform.position = shieldPos;

        transform.parent = null;
        otherShield.transform.parent = _player.transform;
        if (otherShield.GetComponent<Rigidbody>())
        {
            Destroy(otherShield.GetComponent<Rigidbody>());
        }
        _playerScript.EquippedShield = otherShield;
    }

    public void Equip()
    {
        if (GetComponent<Rigidbody>())
        {
            Destroy(GetComponent<Rigidbody>());
        }
        Transform shieldPos = _playerScript.ShieldSocket;
        transform.position = shieldPos.position;
        transform.rotation = shieldPos.rotation;

        transform.parent = _player.transform;
    }

    public void ThrowShield(int throwStrength)
    {
        transform.parent = null;
        gameObject.AddComponent<Rigidbody>();
        GetComponent<Rigidbody>().AddForce(transform.forward * throwStrength);
        _playerScript.EquippedShield = null;
    }

    public void UpdateBullet(BasicProjectile bullet, Vector3 reflectVector)
    {

        switch (GetEffect)
        {
            case BulletEffect.Piercing:
                bullet.Piercing = _bulletEffectLvl;
                break;
            case BulletEffect.Split:
                bullet.SplitBullet(_bulletEffectLvl + 1, reflectVector);
                Destroy(bullet.gameObject);
                break;
            case BulletEffect.Explosion:
                bullet.AddExplosionRadius(_bulletEffectLvl);
                break;
            case BulletEffect.NoEffect:
                break;
            default:
                Debug.Log("No valid bulletEffect in UpdateBullet function");
                break;
        }
    }

    void OnCollisionEnter(Collision other)
    {
        
        if (other.collider.gameObject.tag == "Enemy")
        {
            int damage = (int)GetComponent<Rigidbody>().velocity.magnitude * 3;
            if (damage == 0)
            {
                damage = 5;
            }
            other.collider.GetComponent<Health>().Damage(damage);
            _playerScript.AddShieldDamageDone(damage);
        }
    }
    #endregion
}
