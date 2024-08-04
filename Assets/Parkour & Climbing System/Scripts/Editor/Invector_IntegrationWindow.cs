using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif
using UnityEngine;

namespace FC_ParkourSystem
{
#if UNITY_EDITOR
    public class Invector_IntegrationWindow : EditorWindow
    {
        public static Invector_IntegrationWindow window;

        public AnimatorController controller;
        public GameObject playerTemplate;
        bool integrationButtonClicked;

        [MenuItem("Tools/Parkour && Climbing System/Integration/Invector/Documentation", false, 600)]
        public static void GoToIntegrationDocumentation()
        {
            Application.OpenURL("https://fantacode.gitbook.io/parkour-and-climbing-system/invector-integration");
        }

#if invector
        [MenuItem("Tools/Parkour && Climbing System/Integration/Invector/Helper", false, 600)]
        public static void InItWindow()
        {

            window = GetWindow<Invector_IntegrationWindow>();
            window.titleContent = new GUIContent("Integration");
            SetWindowHeight(82);
            
        }
        private void OnGUI()
        {
            GetWindow();
            GUILayout.Space(15);
            if (controller != null && playerTemplate != null)
                SetWindowHeight(82);
            else if (integrationButtonClicked)
            {
                if (controller == null && playerTemplate == null)
                    EditorGUILayout.HelpBox("Fields are empty", MessageType.Error);
                else if (controller == null)
                    EditorGUILayout.HelpBox("Animator Controller is not assigned", MessageType.Error);
                else if (playerTemplate == null)
                    EditorGUILayout.HelpBox("Player Template is not assigned", MessageType.Error);
                SetWindowHeight(122);
            }
            playerTemplate = (GameObject)UndoField(playerTemplate, EditorGUILayout.ObjectField("Player Template", playerTemplate, typeof(GameObject), true));
            GUILayout.Space(1.5f);
            controller = (AnimatorController)UndoField(controller, EditorGUILayout.ObjectField("Animator Controller", controller, typeof(AnimatorController), true));
            GUILayout.Space(1.5f);
            if (GUILayout.Button("Integrate"))
            {
                if (controller != null && playerTemplate != null)
                    InvectorIntegration();
                else
                    integrationButtonClicked = true;
            }
        }
        void InvectorIntegration()
        {
            GenerateAnimationParameters();
            GenerateTransitions();
            AttachScripts();
        }
        void AttachScripts()
        {
            //Checks if the parkour controller already exists
            var p = GameObject.Find("Parkour Controller");
            if (p != null && p.GetComponent<ParkourController>() != null && p.GetComponent<InvectorIntegrationHelper>())
                return;

            if (playerTemplate.GetComponent<ParkourController>() == null)
            {
                var pc = playerTemplate.AddComponent<ParkourController>();
                var actions = Resources.LoadAll("Parkour Actions", typeof(ParkourAction)).ToList();
                foreach (var a in actions)
                    pc.parkourActions.Add(a as ParkourAction);
            }


            if (playerTemplate.GetComponent<ClimbController>() == null)
                playerTemplate.AddComponent<ClimbController>();
            if (playerTemplate.GetComponent<EnvironmentScanner>() == null)
                playerTemplate.AddComponent<EnvironmentScanner>();
            if (playerTemplate.GetComponent<InputManager>() == null)
                playerTemplate.AddComponent<InputManager>();
            if (playerTemplate.GetComponent<InvectorIntegrationHelper>() == null)
                playerTemplate.AddComponent<InvectorIntegrationHelper>();
        }
        void GenerateTransitions()
        {
            if (controller == null)
            {
                Debug.LogError("Animatior Controller not assigned");
                return;
            }
            var rootStateMachine = controller.layers[0].stateMachine;

            List<AnimatorState> states = new List<AnimatorState>();


            var parkourSMNames = new List<string>() { "Climb Actions", "Jump Actions", "Parkour Actions", "Drop Actions" };

            var parkourStateMachines = rootStateMachine.stateMachines.
                Where(s => parkourSMNames.Contains(s.stateMachine.name)).
                ToDictionary(s => s.stateMachine.name, s => s.stateMachine);

            var locomotionSM = rootStateMachine.stateMachines.FirstOrDefault(s => s.stateMachine.name == "Locomotion").stateMachine;

            var statesToCreateTransition = new[]
            {
                new
                {
                    StateMachine = "Parkour Actions",
                    //States = new [] {"VaultOver", "VaultOn", "MediumStepUp", "Climb Up", "StepUp", "MediumStepUpM" },
                    States = new Dictionary<string,float>{{ "VaultOver", 0}, { "VaultOn", 0 }, { "MediumStepUp", 0 }, { "Climb Up", 1 }, { "StepUp", 0 }, { "MediumStepUpM", 0 } }

                },
                new
                {
                    StateMachine = "Jump Actions",
                    //States = new[] {"LandFromFall", "LandAndStepForward", "LandOnSpot", "FallingToRoll"},
                    States = new Dictionary<string,float> {{ "LandFromFall", 1f}, { "LandAndStepForward", 0 }, { "LandOnSpot", 0 }, { "FallingToRoll", 0 } }
                },
                new
                {
                    StateMachine = "Climb Actions",
                    //States = new[] {"FreeHangClimb", "BracedHangClimb" },
                    States = new Dictionary<string,float> {{ "FreeHangClimb", 1}, { "BracedHangClimb", 1 } }
                },
                 new
                {
                    StateMachine = "Drop Actions",
                    //States = new[] { "Drop To Land" },
                    States = new Dictionary<string,float> { { "Jump Down", 0 },{ "JumpFromHang", 0 }, { "JumpFromFreeHang", 0 }, { "Bracedhang Try Jump Up", 0 }, { "Wall Run", 0 } }
                }
            };
            foreach (var stateTransition in statesToCreateTransition)
            {
                var sm = parkourStateMachines[stateTransition.StateMachine];
                foreach (var state in sm.states.Where(s => stateTransition.States.ContainsKey(s.state.name)).Select(s => s.state))
                {
                    foreach (var transition in state.transitions)
                        state.RemoveTransition(transition);
                    var t = state.AddTransition(locomotionSM, true);
                    var exitTime = stateTransition.States.GetValueOrDefault(state.name);
                    if (exitTime > 0)
                        t.exitTime = exitTime;
                }
            }
            //var BalancingSM = rootStateMachine.stateMachines.FirstOrDefault(s => s.stateMachine.name == "Balancing").stateMachine;
            //var balancingBT = BalancingSM.states.ToList().Where(s => s.state.name == "Invector_Balancing").First().state;
            //foreach (var transition in balancingBT.transitions)
            //    balancingBT.RemoveTransition(transition);
            //var toLocomotionTransition = balancingBT.AddTransition(locomotionSM, true);
            //toLocomotionTransition.AddCondition(AnimatorConditionMode.Less, .5f, "idleType");
            //toLocomotionTransition.hasExitTime = false;

            //var freeMovement = locomotionSM.stateMachines.ToList().Where(s => s.stateMachine.name == "Free Locomotion").ToList().First().stateMachine.states.Where(s => s.state.name == "Free Movement").First().state;
            //foreach (var transition in freeMovement.transitions)
            //{
            //    if (transition.destinationState == balancingBT)
            //        freeMovement.RemoveTransition(transition);
            //}
            //var toBalanceing = freeMovement.AddTransition(balancingBT, true);
            //toBalanceing.AddCondition(AnimatorConditionMode.Greater, .5f, "idleType");
            //toBalanceing.hasExitTime = false;
            //BalancingSM.defaultState = balancingBT;
        }
        void GenerateAnimationParameters()
        {
            if (controller == null)
                Debug.LogError("Animation Controller not assigned");

            var currParams = controller.parameters.ToDictionary(p => p.name);

            var paramsToAdd = new Dictionary<string, AnimatorControllerParameterType>()
            {
                { "moveAmount", AnimatorControllerParameterType.Float },
                { "IsGrounded", AnimatorControllerParameterType.Bool },
                { "mirrorAction", AnimatorControllerParameterType.Bool },
                { "freeHang", AnimatorControllerParameterType.Float },
                { "x", AnimatorControllerParameterType.Float },
                { "y", AnimatorControllerParameterType.Float },
                { "isFalling", AnimatorControllerParameterType.Bool },
                { "landingType", AnimatorControllerParameterType.Int },
                { "jumpInputPressed", AnimatorControllerParameterType.Bool },
                { "mirrorJump", AnimatorControllerParameterType.Bool },
                { "rotation", AnimatorControllerParameterType.Float },
                { "idleType", AnimatorControllerParameterType.Float },
                { "leftFootIK", AnimatorControllerParameterType.Float },
                { "rightFootIK", AnimatorControllerParameterType.Float },
                { "crouchType", AnimatorControllerParameterType.Float },
                { "jumpBackDirection", AnimatorControllerParameterType.Float },
                { "BackJumpMode", AnimatorControllerParameterType.Bool },
                { "BackJumpDir", AnimatorControllerParameterType.Float },
                { "fallAmount", AnimatorControllerParameterType.Float }
            };

            foreach (var p in paramsToAdd)
            {
                if (!currParams.ContainsKey(p.Key))
                    controller.AddParameter(p.Key, p.Value);
            }

            ChangeBlendTreeParameter("LandAndStepForward", "Jump Actions", "InputMagnitude");
            //ChangeBlendTreeParameter("DropToLand", "Drop Actions", "InputMagnitude");
        }

        void ChangeBlendTreeParameter(string blendTreeName, string stateMachineName, string newParameterName)
        {
            var sm = controller.layers[0].stateMachine;
            var bt = sm.stateMachines.ToList().Where(s => s.stateMachine.name == stateMachineName).ToList().First().stateMachine.states.Where(s => s.state.name == blendTreeName).First().state.motion as BlendTree;
            bt.blendParameter = newParameterName;
        }


        void GetWindow()
        {
            if (window == null)
            {
                window = GetWindow<Invector_IntegrationWindow>();
                window.titleContent = new GUIContent("Integration");
                SetWindowHeight(82);
            }
        }
        static void SetWindowHeight(float height)
        {
            window.minSize = new Vector2(400, height);
            window.maxSize = new Vector2(400, height);
        }
        object UndoField(object oldValue, object newValue)
        {
            if (newValue != null && oldValue != null && newValue.ToString() != oldValue.ToString())
            {
                Undo.RegisterCompleteObjectUndo(this, "Update Field");
            }
            return newValue;
        }
#endif
    }
#endif
}