#if gameCreator2
using GameCreator.Runtime.Characters;
using GameCreator.Runtime.Variables;
using System.Collections;
using System.Linq;
#if UNITY_EDITOR
#endif
using UnityEngine;

namespace FC_ParkourSystem
{
    public class GC2_IntegrationHelper : MonoBehaviour, IParkourCharacter
    {
        ParkourController parkourController;

        public Character character;

        Animator gc2Animator;
        public LocalNameVariables jumpVar;
        public Animator parkourAnimator;


        public bool UseRootMotion { get; set; } = false;

        public Vector3 MoveDir => character.Motion.MoveDirection;

        public bool IsGrounded => character.Driver.IsGrounded;

        public float Gravity => -20;

        public bool PreventParkourAction => character.Busy.IsBusy;

        private void Update()
        {
            if (!parkourController.ControlledByParkour)
            {
                parkourAnimator.SetBool("IsGrounded", (gc2Animator.GetFloat("Grounded") == 1f ? true : false));
            }
        }

        private void Awake()
        {
            parkourController = GetComponent<ParkourController>();

            if (character == null)
                character = this.transform.GetComponentInParent<Character>();
            if (gc2Animator == null)
                gc2Animator = character.Animim.Animator;

            parkourAnimator = character.Kernel.Animim.Mannequin.GetComponent<Animator>();

            if (parkourAnimator.gameObject.GetComponent<AnimatorRootmotionController>() == null)
            {
                var controller = parkourAnimator.gameObject.AddComponent<AnimatorRootmotionController>();
                controller.helper = this;
                controller.character = character;
            }

            if (character.gameObject.layer == LayerMask.NameToLayer("Default"))
                character.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            this.transform.localPosition = Vector3.zero;
        }
        IEnumerator LerpParameter(Animator animator, string parameter, float wait)
        {
            yield return new WaitForSeconds(wait);
            if (parkourController.ControlledByParkour)
            {
                animator.SetFloat(parameter, 0);

            }
        }


        public void OnStartParkourAction()
        {
            if (!character.enabled)
                return;

            this.transform.parent = null;
            character.transform.parent = this.transform;
            character.enabled = false;
            StartCoroutine(LerpParameter(gc2Animator, "Grounded", 0.05f));
            character.Motion.StandLevel.Current = 1;

            parkourAnimator.enabled = true;
            parkourAnimator.SetBool("IsGrounded", false);


            parkourAnimator.SetFloat("Movement", gc2Animator.GetFloat("Movement"));
            parkourAnimator.SetFloat("Speed-X", gc2Animator.GetFloat("Speed-X"));
            parkourAnimator.SetFloat("Speed-Y", gc2Animator.GetFloat("Speed-Y"));
            parkourAnimator.SetFloat("Speed-Z", gc2Animator.GetFloat("Speed-Z"));
            parkourAnimator.SetFloat("Pivot", gc2Animator.GetFloat("Pivot"));
            parkourAnimator.SetFloat("Stand", gc2Animator.GetFloat("Stand"));

            if (parkourAnimator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"))
            {
                var normalizedTime = gc2Animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1;
                parkourAnimator.Play("Locomotion", 0, normalizedTime);
                parkourAnimator.Update(0);
            }
            gc2Animator.enabled = false;
            character.States.ChangeWeight(5, 0, 0f);
        }

        IEnumerator OnEndParkour()
        {
            character.Animim.OnStartup(character);

            character.enabled = true;
            character.Motion.StopToDirection(1);
            character.transform.parent = null;
            transform.parent = gc2Animator.transform;
            transform.localPosition = Vector3.zero;

            while (!parkourAnimator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"))
            {
                parkourAnimator.SetFloat("Movement", gc2Animator.GetFloat("Movement"));
                parkourAnimator.SetFloat("Speed-X", gc2Animator.GetFloat("Speed-X"));
                parkourAnimator.SetFloat("Speed-Y", gc2Animator.GetFloat("Speed-Y"));
                parkourAnimator.SetFloat("Speed-Z", gc2Animator.GetFloat("Speed-Z"));
                parkourAnimator.SetFloat("Pivot", gc2Animator.GetFloat("Pivot"));
                parkourAnimator.SetFloat("Stand", gc2Animator.GetFloat("Stand"));
                yield return null;
            }

            gc2Animator.enabled = true;
            if (parkourAnimator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"))
            {
                var normalizedTime = parkourAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1;
                gc2Animator.Play("Locomotion", 0, normalizedTime);
                gc2Animator.Update(0);
            }
            parkourAnimator.enabled = false;
            character.States.ChangeWeight(5, 1, 0.25f);
        }

        public void OnEndParkourAction()
        {
            if (character.enabled)
                return;
            StartCoroutine(OnEndParkour());

        }



        public IEnumerator HandleVerticalJump()
        {
            if (jumpVar != null)
                jumpVar.Set("Jump", !(bool)jumpVar.Get("Jump"));
            yield break;
        }



        public Animator Animator
        {
            get
            {
                return parkourAnimator == null ? FindObjectsOfType<Character>().Where(c => c.IsPlayer).FirstOrDefault().GetComponentInChildren<Animator>() : parkourAnimator;
            }
            set
            {
                parkourAnimator = value;
            }
        }
    }
}

#endif
