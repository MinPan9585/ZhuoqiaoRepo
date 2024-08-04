using System.Collections;
using UnityEngine;

namespace FC_ParkourSystem
{
    public class CameraController : MonoBehaviour
    {
        public Transform followTarget;

        public bool advancedCameraRotation = true;
        Vector3 followTargetPosition;

        [Range(0,1)]
        [SerializeField] float sensitivity = .6f;
        [SerializeField] float distance = 2.5f;
        [SerializeField] float dragEffectDistance = .75f;

        float newDistance, currDistance;

        [SerializeField] float minVerticalAngle = -45;
        [SerializeField] float maxVerticalAngle = 70;

        [SerializeField] Vector3 framingOffset = new Vector3(0, 1, 0);

        [SerializeField] bool invertX;
        [SerializeField] bool invertY = true;


        float rotationX;
        float rotationY;
        float yRot;

        float invertXVal;
        float invertYVal;

        InputManager input;
        ParkourController parkourController;

        public float Distance => distance;
        private void Awake()
        {
            input = FindObjectOfType<InputManager>();
            parkourController = input.GetComponent<ParkourController>();
        }

        private void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            followTargetPosition = followTarget.position - Vector3.forward * 4;
        }

        private void Update()
        {
            invertXVal = (invertX) ? -1 : 1;
            invertYVal = (invertY) ? -1 : 1;
            
            rotationX += input.CameraInput.y * invertYVal * sensitivity;
            rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);

            if (input.CameraInput != Vector2.zero)
                yRot = rotationY += input.CameraInput.x * invertXVal * sensitivity;

            else if (advancedCameraRotation && !parkourController.IsHanging && input.CameraInput.x == 0 && input.DirectionInput.y > -.4f)
            {
                StartCoroutine(CameraRotDelay());
                rotationY = Mathf.Lerp(rotationY, yRot, Time.deltaTime * 25);
            }

            var targetRotation = Quaternion.Euler(rotationX, rotationY, 0);

            followTargetPosition = Vector3.Lerp(followTargetPosition, followTarget.position, 10 * (dragEffectDistance / (Mathf.Abs((followTargetPosition - followTarget.position).magnitude - distance)+0.01f)) * Time.deltaTime);
            if (parkourController.CameraShakeDuration > 0)
            {
                followTargetPosition += Random.insideUnitSphere * parkourController.CurrentCameraShakeAmount * parkourController.cameraShakeAmount * Mathf.Clamp01(parkourController.CameraShakeDuration);
                parkourController.CameraShakeDuration -= Time.deltaTime;
            }

            var forward = transform.forward;
            forward.y = 0;
            var focusPostion = followTargetPosition + new Vector3(framingOffset.x, framingOffset.y) + forward * framingOffset.z;

            RaycastHit hit;

            if (Physics.Raycast(focusPostion, (transform.position - focusPostion), out hit, distance))
                newDistance = Mathf.Clamp(hit.distance, 0.6f, distance);
            else
                newDistance = distance;

            currDistance = Mathf.Lerp(currDistance, newDistance, 10f * Time.deltaTime);

            transform.position = focusPostion - targetRotation * new Vector3(0, 0, currDistance);
            transform.rotation = targetRotation;

            previousPos = followTarget.transform.position;

        }
        public Quaternion PlanarRotation => Quaternion.Euler(0, rotationY, 0);

        bool moving;
        Vector3 previousPos;
        bool inDelay;
        float cameraRotSmooth;
        IEnumerator CameraRotDelay()
        {
            var movDist = Vector3.Distance(previousPos, followTarget.transform.position);
            if (movDist > 0.001f)
            {
                if (!moving)
                {
                    moving = true;
                    inDelay = true;
                    yield return new WaitForSeconds(1.5f);
                    inDelay = false;
                }
            }
            else
            {
                moving = false;
                cameraRotSmooth = 0;
            }

            cameraRotSmooth = Mathf.Lerp(cameraRotSmooth, !inDelay ? 25 : 5, Time.deltaTime);
            yRot = Mathf.Lerp(yRot, yRot + input.DirectionInput.x * invertXVal * 2, Time.deltaTime * cameraRotSmooth);
        }
    }
}