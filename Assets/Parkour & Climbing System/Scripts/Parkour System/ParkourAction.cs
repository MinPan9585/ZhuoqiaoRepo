using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace FC_ParkourSystem
{
    public enum VaultType { Any, VaultOver, VaultOn}

    [CreateAssetMenu(menuName = "Parkour System/New Parkour Action")]
    public class ParkourAction : ScriptableObject
    {
        [Tooltip("Name of the animation to play for performing the parkour action")]
        [SerializeField] string animName;

        [Tooltip("Minimum height of the obstacle on which this parkour action can be performed")]
        [SerializeField] float minHeight;
        [Tooltip("Maximum height of the obstacle on which this parkour action can be performed")]
        [SerializeField] float maxHeight;

        [Tooltip("If true, the player will rotate towards the obstacle while performing the parkour action")]
        [SerializeField] bool rotateToObstacle;

        [Tooltip("Determines if this parkour action makes the player vault over or vault onto the obstacle")]
        [SerializeField] VaultType vaultType;

        [Tooltip("If obstacle tag is given, then this parkour action will only be performed on obstacles with the same tag")]
        [SerializeField] string obstacleTag;

        [Tooltip("Delay before giving the control to the player after the parkour action. Useful for action's that requires an additional transition animation.")]
        [SerializeField] float postActionDelay;

        [Header("Target Matching")]

        [Tooltip("If true, the parkour action will use target matching. This is useful for adapting the same animation to obstacles of different sizes")]
        [SerializeField] bool enableTargetMatching = true;

        [Tooltip("The body part that should be used for target matching")]
        [SerializeField] protected AvatarTarget matchBodyPart;

        [Tooltip("Normalized time of the animation at which the target matching should start")]
        [SerializeField] float matchStartTime;

        [Tooltip("Normalized time of the animation at which the target matching should end")]
        [SerializeField] float matchTargetTime;

        [Tooltip("Determines the axes that the target matching should affect")]
        [SerializeField] Vector3 matchPosWeight = new Vector3(0, 1, 0);

        public Quaternion TargetRotation { get; set; }
        public Vector3 MatchPos { get; set; }
        public bool Mirror { get; set; }

        public virtual bool CheckIfPossible(ObstacleHitData hitData, Transform player)
        {
            if (vaultType == VaultType.VaultOn && !hitData.hasSpaceToVault)
                return false;

            if (vaultType == VaultType.VaultOver && hitData.hasSpaceToVault)
                return false;


            // Check Tag
                if (!string.IsNullOrEmpty(obstacleTag) && hitData.forwardHit.transform.tag != obstacleTag)
                return false;

            // Height Tag
            float height = hitData.heightHit.point.y - player.position.y;
            if (height < minHeight || height > maxHeight)
                return false;


            if (rotateToObstacle)
                TargetRotation = Quaternion.LookRotation(Vector3.Scale((hitData.heightHit.point - player.position), new Vector3(1, 0, 1)));
            //if (rotateToObstacle)
            //    TargetRotation = Quaternion.LookRotation(Vector3.Scale(-hitData.forwardHit.normal, new Vector3(1, 0, 1)));

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
        public bool RotateToObstacle => rotateToObstacle;
        public float PostActionDelay => postActionDelay;

        public bool EnableTargetMatching => enableTargetMatching;
        public AvatarTarget MatchBodyPart => matchBodyPart;
        public float MatchStartTime => matchStartTime;
        public float MatchTargetTime => matchTargetTime;
        public Vector3 MatchPosWeight => matchPosWeight;
    }
}