using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class bulletProjectiles : NetworkBehaviour
{
    private float range = 0.0f;
    private float damage = 0.0f;
    private ulong clientID;
    private Vector3 startPosition = new Vector3();
    public bool isPlayerBullet = false;

    void Start()
    {
        startPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        //calculate distance traveled
        float distance = Mathf.Sqrt(Mathf.Pow((transform.position.x - startPosition.x), 2) + Mathf.Pow((transform.position.y - startPosition.y), 2));
        if (distance > range)
        {
           GameObject.Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.name == "Space Station")
        {
            GameObject.Destroy(gameObject);
        }
    }

    public void setStats(float _range, float _damage, ulong _clientID)
    {
        range = _range;
        damage = _damage;
        clientID = _clientID;
        isPlayerBullet = true;
    }

    public void setStats(float _range, float _damage)
    {
        range = _range;
        damage = _damage;
    }

    public float getDamage()
    {
        return damage;
    }

    public ulong getSpawnerID()
    {
        return clientID;
    }
}
