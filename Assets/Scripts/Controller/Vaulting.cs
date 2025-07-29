using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Controller
{
    public class Vaulting : MonoBehaviour
    {
        StateManager states;
        HandleAnim hAnim;
        Animator anim;
        public bool isVaulting;
        public float origin1Offset = 0.9f;
        public float rayForwardDis = 1f;
        public float origin2Offset = 0.2f;
        public float rayDownDis = 1.5f;
        public float rayHigherForwardDis = 1f;
        public Vector3 startPosition;
        public Vector3 endingPosition;
        public float vaultSpeed = 2f;
        public bool isInit;
        public float animLength;
        public float vaultOffsetPosition = 2f;

        public AnimationClip vaultOver;

        public float vaultT;

        void start()
        {
            hAnim = GetComponent<HandleAnim>();
            states = GetComponent<StateManager>();
            anim = GetComponent<Animator>();
        }

        // Update is called once per frame
        void Update()
        {
            CheckCondition();
            if(isVaulting)
            {
                GetComponent<CapsuleCollider>().enabled = false;
                Execute();
            }
            else
            {
                GetComponent<CapsuleCollider>().enabled = true;
            }

        }

        public bool CheckCondition()
        {
            bool result = false;

            RaycastHit hit;
            Vector3 origin = transform.position;
            origin.y += origin1Offset;
            Vector3 direction = transform.forward;


            Debug.DrawRay(origin, direction * rayForwardDis, Color.green);
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Physics.Raycast(origin, direction, out hit, rayForwardDis))
                {
                    Vector3 origin2 = origin;
                    origin2.y += origin2Offset;

                    Vector3 firstHit = hit.point;
                    firstHit.y -= origin1Offset;
                    Vector3 normalDir = -hit.normal;

                    Debug.DrawRay(origin2, direction * rayForwardDis, Color.green);
                    if (Physics.Raycast(origin2, direction, out hit, rayHigherForwardDis))
                    {

                    }
                    else
                    {
                        Vector3 origin3 = origin2 + (direction * rayHigherForwardDis);
                        Debug.DrawRay(origin3, -Vector3.up * rayDownDis, Color.green);
                        if (Physics.Raycast(origin3, -Vector3.up, out hit, rayDownDis))
                        {
                            //hit ground
                            result = true;
                            animLength = vaultOver.length;
                            isInit = false;
                            isVaulting = true;

                            startPosition = transform.position;

                            Vector3 endPosition = firstHit;
                            endPosition += normalDir * vaultOffsetPosition;
                            endingPosition = endPosition;
                        }
                    }
                }
            }
            return result;
        }

        public void Execute()
        {
            if(!isInit)
            {
                vaultT = 0;
                isInit = true;

                Vector3 dir = endingPosition - startPosition;
                dir.y = 0;
                Quaternion rot = Quaternion.LookRotation(dir);
                transform.rotation = rot;
            }

            float actualSpeed = (Time.deltaTime * vaultSpeed) / animLength;

            vaultT += actualSpeed;

            if(vaultT > 1)
            {
                isInit = false;
                isVaulting = false;
            }

            Vector3 targetPosition = Vector3.Lerp(startPosition, endingPosition, vaultT);
            transform.position = targetPosition;
        }
    }
}
