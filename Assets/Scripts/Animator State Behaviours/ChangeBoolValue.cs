using UnityEngine;
using System.Collections;

public class ChangeBoolValue : StateMachineBehaviour {

    public string boolName;
    public bool value;
    public bool onExitReverse;

	 // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) 
    {
        animator.SetBool(boolName, value);
	}

	// OnStateExit is called when a transition ends and the state machine finishes evaluating this state
	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        
        if(onExitReverse)
            animator.SetBool(boolName, !value);
	}
}
