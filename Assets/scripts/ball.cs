using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.VFX;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

public class ball : MonoBehaviour
{
    public Rigidbody rb;
    public float kickForce = 10f;
    public Transform cameraAngle;
    public Transform bodyAngle;

    public bool isKicked = false;

    public TextMeshProUGUI BotScore;
    float BotPoints = 0;
    public TextMeshProUGUI PlayerScore;
    float PlayerPoints = 0;

    bool KickedByBot = false;

    Vector3 starting_position;

    //public FallPointMovment fallPointMovmentScript;

    Vector3 appliedForce;

    public bool whoTached = false; //false bot, true player;

    private void Start()
    {
        starting_position = transform.position;
    }

    void OnTriggerStay(Collider collision)
    {
        if (collision.CompareTag("topHands"))
        {
            if (!isKicked)
            {
                kickForce = collision.gameObject.GetComponent<HandsManagment>().currentKickForce;
                if (kickForce > 0)
                {
                    rb.velocity = new Vector3(0, 0, 0);
                    appliedForce = collision.gameObject.transform.up * kickForce;
                    rb.AddForce(appliedForce);
                    isKicked = true;
                    whoTached = true;
                }
                //Debug.Log(appliedForce);
                //fallPointMovmentScript.fallPoint.position = fallPointMovmentScript.GetFallPointPosotion();

            }

        }
        else if (collision.CompareTag("bottomHands"))
        {
            if (!isKicked)
            {
                kickForce = collision.gameObject.GetComponent<HandsManagerBottomPosition>().currentKickForce;
                if (kickForce > 0)
                {
                    rb.velocity = new Vector3(0, 0, 0);
                    appliedForce = collision.gameObject.transform.up * kickForce;
                    rb.AddForce(appliedForce);
                    isKicked = true;
                    whoTached = true;
                }
                //Debug.Log(appliedForce);  
                //fallPointMovmentScript.fallPoint.position = fallPointMovmentScript.GetFallPointPosotion();

            }
        }
        else if (collision.gameObject.CompareTag("Wall") && !KickedByBot && !RoundIsOver)
        {
            if (transform.position.y < 27.2f)
            {
                Bot.GetComponent<BotBasicData>().GetTargetPoint(20f);
                Bot.GetComponent<BotBasicData>().Crouch(true);
            }
            if (Vector3.Distance(Bot.GetComponent<BotBasicData>().handsCenter.position, transform.position)<=Bot.GetComponent<BotBasicData>().kickRadius && (transform.position.y>=27.2f || transform.position.y<25.5f))
            {
                if (Bot.GetComponent<BotBasicData>().IsTestMode)
                {
                    // throw ball to player
                    rb.velocity = GetFlySpeed(transform.position, new Vector3(bodyAngle.transform.position.x, 20f, bodyAngle.transform.position.z), 20f + 26.2f);
                    Bot.GetComponent<BotBasicData>().Crouch(false);
                    //Debug.Log("Test Mode");
                }

                // if bot is ready too aim his shot
                else if (Bot.GetComponent<BotBasicData>().ReadyTime > Bot.GetComponent<BotBasicData>().ReadyTimeNeed)
                {
                    // find the furthest corner from player
                    float[] corners = { xMin, zMin, xMin, zMax, xMax, zMin, xMax, zMax };
                    int furthest_corner = 0;
                    for (int i = 1; i < 4; i++)
                    {
                        if (Vector2.Distance(new Vector2(bodyAngle.position.x, bodyAngle.position.z), new Vector2(corners[i * 2], corners[i * 2 + 1])) > Vector2.Distance(new Vector2(bodyAngle.position.x, bodyAngle.position.z), new Vector2(corners[furthest_corner * 2], corners[furthest_corner * 2 + 1])))
                            furthest_corner = i;
                    }

                    // find aim_percentage and limit to MaxAimAccuracy
                    float aim_percentage = Bot.GetComponent<BotBasicData>().ReadyTime / Bot.GetComponent<BotBasicData>().AimTimeNeed;
                    aim_percentage = Mathf.Min(aim_percentage, Bot.GetComponent<BotBasicData>().MaxAimAccuracy);

                    // find aim_center:
                    // add vector going from player to corner multiplied by aim_percentage to player position
                    Vector3 aim_center = bodyAngle.position + (new Vector3(corners[furthest_corner * 2], 0.0f, corners[furthest_corner * 2 + 1]) - bodyAngle.position) * aim_percentage;

                    // find aim_radius
                    float aim_radius = Mathf.Min(Mathf.Abs(aim_center.x - xMin), Mathf.Abs(aim_center.x - xMax), Mathf.Abs(aim_center.z - zMin), Mathf.Abs(aim_center.z - zMax));

                    // random point in circle: https://stackoverflow.com/questions/5837572/generate-a-random-point-within-a-circle-uniformly/50746409#50746409
                    float r = aim_radius * Mathf.Sqrt(Random.value);
                    float theta = Random.value * 2 * Mathf.PI;
                    xEnd = aim_center.x + r * Mathf.Cos(theta);
                    zEnd = aim_center.z + r * Mathf.Sin(theta);

                    float shot_height;
                    shot_height = Random.Range(Mathf.Max(maxHeight * (1 - aim_percentage), minHeight), Mathf.Max(maxHeight * (1 - aim_percentage) - heightWindow, minHeight));

                    // determine needed velocity
                    rb.velocity = GetFlySpeed(transform.position, new Vector3(xEnd, 20f, zEnd), shot_height + 26.2f);
                    //Debug.Log("shot_height: " + shot_height);
                    // Debug.Log("Ready Time: " + Bot.GetComponent<BotBasicData>().ReadyTime);
                    Bot.GetComponent<BotBasicData>().Crouch(false);
                }
                else
                {
                    // random point on field
                    xEnd = Random.Range(xMin, xMax);
                    zEnd = Random.Range(zMin, zMax);

                    // random height, ball can hit the net and miss
                    float shot_height = Random.Range(minHeightWithMiss, maxHeight);
                    rb.velocity = GetFlySpeed(transform.position, new Vector3(xEnd, 20f, zEnd), shot_height + 26.2f);
                    //Debug.Log("Not Ready");
                    Bot.GetComponent<BotBasicData>().Crouch(false);
                    
                }

                //transform.position = new Vector3(transform.position.x, 26.2f, transform.position.z);
                whoTached = false;
                KickedByBot = true;

                Debug.Log(transform.position.y);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        isKicked = false;
        KickedByBot = false;
    }

    bool onPlatform = false;

    float xMin = -40f;
    float xMax = -15f;
    float zMin = -30f;
    float zMax = 30f;
    float minHeightWithMiss = 1f;
    float minHeight = 5f;
    float maxHeight = 40f;
    float heightWindow = 10f;

    float gravity = 30f;
    float xEnd;
    float zEnd;
    public float botNerf = 5f;

    public GameObject Bot;

    //public Transform test;

    public GameObject unityChan;
    public float WinDelay;
    private bool RoundIsOver = false;

    void BotWins()
    {
        BotPoints += 1;
        BotScore.text = BotPoints.ToString();
        RespawneBot();
    }

    void PlayerWins()
    {
        PlayerPoints += 1;
        PlayerScore.text = PlayerPoints.ToString();
        RespawneBot();
    }

    public RectTransform circle;
    public GameObject effectUI;

    private Vector3 GetUIcords(Vector3 pos)
    {
        Debug.Log(pos);
        Vector3 circlePos;
        circlePos.y = pos.x * 0.8f + 30f;
        circlePos.z = 0f;
        circlePos.x = 255f - pos.z * 0.8f;
        return circlePos;
    }

    void DisableEffect()
    {
        effectUI.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {


        if (collision.gameObject.CompareTag("field") && !RoundIsOver)
        {
            RoundIsOver = true;
            circle.anchoredPosition = GetUIcords(transform.position);
            effectUI.SetActive(true);
            Invoke("DisableEffect", 0.3f);

            if (!CheckForOut(transform.position))
            {
                if (whoTached)
                {
                    Invoke("BotWins", WinDelay);
                }
                else
                {
                    Invoke("PlayerWins", WinDelay);
                }
            }
            else
            {
                if (transform.position.x < 0f)
                {
                    Invoke("BotWins", WinDelay);
                }
                else
                {
                    Invoke("PlayerWins", WinDelay);
                }
            }
            onPlatform = true;

            //unityChan.GetComponent<TriggerR>().TriggerSpKick();
        }
    }

    void NotOnPlatform()
    {
        onPlatform = false;
    }

    private Vector2 findIntersetion(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        float a1 = (p1.y - p2.y) / (p1.x - p2.x);
        float b1 = p1.y - a1 * p1.x;
        float a2 = (p3.y - p4.y) / (p3.x - p4.x);
        float b2 = p3.y - a2 * p3.x;

        Vector2 intrsPoint;
        //Debug.Log(a2);
        //Debug.Log(b2);


        intrsPoint.x = (b1 - b2) / (a2 - a1);
        intrsPoint.y = a1 * intrsPoint.x + b1;

        return intrsPoint;
    }

    private Vector3 GetFlySpeed(Vector3 startPoint, Vector3 finishPoint, float heigthInWorldSpace)
    {
        Vector3 velocity;
        Vector2 netPoint = findIntersetion(new Vector2(10, 0), new Vector2(20, 0), new Vector2(startPoint.z, startPoint.x), new Vector2(finishPoint.z, finishPoint.x));
        float c = netPoint.x;
        netPoint.x = netPoint.y;
        netPoint.y = c;

        float k1 = Mathf.Sqrt((netPoint.x - startPoint.x) * (netPoint.x - startPoint.x) + (netPoint.y - startPoint.z) * (netPoint.y - startPoint.z));
        //Debug.Log(netPoint);
        float k2 = Mathf.Sqrt((finishPoint.x - startPoint.x) * (finishPoint.x - startPoint.x) + (finishPoint.z - startPoint.z) * (finishPoint.z - startPoint.z));

        //Debug.Log("k1 " + k1);
        //Debug.Log("k2 " + k2);
        //float ballLevel = 26.2f;

        float a = (heigthInWorldSpace - finishPoint.y) / ((k1 - k2) * k1);
        float b = -(startPoint.y - finishPoint.y) / k2 - a * k2;

        float velocityK = Mathf.Sqrt(-gravity / (2 * a));
        velocity.y = velocityK * b;
        ////velocity.y = Mathf.Sqrt(heigth * gravity / (2 * (k2 - k1) * k1)) * k2;

        ////float velocityK = Mathf.Sqrt(gravity * (k2 - k1) * k1 / (2 * heigth));

        velocity.x = velocityK * (finishPoint.x - startPoint.x) / k2;
        velocity.z = velocityK * (finishPoint.z - startPoint.z) / k2;

        return velocity;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("animtr") && !RoundIsOver)
        {
            unityChan.GetComponent<TriggerR>().TriggerSpKick();
        }
    }

    private bool CheckForOut(Vector3 ballCoors)
    {
        Vector2 topRightCorner = new Vector2(-30f, 41f);
        Vector2 bottomLeftCorner = new Vector2(30f, -41f);

        if (ballCoors.z < bottomLeftCorner.x && ballCoors.z > topRightCorner.x)
        {
            if (ballCoors.x <= topRightCorner.y && ballCoors.x >= bottomLeftCorner.y)
            {
                return true;
            }
        }
        return false;
    }

    private void RespawneBot()
    {
        rb.velocity = new Vector3(0, 0, 0);
        transform.position = starting_position;

        Bot.GetComponent<BotBasicData>().targetPosition = Bot.GetComponent<BotBasicData>().starting_position;
        Bot.GetComponent<BotBasicData>().Teleport();
        Bot.GetComponent<BotBasicData>().ReadyTime = 0.0f;
        Bot.GetComponent<BotBasicData>().Crouch(false);

        RoundIsOver = false;
    }

}
