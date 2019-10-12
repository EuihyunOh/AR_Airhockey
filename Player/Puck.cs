using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
/// 
///  Puck은 종합적인 공의 동작을 관리합니다.
/// 줄 36과 45을 주석처리함. 이는 생성한 벽이 좌우앞뒤 구분이 없기 때문이고, addforce를 퍽에 매번 적용시키는 것보다 마찰을
/// 0으로 만드는게 당장 에디터에서 할땐 나아서. 만약 마찰을 넣으려면 addforce값과의 균형을 맞출것 
/// Puck의 Collider에서 Material의 Friction(마찰) 값을 0.0001로 설정했음
/// 
/// 
public class Puck : MonoBehaviourPunCallbacks, IPunObservable
{
    //볼오브젝트.
    //충돌후 속도감소(1m/s->0.9m/s)
    public float Elasticity = 0.9f;


    //볼동작
    [System.NonSerialized] public Vector3 Movement;
    [System.NonSerialized] public Rigidbody _Rigidbody;

    private ARHockeyGameController gameController;

    //공 초기세팅.x, z축회전막아서 평면회전시킴.
    private void Start()
    {
        gameController = FindObjectOfType<ARHockeyGameController>();

        _Rigidbody = GetComponent<Rigidbody>();
    }


    //매번 볼동작시킴.
    void Update()
    {
        //y 축값 고정
        gameController.ScoreText.text = transform.position+ "G1234";
        Debug.Log("err : PothonError ddd");

        if (!photonView.IsMine)
        {

            if ((transform.position - currentPos).sqrMagnitude >= 10.0f * 10.0f)
            {
                transform.position = currentPos;
                transform.rotation = currentRot;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, currentPos, Time.deltaTime * 10.0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, currentRot, Time.deltaTime * 10.0f);
            }
        }
        //퍽 속도 체크
        Movement = _Rigidbody.velocity;
    }

    //충돌한 오브젝트에 따라 동작.
    void OnCollisionEnter(Collision coll)
    {
        GameObject hitObject = coll.gameObject;
        Vector3 vec3;
        if (hitObject.name == "Wall_Left" || hitObject.name == "Wall_Right")
        {
            Vector3 CurrentVelocity = _Rigidbody.velocity;
            CurrentVelocity.x *= -0.95f;

        }
        if (hitObject.tag == "Striker")
        {
            HockeyStriker hockeyStrikerInfor = hitObject.GetComponent<HockeyStriker>();
            vec3 = hockeyStrikerInfor.StrikerMoveBall() * 10f; //이정도 값을 해야 좀 속도가 났음
            _Rigidbody.AddForce(vec3);
        }
    }


    //골에 닿을시 작동. 코너 트리거 박스면 튕기는 듯한 이펙트
    private void OnTriggerEnter(Collider trigger)
    {
        if (trigger.tag == "Goal")
        {
            if (trigger.GetComponent<GoalInf>())
            {
                switch (trigger.GetComponent<GoalInf>().TeamNo)
                {
                    case 0:
                        gameController.ScoreUp(1);
                        gameController.SpawnNewPuck(0);
                        Destroy(gameObject);
                        break;

                    case 1:
                        gameController.ScoreUp(0);
                        gameController.SpawnNewPuck(1);
                        Destroy(gameObject);
                        break;

                    default:
                        Debug.Log(trigger.GetComponent<GoalInf>().TeamNo);
                        return;
                }
            }
        }
        if (trigger.tag == "Corner")
        {
            //반사각 계산을 위한 코너 벽면의 법선 산출
            Vector3 inNormal_OfTrigger = new Vector3(Mathf.Cos(Mathf.Deg2Rad * trigger.transform.eulerAngles.y), 0, -Mathf.Sin(Mathf.Deg2Rad * trigger.transform.eulerAngles.y));
            _Rigidbody.velocity = Vector3.Reflect(_Rigidbody.velocity, inNormal_OfTrigger);
            //Debug.Log(_Rigidbody.velocity.ToString());
        }
    }

    //골을 통과하면 공을 삭제
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Team1" || other.tag == "Team2")
        {
            Destroy(gameObject);
        }
    }

    #region 포톤뷰 통신부분
    private Vector3 currentPos;
    private Quaternion currentRot;
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            currentPos = (Vector3)stream.ReceiveNext();
            currentRot = (Quaternion)stream.ReceiveNext();
        }
    }
    #endregion

}
