using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Climbing
{
    public class ClimbBehaviour : MonoBehaviour
    {
        #region Variables
        //vairables for the start of the behaviour
        public bool climbing;
        bool initClimb;
        bool waitToStartClimb;

        Animator anim;
        ClimbIK ik;

        //point variables
        Manager curManager;
        Point targetPoint;
        Point curPoint;
        Point prevPoint;
        Neighbour neighbour;
        ConnectionType curConnection;

        //current and target states
        ClimbStates climbState;
        ClimbStates targetState;

        public enum ClimbStates
        {
            onPoint,
            betweenPoints,
            inTransit
        }

        #region Curves
        //variables for curve-like movements
        CurvesHolder curvesHolder;
        BezierCurve directCurveHorizontal;
        BezierCurve directCurveVertical;
        BezierCurve dismountCurve;
        BezierCurve mountCurve;
        BezierCurve curCurve;
        #endregion

        //interpolation variables
        Vector3 _startPos;
        Vector3 _targetPos;
        float _distance;
        float _t;
        bool initTransit;
        bool rootReached;
        bool ikLandSideReached;
        bool ikFollowSideReached;

        //input variables
        bool lockInput;
        Vector3 inputDirection;
        Vector3 targetPosition;

        //tweakable variables
        public Vector3 rootOffset = new Vector3(0, -0.86f, 0); //how much the hips of the animation are above the ground
        public float speed_linear = 1.3f;
        public float speed_direct = 2.0f;

        public AnimationCurve a_jumpingCurve;
        public AnimationCurve a_mountCurve;
        public bool enableRootMovement;
        float _rmMax = 0.25f; //max fail safe for root movement
        float _rmT;
        #endregion

        void SetCurveReferences()
        {
            //Creates a new gameobject which has all our desired curves in it and assigns them
            GameObject chPrefab = Resources.Load("CurvesHolder") as GameObject;
            GameObject chGO = Instantiate(chPrefab) as GameObject;

            curvesHolder = chGO.GetComponent<CurvesHolder>();

            directCurveHorizontal = curvesHolder.ReturnCurve(CurveType.horizontal);
            directCurveVertical = curvesHolder.ReturnCurve(CurveType.vertical);
            dismountCurve = curvesHolder.ReturnCurve(CurveType.dismount);
            mountCurve = curvesHolder.ReturnCurve(CurveType.mount);
        }

        void Start()
        {
            anim = GetComponentInChildren<Animator>();
            ik = GetComponentInChildren<ClimbIK>();
            SetCurveReferences();
        }

        void FixedUpdate()
        {
            if (climbing)
            {
                if (!waitToStartClimb)
                {
                    HandleClimbing();
                    InitiateFallOff();
                }
                else
                {
                    InitClimbing();
                    HandleMount();
                }
            }
            else
            {
                if (initClimb)
                {
                    transform.parent = null;
                    initClimb = false;
                }

                if (Input.GetKey(KeyCode.Space))
                    LookForClimbSpot();
            }
        }

        void LookForClimbSpot()
        {
            //Can be changed according to the controller
            Transform camTrans = Camera.main.transform;
            Ray ray = new Ray(camTrans.position, camTrans.forward);

            RaycastHit hit;
            LayerMask lm = (1 << gameObject.layer) | (1 << 3);
            lm = ~lm;

            float maxDistance = 20.0f;

            if (Physics.Raycast(ray, out hit, maxDistance, lm))
            {
                if (hit.transform.GetComponentInParent<Manager>())
                {
                    Manager tm = hit.transform.GetComponentInParent<Manager>();

                    Point closestPoint = tm.ReturnClosest(transform.position);

                    float distanceToPoint = Vector3.Distance(transform.position, closestPoint.transform.parent.position);

                    if (distanceToPoint < 5)
                    {
                        curManager = tm;
                        targetPoint = closestPoint;
                        targetPosition = closestPoint.transform.position;
                        curPoint = closestPoint;
                        climbing = true;
                        lockInput = true;
                        targetState = ClimbStates.onPoint;

                        anim.CrossFade("To_Climb", 0.4f);
                        GetComponent<Controller.StateManager>().DisableController();

                        waitToStartClimb = true;
                    }
                }
            }
        }

        void HandleClimbing()
        {
            if (!lockInput)
            {
                //handles the input whenever we are not already moving
                inputDirection = Vector3.zero;

                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical");

                inputDirection = ConvertToInputDirection(h, v); //converts input to direction

                if (inputDirection != Vector3.zero)
                {
                    switch (climbState)
                    {
                        case ClimbStates.onPoint:
                            OnPoint(inputDirection);
                            break;
                        case ClimbStates.betweenPoints:
                            BetweenPoints(inputDirection);
                            break;
                    }
                }

                //These lines make it so that the player's position changes along with the point
                //while he's OnPoint
                transform.parent = curPoint.transform.parent;

                if (climbState == ClimbStates.onPoint)
                {
                    ik.UpdateAllTargetPositions(curPoint);
                    ik.ImmediatePlaceHelpers();
                }
            }
            else
            {
                //If input is locked then player is moving
                InTransit(inputDirection);
            }
        }

        Vector3 ConvertToInputDirection(float horizontal, float vertical)
        {
            int h = (horizontal != 0) ?
                (horizontal < 0) ? -1 : 1
                : 0;

            int v = (vertical != 0) ?
                (vertical < 0) ? -1 : 1
                : 0;

            int z = v + h;

            z = (z != 0) ?
                (z < 0) ? -1 : 1
                : 0;

            Vector3 retVal = Vector3.zero;
            retVal.x = h;
            retVal.y = v;

            return retVal;
        }

        void OnPoint(Vector3 inputD)
        {
            //find a neighbour if it exists, towards the desired direction
            neighbour = null;
            neighbour = curManager.ReturnNeighbour(inputD, curPoint);

            if (neighbour != null)
            {
                targetPoint = neighbour.target; //sets neighbour as target
                prevPoint = curPoint; //the previous point is currently the point we are on now
                climbState = ClimbStates.inTransit; //whatever we are doing, next state would be us moving
                UpdateConnectionTransitionByType(neighbour, inputD); //update the variables depending on our connection
                lockInput = true; //No input allowed while movement/animation plays
            }
        }

        void BetweenPoints(Vector3 inputD)
        {
            Neighbour n = targetPoint.ReturnNeighbour(prevPoint);

            if (n != null)
            {
                if (inputD == n.direction)
                    targetPoint = prevPoint;
            }
            else
            {
                targetPoint = curPoint;
            }

            targetPosition = targetPoint.transform.position;
            climbState = ClimbStates.inTransit;
            targetState = ClimbStates.onPoint;
            prevPoint = curPoint;
            lockInput = true;
            anim.SetBool("Move", false);
        }

        void UpdateConnectionTransitionByType(Neighbour n, Vector3 inputD)
        {
            Vector3 desiredPos = Vector3.zero;
            curConnection = n.cType;

            Vector3 direction = targetPoint.transform.position - curPoint.transform.position;
            direction.Normalize();

            switch (n.cType)
            {
                //Connection type is a 2 step transition aka InBetween
                case ConnectionType.inBetween:
                    float distance = Vector3.Distance(curPoint.transform.position, targetPoint.transform.position);
                    desiredPos = curPoint.transform.position + (direction * (distance / 2)); //target position is in the middle of the 2 points
                    targetState = ClimbStates.betweenPoints; //when current transition ends the player will be at this state
                    TransitDir transitDir = ReturnTransitDirection(inputD, false);
                    PlayAnim(transitDir);
                    break;
                //Connection type is 1 step transition where the curve handles most of the work
                case ConnectionType.direct:
                    desiredPos = targetPoint.transform.position;
                    targetState = ClimbStates.onPoint; //when current transition ends the player will be on a point again
                    TransitDir transitDir2 = ReturnTransitDirection(direction, true);
                    PlayAnim(transitDir2, true);
                    break;
                case ConnectionType.dismount:
                    desiredPos = targetPoint.transform.position;
                    anim.SetInteger("JumpType", 20);
                    anim.SetBool("Move", true);
                    break;
            }
            targetPosition = desiredPos;
        }

        void InTransit(Vector3 inputD)
        {
            switch (curConnection)
            {
                case ConnectionType.inBetween:
                    UpdateLinearVariables();
                    Linear_RootMovement();
                    LerpIKLandingSide_Linear();
                    WrapUp();
                    break;
                case ConnectionType.direct:
                    UpdateDirectVariables(inputDirection);
                    Direct_RootMovement();
                    DirectHandleIK();
                    WrapUp(true);
                    break;
                case ConnectionType.dismount:
                    HandleDismountVariables();
                    Dismount_RootMovement();
                    HandleDismountIK();
                    DismountWrapUp();
                    break;
            }
        }

        #region Linear (2 step)
        void UpdateLinearVariables()
        {
            if (!initTransit)
            {
                initTransit = true;
                enableRootMovement = true;
                rootReached = false;
                ikFollowSideReached = false;
                ikLandSideReached = false;
                _t = 0;
                _startPos = transform.position;
                _targetPos = targetPosition + rootOffset;
                Vector3 directionToPoint = (_targetPos - _startPos).normalized;

                //makes it so that the movement doesn't look quite linear and gives it a more realistic and fluid look
                bool twoStep = (targetState == ClimbStates.betweenPoints);
                Vector3 back = -transform.forward * 0.05f;
                if (twoStep)
                    _targetPos += back;

                _distance = Vector3.Distance(_targetPos, _startPos);

                InitIK(directionToPoint, !twoStep);
            }
        }

        void Linear_RootMovement()
        {
            float speed = speed_linear * Time.deltaTime;
            float lerpSpeed = speed / _distance;
            _t += lerpSpeed;

            if (_t > 1)
            {
                _t = 1;
                rootReached = true;
            }

            Vector3 currentPosition = Vector3.LerpUnclamped(_startPos, _targetPos, _t);
            transform.position = currentPosition;

            HandleRotation();
        }

        void LerpIKLandingSide_Linear()
        {
            float speed = speed_linear * Time.deltaTime;
            float lerpSpeed = speed / _distance;

            _ikT += lerpSpeed * 3;

            if (_ikT > 1)
            {
                _ikT = 1;
                ikLandSideReached = true;
            }

            Vector3 ikPosition = Vector3.LerpUnclamped(_ikStartPos[0], _ikTargetPos[0], _ikT);
            ik.UpdateTargetPosition(ik_L, ikPosition);

            _fikT += lerpSpeed * 2;
            if(_fikT > 1)
            {
                _fikT = 1;
                ikFollowSideReached = true;
            }

            Vector3 followSide = Vector3.LerpUnclamped(_ikStartPos[1], _ikTargetPos[1], _fikT);
            ik.UpdateTargetPosition(ik_F, followSide);
        }

        #endregion

        #region Direct (1 step)
        void UpdateDirectVariables(Vector3 inputD)
        {
            if (!initTransit)
            {
                initTransit = true;
                enableRootMovement = false;
                rootReached = false;
                ikFollowSideReached = false;
                ikLandSideReached = false;
                _t = 0;
                _rmT = 0;
                _targetPos = targetPosition + rootOffset;
                _startPos = transform.position;

                //if we are going vertical we are using a different curve than horizontal
                bool vertical = (Mathf.Abs(inputD.y) > 0.1f);
                curCurve = (vertical) ? directCurveVertical : directCurveHorizontal;
                curCurve.transform.rotation = curPoint.transform.rotation;

                if (!vertical)
                {
                    if (!(inputD.x > 0)) //!right
                    {
                        Vector3 eulers = curCurve.transform.eulerAngles;
                        eulers.y = -180;
                        curCurve.transform.eulerAngles = eulers;
                    }
                }
                else
                {
                    if (!(inputD.y > 0)) //!up
                    {
                        Vector3 eulers = curCurve.transform.eulerAngles;
                        eulers.x = 180;
                        eulers.y = 180;
                        curCurve.transform.eulerAngles = eulers;
                    }
                }

                //sets the first point of the curve on the starting point and sets the last point of the curve on the target position
                BezierPoint[] points = curCurve.GetAnchorPoints();
                points[0].transform.position = _startPos;
                points[points.Length - 1].transform.position = _targetPos;

                InitIK_Direct(inputDirection);
            }
        }

        void Direct_RootMovement()
        {
            if (enableRootMovement)
            {
                _t += Time.deltaTime * speed_direct;
            }
            else
            {
                if (_rmT < _rmMax)
                    _rmT += Time.deltaTime;
                else
                    enableRootMovement = true;
            }

            if (_t > 0.95f)
            {
                _t = 1;
                rootReached = true;
            }

            HandleWeightAll(_t, a_jumpingCurve);

            Vector3 targetPos = curCurve.GetPointAt(_t);
            transform.position = targetPos;

            HandleRotation();
        }

        void DirectHandleIK()
        {
            if(inputDirection.y != 0)
            {
                LerpIKHands_Direct();
                LerpIKFeet_Direct();
            }
            else
            {
                LerpIKLandingSide_Direct();
                LerpIKFollowSide_Direct();
            }
        }

        #endregion

        #region Mount

        void InitClimbing()
        {
            if (!initClimb)
            {
                initClimb = true;

                if (ik != null)
                {
                    ik.UpdateAllPointsOnOne(targetPoint);
                    ik.UpdateAllTargetPositions(targetPoint);
                    ik.ImmediatePlaceHelpers();

                    //current connection is 1 step transition
                    curConnection = ConnectionType.direct;
                    //state when the current one ends
                    targetState = ClimbStates.onPoint;
                }
            }
        }

        //handles curve movement when mounting the wall
        void HandleMount()
        {
            if (!initTransit)
            {
                initTransit = true;
                ikFollowSideReached = false;
                ikLandSideReached = false;
                _t = 0;
                _startPos = transform.position;
                _targetPos = targetPosition + rootOffset;

                curCurve = mountCurve;
                curCurve.transform.rotation = targetPoint.transform.rotation;
                BezierPoint[] points = curCurve.GetAnchorPoints();
                points[0].transform.position = _startPos;
                points[points.Length - 1].transform.position = _targetPos;
            }

            if (enableRootMovement)
            {
                _t += Time.deltaTime * 2;
            }

            if (_t >= 0.99f)
            {
                _t = 1;
                waitToStartClimb = false;
                lockInput = false;
                initTransit = false;
                ikLandSideReached = false;
                climbState = targetState;
            }

            Vector3 targetPos = curCurve.GetPointAt(_t);
            transform.position = targetPos;

            HandleWeightAll(_t, a_mountCurve);

            HandleRotation();
        }

        #endregion

        #region Dismount
        void HandleDismountVariables()
        {
            if (!initTransit)
            {
                initTransit = true;
                enableRootMovement = false;
                rootReached = false;
                ikLandSideReached = false;
                ikFollowSideReached = false;
                _t = 0;
                _rmT = 0;
                _startPos = transform.position;
                _targetPos = targetPosition;

                curCurve = dismountCurve;
                BezierPoint[] points = curCurve.GetAnchorPoints();
                curCurve.transform.rotation = transform.rotation;
                points[0].transform.position = _startPos;
                points[points.Length - 1].transform.position = _targetPos;

                _ikT = 0;
                _fikT = 0;
            }
        }

        void Dismount_RootMovement()
        {
            if (enableRootMovement)
                _t += Time.deltaTime / 2;

            if (_t >= 0.99f)
            {
                _t = 1;
                rootReached = true;
            }

            Vector3 targetPos = curCurve.GetPointAt(_t);
            transform.position = targetPos;
        }

        void HandleDismountIK()
        {
            if (enableRootMovement)
                _ikT += Time.deltaTime * 3;

            _fikT += Time.deltaTime * 2;

            HandleIKWeight_Dismount(_ikT, _fikT, 1, 0);
        }

        void HandleIKWeight_Dismount(float ht, float ft, float from, float to)
        {
            float t1 = ht * 3;

            if(t1 > 1)
            {
                t1 = 1;
                ikLandSideReached = true;
            }

            float handsWeight = Mathf.Lerp(from, to, t1);
            ik.InfluenceWeight(AvatarIKGoal.LeftHand, handsWeight);
            ik.InfluenceWeight(AvatarIKGoal.RightHand, handsWeight);

            float t2 = ft * 1;

            if (t2 > 1)
            {
                t2 = 1;
                ikFollowSideReached = true;
            }

            float feetWeight = Mathf.Lerp(from, to, t2);
            ik.InfluenceWeight(AvatarIKGoal.LeftFoot, feetWeight);
            ik.InfluenceWeight(AvatarIKGoal.RightFoot, feetWeight);
        }

        void DismountWrapUp()
        {
            if (rootReached)
            {
                climbing = false;
                initTransit = false;
                GetComponent<Controller.StateManager>().EnableController();
            }
        }

        #endregion

        #region Falloff
        void InitiateFallOff()
        {
            if (climbState == ClimbStates.onPoint)
            {
                if (Input.GetKeyUp(KeyCode.X))
                {
                    climbing = false;
                    initTransit = false;
                    ik.AddWeightInfluenceAll(0);
                    GetComponent<Controller.StateManager>().EnableController();
                    anim.SetBool("onAir", true);
                }
            }
        }
        #endregion

        #region Universal

        bool waitForWrapUp;

        void WrapUp(bool direct = false)
        {
            //add delay if needed

            //if root is finished, you can add an additional if statement if the iks have finished
            if (rootReached)
            {
                if (!anim.GetBool("Jump"))
                {
                    if (!waitForWrapUp)
                    {
                        StartCoroutine(WrapUpTransition(0.05f));
                        waitForWrapUp = true;
                    }
                }
            }
        }

        IEnumerator WrapUpTransition(float t)
        {
            yield return new WaitForSeconds(t);
            climbState = targetState; //set our current state

            if (climbState == ClimbStates.onPoint)
                curPoint = targetPoint; //updates to target point only if the player is not in between

            //reset variables
            initTransit = false;
            lockInput = false;
            inputDirection = Vector3.zero;
            waitForWrapUp = false;
        }

        void HandleRotation()
        {
            Vector3 targetDir = targetPoint.transform.forward;

            if (targetDir == Vector3.zero)
                targetDir = transform.forward;

            Quaternion targetRot = Quaternion.LookRotation(targetDir);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5);
        }

        #endregion

        #region IK
 void LerpIKHands_Direct()
        {
            if (enableRootMovement)
                _ikT += Time.deltaTime * 5;
 
            if (_ikT > 1)
            {
                _ikT = 1;
                ikLandSideReached = true;
            }
 
            Vector3 lhPosition = Vector3.LerpUnclamped(_ikStartPos[0], _ikTargetPos[0], _ikT);
            ik.UpdateTargetPosition(AvatarIKGoal.LeftHand, lhPosition);
 
            Vector3 rhPosition = Vector3.LerpUnclamped(_ikStartPos[2], _ikTargetPos[2], _ikT);
            ik.UpdateTargetPosition(AvatarIKGoal.RightHand, rhPosition);
        }
 
        void LerpIKFeet_Direct()
        {
            //if (targetPoint.pointType == PointType.hanging)
            //{
            //    ik.InfluenceWeight(AvatarIKGoal.LeftFoot, 0);
            //    ik.InfluenceWeight(AvatarIKGoal.RightFoot, 0);
            //}
            //else
            //{
                if (enableRootMovement)
                    _fikT += Time.deltaTime * 5;
 
                if (_fikT > 1)
                {
                    _fikT = 1;
                    ikFollowSideReached = true;
                }
 
                Vector3 lfPosition = Vector3.LerpUnclamped(_ikStartPos[1], _ikTargetPos[1], _fikT);
                ik.UpdateTargetPosition(AvatarIKGoal.LeftFoot, lfPosition);
 
                Vector3 rfPosition = Vector3.LerpUnclamped(_ikStartPos[3], _ikTargetPos[3], _fikT);
                ik.UpdateTargetPosition(AvatarIKGoal.RightFoot, rfPosition);
            //}
        }
 
        void LerpIKLandingSide_Direct()
        {
            if (enableRootMovement)
                _ikT += Time.deltaTime * 3.2f;
 
            if (_ikT > 1)
            {
                _ikT = 1;
                ikLandSideReached = true;
            }
 
            Vector3 landPosition = Vector3.LerpUnclamped(_ikStartPos[0], _ikTargetPos[0], _ikT);
            ik.UpdateTargetPosition(ik_L, landPosition);
 
            //if (targetPoint.pointType == PointType.hanging)
            //{
            //    ik.InfluenceWeight(AvatarIKGoal.LeftFoot, 0);
            //    ik.InfluenceWeight(AvatarIKGoal.RightFoot, 0);
            //}
            //else
            //{
                Vector3 followPosition = Vector3.LerpUnclamped(_ikStartPos[1], _ikTargetPos[1], _ikT);
                ik.UpdateTargetPosition(ik_F, followPosition);
            //}
        }
 
        void LerpIKFollowSide_Direct()
        {
            if (enableRootMovement)
                _fikT += Time.deltaTime * 2.6f;
 
            if (_fikT > 1)
            {
                _fikT = 1;
                ikFollowSideReached = true;
            }
 
            Vector3 landPosition = Vector3.LerpUnclamped(_ikStartPos[2], _ikTargetPos[2], _fikT);
            ik.UpdateTargetPosition(ik.ReturnOppositeIK(ik_L), landPosition);
 
            //if (targetPoint.pointType == PointType.hanging)
            //{
            //    ik.InfluenceWeight(AvatarIKGoal.LeftFoot, 0);
            //    ik.InfluenceWeight(AvatarIKGoal.RightFoot, 0);
            //}
            //else
            //{
 
                Vector3 followPosition = Vector3.LerpUnclamped(_ikStartPos[3], _ikTargetPos[3], _fikT);
                ik.UpdateTargetPosition(ik.ReturnOppositeIK(ik_F), followPosition);
            //}
        }
 
        AvatarIKGoal ik_L; //ik for the landing side
        AvatarIKGoal ik_F; //ik for the following side
        float _ikT;
        float _fikT;
        Vector3[] _ikStartPos = new Vector3[4];
        Vector3[] _ikTargetPos = new Vector3[4];
 
        void InitIK(Vector3 directionToPoint, bool opposite)
        {
            Vector3 relativeDirection = transform.InverseTransformDirection(directionToPoint);
 
            if (Mathf.Abs(relativeDirection.y) > 0.5f)
            {
                float targetAnim = 0;
 
                if (targetState == ClimbStates.onPoint)
                {
                    ik_L = ik.ReturnOppositeIK(ik_L);
                }
                else
                {
                    if (Mathf.Abs(relativeDirection.x) > 0)
                    {
                        if (relativeDirection.x < 0)
                            ik_L = AvatarIKGoal.LeftHand;
                        else
                            ik_L = AvatarIKGoal.RightHand;
                    }
 
                    targetAnim = (ik_L == AvatarIKGoal.RightHand) ? 1 : 0;
                    if (relativeDirection.y < 0)
                        targetAnim = (ik_L == AvatarIKGoal.RightHand) ? 0 : 1;
 
                    anim.SetFloat("Movement", targetAnim);
                }
 
            }
            else
            {
                ik_L = (relativeDirection.x < 0) ? AvatarIKGoal.LeftHand : AvatarIKGoal.RightHand;
 
                if (opposite)
                {
                    ik_L = ik.ReturnOppositeIK(ik_L);
                }
 
            }
 
            _ikT = 0;
            UpdateIKTarget(0, ik_L, targetPoint);
 
            ik_F = ik.ReturnOppositeLimb(ik_L);
            _fikT = 0;
            UpdateIKTarget(1, ik_F, targetPoint);
        }
 
        void InitIK_Direct(Vector3 directionToPoint)
        {
            if (directionToPoint.y != 0)
            {
                _fikT = 0;
                _ikT = 0;
 
                UpdateIKTarget(0, AvatarIKGoal.LeftHand, targetPoint);
                UpdateIKTarget(1, AvatarIKGoal.LeftFoot, targetPoint);
 
                UpdateIKTarget(2, AvatarIKGoal.RightHand, targetPoint);
                UpdateIKTarget(3, AvatarIKGoal.RightFoot, targetPoint);
            }
            else
            {
                InitIK(directionToPoint, false);
                InitIKOpposite();
            }
        }
 
        void InitIKOpposite()
        {
            UpdateIKTarget(2, ik.ReturnOppositeIK(ik_L), targetPoint);
            UpdateIKTarget(3, ik.ReturnOppositeIK(ik_F), targetPoint);
        }
 
        void UpdateIKTarget(int posIndex, AvatarIKGoal _ikGoal, Point tp)
        {
            _ikStartPos[posIndex] = ik.ReturnCurrentPointPosition(_ikGoal);
            _ikTargetPos[posIndex] = tp.ReturnIK(_ikGoal).target.transform.position;
            ik.UpdatePoint(_ikGoal, tp);
        }
 
        void HandleWeightAll(float t, AnimationCurve aCurve)
        {
            float inf = aCurve.Evaluate(t);
            ik.AddWeightInfluenceAll(1 - inf);
 
            //close the ik for the feet if going from hanging to braced
            //if (curPoint.pointType == PointType.hanging && targetPoint.pointType == PointType.braced)
            //{
            //    float inf2 = a_zeroToOne.Evaluate(t);
            //
            //    ik.InfluenceWeight(AvatarIKGoal.LeftFoot, inf2);
            //    ik.InfluenceWeight(AvatarIKGoal.RightFoot, inf2);
            //}
 
            //if(curPoint.pointType == PointType.hanging && targetPoint.pointType == PointType.hanging)
            //{
            //    ik.InfluenceWeight(AvatarIKGoal.LeftFoot, 0);
            //    ik.InfluenceWeight(AvatarIKGoal.RightFoot, 0);
            //}
        }

        #endregion

        #region Animations

        TransitDir ReturnTransitDirection(Vector3 inputD, bool jump)
        {
            TransitDir retVal = default(TransitDir);

            float targetAngle = Mathf.Atan2(inputD.x, inputD.y) * Mathf.Rad2Deg;

            if (!jump)
            {
                if (Mathf.Abs(inputD.y) > 0)
                {
                    retVal = TransitDir.m_vert;
                }
                else
                {
                    retVal = TransitDir.m_hor;
                }
            }
            else
            {
                if (targetAngle < 22.5f && targetAngle > -22.5f)
                {
                    retVal = TransitDir.j_up;
                }
                else if (targetAngle < 180 + 22.5f && targetAngle > 180 - 22.5f)
                {
                    retVal = TransitDir.j_down;
                }
                else if (targetAngle < 90 + 22.5f && targetAngle > 90 - 22.5f)
                {
                    retVal = TransitDir.j_right;
                }
                else if (targetAngle < -90 + 22.5f && targetAngle > -90 - 22.5f)
                {
                    retVal = TransitDir.j_left;
                }

                if (Mathf.Abs(inputD.y) > Mathf.Abs(inputD.x))
                {
                    if (inputD.y < 0)
                        retVal = TransitDir.j_down;
                    else
                        retVal = TransitDir.j_up;
                }
            }
            return retVal;
        }

        void PlayAnim(TransitDir dir, bool jump = false)
        {
            //If target = 6, player moves vertical. If target = 5, player moves horizontal. If target = 0, player moves up.
            //If target = 1, player moves down, If target = 2, player moves right. If target = 3, player moves left.

            int target = 0;

            switch (dir)
            {
                case TransitDir.m_hor:
                    target = 5;
                    break;
                case TransitDir.m_vert:
                    target = 6;
                    break;
                case TransitDir.j_up:
                    target = 0;
                    break;
                case TransitDir.j_down:
                    target = 1;
                    break;
                case TransitDir.j_left:
                    target = 3;
                    break;
                case TransitDir.j_right:
                    target = 2;
                    break;
            }

            anim.SetInteger("JumpType", target);

            if (!jump)
                anim.SetBool("Move", true);
            else
                anim.SetBool("Jump", true);
        }

        enum TransitDir
        {
            m_hor,
            m_vert,
            j_up,
            j_down,
            j_left,
            j_right
        }
        #endregion
    }
}