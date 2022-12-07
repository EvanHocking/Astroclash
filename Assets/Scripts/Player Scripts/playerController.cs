using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Collections;
using Astroclash;

public class playerController : NetworkBehaviour
{

    //acceleration amount per second added to velocity. de-acceleration is 2:1 proportional to acceleration
    private float acceleration = 1.0f;
    //maximum speed of player ship
    private float maxVelocity = 5.0f;
    private float velocity = 0.0f;
    private Vector3 rotationDieOff = new Vector3(0.0f, 0.0f, 0.0f);
    private int debrisAmount = 3;

    private float money = 5000.0f;
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
    private GameObject scoreBoardUI;
    private GameObject energyBar;
    private GameObject levelUI;
    private GameObject escapeUI;
    private GameObject usernameUI;

    public List<GameObject> weaponRegistry = new List<GameObject>();
    private GameObject healthBar;

    private GameObject UIplates;
    private GameObject healthPlate;
    private GameObject namePlate;

    // player health for UI

    private const float DEFAULT_MAX_HEALTH = 100;
    private float maxHealth = DEFAULT_MAX_HEALTH;
    // private float health = DEFAULT_MAX_HEALTH;
    private int score = 0;

    // private Camera playerCamera;
    // public float health = DEFAULT_MAX_HEALTH;
    public NetworkVariable<float> health = new NetworkVariable<float>(DEFAULT_MAX_HEALTH);
    private float healthFrameValue;
    private bool inCombat = false;
    private bool countDownStaterted = false;
    private bool isDead = false;
    private float combatTimer = 0.0f;

    // player energy for UI
    private const float DEFAULT_MAX_ENERGY = 20.0f;
    private float maxEnergy = DEFAULT_MAX_ENERGY;
    public float energy = DEFAULT_MAX_ENERGY;

    public Camera playerCamera;
    public static string playerName;
    public NetworkVariable<FixedString64Bytes> networkName = new NetworkVariable<FixedString64Bytes>("Anonymous");

    void Start()
    {
        UIplates = gameObject.transform.parent.transform.Find("UI Plates").gameObject;
        // Find nameplate, give name
        namePlate = UIplates.transform.Find("Nameplate").gameObject;
        if (IsOwner)
            setNameServerRpc(playerName);
        // Find health plate and nameplate
        healthPlate = UIplates.transform.Find("Health bar").gameObject;
        healthPlate.GetComponent<UIBar>().SetValue(maxHealth);

        if (IsOwner)
        {
            UIplates.SetActive(false);

            healthFrameValue = health.Value;
            GameObject parent = gameObject.transform.parent.gameObject;
            playerCamera = parent.GetComponentInChildren<Camera>();
            if (playerCamera == null)
                Debug.Log("Player camera is null!");

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
            
            //Find weapon objects
            bulletweaponObject = gameObject.transform.Find("BulletWeapon").gameObject;

            //Find weapon object UI
            bulletUpgradeUI = canvas.transform.Find("BulletUpgradeUI").gameObject;

            levelUI = canvas.transform.Find("Level").gameObject;
            usernameUI = canvas.transform.Find("Username").gameObject;

            escapeUI = canvas.transform.Find("Escape Menu").gameObject;

            bulletweaponObject.GetComponent<bulletWeapon>().setUpgradeUI(bulletUpgradeUI);
            bulletweaponObject.GetComponent<bulletWeapon>().setCanvas(canvas);  //Sets the canvas object to draw debug

            //Find the UILogic object
            UILogic = canvas.transform.Find("UILogic").gameObject;

            //Find score board UI
            scoreBoardUI = canvas.transform.Find("High Score UI").gameObject;

            //setup and turn off canvas and event system
            canvas.transform.SetParent(null);
            eventSystem.transform.SetParent(null);

            // turn off UI elements by default
            spaceStationUI.SetActive(false);
            spaceStationButton.SetActive(false);
            bulletUpgradeUI.SetActive(false);
            shipUpgradeUI.SetActive(false);
            escapeUI.SetActive(false);
        }
        // Remove other non-client player's UI elements and event system
        else if (!IsServer && !IsOwner)
        {
            gameObject.transform.Find("Canvas").gameObject.SetActive(false);
            gameObject.transform.Find("EventSystem").gameObject.SetActive(false);
        }

    }

