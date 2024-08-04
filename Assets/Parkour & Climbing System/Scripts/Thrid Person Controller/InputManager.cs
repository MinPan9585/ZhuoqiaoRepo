using System;
using UnityEngine;
#if inputsystem
using UnityEngine.InputSystem;
#endif

namespace FC_ParkourSystem
{
    public class InputManager : MonoBehaviour
    {
#if inputsystem
        InputActionManager input;

        private void OnEnable()
        {
            input = new InputActionManager();
            input.Enable();
        }
        private void OnDisable()
        {
            input.Disable();
        }
#endif
        [Header("Keys")]
        [SerializeField] KeyCode jumpKey = KeyCode.Space;
        [SerializeField] KeyCode dropKey = KeyCode.E;
        [SerializeField] KeyCode jumpFromHangKey = KeyCode.Q;
        [SerializeField] KeyCode moveType = KeyCode.Tab;
        [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;


        [Header("Buttons")]
        [SerializeField] string jumpButton;
        [SerializeField] string dropButton;
        [SerializeField] string jumpFromHangButton;
        [SerializeField] string moveTypeButton;
        [SerializeField] string sprintButton;

        public bool Jump { get; set; }
        public bool JumpKeyDown { get; set; }
        public bool Drop { get; set; }
        public bool JumpFromHang { get; set; }
        public Vector2 DirectionInput { get; set; }
        public Vector2 CameraInput { get; set; }
        public bool ToggleRun { get; set; }
        public bool SprintKey { get; set; }

        private void Update()
        {
#if inputsystem
            DirectionInput = input.Climbing.MoveInput.ReadValue<Vector2>();
#else
            //Horizontal and Vertical Movement
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            DirectionInput = new Vector2(h, v);
#endif

#if inputsystem
            CameraInput = input.Climbing.CameraInput.ReadValue<Vector2>();
#else
            //Camera Movement
            float x = Input.GetAxis("Mouse X");
            float y = Input.GetAxis("Mouse Y");
            CameraInput = new Vector2(x, y);
#endif

            //Jump
#if inputsystem
            Jump = input.Climbing.Jump.inProgress;
#else
            Jump = Input.GetKey(jumpKey) || (String.IsNullOrEmpty(jumpButton) ? false : Input.GetButton(jumpButton));
#endif


            //JumpKeyDown
#if inputsystem
            JumpKeyDown = input.Climbing.Jump.WasPressedThisFrame();
#else
            JumpKeyDown = Input.GetKeyDown(jumpKey) || (String.IsNullOrEmpty(jumpButton) ? false : Input.GetButtonDown(jumpButton));
#endif

            //Drop
#if inputsystem
            Drop =  input.Climbing.Drop.inProgress;
#else
            Drop = Input.GetKey(dropKey) || (String.IsNullOrEmpty(dropButton) ? false : Input.GetButton(dropButton));
#endif

            //DropBack
#if inputsystem
            JumpFromHang = input.Climbing.JumpFromHang.inProgress;
#else
            JumpFromHang = Input.GetKey(jumpFromHangKey) || (String.IsNullOrEmpty(jumpFromHangButton) ? false : Input.GetButton(jumpFromHangButton));
#endif

            //Walk or Run 
#if inputsystem
            ToggleRun = input.Climbing.SprintMode.WasPressedThisFrame();
#else
            ToggleRun = Input.GetKeyDown(moveType) || (String.IsNullOrEmpty(moveTypeButton) ? false : Input.GetButtonDown(moveTypeButton));
#endif
            //Sprint Key
#if inputsystem
            SprintKey =  input.Climbing.SprintKey.inProgress;
#else
            SprintKey = Input.GetKey(sprintKey) || (String.IsNullOrEmpty(sprintButton) ? false : Input.GetButton(sprintButton));
#endif
        }
    }
}
