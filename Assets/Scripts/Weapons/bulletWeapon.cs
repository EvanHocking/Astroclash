using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Astroclash;

public class bulletWeapon : MonoBehaviour
{
    //Bullet aspects that can change (defaults)
    private float fireRate = 3.0f;         //Rounds per second
    private float fireSpread = 0.2f;        //Angle of spread in radians
    private float projectileSpeed = 10.0f;  //Projectile speed in units/second
    private float projectileDamage = 1.0f;  //Damage each projectile deals
    private float projectileRange = 20.0f;  //Projectile range until destroyed
    private float projectileCount = 1.0f;   //Count of projectiles fired
    private float burstTime = 0.33f;        //Time between shots in a burst in seconds
    public GameObject bulletPref;

    public int damageUpgrades = 0;
    public int spreadUpgrades = 0;
    public int countUpgrades = 0;

    private List<float> weaponStats = new List<float>();
    private List<string> statNames = new List<string>()
    {
        "Fire Rate",
        "Shot Spead",
        "Projectile Speed",
        "Projectile Damage",
        "Projectile Range",
        "Projectile Count",
        "Burst Timing"
    };
    private weaponDebugUI debugUI;
    private bool debug = false;
    private bool burst = false;
    private float sumDeltaTime = 0.0f;
    private float burstCount = 0.0f;

    //weapon type flags (behavior flags)
    public bool isSniper = false;
    public bool isMachinegun = false;
    public bool isShotgun = false;
    
