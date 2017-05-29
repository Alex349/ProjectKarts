﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class coinScript : MonoBehaviour
{
    void OnTriggerEnter(Collider col)
    {

        if (col.tag == "Player" && col.GetComponent<m_carItem>().money > 10)
        {
            col.GetComponent<m_carItem>().money++;
        }
        else if (col.tag == "Kart" && col.GetComponent<IA_Item>().money > 10)
        {
            col.GetComponent<IA_Item>().money++;

        }

        Destroy(this.gameObject);

    }
}
