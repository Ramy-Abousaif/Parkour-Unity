using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Controller
{
    public class WallRun : MonoBehaviour
    {
        StateManager states;
        HandleMovement hMove;
        public bool isWallR = false;
        public bool isWallL = false;
        private RaycastHit hitR;
        private RaycastHit hitL;
        public Rigidbody rb;
        public Transform cameraEffect;
        public Animator anim;
        public bool canJump;
        private float gravityScale = 0f;
        public float normalGravity = -9.81f;
        public float wallRunReach = 1f;
        public float wallJumpUp = 1f;
        public float wallJumpSide = 1f;

        // Use this for initialization
        void Start()
        {
            states = GetComponent<StateManager>();
            hMove = GetComponent<HandleMovement>();
            rb = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        void Update()
        {
            if (states.onGround)
            {
                gravityScale = 0f;
                hMove.jumpCount = 0;
                isWallL = false;
                isWallR = false;
            }

            if (canJump == true && Input.GetKeyDown(KeyCode.Space))
            {
                gravityScale = 0f;
                rb.AddForce(Vector3.up * wallJumpUp, ForceMode.Impulse);
                if (isWallL)
                {
                    Vector3 force = this.transform.right * wallJumpSide;
                    rb.AddForceAtPosition(force, this.transform.position, ForceMode.Impulse);
                }
                if (isWallR)
                {
                    Vector3 force = -this.transform.right * wallJumpSide;
                    rb.AddForceAtPosition(force, this.transform.position, ForceMode.Impulse);
                }
            }

            if (!states.onGround)
            {
                if (Physics.Raycast(transform.position, transform.right, out hitR, wallRunReach))
                {
                        canJump = true;
                        isWallR = true;
                        isWallL = false;
                        hMove.jumpCount += 1;
                        if(gravityScale > normalGravity)
                        {
                            gravityScale -= Time.deltaTime * 4f;
                        }
                        Physics.gravity = new Vector3(Physics.gravity.x, gravityScale, Physics.gravity.z);
                }
                if (!Physics.Raycast(transform.position, transform.right, out hitR, wallRunReach))
                {

                    isWallR = false;
                    hMove.jumpCount += 1;
                    if (isWallL == false)
                    {
                        canJump = false;
                        Physics.gravity = new Vector3(Physics.gravity.x, normalGravity, Physics.gravity.z);
                    }
                }
                if (Physics.Raycast(transform.position, -transform.right, out hitL, wallRunReach))
                {
                        canJump = true;
                        isWallL = true;
                        isWallR = false;
                        hMove.jumpCount += 1;
                        if (gravityScale > normalGravity)
                        {
                            gravityScale -= Time.deltaTime * 4f;
                        }
                        Physics.gravity = new Vector3(Physics.gravity.x, gravityScale, Physics.gravity.z);
                }
                if (!Physics.Raycast(transform.position, -transform.right, out hitL, wallRunReach))
                {

                    isWallL = false;
                    hMove.jumpCount += 1;
                    if (isWallR == false)
                    {
                        canJump = false;
                        Physics.gravity = new Vector3(Physics.gravity.x, normalGravity, Physics.gravity.z);
                    }
                }
            }
        }
    }
}