    // Update is called once per frame
    void Update()
    {
        namePlate.GetComponent<TMP_Text>().text = networkName.Value.ToString();
        // update UI plates to follow camera
        UIplates.transform.position = gameObject.transform.position;
        healthPlate.GetComponent<UIBar>().SetValue(health.Value);

        if (IsOwner)
        {
            usernameUI.GetComponent<TMP_Text>().text = networkName.Value.ToString();

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

            if (Input.GetKey(KeyCode.Escape))
            {
                escapeUI.SetActive(true);
            }

            currencyUI.GetComponent<TMP_Text>().text = money.ToString();
            if (inCombat && countDownStaterted == false)
            {
                StartCoroutine(combatTimerRoutine());
                countDownStaterted = true;
            }
            else if (!inCombat)
            {
                repair();
            }
            

            if (!Input.GetKey(KeyCode.Space))
            {
                recharge();
            }

            healthBar.GetComponent<UIBar>().SetValue(healthFrameValue);

            if (healthFrameValue != health.Value)
            {
                setHealthServerRpc(healthFrameValue);
            }

            checkDeath();
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
            Debug.Log("Hit by enemy bullet!");
            
            // TakeDamage(collider.gameObject.GetComponent<bulletProjectiles>().getDamage());
            TakeDamage(collider.gameObject.GetComponent<bulletProjectiles>().getSpawnerID(), collider.gameObject.GetComponent<bulletProjectiles>().getDamage());

            // get the spawner ID of the bullet
            // send ServerRPC that executes only on the client with the spawner ID with message "DAMAGE"
            // if client ID matches spawner id, award 10 points
            Debug.Log("Despawning Bullet");
            despawnBulletServerRpc(collider.gameObject.GetComponent<NetworkObject>().NetworkObjectId);
            Debug.Log("Despawned Bullet!");
            inCombat = true;
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

    [ServerRpc]
    private void testMessageServerRpc(string message, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log(message);
    }

    // make sure to pass in spawner id
    //private void TakeDamage(float damage)
    private void TakeDamage(ulong spawnerID, float damage)
    {
        healthFrameValue -= damage;
        inCombat = true;
        countDownStaterted = false;
        combatTimer = 0.0f;

        Debug.Log("TAKE DAMAGE FUNCTION ACCESSED");

        healthBar.GetComponent<UIBar>().SetValue(healthFrameValue);
        awardPointsServerRpc(spawnerID, "DAMAGE");
        //awardPointsClientRpc(spawnerID, "DAMAGE");

        // pass in spawnder id
        // checkDeath();
        checkDeathByBullet(spawnerID);
    }

    [ServerRpc]
    private void awardPointsServerRpc(ulong spawnerID, string message, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log("Server received RPC message about " + message);
        awardPointsClientRpc(spawnerID, message);
    }

    [ClientRpc]
    private void awardPointsClientRpc(ulong spawnerID, string message)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { spawnerID }
            }
        };

        Debug.Log("Client received RPC message about " + message);

