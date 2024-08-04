using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace FC_ParkourSystem
{
    public class PlayerController : MonoBehaviour, IParkourCharacter
    {
        [field: Tooltip("Automatically stopping movement on ledges")]
        [SerializeField] bool preventFallingFromLedge = true;
        [field: Tooltip("Enables balance walking on narrow beams")]
        public bool enableBalanceWalk = true;
        [field: Space(10)]

        [SerializeField] float sprintSpeed = 6.5f;
        [SerializeField] float runSpeed = 4.5f;
        [SerializeField] float walkSpeed = 2f;
        [Tooltip("Defines how long it takes for the character to reach the peak of the jump")]
        public float timeToJump = 0.4f;

        float moveSpeed = 0;
        [SerializeField] float rotationSpeed = 2.5f;

        [Header("Ground Check Settings")]
        [Tooltip("Radius of ground detection sphere")]
        [SerializeField] float groundCheckRadius = 0.2f;

        [Tooltip("Offet between the player's root position and the ground detection sphere")]
        [SerializeField] Vector3 groundCheckOffset = new Vector3(0f, 0.15f, 0.07f);

        [Tooltip("All layers that should be considered as ground")]
        public LayerMask groundLayer = 1;






        float controllerDefaultHeight = .87f;
        float controllerDefaultYOffset = 1.7f;

        bool isGrounded;

        Vector3 desiredMoveDir;
        Vector3 moveInput;
        Vector3 moveDir;
        float moveAmount;
        Vector3 velocity;

        float ySpeed;
        Quaternion targetRotation;

        CameraController cameraController;
        Animator animator;
        CharacterController characterController;
        ParkourController parkourController;
        InputManager inputManager;
        FootIK footIk;
        EnvironmentScanner environmentScanner;

        float rotationValue = 0;
        bool isRunning = true;
        float crouchVal = 0;
        float footOffset = .1f;
        float footRayHeight = .8f;

        bool turnBack;
        bool inLocomotionAction;
        bool useRootMotion;
        bool useRootmotionMovement;

        Vector3 prevAngle;
        bool prevValue;

        float headIK;

        float addedMomentum = 0f;

        //Vertical Jump
        float jumpMoveSpeed = 4f;
        float jumpHeightDiff;
        float minJumpHeightForHardland = 3f;
        float jumpMaxPosY;

        public Vector3 GroundCheckOffset
        {
            get { return groundCheckOffset; }
            set { groundCheckOffset = value; }
        }

        public float MoveAmount => animator.GetFloat("moveAmount");


        private void Awake()
        {
            cameraController = Camera.main.GetComponent<CameraController>();
            animator = GetComponent<Animator>();
            characterController = GetComponent<CharacterController>();
            controllerDefaultHeight = characterController.height;
            controllerDefaultYOffset = characterController.center.y;
            parkourController = GetComponent<ParkourController>();
            inputManager = GetComponent<InputManager>();
            footIk = GetComponent<FootIK>();
            environmentScanner = GetComponent<EnvironmentScanner>();
            if (!(groundLayer == (groundLayer | (1 << LayerMask.NameToLayer("Ledge")))))
                groundLayer += 1 << LayerMask.NameToLayer("Ledge");
        }


        private void OnAnimatorIK(int layerIndex)
        {
            var hipPos = animator.GetBoneTransform(HumanBodyBones.Hips).transform;
            var headPos = animator.GetBoneTransform(HumanBodyBones.Head).transform.position;


            var offset = Vector3.Distance(hipPos.position, headPos);
            animator.SetLookAtPosition(cameraController.transform.position + cameraController.transform.forward * (cameraController.Distance + 1) + new Vector3(0, offset, 0));

            if ((!parkourController.ControlledByParkour && !parkourController.IsHanging) && IsGrounded)
            {
                headIK = Mathf.Clamp01(headIK + 0.1f * Time.deltaTime) * 0.3f;
                animator.SetLookAtWeight(headIK);
            }
            else if (!parkourController.IsHanging)
            {
                headIK = Mathf.Clamp01(headIK - 0.2f * Time.deltaTime) * 0.3f;
                animator.SetLookAtWeight(headIK);
            }
        }

        IEnumerator HandleLanding()
        {
            //parkourController.InAction = true;
            parkourController.ResetPositionY();
            yield return parkourController.DoAction("LandAndStepForward");

            //animator.SetBool("isFalling", false);
        }
        private void FixedUpdate()
        {
            if (parkourController.ControlledByParkour || parkourController.IsHanging || UseRootMotion || inLocomotionAction)
                return;
            if (enableBalanceWalk)
                HandBalanceOnNarrowBeam();
        }

        private void Update()
        {

            GetInput();

            if (parkourController.ControlledByParkour || parkourController.IsHanging || UseRootMotion || inLocomotionAction)
            {
                ySpeed = Gravity / 4;
                return;
            }

            var tempGrounded = isGrounded;
            GroundCheck();

            if (tempGrounded == false && isGrounded == true)
            {
                if (ySpeed < Gravity)
                {
                    parkourController.CurrentCameraShakeAmount = Mathf.Clamp(Mathf.Abs(ySpeed) * 0.0007f, 0.0f, 0.01f);
                    parkourController.CameraShakeDuration = 1f;
                    animator.SetFloat("fallAmount", Mathf.Clamp(Mathf.Abs(ySpeed) * 0.06f, 0.6f, 1f));
                    StartCoroutine(DoLocomotionAction("Landing", true));
                }
                else
                    animator.SetFloat("fallAmount", 0);
                //if(animator.GetCurrentAnimatorStateInfo(0).IsName("DropFallIdle"))
                //{
                //    StartCoroutine(DoLocomotionAction("LandAndStepForward"));
                //}
            }
           
            velocity = Vector3.zero;



            if (isGrounded)
            {

                ySpeed = Gravity / 2;
                footIk.IkEnabled = true;

                isRunning = inputManager.ToggleRun ? !isRunning : isRunning;

                float normalizedSpeed = isRunning ? 1 : .2f;
                normalizedSpeed = inputManager.SprintKey ? 1.5f : normalizedSpeed;

                moveSpeed = normalizedSpeed == 1 ? runSpeed : walkSpeed;
                moveSpeed = normalizedSpeed == 1.5f ? sprintSpeed : moveSpeed;

                if (crouchVal == 1)
                    moveSpeed *= .6f;

                animator.SetFloat("idleType", crouchVal, 0.5f, Time.deltaTime);

                velocity = desiredMoveDir * moveSpeed;

                HandleTurning();

                // LedgeMovement will stop the player from moving if there is ledge in front.
                // Pass your moveDir and velocity to the LedgeMovment function and it will return the new moveDir and Velocity while also taking ledges to the account
                if (preventFallingFromLedge)
                    (moveDir, velocity) = parkourController.LedgeMovement(desiredMoveDir, velocity);

                float animSmoothTime = 0.15f;
                if (velocity == Vector3.zero)
                    animSmoothTime = 0.15f;   // If player stopped moving, then deccelerate 
                else
                {
                    float acceleration = 0.6f;
                    if (addedMomentum > 0)
                    {
                        acceleration += addedMomentum;
                        addedMomentum = 0f;
                    }
                    var mag = Mathf.MoveTowards(characterController.velocity.magnitude, velocity.magnitude, acceleration * 400f * Time.deltaTime);
                    velocity = Vector3.ClampMagnitude(velocity, mag);

                    animSmoothTime = acceleration;
                }

                animator.SetFloat("moveAmount", Mathf.Min(normalizedSpeed, characterController.velocity.magnitude), animSmoothTime, Time.deltaTime);


                // If we're playing running animation but the velocity is close to zero, then play run to stop action
                if (MoveAmount > 0.6f && characterController.velocity.magnitude < sprintSpeed * 0.2f && crouchVal < 0.5f)
                {
                    StartCoroutine(DoLocomotionAction("Run To Stop", onComplete: () =>
                    {
                        animator.SetFloat("moveAmount", 0);
                    }));
                }
            }
            else
            {
                footIk.IkEnabled = false;
                ySpeed = Mathf.Clamp(ySpeed + Gravity * Time.deltaTime, -30, Mathf.Abs(Gravity) * timeToJump);
            }


            velocity.y = ySpeed;

            if (velocity != Vector3.zero)
                characterController.Move(velocity * Time.deltaTime);


            if (moveAmount > 0 && moveDir.magnitude > 0.2)
                targetRotation = Quaternion.LookRotation(moveDir);

            float turnSpeed = Mathf.Lerp(rotationSpeed * 100f, 2 * rotationSpeed * 100f, moveSpeed / sprintSpeed);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }


        void HandBalanceOnNarrowBeam()
        {
            bool leftFootHit, rightFootHit;
            int hitCount = 0;

            Vector3 right = transform.right * 0.3f, forward = transform.forward * 0.3f , up = Vector3.up * 0.2f;

            //hitCount += Physics.CheckCapsule(transform.position - right, transform.position - right - up, 0.1f, groundLayer) || Physics.Linecast(transform.position + up, transform.position - right)? 1:0;
            //hitCount += Physics.CheckCapsule(transform.position + right, transform.position + right - up, 0.1f, groundLayer) || Physics.Linecast(transform.position + up, transform.position + right) ? 1 : 0;
            //hitCount += (rightFootHit = Physics.CheckCapsule(transform.position + forward, transform.position + forward - up, 0.1f, groundLayer)) || Physics.Linecast(transform.position + up, transform.position + forward) ? 1 : 0;
            //hitCount += (leftFootHit = Physics.CheckCapsule(transform.position - forward, transform.position -forward - up, 0.1f, groundLayer)) || Physics.Linecast(transform.position + up, transform.position - forward) ? 1 : 0;

            hitCount += Physics.CheckCapsule(transform.position - right + up, transform.position - right - up, 0.1f, groundLayer) ? 1 : 0;
            hitCount += Physics.CheckCapsule(transform.position + right + up, transform.position + right - up, 0.1f, groundLayer) ? 1 : 0;
            hitCount += (rightFootHit = Physics.CheckCapsule(transform.position + forward + up, transform.position + forward - up, 0.1f, groundLayer)) ? 1 : 0;
            hitCount += (leftFootHit = Physics.CheckCapsule(transform.position - forward + up, transform.position - forward - up, 0.1f, groundLayer))? 1 : 0;


            if ((rightFootHit || leftFootHit) && !Physics.Linecast(transform.position + up, transform.position - up, groundLayer)) // for predictive jump cases
                hitCount -= 1;
            crouchVal = hitCount > 2 ? 0f : 1f;
            animator.SetFloat("idleType", crouchVal, 0.2f, Time.deltaTime);
            if (animator.GetFloat("idleType") > .2f)
            {
                var hasSpace = leftFootHit && rightFootHit;
                animator.SetFloat("crouchType", hasSpace ? 0 : 1, 0.2f, Time.deltaTime);
            }
            characterController.center = new Vector3(characterController.center.x, crouchVal == 1 ? controllerDefaultYOffset * .7f : controllerDefaultYOffset, characterController.center.z);
            characterController.height = crouchVal == 1 ? controllerDefaultHeight * .7f : controllerDefaultHeight;
        }
        void HandBalanceOnNarrowBeamWithTag()
        {
            var hitObjects = Physics.OverlapSphere(transform.TransformPoint(new Vector3(0f, 0.15f, 0.07f)), .2f).ToList().Where(g => g.gameObject.tag == "NarrowBeam" || g.gameObject.tag == "SwingableLedge").ToArray();
            crouchVal = hitObjects.Length > 0 ? 1f : 0;
            animator.SetFloat("idleType", crouchVal, 0.2f, Time.deltaTime);

            if (animator.GetFloat("idleType") > .2f)
            {
                var leftFootHit = Physics.SphereCast(transform.position - transform.forward * 0.3f + Vector3.up * footRayHeight / 2, 0.1f, Vector3.down, out RaycastHit leftHit, footRayHeight + footOffset, groundLayer);
                var rightFootHit = Physics.SphereCast(transform.position + transform.forward * 0.3f + Vector3.up * footRayHeight / 2, 0.1f, Vector3.down, out RaycastHit rightHit, footRayHeight + footOffset, groundLayer);
                var hasSpace = leftFootHit && rightFootHit;
                animator.SetFloat("crouchType", hasSpace ? 0 : 1, 0.2f, Time.deltaTime);
            }
            characterController.center = new Vector3(characterController.center.x, crouchVal == 1 ? controllerDefaultYOffset * .7f : controllerDefaultYOffset, characterController.center.z);
            characterController.height = crouchVal == 1 ? controllerDefaultHeight * .7f : controllerDefaultHeight;
        }

        void HandleTurning()
        {

            var rotDiff = transform.eulerAngles - prevAngle;
            var threshold = moveSpeed >= runSpeed ? 0.025 : 0.1;
            if (rotDiff.sqrMagnitude < threshold)
            {
                rotationValue = 0;
            }
            else
            {
                rotationValue = Mathf.Sign(rotDiff.y) * .5f;

                var angle = Vector3.Angle(transform.forward, MoveDir);
                if (angle > 100)
                {
                    // If the rotation angle is high (like turning back), then reduce velocity 
                    velocity = velocity / 4;
                }
                else
                {
                    // If not in crouch, then reduce the velocity during rotation
                    if (crouchVal == 0)
                        velocity *= 0.75f;
                }
            }
            animator.SetFloat("rotation", rotationValue, 0.35f, Time.deltaTime);

            prevAngle = transform.eulerAngles;
        }


        void GetInput()
        {
            float h = inputManager.DirectionInput.x;
            float v = inputManager.DirectionInput.y;

            moveAmount = Mathf.Clamp01(Mathf.Abs(h) + Mathf.Abs(v));
            moveInput = (new Vector3(h, 0, v)).normalized;
            desiredMoveDir = cameraController.PlanarRotation * moveInput;
            //desiredMoveDir = Vector3.MoveTowards(prevDir, cameraController.PlanarRotation * moveInput,Time.deltaTime * rotationSpeed * 2);
            moveDir = desiredMoveDir;

        }

        bool Turnback()
        {
            GetInput();
            var angle = Vector3.SignedAngle(transform.forward, MoveDir, Vector3.up);

            if (Mathf.Abs(angle) > 120 && MoveAmount > .8f && crouchVal < 0.5f && Physics.Raycast(transform.position + Vector3.up * 0.1f + transform.forward, Vector3.down, 0.3f) && !Physics.Raycast(transform.position + Vector3.up * 0.1f, transform.forward, 0.6f))
            {
                turnBack = true;
                animator.SetBool("turnback Mirror", angle <= 0);

                StartCoroutine(DoLocomotionAction("Running Turn 180", onComplete: () =>
                {
                    animator.SetFloat("moveAmount", 0.3f);
                    addedMomentum = 0.3f;
                    targetRotation = transform.rotation;
                    turnBack = false;
                }, crossFadeTime: 0.05f));
                return true;
            }
            return false;
        }


        void GroundCheck()
        {
            isGrounded = Physics.CheckSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius, groundLayer);
            animator.SetBool("IsGrounded", isGrounded);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius);
        }

        public IEnumerator DoLocomotionAction(string anim,bool useRootmotionMovement = false, Action onComplete = null, float crossFadeTime = .2f)
        {
            inLocomotionAction = true;
            this.useRootmotionMovement = useRootmotionMovement;
            EnableRootMotion();
            animator.CrossFade(anim, crossFadeTime);

            yield return null;
            var animState = animator.GetNextAnimatorStateInfo(0);

            float timer = 0f;
            while (timer <= animState.length)
            {
                if (!turnBack && Turnback()) yield break;

                timer += Time.deltaTime;
                yield return null;
            }

            DisableRootMotion();
            this.useRootmotionMovement = false;
            onComplete?.Invoke();
            inLocomotionAction = false;
        }

        public void EnableRootMotion()
        {
            prevValue = useRootMotion;
            useRootMotion = true;
        }

        public void DisableRootMotion()
        {
            prevValue = useRootMotion;
            useRootMotion = false;
        }

        private void OnAnimatorMove()
        {
            if (useRootMotion)
            {
                transform.rotation *= animator.deltaRotation;
                if(useRootmotionMovement)
                    transform.position += animator.deltaPosition;
            }
        }

        #region Interface

        public void OnStartParkourAction()
        {
            targetRotation = transform.rotation;
            isGrounded = false;
            animator.SetBool("IsGrounded", isGrounded);
            StartCoroutine(parkourController.TweenVal(animator.GetFloat("moveAmount"), 0, 0.15f, (lerpVal) => { animator.SetFloat("moveAmount", lerpVal); })); ;    
        }

        public void OnEndParkourAction()
        {
            targetRotation = transform.rotation;
        }

        public IEnumerator HandleVerticalJump()
        {
            jumpMaxPosY = transform.position.y - 1;
            var velocity = Vector3.zero;
            //Calculates the initial vertical velocity required for jumping
            var velocityY = Mathf.Abs(Gravity) * timeToJump;
            parkourController.IsJumping = true;
            animator.SetFloat("moveAmount", 0);
            isGrounded = false;
            animator.CrossFadeInFixedTime("Vertical Jump", .2f);

            while (!isGrounded)
            {

                velocityY += Gravity * Time.deltaTime;
                velocity = new Vector3((moveDir * jumpMoveSpeed).x, velocityY, (moveDir * jumpMoveSpeed).z);

                characterController.Move(velocity * Time.deltaTime);
                if (velocityY < 0)
                    GroundCheck();

                // To get max jump height
                if (jumpMaxPosY < transform.position.y)
                    jumpMaxPosY = transform.position.y;

                if (moveDir != Vector3.zero)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 100 * rotationSpeed);
                yield return null;
            }

            StartCoroutine(VerticalJumpLanding());
            parkourController.IsJumping = false;
        }

        IEnumerator VerticalJumpLanding()
        {
            jumpHeightDiff = Mathf.Abs(jumpMaxPosY - transform.position.y);
            if (jumpHeightDiff > minJumpHeightForHardland)
            {
                characterController.Move(Vector3.down);
                var halfExtends = new Vector3(.3f, .9f, 0.01f);
                var hasSpaceForRoll = Physics.BoxCast(transform.position + Vector3.up, halfExtends, transform.forward, Quaternion.LookRotation(transform.forward), 2.5f, environmentScanner.ObstacleLayer);

                halfExtends = new Vector3(.1f, .9f, 0.01f);
                var heightHiting = true;
                for (int i = 0; i < 6 && heightHiting; i++)
                    heightHiting = Physics.BoxCast(transform.position + Vector3.up * 1.8f + transform.forward * (i * .5f + .5f), halfExtends, Vector3.down, Quaternion.LookRotation(Vector3.down), 2.2f + i * .1f, environmentScanner.ObstacleLayer);

                parkourController.EnableRootMotion();
                if (!hasSpaceForRoll && heightHiting)
                    yield return parkourController.DoAction("FallingToRoll", crossFadeTime: .1f);
                else
                    yield return parkourController.DoAction("LandFromFall", crossFadeTime: .1f);
                parkourController.ResetRootMotion();
            }
            else
                animator.CrossFadeInFixedTime("LandAndStepForward", .1f);
        }

        public bool UseRootMotion { get; set; } = false;
        public Vector3 MoveDir { get { return desiredMoveDir; } set { desiredMoveDir = value; } }
        public bool IsGrounded => isGrounded;
        public float Gravity => -20;
        public bool PreventParkourAction => false;

        public Animator Animator
        {
            get { return animator == null ? GetComponent<Animator>() : animator; }
            set
            {
                animator = value;
            }
        }
        #endregion
    }
}
