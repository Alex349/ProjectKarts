﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnItem : MonoBehaviour
{

    public GameObject item;
    private GameObject item_Instance;

    public float itemCounter = 10;
    public float deltaRotation = 5;

    void Start()
    {
        item_Instance = Instantiate(item, new Vector3(transform.position.x,
                        transform.position.y, transform.position.z),
                        this.transform.rotation, this.transform);
    }

    void Update()
    {
        if (item_Instance != null)
        {
            item_Instance.transform.Rotate(new Vector3(0, 1, 0), deltaRotation * Time.deltaTime);
           // item_Instance.transform.position = new Vector3(transform.position.x, transform.position.y + (Mathf.Sin(Time.time) * 0.2f), transform.position.z);
        }
        if (item_Instance == null)
        {
            IsDestroyed();
            itemCounter -= Time.deltaTime;

            if (itemCounter <= 0)
            {
                item_Instance = Instantiate(item, new Vector3(transform.position.x,
                                transform.position.y, transform.position.z),
                                this.transform.rotation, this.transform);
                itemCounter = 10;
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(transform.position, new Vector3 (1,1,1));
    }
    void IsDestroyed()
    {
       
    }
}
