using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public static int Life = 4;

    float targetSpeed;

    int coolDown = 0;
    float HitStop = 0;
    bool jumpInput = false;
    
    static bool HighSpeed = false;
    static int invisTime = 0;

    // 引っ張り処理用
    [SerializeField] float MinPower = 100;
    [SerializeField] float MaxPower = 200;

    Vector3 clickStartPos;
    Vector2 power;


    Rigidbody2D rigidBody2d;
    SpriteRenderer sr;

    [SerializeField] GameObject mainCamera;
    CameraMove camMove;
    [SerializeField] GameObject powerArrow;
    PowerArrowBehaviour arrow;

    // Start is called before the first frame update
    void Start()
    {
        Life = 4;
        rigidBody2d = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        camMove = mainCamera.GetComponent<CameraMove>();
        arrow = powerArrow.GetComponent<PowerArrowBehaviour>();
    }

    // Update is called once per frame
    void Update()
    {
        targetSpeed = 0;
        if (Input.GetKey(KeyCode.A))
        {
            targetSpeed = -6f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            targetSpeed = 6f;
        }
        
        if(HitStop > 0)
        {
            HitStop -= Time.unscaledDeltaTime;
        } else
        {
            HitStop = 0;
            Time.timeScale = 1;
        }

    }

    private void FixedUpdate()
    {
        if(Life <= 0)
        {
            Life = 4;
            transform.position = new Vector3(-14, -7, 0);
            rigidBody2d.velocity = Vector3.zero;
        }

        //Debug.Log(HighSpeed);
        Invincible();
        if (HighSpeed)
        {
            Color color = sr.color;
            color.r = Mathf.Max(50 - rigidBody2d.velocity.magnitude, 0) / 50;
            sr.color = color;

            if (rigidBody2d.velocity.magnitude < 10)
            {
                HighSpeed = false;
            }
        }


        UpdateStatus();
        HorizontalMove();
        Jump();

        coolDown = Mathf.Max(0, coolDown - 1);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // 圧縮（解放）の処理
        if (collision.CompareTag("PressMachine"))
        {
            // クリック中の処理
            if (Input.GetMouseButton(0))
            {
                // クリック時の処理
                if(clickStartPos == Vector3.zero)
                {
                    clickStartPos = Input.mousePosition;
                }
                else
                {
                    // 長押し中の処理
                    power = clickStartPos - Input.mousePosition;

                    // 力の上限
                    if(power.magnitude > MaxPower)
                    {
                        float powerCorrect = MaxPower / power.magnitude;
                        power.x *= powerCorrect; 
                        power.y *= powerCorrect;
                    }

                    // 下方向には飛ばないようにする
                    float angle = Mathf.Atan2(power.y, power.x) * Mathf.Rad2Deg;
                    if (angle < 0 && angle > -90)
                    {
                        power.x = power.magnitude;
                        power.y = 0;
                    }
                    if(angle <= -90)
                    {
                        power.x = -power.magnitude;
                        power.y = 0;
                    }

                    arrow.drawUpdate(power);
                    
                    // 力が弱すぎるなら矢印を消す（飛ばせないので） 
                    if(power.magnitude < MinPower)
                    {
                        powerArrow.SetActive(false);
                    } else
                    {
                        powerArrow.SetActive(true);
                    }
                }
                
            }


            // リリース処理
            if (!Input.GetMouseButton(0) && clickStartPos != Vector3.zero)
            {
                if (power.magnitude >= MinPower && coolDown <= 0)
                {
                    Life--;
                    rigidBody2d.AddForce(power * 0.15f * rigidBody2d.mass, ForceMode2D.Impulse);
                    coolDown = 60;
                    HighSpeed = true;
                    SoundPlayer.playSound(SE.Shot);
                }
                clickStartPos = Vector3.zero;
                powerArrow.SetActive(false);

            }
        }

    }


    // 衝突時の演出
    private void OnCollisionEnter2D(Collision2D collision)
    {
        ContactPoint2D[] contacts = new ContactPoint2D[collision.contactCount];
        collision.GetContacts(contacts);
        float totalImpulse = 0;
        foreach(ContactPoint2D contact in contacts)
        {
            totalImpulse += contact.normalImpulse;
        }
        //Debug.Log(totalImpulse);

        if (totalImpulse > 40)
        {
            //Debug.Log("Stop");
            //HitStop = 0.02f;
            //Time.timeScale = 0;
        }
    }


    void UpdateStatus()
    {
        switch(Life)
        {
            // 質量4で飛ぶ事は無い
            case 4:
                rigidBody2d.mass = 5;
                rigidBody2d.drag = 0;
                break;
            case 3:
                rigidBody2d.mass = 3;
                rigidBody2d.drag = 0.25f;
                break;
            case 2:
                rigidBody2d.mass = 2;
                rigidBody2d.drag = 0.5f;
                break;
            case 1:
                rigidBody2d.mass = 1;
                rigidBody2d.drag = 1.0f;
                break;

        }
        transform.localScale = new Vector3(0.1f * Life + 0.15f, 0.1f * Life + 0.15f);
    }

    // 左右移動の処理
    void HorizontalMove()
    {
        float velocity = 0;

        if (targetSpeed > 0)
            velocity = Mathf.Clamp(targetSpeed - rigidBody2d.velocity.x, 0, targetSpeed);

        if (targetSpeed < 0)
            velocity = Mathf.Clamp(targetSpeed - rigidBody2d.velocity.x, targetSpeed, 0);


        Vector2 moveForce = new Vector2(velocity * 8.0f, 0);

        rigidBody2d.AddForce(moveForce * rigidBody2d.mass);
    }

    // ジャンプの処理
    void Jump()
    {
        if(Input.GetKey(KeyCode.Space))
        {
            if (jumpInput == false)
            {
                RaycastHit2D raycastHit2D = Physics2D.Raycast(transform.position, Vector2.down, 10f);
                //Debug.Log(raycastHit2D.distance);
                if (raycastHit2D.distance < transform.localScale.y * 2.0f + 0.1f)
                {
                    rigidBody2d.AddForce(new Vector3(0, 6) * rigidBody2d.mass, ForceMode2D.Impulse);
                }
            }
            jumpInput = true;
        } else
        {
            jumpInput = false;
        }
    }


    void Invincible()
    {
        if(invisTime > 0)
        {
            Color color = Color.white;
            color.a = 0.5f;
            sr.color = color;
            invisTime--;
        } else
        {
            sr.color = Color.white;
        }
    }

    public static bool TakeDamage()
    {
        if (HighSpeed)
            return false;

        if(invisTime <= 0)
        {
            Life--;
            invisTime = 60;
        }
        return true;
    }

}
