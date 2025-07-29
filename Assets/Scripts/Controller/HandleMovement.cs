using UnityEngine;
using System.Collections;

namespace Controller
{
    public class HandleMovement : MonoBehaviour
    {
        public HandleAnim hAnim;
        public Rigidbody rb;
        StateManager states;
        WallRun wr;

        InputHandler ih;

        public float maxSpeed = 35f;
        public float normalSpeed = 25f;
        public float moveSpeed = 0f;
        public float normalRotateSpeed = 4f;
        public int jumpCount = 0;
        public int maxJumps = 1;

        Vector3 storeDirection;
        [HideInInspector]
        public float rotateSpeed;

        public void Init()
        {
            wr = GetComponent<WallRun>();
            hAnim = GetComponent<HandleAnim>();
            states = GetComponent<StateManager>();
            rb = GetComponent<Rigidbody>();
            ih = GetComponent<InputHandler>();
            moveSpeed = normalSpeed;

            rb.angularDrag = 999;
            rb.drag = 4;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        public void Tick()
        {
            if (wr.isWallL || wr.isWallR)
            {
                rotateSpeed = 0f;
            }
            else
            {
                rotateSpeed = normalRotateSpeed;
            }
            Vector3 v = ih.camHolder.forward * states.vertical;
            Vector3 h = ih.camHolder.right * states.horizontal;

            v.y = 0;
            h.y = 0;

            if (states.onGround)
            {
                jumpCount = 0;
                rb.AddForce((v + h).normalized * Speed());
            }
            else
            {
                jumpCount = maxJumps;
            }


            if(Mathf.Abs(states.vertical) > 0 || Mathf.Abs(states.horizontal) > 0)
            {
                storeDirection = (v + h).normalized;

                storeDirection += transform.position;

                Vector3 targetDir = (storeDirection - transform.position).normalized;
                targetDir.y = 0;

                if (targetDir == Vector3.zero)
                    targetDir = transform.forward;

                Quaternion targetRot = Quaternion.LookRotation(targetDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
            }

            if(Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumps)
            {
                ++jumpCount;
                rb.AddForce(transform.up * 225f);
            }

            if(Input.GetKey(KeyCode.LeftShift))
            {
                moveSpeed = maxSpeed;
            }
            else
            {
                moveSpeed = normalSpeed;
            }
        }

        float Speed()
        {
            return moveSpeed;
        }
    }
}
