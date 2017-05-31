﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class m_carController : MonoBehaviour
{

    public float g_RPM = 500f;
    public float max_RPM = 1000f;
    public Rigidbody m_rigidbody;

    public Transform centerOfGravity;

    public WheelCollider wheelFR;
    public WheelCollider wheelFL;
    public WheelCollider wheelBR;
    public WheelCollider wheelBL;

    private ParticleSystem m_particleSystem1;
    private ParticleSystem m_particleSystem2;

    public float baseAcc;
    public float currentAcc;
    public float gravity = 9.81f;
    public float turnRadius = 6f;
    public float torque = 100f;
    public float brakeTorque = 100f;
    public float frontMaxSpeed = 100f;
    public float rearMaxSpeed = 100f;
    private float maxSpeed;
    public enum DriveMode {Front, Rear, Drift, Stoped, All};
    public float turboForce, startTurboForce;
    public float miniTurboForce, endDriftTurboForce;

    public DriveMode driveMode = DriveMode.All;

    private float timeCounter, slowDownCounter;
    private float scaledTorque;

    private GameObject[] nodes;
    private Vector3 distanceToRespawnPoint;

    private float cameraSpeed = 0.01f;
    public float AntiRoll = 1000f;

    private WheelFrictionCurve wheelBLDriftFriction;
    private WheelFrictionCurve wheelBRDriftFriction;
    private WheelFrictionCurve wheelFRDriftFriction;
    private WheelFrictionCurve wheelFLDriftFriction;

    private WheelFrictionCurve wheelBLFrontFriction;
    private WheelFrictionCurve wheelBRFrontFriction;
    private WheelFrictionCurve wheelFLFrontFriction;
    private WheelFrictionCurve wheelFRFrontFriction;

    private float wheelFRDamp, wheelFLDamp, wheelBRDamp, wheelBLDamp;

    private float driftForce;
    public float currentSpeed;
    private float driftCounter = 2f;
    private float rebufoCounter = 0;
    private m_carHUD m_hud;

    private bool Drifting;
    private float driftDelay = 1f;
    public bool leftDrift, rightDrift;
    private bool isDrifting = false;
    public float baseDriftForce;

    private Vector3 driftFrwd;
    private float stifness = 0;
    public ParticleSystem[] Sfire;
    private ParticleSystem[] SubSFire = new ParticleSystem[16];

    private bool isSpaceDown = false;
    private bool isSpaceJustUp = false;

    public float rotationSpeed;
    public float slowDownForce;
    public float frontTurnRadius, rearTurnRadius, driftTurnRadius;
    public Animator m_animator;
    private int inputAcc;
    public TrailRenderer wheelBRTrail, wheelBLTrail;

    void Start()
    {
        nodes = GameObject.FindGameObjectsWithTag("Node");
        m_hud = FindObjectOfType<m_carHUD>();

        m_rigidbody = GetComponentInChildren<Rigidbody>();
        m_rigidbody.centerOfMass = centerOfGravity.localPosition;

        m_particleSystem1 = wheelBL.GetComponent<ParticleSystem>();
        m_particleSystem2 = wheelBR.GetComponent<ParticleSystem>();

        currentAcc = 0;
        driftForce = baseDriftForce;

    }

    public float Speed()
    {
        //convert to km/h
        return 2 * wheelBR.radius * Mathf.PI * wheelBR.rpm * 60f / 1000f;
    }

    public float Rpm()
    {
        return wheelBL.rpm;
    }

    void Update()
    {
        isSpaceDown = Input.GetKey("space");
        isSpaceJustUp = Input.GetKeyUp("space");
        isDrifting = Input.GetButton("Drift");

        if (Input.GetJoystickNames() != null && Input.GetButton("Accelerate"))
        {
            inputAcc = 1;
        }
        else
        {
            inputAcc = 0;
        }
    }

    void FixedUpdate()
    {
        m_rigidbody.AddRelativeForce(new Vector3(0, Mathf.Abs(m_rigidbody.transform.forward.y), 0).normalized * -gravity, ForceMode.Acceleration);

        if (m_hud.StartRace == true)
        {
            if (Input.GetAxis("HorizontalXbox") != 0 || inputAcc != 0 || Input.GetButton("Brake"))
            {
                scaledTorque = inputAcc * torque * currentAcc;
                wheelFR.steerAngle = Input.GetAxis("HorizontalXbox") * turnRadius;
                wheelFL.steerAngle = Input.GetAxis("HorizontalXbox") * turnRadius;             
            }
            else
            {
                scaledTorque = Input.GetAxis("Vertical") * torque * currentAcc;
                wheelFR.steerAngle = Input.GetAxis("Horizontal") * turnRadius;
                wheelFL.steerAngle = Input.GetAxis("Horizontal") * turnRadius;
            }
            if (wheelBL.rpm < g_RPM)
            {
                scaledTorque = Mathf.Lerp(scaledTorque / 10, scaledTorque, wheelBL.rpm / g_RPM);
            }
            else
            {
                scaledTorque = Mathf.Lerp(scaledTorque, 0, (wheelBL.rpm - g_RPM) / (max_RPM - g_RPM));
            }
            if (currentSpeed >= maxSpeed)
            {
                currentAcc = 0;
            }
            else if (currentSpeed < maxSpeed)
            {
                currentAcc = baseAcc;
            }
        }        

        Stabilizer(wheelBL, wheelBR, wheelFL, wheelFR);

        currentSpeed = m_rigidbody.velocity.magnitude;

        for (int i = 0; i < Sfire.Length; i++)
        {
            for (int r = 0; r < SubSFire.Length; r++)
            {
                SubSFire = Sfire[i].GetComponentsInChildren<ParticleSystem>();
                ParticleSystem.VelocityOverLifetimeModule fireVelocity = SubSFire[r].GetComponent<ParticleSystem>().velocityOverLifetime;
                fireVelocity.xMultiplier = -currentSpeed;
                ParticleSystem.ColorOverLifetimeModule fireColor = SubSFire[r].GetComponent<ParticleSystem>().colorOverLifetime;
                fireVelocity.z = 0;

                if (Drifting)
                {
                    //fireColor.color = Color.blue / 2;
                    //fireVelocity.x = -currentSpeed / 2;
                    //fireVelocity.z = -currentSpeed / 2f;
                }
                //else
                //{
                //    fireColor.color = Color.gray;
                //}
            }

        }
        if ((Input.GetAxis("Vertical") > 0 || Input.GetButton("Accelerate")) && !Drifting)
        {
            driveMode = DriveMode.Front;
            m_rigidbody.AddRelativeForce(new Vector3(0, 0, Mathf.Abs(transform.forward.z)).normalized * currentAcc, ForceMode.Acceleration);

            wheelFR.brakeTorque = 0;
            wheelFL.brakeTorque = 0;
            wheelBR.brakeTorque = 0;
            wheelBL.brakeTorque = 0;

            //Sfire[1].Play();
            //Sfire[2].Play();
            //Sfire[4].Play();
            //Sfire[5].Play();

            wheelBLTrail.enabled = false;
            wheelBRTrail.enabled = false;
        }

        else if ((Input.GetAxis("Vertical") < 0 || Input.GetButton("Brake")) && !Drifting)
        {
            driveMode = DriveMode.Rear;
            m_rigidbody.AddRelativeForce(new Vector3(0, 0, -Mathf.Abs(transform.forward.z)).normalized * currentAcc * 3, ForceMode.Acceleration);

            wheelFR.brakeTorque = 0;
            wheelFL.brakeTorque = 0;
            wheelBR.brakeTorque = 0;
            wheelBL.brakeTorque = 0;

            //Sfire[1].Stop();
            //Sfire[2].Stop();
            //Sfire[4].Stop();
            //Sfire[5].Stop();

            wheelBLTrail.enabled = false;
            wheelBRTrail.enabled = false;
        }
        else if ((Input.GetAxis("Vertical") == 0 && !Input.GetButton("Accelerate") && !Input.GetButton("Brake")) && currentSpeed > 0.2f)
        {
            //Sfire[1].Stop();
            //Sfire[2].Stop();
            //Sfire[4].Stop();
            //Sfire[5].Stop();

            if (driveMode == DriveMode.Front)
            {
                if (currentSpeed <= 1f)
                {
                    wheelFR.brakeTorque = brakeTorque;
                    wheelFL.brakeTorque = brakeTorque;
                    wheelBR.brakeTorque = brakeTorque;
                    wheelBL.brakeTorque = brakeTorque;
                }
                else
                {
                    m_rigidbody.AddRelativeForce(new Vector3(0, 0, -Mathf.Abs(transform.forward.z)) * slowDownForce, ForceMode.Force);
                }

            }
            else if (driveMode == DriveMode.Rear)
            {
                if (currentSpeed <= 1f)
                {
                    wheelFR.brakeTorque = brakeTorque;
                    wheelFL.brakeTorque = brakeTorque;
                    wheelBR.brakeTorque = brakeTorque;
                    wheelBL.brakeTorque = brakeTorque;
                }
                else
                {
                    m_rigidbody.AddRelativeForce(new Vector3(0, 0, Mathf.Abs(transform.forward.z)) * slowDownForce, ForceMode.Force);

                }
            }


        }
        //Debug.Log("SPACE:"+Input.GetKey("space") +", DRIFT: " + Input.GetButton("Drift")+", HORI:" +Input.GetAxis("Horizontal"));
        //drift
        if ((isSpaceDown || isDrifting) && ((Input.GetAxis("Horizontal") < -0.5f || Input.GetAxis("Horizontal") > 0.5f) 
            || (Input.GetAxis("HorizontalXbox") <-0.5f || Input.GetAxis("HorizontalXbox") > 0.5f)))
        {                       
            if (!Drifting && driftDelay >= 0.9f)
            {
                driftDelay = 0;
                m_rigidbody.AddForceAtPosition(Vector3.up * 150, m_rigidbody.transform.position, ForceMode.Acceleration);

                if (Input.GetAxis("Horizontal") < -0.5f || Input.GetAxis("HorizontalXbox") < -0.5f)
                {
                    m_rigidbody.transform.Rotate(Vector3.up, Mathf.Lerp(m_rigidbody.transform.rotation.y,
                                                m_rigidbody.transform.rotation.y - 40f, rotationSpeed * Time.deltaTime));
                    leftDrift = true;
                    rightDrift = false;
                    stifness = 0;
                    driveMode = DriveMode.Drift;
                }
                else if (Input.GetAxis("Horizontal") > 0.5f || Input.GetAxis("HorizontalXbox") > 0.5f)
                {
                    m_rigidbody.transform.Rotate(Vector3.up, Mathf.Lerp(m_rigidbody.transform.rotation.y,
                                                 m_rigidbody.transform.rotation.y + 40f, rotationSpeed * Time.deltaTime));
                    rightDrift = true;
                    leftDrift = false;
                    stifness = 0;
                    driveMode = DriveMode.Drift;
                }
            }
        }
        else if (isSpaceJustUp || !isDrifting)
        {
            Drifting = false;
            rightDrift = false;
            leftDrift = false;

            if (Input.GetAxis("Vertical") > 0 || Input.GetButton("Accelerate"))
            {
                driveMode = DriveMode.Front;
            }
            else if (Input.GetAxis("Vertical") < 0 || Input.GetButton("Brake"))
            {
                driveMode = DriveMode.Rear;
            }
        }

        if (driveMode == DriveMode.Front)
        {
            maxSpeed = frontMaxSpeed;

            wheelBR.motorTorque = scaledTorque;
            wheelBL.motorTorque = scaledTorque;
            wheelFR.motorTorque = scaledTorque;
            wheelFL.motorTorque = scaledTorque;

            wheelBLFrontFriction = wheelBL.forwardFriction;

            if (wheelBLFrontFriction.stiffness < 0.9f)
            {
                wheelBLFrontFriction = wheelBL.forwardFriction;
                wheelBLFrontFriction.stiffness = wheelBLFrontFriction.stiffness + Time.deltaTime;
                wheelBL.forwardFriction = wheelBLFrontFriction;

                wheelBRFrontFriction = wheelBR.forwardFriction;
                wheelBRFrontFriction.stiffness = wheelBRFrontFriction.stiffness + Time.deltaTime;
                wheelBR.forwardFriction = wheelBRFrontFriction;

                wheelFLFrontFriction = wheelFL.forwardFriction;
                wheelFLFrontFriction.stiffness = wheelFLFrontFriction.stiffness + Time.deltaTime;
                wheelFL.forwardFriction = wheelFLFrontFriction;

                wheelFRFrontFriction = wheelFR.forwardFriction;
                wheelFRFrontFriction.stiffness = wheelFRFrontFriction.stiffness + Time.deltaTime;
                wheelFR.forwardFriction = wheelFRFrontFriction;

            }
            else
            {
                wheelBLFrontFriction = wheelBL.forwardFriction;
                wheelBLFrontFriction.stiffness = 1;
                wheelBL.forwardFriction = wheelBLFrontFriction;

                wheelBRFrontFriction = wheelBR.forwardFriction;
                wheelBRFrontFriction.stiffness = 1;
                wheelBR.forwardFriction = wheelBRFrontFriction;

                wheelFLFrontFriction = wheelFL.forwardFriction;
                wheelFLFrontFriction.stiffness = 1;
                wheelFL.forwardFriction = wheelFLFrontFriction;

                wheelFRFrontFriction = wheelFR.forwardFriction;
                wheelFRFrontFriction.stiffness = 1;
                wheelFR.forwardFriction = wheelFRFrontFriction;
            }

            wheelBRDriftFriction = wheelBR.sidewaysFriction;

            if (wheelBLDriftFriction.stiffness < 0.9f)
            {
                wheelBRDriftFriction = wheelBR.sidewaysFriction;
                wheelBRDriftFriction.stiffness = wheelBRDriftFriction.stiffness + Time.deltaTime;
                wheelBR.sidewaysFriction = wheelBRDriftFriction;

                wheelBLDriftFriction = wheelBL.sidewaysFriction;
                wheelBLDriftFriction.stiffness = wheelBLDriftFriction.stiffness + Time.deltaTime;
                wheelBL.sidewaysFriction = wheelBLDriftFriction;

                wheelFLDriftFriction = wheelFL.sidewaysFriction;
                wheelFLDriftFriction.stiffness = wheelFLDriftFriction.stiffness + Time.deltaTime;
                wheelFL.sidewaysFriction = wheelFLDriftFriction;

                wheelFRDriftFriction = wheelFR.sidewaysFriction;
                wheelFRDriftFriction.stiffness = wheelFRDriftFriction.stiffness + Time.deltaTime;
                wheelFR.sidewaysFriction = wheelFRDriftFriction;
            }

            else
            {
                wheelBRDriftFriction = wheelBR.sidewaysFriction;
                wheelBRDriftFriction.stiffness = 1;
                wheelBR.sidewaysFriction = wheelBRDriftFriction;

                wheelBLDriftFriction = wheelBL.sidewaysFriction;
                wheelBLDriftFriction.stiffness = 1;
                wheelBL.sidewaysFriction = wheelBLDriftFriction;

                wheelFLDriftFriction = wheelFL.sidewaysFriction;
                wheelFLDriftFriction.stiffness = 1;
                wheelFL.sidewaysFriction = wheelFLDriftFriction;

                wheelFRDriftFriction = wheelFR.sidewaysFriction;
                wheelFRDriftFriction.stiffness = 1;
                wheelFR.sidewaysFriction = wheelFRDriftFriction;
            }

            wheelBLDamp = wheelBL.wheelDampingRate;
            wheelBLDamp = 0.5f;
            wheelBL.wheelDampingRate = wheelBLDamp;

            wheelBRDamp = wheelBR.wheelDampingRate;
            wheelBRDamp = 0.5f;
            wheelBR.wheelDampingRate = wheelBRDamp;

            wheelFLDamp = wheelFL.wheelDampingRate;
            wheelFLDamp = 0.5f;
            wheelFL.wheelDampingRate = wheelFLDamp;

            wheelFRDamp = wheelFR.wheelDampingRate;
            wheelFRDamp = 0.5f;
            wheelFR.wheelDampingRate = wheelFRDamp;

            driftCounter = 2f;
            driftDelay += Time.deltaTime;

            Drifting = false;
            rightDrift = false;
            leftDrift = false;

            if (currentSpeed >= 1 && currentSpeed <= maxSpeed / 1.5f)
            {
                turnRadius = frontTurnRadius * 2;
            }
            else
            {
                turnRadius = frontTurnRadius;
            }

            wheelBLTrail.startColor = Color.red;
            wheelBRTrail.startColor = Color.red;
            wheelBLTrail.endColor = Color.black;
            wheelBRTrail.endColor = Color.black;
        }
        if (driveMode == DriveMode.Rear)
        {
            maxSpeed = rearMaxSpeed;

            wheelBR.motorTorque = scaledTorque;
            wheelBL.motorTorque = scaledTorque;
            wheelFR.motorTorque = scaledTorque;
            wheelFL.motorTorque = scaledTorque;

            wheelBRDriftFriction = wheelBR.sidewaysFriction;
            wheelBRDriftFriction.stiffness = 1;
            wheelBR.sidewaysFriction = wheelBRDriftFriction;

            wheelBLDriftFriction = wheelBL.sidewaysFriction;
            wheelBLDriftFriction.stiffness = 1;
            wheelBL.sidewaysFriction = wheelBLDriftFriction;

            wheelFLDriftFriction = wheelFL.sidewaysFriction;
            wheelFLDriftFriction.stiffness = 1;
            wheelFL.sidewaysFriction = wheelFLDriftFriction;

            wheelFRDriftFriction = wheelFR.sidewaysFriction;
            wheelFRDriftFriction.stiffness = 1;
            wheelFR.sidewaysFriction = wheelFRDriftFriction;

            wheelBLDriftFriction = wheelBL.forwardFriction;
            wheelBLFrontFriction.stiffness = 1;
            wheelBL.forwardFriction = wheelBLFrontFriction;

            wheelBRFrontFriction = wheelBR.forwardFriction;
            wheelBRFrontFriction.stiffness = 1;
            wheelBR.forwardFriction = wheelBRFrontFriction;

            wheelFLFrontFriction = wheelFL.forwardFriction;
            wheelFLFrontFriction.stiffness = 1;
            wheelFL.forwardFriction = wheelFLFrontFriction;

            wheelFRFrontFriction = wheelFR.forwardFriction;
            wheelFRFrontFriction.stiffness = 1;
            wheelFR.forwardFriction = wheelFRFrontFriction;

            driftCounter = 2;
            driftDelay += Time.deltaTime;

            Drifting = false;
            rightDrift = false;
            leftDrift = false;

            turnRadius = rearTurnRadius;
        }

        if (driveMode == DriveMode.Drift)
        {
            Drifting = true;

            wheelBLTrail.enabled = true;
            wheelBRTrail.enabled = true;

            wheelBLFrontFriction = wheelBL.forwardFriction;
            wheelBLFrontFriction.stiffness = 1;
            wheelBL.forwardFriction = wheelBLFrontFriction;

            wheelBRFrontFriction = wheelBR.forwardFriction;
            wheelBRFrontFriction.stiffness = 1;
            wheelBR.forwardFriction = wheelBRFrontFriction;

            wheelFLFrontFriction = wheelFL.forwardFriction;
            wheelFLFrontFriction.stiffness = 1;
            wheelFL.forwardFriction = wheelFLFrontFriction;

            wheelFRFrontFriction = wheelFR.forwardFriction;
            wheelFRFrontFriction.stiffness = 1;
            wheelFR.forwardFriction = wheelFRFrontFriction;

            stifness += Time.deltaTime;

            if (stifness >= 0.1f)
            {
                stifness = 0.1f;
            }
            driftFrwd = m_rigidbody.transform.right;

            DriftBehaviour(wheelBL, wheelBR, wheelFL, wheelFR);

            driftCounter -= Time.deltaTime;

            if (driftCounter <= 0)
            {
                wheelBLTrail.startColor = Color.blue;
                wheelBRTrail.startColor = Color.blue;
                wheelBLTrail.endColor = Color.blue;
                wheelBRTrail.endColor = Color.blue;

                if ((Input.GetKey("left shift") || Input.GetButton("TurboDrift")))
                {
                    m_rigidbody.AddRelativeForce(new Vector3(0, 0, Mathf.Abs(m_rigidbody.transform.forward.z)) * endDriftTurboForce, ForceMode.VelocityChange);

                    driftCounter = 2f;
                    Drifting = false;
                    driveMode = DriveMode.Front;
                }               
            }
            Sfire[0].Play();
            Sfire[1].Play();
            Sfire[2].Play();
            Sfire[3].Play();
            Sfire[4].Play();
            Sfire[5].Play();
            Sfire[6].Play();
            Sfire[7].Play();

           
        }

        WheelHit hit;
        RaycastHit hitFloor1;

        bool groundedWheel = wheelBL.GetGroundHit(out hit);
        bool groundedWheel2 = wheelBR.GetGroundHit(out hit);
        bool groundedWheel3 = wheelFR.GetGroundHit(out hit);
        bool groundedWheel4 = wheelFL.GetGroundHit(out hit);

        if ((!groundedWheel && !groundedWheel2 && !groundedWheel3 && !groundedWheel4) && (!isSpaceDown && !isSpaceJustUp))
        {
            if (Physics.Raycast(transform.position, -m_rigidbody.transform.up, out hitFloor1, 10f))
            {
                gravity = 8;

                wheelBLDamp = wheelBL.wheelDampingRate;
                wheelBLDamp = 0.5f;
                wheelBL.wheelDampingRate = wheelBLDamp;

                wheelBRDamp = wheelBR.wheelDampingRate;
                wheelBRDamp = 0.5f;
                wheelBR.wheelDampingRate = wheelBRDamp;

                wheelFLDamp = wheelFL.wheelDampingRate;
                wheelFLDamp = 0.5f;
                wheelFL.wheelDampingRate = wheelFLDamp;

                wheelFRDamp = wheelFR.wheelDampingRate;
                wheelFRDamp = 0.5f;
                wheelFR.wheelDampingRate = wheelFRDamp;
            }
            else
            {
                gravity = 15;

                wheelBLDriftFriction = wheelBL.forwardFriction;
                wheelBLFrontFriction.stiffness = 0;
                wheelBL.forwardFriction = wheelBLFrontFriction;

                wheelBRFrontFriction = wheelBR.forwardFriction;
                wheelBRFrontFriction.stiffness = 0;
                wheelBR.forwardFriction = wheelBRFrontFriction;

                wheelFLFrontFriction = wheelFL.forwardFriction;
                wheelFLFrontFriction.stiffness = 0;
                wheelFL.forwardFriction = wheelFLFrontFriction;

                wheelFRFrontFriction = wheelFR.forwardFriction;
                wheelFRFrontFriction.stiffness = 0;
                wheelFR.forwardFriction = wheelFRFrontFriction;
            }


            timeCounter += Time.deltaTime;

            if (timeCounter >= 3)
            {
                ResetPosition();
                timeCounter = 0;
            }
        }
        else if (groundedWheel ||groundedWheel2 ||groundedWheel3 || groundedWheel4)
        {
            timeCounter = 0;
        }
    }

    void DriftBehaviour(WheelCollider WheelBL, WheelCollider WheelBR, WheelCollider WheelFL, WheelCollider WheelFR)
    {
        WheelHit hit;
        Vector3 localPosition = transform.localPosition;

        bool groundedBL = WheelBL.GetGroundHit(out hit);
        bool groundedBR = WheelBR.GetGroundHit(out hit);

        turnRadius = driftTurnRadius;

        wheelFR.motorTorque = scaledTorque;
        wheelFL.motorTorque = scaledTorque;

        driftFrwd = m_rigidbody.transform.right;

        if (groundedBL && groundedBR)
        {
            if (Input.GetAxis("Vertical") > 0 || Input.GetButton("Accelerate"))
            {
                WheelFL.motorTorque = scaledTorque * 5;
                WheelFR.motorTorque = scaledTorque * 5;
                WheelBL.motorTorque = scaledTorque * 5;
                WheelBR.motorTorque = scaledTorque * 5;

                if (rightDrift)
                {
                    wheelBLDriftFriction = WheelBL.sidewaysFriction;
                    wheelBLDriftFriction.stiffness = stifness / 1.5f;
                    WheelBL.sidewaysFriction = wheelBLDriftFriction;

                    wheelBRDriftFriction = WheelBR.sidewaysFriction;
                    wheelBRDriftFriction.stiffness = stifness / 1.5f;
                    WheelBR.sidewaysFriction = wheelBRDriftFriction;

                    wheelFLDriftFriction = WheelFL.sidewaysFriction;
                    wheelFLDriftFriction.stiffness = stifness / 1.5f;
                    WheelFL.sidewaysFriction = wheelFLDriftFriction;

                    wheelFRDriftFriction = WheelFR.sidewaysFriction;
                    wheelFRDriftFriction.stiffness = stifness / 1.5f;
                    WheelFR.sidewaysFriction = wheelFRDriftFriction;

                    if ((Input.GetAxis("Horizontal") >= 1 && Input.GetAxis("Horizontal") > 0))
                    {
                        m_rigidbody.AddRelativeForce((m_rigidbody.transform.forward * Input.GetAxis("Vertical") +
                                                      driftFrwd * 2) * driftForce, ForceMode.Force);

                        Debug.DrawRay(m_rigidbody.transform.position, (m_rigidbody.transform.forward * Input.GetAxis("Vertical") +
                                                                       driftFrwd * 2) * driftForce, Color.yellow);
                    }
                    else if (Input.GetAxis("HorizontalXbox") >= 1 && Input.GetAxis("HorizontalXbox") > 0)
                    {
                        m_rigidbody.AddRelativeForce((m_rigidbody.transform.forward +
                                                      driftFrwd * 2) * driftForce, ForceMode.Force);

                        Debug.DrawRay(m_rigidbody.transform.position, (m_rigidbody.transform.forward +
                                                                       driftFrwd * 2) * driftForce, Color.yellow);
                    }

                    else if (Input.GetAxis("Horizontal") == -1)
                    {
                        Debug.Log("contravolant R");
                        m_rigidbody.AddRelativeForce((m_rigidbody.transform.forward * 2 - driftFrwd) * driftForce, ForceMode.Force);
                        Debug.DrawRay(m_rigidbody.transform.position, (m_rigidbody.transform.forward * 2 - driftFrwd) * driftForce, Color.green);

                        m_rigidbody.transform.Rotate(m_rigidbody.transform.up, Mathf.Lerp(m_rigidbody.transform.rotation.y,
                                                                                          m_rigidbody.transform.rotation.y - 10, 0.1f));
                    }
                    else if (Input.GetAxis("HorizontalXbox") == -1)
                    {
                        Debug.Log("contravolant R");
                        m_rigidbody.AddRelativeForce((m_rigidbody.transform.forward * 2 - driftFrwd) * driftForce, ForceMode.Force);
                        Debug.DrawRay(m_rigidbody.transform.position, (m_rigidbody.transform.forward * 2 - driftFrwd) * driftForce, Color.green);

                        m_rigidbody.transform.Rotate(m_rigidbody.transform.up, Mathf.Lerp(m_rigidbody.transform.rotation.y,
                                                                                          m_rigidbody.transform.rotation.y - 10, 0.1f));
                    }
                }

                else if (leftDrift)
                {
                    wheelBLDriftFriction = WheelBL.sidewaysFriction;
                    wheelBLDriftFriction.stiffness = stifness / 1.5f;
                    WheelBL.sidewaysFriction = wheelBLDriftFriction;

                    wheelBRDriftFriction = WheelBR.sidewaysFriction;
                    wheelBRDriftFriction.stiffness = stifness / 1.5f;
                    WheelBR.sidewaysFriction = wheelBRDriftFriction;

                    wheelFLDriftFriction = WheelFL.sidewaysFriction;
                    wheelFLDriftFriction.stiffness = stifness / 1.5f;
                    WheelFL.sidewaysFriction = wheelFLDriftFriction;

                    wheelFRDriftFriction = WheelFR.sidewaysFriction;
                    wheelFRDriftFriction.stiffness = stifness / 1.5f;
                    WheelFR.sidewaysFriction = wheelFRDriftFriction;

                    if (Input.GetAxis("Horizontal") <= -1 && Input.GetAxis("Horizontal") < 0)
                    {
                        m_rigidbody.AddRelativeForce((-m_rigidbody.transform.forward * Input.GetAxis("Vertical") -
                                                      driftFrwd) * driftForce, ForceMode.Force);

                        Debug.DrawRay(m_rigidbody.transform.position, (-m_rigidbody.transform.forward * Input.GetAxis("Vertical") -
                                                                        driftFrwd) * driftForce, Color.black);
                    }
                    else if (Input.GetAxis("HorizontalXbox") <= -1 && Input.GetAxis("HorizontalXbox") < 0)
                    {
                        m_rigidbody.AddRelativeForce((-m_rigidbody.transform.forward -
                                                      driftFrwd) * driftForce, ForceMode.Force);

                        Debug.DrawRay(m_rigidbody.transform.position, (-m_rigidbody.transform.forward -
                                                                        driftFrwd) * driftForce, Color.black);
                    }
                    else if (Input.GetAxis("Horizontal") == 1)
                    {
                        Debug.Log("contravolant L");
                        m_rigidbody.AddRelativeForce((m_rigidbody.transform.forward + driftFrwd) * driftForce, ForceMode.Force);
                        Debug.DrawRay(m_rigidbody.transform.position, (m_rigidbody.transform.forward * 2 + driftFrwd) * driftForce, Color.magenta);

                        m_rigidbody.transform.Rotate(m_rigidbody.transform.up, Mathf.Lerp(m_rigidbody.transform.rotation.y,
                                                                                          m_rigidbody.transform.rotation.y + 10, 0.1f));
                    }
                    else if (Input.GetAxis("HorizontalXbox") == 1)
                    {
                        Debug.Log("contravolant L");
                        m_rigidbody.AddRelativeForce((m_rigidbody.transform.forward + driftFrwd) * driftForce, ForceMode.Force);
                        Debug.DrawRay(m_rigidbody.transform.position, (m_rigidbody.transform.forward * 2 + driftFrwd) * driftForce, Color.magenta);

                        m_rigidbody.transform.Rotate(m_rigidbody.transform.up, Mathf.Lerp(m_rigidbody.transform.rotation.y,
                                                                                          m_rigidbody.transform.rotation.y + 10, 0.1f));
                    }

                }
            }
        }

    }

    void Stabilizer(WheelCollider WheelBL, WheelCollider WheelBR, WheelCollider WheelFL, WheelCollider WheelFR)
    {
        WheelHit hit;
        RaycastHit hitFloor;

        float travelFL = 1.0f;
        float travelFR = 1.0f;
        float travelBL = 1.0f;
        float travelBR = 1.0f;

        bool groundedBL = wheelBL.GetGroundHit(out hit);
        bool groundedBR = wheelBR.GetGroundHit(out hit);
        bool groundedFL = wheelFL.GetGroundHit(out hit);
        bool groundedFR = wheelFR.GetGroundHit(out hit);

        if (groundedBL)
            travelBL = (-wheelBL.transform.InverseTransformPoint(hit.point).y - wheelBL.radius) / wheelBL.suspensionDistance;

        if (groundedBR)
            travelBR = (-wheelBR.transform.InverseTransformPoint(hit.point).y - wheelBR.radius) / wheelBR.suspensionDistance;

        if (groundedFL)
            travelFL = (-wheelFL.transform.InverseTransformPoint(hit.point).y - wheelFL.radius) / wheelFL.suspensionDistance;

        if (groundedFR)
            travelFR = (-wheelFR.transform.InverseTransformPoint(hit.point).y - wheelFR.radius) / wheelFR.suspensionDistance;

        float antiRollForceBack = (travelBL - travelBR) * AntiRoll;
        float antiRollForceFront = (travelFL - travelFR) * AntiRoll;

        if (groundedBL)
        {
            m_rigidbody.AddForceAtPosition(wheelBL.transform.up * -antiRollForceBack, wheelBL.transform.position);
        }
        if (groundedBR)
        {
            m_rigidbody.AddForceAtPosition(wheelBR.transform.up * antiRollForceBack, wheelBR.transform.position);
        }
        if (groundedFL)
        {
            m_rigidbody.AddForceAtPosition(wheelFL.transform.up * -antiRollForceFront, wheelFL.transform.position);
        }
        if (groundedFR)
        {
            m_rigidbody.AddForceAtPosition(wheelFR.transform.up * antiRollForceFront, wheelFR.transform.position);
        }
        else if ((!groundedFR && !groundedFL) || (!groundedBL && !groundedBR) && (!leftDrift && !rightDrift))
        {
            if (Physics.Raycast(transform.position, -m_rigidbody.transform.up, out hitFloor, 50f))
            {
                Quaternion m_new_rotation = new Quaternion(0, m_rigidbody.transform.rotation.y, 0, 1);

                m_rigidbody.transform.rotation = new Quaternion(Mathf.Lerp(m_rigidbody.transform.rotation.x, m_new_rotation.x, 0.2f + Time.deltaTime), m_rigidbody.transform.rotation.y,
                                                                Mathf.Lerp(m_rigidbody.transform.rotation.z, m_new_rotation.z, 0.2f + Time.deltaTime), m_rigidbody.transform.rotation.w);



            }
            else if (Physics.Raycast(transform.position, -m_rigidbody.transform.right, out hitFloor, 50f))
            {
                Quaternion m_new_rotation = new Quaternion(0, m_rigidbody.transform.rotation.y, 0, 1);

                m_rigidbody.transform.rotation = new Quaternion(Mathf.Lerp(m_rigidbody.transform.rotation.x, m_new_rotation.x, 0.2f + Time.deltaTime), m_rigidbody.transform.rotation.y,
                                                                Mathf.Lerp(m_rigidbody.transform.rotation.z, m_new_rotation.z, 0.2f + Time.deltaTime), m_rigidbody.transform.rotation.w);

            }
        }
    }
    void OnTriggerEnter(Collider col)
    {
        if (col.tag == "Turbo")
        {
            Debug.Log("Trubo");
            m_rigidbody.AddRelativeForce(new Vector3(0, 0, Mathf.Abs(m_rigidbody.transform.forward.z)).normalized * turboForce, ForceMode.Impulse);
        }
        if (col.tag == "Kart")
        {
            Debug.Log("is knocked");
            //m_animator.SetBool("isKnockedUp", true);
        }
        else
        {
            //m_animator.SetBool("isKnockedUp", false);
        }
    }
    void OnTriggerStay(Collider col)
    {
        if (col.tag == "RoughFloor")
        {
            maxSpeed = 10;

            wheelBLTrail.startColor = Color.green;
            wheelBLTrail.endColor = Color.green;
            wheelBRTrail.startColor = Color.green;
            wheelBRTrail.endColor = Color.green;
        }
        
        if (col.tag == "IA")
        {
            rebufoCounter += Time.deltaTime;

            if (rebufoCounter >= 2)
            {
                m_rigidbody.AddRelativeForce(new Vector3(0, 0, Mathf.Abs(m_rigidbody.transform.forward.z)).normalized * miniTurboForce, ForceMode.Acceleration);
                rebufoCounter = 0;
            }
            Debug.Log(rebufoCounter);
        }
    }
    void OnTriggerExit(Collider col)
    {
        if (col.tag == "IA")
        {
            rebufoCounter = 0;
        }
        if (col.tag == "RoughFloor")
        {
            maxSpeed = frontMaxSpeed;

            wheelBLTrail.startColor = Color.gray;
            wheelBLTrail.endColor = Color.gray;
            wheelBRTrail.startColor = Color.gray;
            wheelBRTrail.endColor = Color.gray;
        }
    }

    private Vector3 ResetPosition()
    {
        Vector3 respawnPosition;
        GameObject kart = gameObject;

        for (int i = 0; i < nodes.Length; i++)
        {
            distanceToRespawnPoint = nodes[i].transform.position - gameObject.transform.position;

            if (distanceToRespawnPoint.magnitude <= 30)
            {
                respawnPosition = nodes[i].transform.position;
                Debug.Log("Is Respawning");
                transform.position = respawnPosition;
                transform.rotation = nodes[i].transform.rotation;

            }
        }
        return transform.position;
    }
}
