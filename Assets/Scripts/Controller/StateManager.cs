using UnityEngine;
using System.Collections;

namespace Controller
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(HandleAnim))]
    public class StateManager : MonoBehaviour
    {
        public float horizontal;
        public float vertical;
        public bool dummy;
        public bool onGround = true;

        [HideInInspector]
        public HandleAnim hAnim;
        [HideInInspector]
        public HandleMovement hMovement;

        void Start()
        {
            hAnim = GetComponent<HandleAnim>();
            hMovement = GetComponent<HandleMovement>();

            hAnim.Init(this);
            hMovement.Init();
        }

        void Update()
        {
            if (!dummy)
            {
                hAnim.Tick();
                hMovement.Tick();
                IsOnGround();

                if (hAnim.slide_timer < 0.85f && hAnim.sliding)
                {
                    GetComponent<Rigidbody>().AddForce(transform.forward * (hMovement.moveSpeed * 0.4f));
                    GetComponent<CapsuleCollider>().height = 0.5f;
                    GetComponent<CapsuleCollider>().center = new Vector3(0f, 1f, 0f);
                }

                else
                {
                    GetComponent<CapsuleCollider>().height = 2f;
                    GetComponent<CapsuleCollider>().center = new Vector3(0f, 1f, 0f);
                }
            }
        }

        public void EnableController()
        {
            dummy = false;
            hMovement.rb.isKinematic = false;
            GetComponent<Collider>().isTrigger = false;
        }

        public void DisableController()
        {
            dummy = true;
            hMovement.rb.isKinematic = true;
            GetComponent<Collider>().isTrigger = true;
        }

        void IsOnGround()
        {
            onGround = OnGround();

            if(onGround)
            {
                hMovement.rb.drag = 4;
            }
            else
            {
                hMovement.rb.drag = 0;                
            }
        }

        bool OnGround()
        {
            bool retVal = false;

            Vector3 origin = transform.position + Vector3.up / 18;
            Vector3 direction = -Vector3.up;
            float distance =  0.2f;
            LayerMask lm = ~(1 << gameObject.layer);
            RaycastHit hit;

            if(Physics.Raycast(origin, direction, out hit, distance, lm))
            {
                if (hit.transform.gameObject.layer == gameObject.layer)
                    Debug.Log("OnGround hit an object with the same layer as the controller!!");

                retVal = true;
            }

            return retVal;
        }
    }
}
