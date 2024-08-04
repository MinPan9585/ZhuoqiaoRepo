using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace FC_ParkourSystem
{

    //[CreateAssetMenu(menuName = "Parkour System/New custom actions/Vault Action")]
    public class VaultAction : ParkourAction
    {
        public override bool CheckIfPossible(ObstacleHitData hitData, Transform player)
        {
            if (!base.CheckIfPossible(hitData, player))
                return false;

            var obstacle = hitData.forwardHit.transform;
            var hitPointLocal = obstacle.InverseTransformPoint(hitData.forwardHit.point);

            if (hitPointLocal.z < 0 && hitPointLocal.x < 0 || hitPointLocal.z > 0 && hitPointLocal.x > 0)
            {
                matchBodyPart = AvatarTarget.RightHand;
                Mirror = true;
            }
            else
            {
                matchBodyPart = AvatarTarget.LeftHand;
                Mirror = false;
            }

            return true;
        }
    }
}