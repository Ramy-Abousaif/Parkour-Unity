using UnityEngine;
using System.Collections;

namespace Controller
{
    public class HandleAnim : MonoBehaviour
    {

        StateManager states;
        public Animator anim;
        Vaulting vault;
        WallRun wr;

        public bool sliding = false;
        public float slide_timer = 0f;

        public void Init(StateManager st)
        {
            states = st;
            wr = GetComponent<WallRun>();
            anim = GetComponent<Animator>();
            vault = GetComponent<Vaulting>();

            Animator[] childAnims = GetComponentsInChildren<Animator>();

            for (int i = 0; i < childAnims.Length; i++)
            {
                if(childAnims[i] != anim)
                {
                    anim.avatar = childAnims[i].avatar;
                    Destroy(childAnims[i]);
                    break;
                }
            }
        }

        public void Tick()
        {
            float animValue = Mathf.Abs(states.horizontal) + Mathf.Abs(states.vertical);
            animValue = Mathf.Clamp01(animValue);

            anim.SetFloat("Movement",animValue);
            if (Input.GetKeyDown(KeyCode.E) && !sliding && (states.vertical != 0f || states.horizontal != 0f))
            {
                slide_timer = 0f;
                anim.SetBool("Sliding", true);
                sliding = true;
            }
            if(sliding)
            {
                slide_timer += Time.deltaTime;

                if(slide_timer > 1.5f)
                {
                    sliding = false;
                    anim.SetBool("Sliding", false);
                }
            }

            if(states.onGround)
            {
                anim.SetBool("onAir", false);
            }
            else
            {
                anim.SetBool("onAir", true);
            }

            if(vault.isInit)
            {
                anim.SetBool("Vaulting", true);
            }
            else
            {
                anim.SetBool("Vaulting", false);
            }

            if (wr.isWallL)
            {
                anim.SetBool("onAir", false);
                anim.SetBool("isWallL", true);
            }
            else
            {
                anim.SetBool("isWallL", false);
            }

            if (wr.isWallR)
            {
                anim.SetBool("onAir", false);
                anim.SetBool("isWallR", true);
            }
            else
            {
                anim.SetBool("isWallR", false);
            }
        }
    }
}
