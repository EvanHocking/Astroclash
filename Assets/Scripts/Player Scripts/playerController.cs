using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.SceneManagement;
using TMPro;

public class playerController : NetworkBehaviour
{

    //acceleration amount per second added to velocity. de-acceleration is 2:1 proportional to acceleration
    private float acceleration = 1.0f;
    //maximum speed of player ship
    private float maxVelocity = 3.0f;
    private float velocity = 0.0f;
    private Vector3 rotationDieOff = new Vector3(0.0f, 0.0f, 0.0f);
    private int debrisAmount = 3;

    private float money = 10000.0f;
    private float repairAmount = 1.0f; //hull repaired passively per second
    private float rechargeRate = 10.0f;

    public GameObject UILogic;
    private GameObject canvas;
    private GameObject eventSystem;
    private GameObject bulletweaponObject;
    private GameObject bulletUpgradeUI;
    private GameObject spaceStationUI;
    private GameObject shipUpgradeUI;
    private GameObject spaceStationButton;
    private GameObject currencyUI;
    private GameObject energyBar;
    private GameObject levelUI;

    public List<GameObject> weaponRegistry = new List<GameObject>();
    private GameObject healthBar;

    // player health for UI
    
    private const float DEFAULT_MAX_HEALTH = 100;
    private float maxHealth = DEFAULT_MAX_HEALTH;
    public float health = DEFAULT_MAX_HEALTH;

    // player energy for UI
    private const float DEFAULT_MAX_ENERGY = 20.0f;
    private float maxEnergy = DEFAULT_MAX_ENERGY;
    public float energy = DEFAULT_MAX_ENERGY;
    
    // player currency for round
    private NetworkVariable<float> credits = new NetworkVariable<float>();

    private Camera playerCamera;