    //Weapon Logic
    void Start()
    {
        weaponStats.Add(fireRate);
        weaponStats.Add(fireSpread);
        weaponStats.Add(projectileSpeed);
        weaponStats.Add(projectileDamage);
        weaponStats.Add(projectileRange);
        weaponStats.Add(projectileCount);
        weaponStats.Add(burstTime);

        debugUI = new weaponDebugUI(weaponStats, statNames, this.GetComponent<bulletWeapon>());    
    }
    void Update()
    {
        shootSniper(); //controls burst

        if (Input.GetMouseButton(0))
        {
            shoot();
        }

        //debug tools block
        if (debug)
        {
            Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane);
            Vector3 mouseRealtivePosition = Camera.main.ScreenToWorldPoint(mousePosition);
            if (isMachinegun != true && isShotgun != true)
                Debug.DrawLine(transform.position, mouseRealtivePosition, Color.red);
            drawAngle(fireSpread);
            setWeaponStats();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (debug)
            {
                Debug.Log("Debug Mode Deactivated");
                debug = false;
            }
            else
            {
                Debug.Log("Debug Mode Activated");
                debug = true;
            }

            debugUI.toggle();
        }
    }
    void shoot()
    {
        sumDeltaTime += Time.deltaTime;
        float secondsPerRound = 1.0f / fireRate;

        if (sumDeltaTime >= secondsPerRound)
        {
            //machinegun path logic
            if (isMachinegun)
            {
                //odd streams
                if (projectileCount % 2 == 1 && projectileCount != 1)
                {
                    float temp = projectileCount + 2;
                    float rotationAngle = fireSpread / (temp-1);
                    for (int i = 1; i < (temp - 1)/2; i++)
                    {
                        Vector3 posPoint = rotateVector(transform.right, rotationAngle * (i));
                        Vector3 negPoint = rotateVector(transform.right, -rotationAngle * (i));

                        GameObject bullet1 = Instantiate(bulletPref, transform.position, transform.rotation);
                        GameObject bullet2 = Instantiate(bulletPref, transform.position, transform.rotation);
                        GameObject bullet3 = Instantiate(bulletPref, transform.position, transform.rotation);

                        bullet1.GetComponent<Rigidbody2D>().AddForce(posPoint * projectileSpeed, ForceMode2D.Impulse);
                        bullet1.GetComponent<bulletProjectiles>().setStats(projectileRange, projectileDamage);

                        bullet2.GetComponent<Rigidbody2D>().AddForce(negPoint * projectileSpeed, ForceMode2D.Impulse);
                        bullet2.GetComponent<bulletProjectiles>().setStats(projectileRange, projectileDamage);

                        bullet3.GetComponent<Rigidbody2D>().AddForce(transform.right * projectileSpeed, ForceMode2D.Impulse);
                        bullet3.GetComponent<bulletProjectiles>().setStats(projectileRange, projectileDamage);
                    }
                }
                //even streams
                else if (projectileCount % 2 == 0)
                {
                    if (projectileCount > 2)
                    {
                        Vector3 point1 = rotateVector(transform.right, fireSpread/2);
                        float rotationAngle = fireSpread / projectileCount;
                        rotationAngle -= rotationAngle / projectileCount;
                        float temp = projectileCount + 1;
                        for (int i = 1; i < temp; i++)
                        {
                            Vector3 negPoint = rotateVector(point1, -rotationAngle * (i));
                            GameObject bullet = Instantiate(bulletPref, transform.position, transform.rotation);
                            bullet.GetComponent<Rigidbody2D>().AddForce(negPoint * projectileSpeed, ForceMode2D.Impulse);
                            bullet.GetComponent<bulletProjectiles>().setStats(projectileRange, projectileDamage);
                        }
                    }
                    else
                    {
                        float rotationAngle = fireSpread / 5;
                        Vector3 posPoint = rotateVector(transform.right, rotationAngle);
                        Vector3 negPoint = rotateVector(transform.right, -rotationAngle);

                        GameObject bullet1 = Instantiate(bulletPref, transform.position, transform.rotation);
                        GameObject bullet2 = Instantiate(bulletPref, transform.position, transform.rotation);

                        bullet1.GetComponent<Rigidbody2D>().AddForce(posPoint * projectileSpeed, ForceMode2D.Impulse);
                        bullet1.GetComponent<bulletProjectiles>().setStats(projectileRange, projectileDamage);

                        bullet2.GetComponent<Rigidbody2D>().AddForce(negPoint * projectileSpeed, ForceMode2D.Impulse);
                        bullet2.GetComponent<bulletProjectiles>().setStats(projectileRange, projectileDamage);
                    }
                    
                }
                //single stream
                else
                {
                    GameObject bullet = Instantiate(bulletPref, transform.position, transform.rotation);
                    bullet.GetComponent<Rigidbody2D>().AddForce(transform.right * projectileSpeed, ForceMode2D.Impulse);
                    bullet.GetComponent<bulletProjectiles>().setStats(projectileRange, projectileDamage);
                }
            }
            //sniper path logic
            else if (isSniper && burst == false)
            {
                burst = true;
            }
            //shotgun path logic
            else if (isShotgun)
            {
                for (int i = 0; i < projectileCount; i++)
                {
                    //randomize vector direction to fire based on spread angle
                    float randomAngle = Random.Range(-fireSpread/2, fireSpread/2);
                    Vector3 fireVector = rotateVector(transform.right, randomAngle);

                    GameObject bullet = Instantiate(bulletPref, transform.position, transform.rotation);
                    bullet.GetComponent<Rigidbody2D>().AddForce(fireVector * projectileSpeed, ForceMode2D.Impulse);
                    bullet.GetComponent<bulletProjectiles>().setStats(projectileRange, projectileDamage);
                }
                for (int i = 0; i < projectileCount/2; i++)
                {
                    //randomize vector direction to fire based on spread angle
                    float randomAngle = Random.Range(-fireSpread/2, fireSpread/2);
                    Vector3 fireVector = rotateVector(transform.right, randomAngle);

                    GameObject bullet = Instantiate(bulletPref, transform.position, transform.rotation);
                    bullet.GetComponent<Rigidbody2D>().AddForce(fireVector * projectileSpeed * 0.8f, ForceMode2D.Impulse);
                    bullet.GetComponent<bulletProjectiles>().setStats(projectileRange, projectileDamage);
                }
                for (int i = 0; i < projectileCount/4; i++)
                {
                    //randomize vector direction to fire based on spread angle
                    float randomAngle = Random.Range(-fireSpread/2, fireSpread/2);
                    Vector3 fireVector = rotateVector(transform.right, randomAngle);

                    GameObject bullet = Instantiate(bulletPref, transform.position, transform.rotation);
                    bullet.GetComponent<Rigidbody2D>().AddForce(fireVector * projectileSpeed * 0.6f, ForceMode2D.Impulse);
                    bullet.GetComponent<bulletProjectiles>().setStats(projectileRange, projectileDamage);
                }
            }
            //basic gun logic
            else if (burst == false)
            {
                for (int i = 0; i < projectileCount; i++)
                {
                    //randomize vector direction to fire based on spread angle
                    float randomAngle = Random.Range(-fireSpread/2, fireSpread/2);
                    Vector3 fireVector = rotateVector(transform.right, randomAngle);

                    GameObject bullet = Instantiate(bulletPref, transform.position, transform.rotation);
                    bullet.GetComponent<Rigidbody2D>().AddForce(fireVector * projectileSpeed, ForceMode2D.Impulse);
                    bullet.GetComponent<bulletProjectiles>().setStats(projectileRange, projectileDamage);
                }
            }

            //set time to fire
            sumDeltaTime = 0;
        }
    }
    void shootSniper()
    {
        sumDeltaTime += Time.deltaTime;

        if (burst && isSniper)
        {
            if (sumDeltaTime >= burstTime && burstCount < projectileCount)
            {
                Debug.Log("Firing burst");
                //create bullet and send it
                GameObject bullet = Instantiate(bulletPref, transform.position, transform.rotation);
                bullet.GetComponent<Rigidbody2D>().AddForce(transform.right * projectileSpeed, ForceMode2D.Impulse);
                bullet.GetComponent<bulletProjectiles>().setStats(projectileRange, projectileDamage);
                //reset sum delta
                sumDeltaTime = 0.0f;
                //increment burst count
                burstCount++;
            }
            else if (burstCount >= projectileCount)
            {
                Debug.Log("burst ended");
                //set burst count to zero
                burstCount = 0.0f;
                //set burst to false
                burst = false;
            }
        }
    }
    
    // Debug functions
    void drawAngle(float _angle)
    {
        float halfSpread = _angle / 2;
        Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane);
        Vector3 mouseRealtivePosition = Camera.main.ScreenToWorldPoint(mousePosition);

        Vector3 point1 = rotateVector(mouseRealtivePosition, halfSpread);
        Vector3 point2 = rotateVector(mouseRealtivePosition, -halfSpread);

        if (isSniper == false)
        {
            Debug.DrawLine(transform.position, point1, Color.yellow);
            Debug.DrawLine(transform.position, point2, Color.yellow);
        }

        //debug mode
        if (isMachinegun)
        {
            //odd streams
            if (projectileCount % 2 == 1 && projectileCount != 1)
            {
                projectileCount += 2;
                float rotationAngle = fireSpread / (projectileCount-1);
                for (int i = 1; i < (projectileCount - 1)/2; i++)
                {
                    Vector3 posPoint = rotateVector(mouseRealtivePosition, rotationAngle * (i));
                    Vector3 negPoint = rotateVector(mouseRealtivePosition, -rotationAngle * (i));
                    Debug.DrawLine(transform.position, posPoint, Color.red);
                    Debug.DrawLine(transform.position, negPoint, Color.red);
                    Debug.DrawLine(transform.position, mouseRealtivePosition, Color.red);
                }
            }
            //even streams
            else if (projectileCount % 2 == 0)
            {
                if (projectileCount > 2)
                {
                    float rotationAngle = fireSpread / projectileCount;
                    rotationAngle -= rotationAngle / projectileCount;
                    projectileCount += 1;
                    for (int i = 1; i < projectileCount; i++)
                    {
                        Vector3 negPoint = rotateVector(point1, -rotationAngle * (i));
                        Debug.DrawLine(transform.position, negPoint, Color.red);
                    }
                }
                else
                {
                    float rotationAngle = fireSpread / 5;
                    Vector3 posPoint = rotateVector(mouseRealtivePosition, rotationAngle);
                    Vector3 negPoint = rotateVector(mouseRealtivePosition, -rotationAngle);

                    Debug.DrawLine(transform.position, negPoint, Color.red);
                    Debug.DrawLine(transform.position, posPoint, Color.red);
                }
                
            }
            //single stream
            else
            {
                Debug.DrawLine(transform.position, mouseRealtivePosition, Color.red);
            }
        }

    }
    public void setWeaponStats()
    {
        List<string> input = debugUI.getInput();
        fireRate = float.Parse(input[0]);               
        fireSpread = float.Parse(input[1]);        
        projectileSpeed = float.Parse(input[2]);   
        projectileDamage = float.Parse(input[3]);       
        projectileRange = float.Parse(input[4]);   
        projectileCount = float.Parse(input[5]);  
        burstTime = float.Parse(input[6]);      
    }
    
    // Helper Functions
    private Vector3 rotateVector(Vector3 vector, float angle)
    {
        Vector3 transVector = new Vector3(
            vector.x * Mathf.Cos(angle) - vector.y * Mathf.Sin(angle),
            vector.x * Mathf.Sin(angle) + vector.y * Mathf.Cos(angle),
            vector.z
        );

        return transVector;
    }

    public void upgradeDamage(float amount)
    {
        projectileDamage += amount;
        damageUpgrades++;
    }
    public void upgradeSpread(float amount)
    {
        fireSpread += amount;
        spreadUpgrades++;
    }
    public void upgradeCount(float amount)
    {
        projectileCount += amount;
        countUpgrades++;
    }

    public void setSniper()
    {
        isSniper = true;
    }

    public void setMachinegun()
    {
        isMachinegun = true;
    }

    public void setShotgun()
    {
        isShotgun = true;
    }
}
