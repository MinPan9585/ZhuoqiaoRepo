using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FC_ParkourSystem
{

    //[CreateAssetMenu(menuName = "Climbing System/New Climbing action")]
    public class ClimbActions : ScriptableObject
    {
        [SerializeField] string animName;
        [SerializeField] string obstacleTag;

        [SerializeField] float minHeight;
        [SerializeField] float maxHeight;

        [SerializeField] bool rotateToObstacle;
        [SerializeField] float postActionDelay;

        [Header("Target Matching")]
        [SerializeField] bool enableTargetMatching = true;

        public AvatarTarget matchBodyPart;
        public float matchStartTime;
        public float matchTargetTime;
        public Vector3 matchPosWeight = new Vector3(0, 1, 0);

        //[HideInInspector]
        //public float IKWeight = 1f;

        public Vector3 IKOffsets;

        [HideInInspector]
        public ClimbPoint previousPoint;

        [HideInInspector]
        public IkPart leftHand, rightHand, leftFoot, rightFoot;

        public bool IKEnabled;




        public Quaternion TargetRotation { get; set; }
        public Vector3 MatchPos { get; set; }
        public bool Mirror { get; set; }

        public virtual bool CheckIfPossible(ObstacleHitData hitData, Transform player)
        {
            // Check Tag
            if (!string.IsNullOrEmpty(obstacleTag) && hitData.forwardHit.transform.tag != obstacleTag)
                return false;

            // Height Tag
            float height = hitData.heightHit.point.y - player.position.y;
            if (height < minHeight || height > maxHeight)
                return false;

            if (rotateToObstacle)
                TargetRotation = Quaternion.LookRotation(-hitData.forwardHit.normal);

            if (enableTargetMatching)
                MatchPos = hitData.heightHit.point;

            return true;
        }

        public string AnimName
        {
            get => animName;
            set
            {
                animName = value;
            }
        }
        //public bool RotateToObstacle => rotateToObstacle;
        //public float PostActionDelay => postActionDelay;

        public bool EnableTargetMatching => enableTargetMatching;
    }
    [System.Serializable]
    public class IkPart
    {
        public float ikStartTime;
        public float ikEndTime;
        [HideInInspector]
        public float IKWeight;
    }
}