    void Start()
    {
        if (IsOwner)
        {
            GameObject parent = gameObject.transform.parent.gameObject;
            playerCamera = parent.GetComponentInChildren<Camera>();

            //Find canvas/event system
            canvas = gameObject.transform.Find("Canvas").gameObject;
            eventSystem = gameObject.transform.Find("EventSystem").gameObject;

            canvas.GetComponent<Canvas>().worldCamera = playerCamera;
            canvas.GetComponent<Canvas>().planeDistance = 10;

            //General UI (Health, Currency, ETC.)
            currencyUI = canvas.transform.Find("Currency UI").gameObject;

            //Find space station UI
            spaceStationUI = canvas.transform.Find("Space Station UI").gameObject;
            spaceStationButton = canvas.transform.Find("Space UI Button").gameObject;
            shipUpgradeUI = canvas.transform.Find("ShipUpgradeUI").gameObject;

            //Find health bar, give base health
            //healthBar = GameObject.Find("Health bar");
            healthBar = canvas.transform.Find("Health bar").gameObject;
            healthBar.GetComponent<UIBar>().SetMaxValue(maxHealth);

            energyBar = canvas.transform.Find("Energy Bar").gameObject;
            energyBar.GetComponent<UIBar>().SetMaxValue(maxEnergy);

            //Set initial credits for round
            credits.Value = 0;

            //Find weapon objects
            bulletweaponObject = gameObject.transform.Find("BulletWeapon").gameObject;

            //Find weapon object UI
            bulletUpgradeUI = canvas.transform.Find("BulletUpgradeUI").gameObject;

            levelUI = canvas.transform.Find("Level").gameObject;

            bulletweaponObject.GetComponent<bulletWeapon>().setUpgradeUI(bulletUpgradeUI);
            bulletweaponObject.GetComponent<bulletWeapon>().setCanvas(canvas);  //Sets the canvas object to draw debug

            //Find the UILogic object
            UILogic = canvas.transform.Find("UILogic").gameObject;

            //setup and turn off canvas and event system
            canvas.transform.SetParent(null);
            eventSystem.transform.SetParent(null);

            // turn off UI elements by default
            spaceStationUI.SetActive(false);
            spaceStationButton.SetActive(false);
            bulletUpgradeUI.SetActive(false);
            shipUpgradeUI.SetActive(false);
        }
        // Remove other non-client player's UI elements and event system
        else
        {
            gameObject.transform.Find("Canvas").gameObject.SetActive(false);
            gameObject.transform.Find("EventSystem").gameObject.SetActive(false);
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (IsOwner)
        {
            //update the camera origin
            Vector3 newPosition = playerCamera.transform.position;
            newPosition.x = gameObject.transform.position.x;
            newPosition.y = gameObject.transform.position.y;
            playerCamera.transform.position = newPosition;

            //Movement is broken down into: X = Movement_Speed * cos(rotation_angle) Y = Movement_Speed * sin(rotation_angle)
            float angle = (float)(transform.eulerAngles.z * (Math.PI / 180));
            transform.position += new Vector3(velocity * (float)Math.Cos(angle), velocity * (float)Math.Sin(angle), 0) * Time.deltaTime;
            transform.Rotate(rotationDieOff * Time.deltaTime);
            rotationDieOff -= rotationDieOff * 0.50f * Time.deltaTime;

            int shipLevel = UILogic.GetComponent<shipUpgrades>().getShipLevel();
            int bulletWeaponLevel = bulletweaponObject.GetComponent<bulletWeapon>().getController().getWeaponLevel();

            int totalLevel = shipLevel + bulletWeaponLevel;

            levelUI.GetComponent<TMP_Text>().text = "Lv. " + totalLevel.ToString();

            if (Input.GetKey(KeyCode.W))
            {
                //adding player speed, until max speed is reached
                if (velocity + acceleration <= maxVelocity)
                {
                    velocity += acceleration * Time.deltaTime;
                }
                else if ((velocity + acceleration) >= maxVelocity && velocity + 0.25f <= maxVelocity)
                {
                    velocity += 0.25f * Time.deltaTime;
                }
                else
                {
                    velocity = maxVelocity;
                }
            }
            //Slow down
            else if (Input.GetKey(KeyCode.S))
            {
                if ((velocity - acceleration) > 0)
                {
                    velocity -= (acceleration) * Time.deltaTime;
                }
                else if ((velocity - acceleration) < 0 && velocity - 0.25f >= 0)
                {
                    velocity -= 0.25f * Time.deltaTime;
                }
                else
                {
                    velocity = 0.0f;
                }
            }

            currencyUI.GetComponent<TMP_Text>().text = money.ToString();
            repair();

            if (!Input.GetKey(KeyCode.Space))
            {
                recharge();
            }
            

            healthBar.GetComponent<UIBar>().SetValue(health);
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (IsOwner)
        {
            if (collider.gameObject.name == "Space Station")
            {
                UILogic.GetComponent<UIRegistrar>().enableIndex(0);
                disableWeapons();
            }

        }

        // bullet weapon interaction logic
        if (collider.gameObject.tag == "enemyBullet" && IsOwner)
        {
            GameObject.Destroy(collider.gameObject);
            TakeDamage(collider.gameObject.GetComponent<bulletProjectiles>().getDamage());
        }

        // logic to destroy the bullet that collided with other objects
        if (collider.gameObject.tag == "friendlyBullet" && IsOwner == false)
        {
            GameObject.Destroy(collider.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (IsOwner)
        {
            if (collider.gameObject.name == "Space Station")
            {
                UILogic.GetComponent<UIRegistrar>().disableAll();
                enableWeapons();
            }   
        }
    }

    private void TakeDamage(float damage)
    {
        health -= damage;

        healthBar.GetComponent<UIBar>().SetValue(health);

        if (health <= 0)
        {
            spawnDebrisServerRpc(gameObject.transform.position);
            ulong clientID = gameObject.transform.parent.gameObject.GetComponent<NetworkObject>().OwnerClientId;
            ulong objectID = gameObject.transform.parent.gameObject.GetComponent<NetworkObject>().NetworkObjectId;
            despawnPlayerServerRpc(clientID, objectID);
        }
    }

    private void repair()
    {
        if (health + (repairAmount * Time.deltaTime) <= maxHealth)
        {
            health += repairAmount * Time.deltaTime;
        }
        else if (health + (repairAmount * Time.deltaTime) > maxHealth)
        {
            health = maxHealth;
        }
    }
    private void recharge()
    {
        if (energy + (rechargeRate * Time.deltaTime) <= maxEnergy)
        {
            energy += rechargeRate * Time.deltaTime;
            setEnergy(energy);
        }
        else if (energy + (rechargeRate * Time.deltaTime) <= maxEnergy)
        {
            energy = maxEnergy;
            setEnergy(energy);
        }
    }
    private void disableWeapons()
    {
        for (int i = 0; i < weaponRegistry.Count; i++)
        {
            weaponRegistry[i].SetActive(false);
        }
    }
    private void enableWeapons()
    {
        for (int i = 0; i < weaponRegistry.Count; i++)
        {
            weaponRegistry[i].SetActive(true);
        }
    }

    // Setters and Getters
    public float getCurrency()
    {
        return money;
    }
    public void addCurrency(float _money)
    {
        money += _money;
    }
    public void subtractCurrency(float _money)
    {
        money -= _money;
    }
    public float getHealth()
    {
        return health;
    }
    public float getMaxHealth()
    {
        return maxHealth;
    }
    public void setHealth(float _health)
    {
        health = _health;
    }
    public void setMaxHealth(float _maxHealth)
    {
        maxHealth = _maxHealth;
        healthBar.GetComponent<UIBar>().SetMaxValue(maxHealth);
        healthBar.GetComponent<UIBar>().increaseBar();
    }
    public float getMaxVelocity()
    {
        return maxVelocity;
    }
    public void setMaxVelocity(float _maxVelocity)
    {
        maxVelocity = _maxVelocity;
    }
    public float getVelocity()
    {
        return velocity;
    }
    public float getAcceleration()
    {
        return acceleration;
    }
    public void setAcceleration(float _acceleration)
    {
        acceleration = _acceleration;
    }
    public float getRepair()
    {
        return repairAmount;
    }
    public void setRepair(float _repairAmount)
    {
        repairAmount = _repairAmount;
    }
    public void setEnergy(float _energy)
    {
        energy = _energy;
        energyBar.GetComponent<UIBar>().SetValue(energy);
    }
    public float getEnergy()
    {
        return energy;
    }
    public float getMaxEnergy()
    {
        return maxEnergy;
    }
    public void setMaxEnergy(float _maxEnergy)
    {
        maxEnergy = _maxEnergy;
        energyBar.GetComponent<UIBar>().SetMaxValue(maxEnergy);
        energyBar.GetComponent<UIBar>().increaseBar();
    }
    public void setRechargeRate(float _recharge)
    {
        rechargeRate = _recharge;
    }
    public float getRechargeRate()
    {
        return rechargeRate;
    }

    //handles player despawn sync
    [ServerRpc]
    private void spawnDebrisServerRpc(Vector3 _position)
    {
        spawnDebrisClientRpc(_position);
    }
    [ServerRpc]
    private void despawnPlayerServerRpc(ulong _clientID, ulong _objectID)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{_clientID}
            }
        };
        loadDeathSceneClientRpc(clientRpcParams);
        GameObject.FindGameObjectWithTag("Spawn Manager").GetComponent<SpawnManager>().despawnEntity(_objectID);
    }
    [ClientRpc]
    private void spawnDebrisClientRpc(Vector3 _position)
    {
        UnityEngine.Object debris = Resources.Load("prefabs/Debris");
        for (int i = 0; i < debrisAmount; i++)
        {
            Instantiate(debris, _position, Quaternion.identity);
        }
    }
    [ClientRpc]
    private void loadDeathSceneClientRpc(ClientRpcParams clientRpcParams = default)
    {
        NetworkManager.Singleton.Shutdown();
        // SceneManager.LoadScene("DeathScreen");
        SceneManager.LoadScene("EvanDeathScreen");
        GameObject.Destroy(GameObject.Find("Network Manager"));
    }
}