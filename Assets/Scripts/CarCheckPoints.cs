﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarCheckPoints : MonoBehaviour
{
    public Transform[] checkPointArray; //Checkpoint GameObjects stored as an array
    public int currentCheckpoint = 0; //Current checkpoint
    public int currentLap; //Current lap

    public int currentCheckpointReal = 0, expectedCheckpoint = 0, previousCheckpoint = 0;

    void Start()
    {
        for (int i = 0; i < checkPointArray.Length; i++)
        {
           // checkPointArray[i] = GameObject.Find("Checkpoint").transform;
        }        
    }
}