        int _score = Player.Instance.getScore();
        if (message == "DAMAGE")
        {
            testMessageServerRpc("DAMAGE");
            int finalScore = _score + 10;
            Player.Instance.setScore(finalScore);
        }
        else if (message == "DEATH")
        {
            testMessageServerRpc("DEATH");
            int finalScore = _score + 90;
            Player.Instance.setScore(finalScore);
        }
    }

    private IEnumerator combatTimerRoutine()
    {
        while (combatTimer <= 5.0f)
        {
            combatTimer += Time.deltaTime;
            yield return null;
        }
        inCombat = false;
        countDownStaterted = false;
    }
    
    private IEnumerator deathTimerRoutine()
    {
        float deathTimer = 0.0f;
        while (deathTimer <= 3.0f)
        {
            deathTimer += Time.deltaTime;
            yield return null;
        }

        Debug.Log("De-Spawning player");
        //SceneManager.LoadScene("DeathScreen");
        ulong clientID = gameObject.transform.parent.gameObject.GetComponent<NetworkObject>().OwnerClientId;
        ulong objectID = gameObject.transform.parent.gameObject.GetComponent<NetworkObject>().NetworkObjectId;
        despawnPlayerServerRpc(clientID, objectID);
    }
    
    private void repair()
    {
        if (healthFrameValue + (repairAmount * Time.deltaTime) <= maxHealth)
        {
            healthFrameValue += (repairAmount * 10.0f *  Time.deltaTime);
        }
        else if (health.Value + (repairAmount * Time.deltaTime) > maxHealth && healthFrameValue != maxHealth)
        {
            healthFrameValue = maxHealth;
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
    
    //private void checkDeath()
    private void checkDeathByBullet(ulong spawnerID)
    {
        if (healthFrameValue <= 0 && isDead == false)
        {
            isDead = true;
            spawnDebrisServerRpc(gameObject.transform.position);
            disablePlayerServerRpc(gameObject.transform.parent.GetComponent<NetworkObject>().NetworkObjectId);
            awardPointsServerRpc(spawnerID, "DEATH");



            // send ServerRPC with spawner id with message "DEAD"
            // if spawner id matches client id, award them 90 points
            // will give player 10 for hitting and 90 for killing with 100 for total
            canvas.SetActive(false);
            StartCoroutine(deathTimerRoutine());
        }
    }

    private void checkDeath()
    {
        if (healthFrameValue <= 0 && isDead == false)
        {
            isDead = true;
            spawnDebrisServerRpc(gameObject.transform.position);
            disablePlayerServerRpc(gameObject.transform.parent.GetComponent<NetworkObject>().NetworkObjectId);
            canvas.SetActive(false);
            StartCoroutine(deathTimerRoutine());
        }
    }

    public void setHealthFrame(float _frame)
    {
        healthFrameValue = _frame;
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
        return health.Value;
    }
    public float getMaxHealth()
    {
        return maxHealth;
    }
    [ServerRpc]
    public void setHealthServerRpc(float _health)
    {
        health.Value = _health;
    }
    public void setMaxHealth(float _maxHealth)
    {
        maxHealth = _maxHealth;
        healthBar.GetComponent<UIBar>().SetMaxValue(maxHealth);
        healthBar.GetComponent<UIBar>().increaseBar();
        updateUIPlateServerRpc(gameObject.transform.parent.gameObject.GetComponent<NetworkObject>().NetworkObjectId, _maxHealth);
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
    public int getScore()
    {
        return score;
    }
    public void setScore(int _score)
    {
        score = _score;
    }
    [ServerRpc]
    public void setNameServerRpc(string name)
    {
        networkName.Value = name;
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
                TargetClientIds = new ulong[] { _clientID }
            }
        };
        loadDeathSceneClientRpc(clientRpcParams);
        GameObject.FindGameObjectWithTag("Spawn Manager").GetComponent<SpawnManager>().despawnEntity(_objectID);
    }
    [ServerRpc(RequireOwnership = false)]
    private void despawnBulletServerRpc(ulong _bulletID)
    {
        NetworkManager.SpawnManager.SpawnedObjects[_bulletID].gameObject.GetComponent<NetworkObject>().Despawn();
    }
    [ServerRpc]
    private void disablePlayerServerRpc(ulong _playerID)
    {
        disablePlayerClientRpc(_playerID);
    }
    [ServerRpc]
    private void updateUIPlateServerRpc(ulong _playerID, float _health)
    {
        updateUIPlateClientRpc(_playerID, _health);
    }

    [ClientRpc]
    private void spawnDebrisClientRpc(Vector3 _position)
    {
        UnityEngine.Object debris = Resources.Load("prefabs/Player Debris");
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
        SceneManager.LoadScene("DeathScreen");
        GameObject.Destroy(GameObject.Find("Network Manager"));
    }
    [ClientRpc]
    private void disablePlayerClientRpc(ulong _playerID)
    {
        GameObject targetPlayer = NetworkManager.SpawnManager.SpawnedObjects[_playerID].transform.Find("Player").gameObject;
        GameObject UIPlate = NetworkManager.SpawnManager.SpawnedObjects[_playerID].transform.Find("UI Plates").gameObject;
        targetPlayer.GetComponent<playerController>().enabled = false;
        targetPlayer.GetComponent<SpriteRenderer>().enabled = false;
        targetPlayer.GetComponent<BoxCollider2D>().enabled = false;
        UIPlate.SetActive(false);
    }
    [ClientRpc]
    private void updateUIPlateClientRpc(ulong _playerID, float _health)
    {
        Debug.Log("Updating hover health bar");
        GameObject plateUI = NetworkManager.SpawnManager.SpawnedObjects[_playerID].gameObject.transform.Find("UI Plates").gameObject;
        GameObject healthbar = plateUI.transform.Find("Health bar").gameObject;
        healthbar.GetComponent<UIBar>().SetMaxValue(_health);
    }
}