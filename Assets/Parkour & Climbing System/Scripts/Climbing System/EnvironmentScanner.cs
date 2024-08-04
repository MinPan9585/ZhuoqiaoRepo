using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FC_ParkourSystem
{

    public class EnvironmentScanner : MonoBehaviour
    {
        //[SerializeField] Vector3 forwardRayOffset = new Vector3(0, 2.5f, 0);
        [SerializeField] float heightRayLength = 4;

        [field: SerializeField] public LayerMask ObstacleLayer { get; set; } = 1;
        [field: SerializeField] public LayerMask LedgeLayer { get; set; } = 1;

        [SerializeField] float obstacleCheckRange = 0.7f;

        [SerializeField] float minJumpDistance = 1f;


        public float ledgeHeightThreshold = 1f;

        Vector2 boxCastRange = new Vector2(0.1f, 1.2f);
        Vector3 parabolaBoxCastHalf = new Vector3(0.1f, 0.01f, 0.01f);
        Vector3 downwardBoxCastHalf = new Vector3(0.1f, 0.01f, 0.2f);
        Vector3 obstacleBoxCastHalf = new Vector3(0.02f, 0.01f, 0.05f);

        IParkourCharacter player;

        private void OnEnable()
        {
            player = GetComponent<IParkourCharacter>();
            if (!(LedgeLayer == (LedgeLayer | (1 << LayerMask.NameToLayer("Ledge")))))
                LedgeLayer = 1 << LayerMask.NameToLayer("Ledge");
            if (!(ObstacleLayer == (ObstacleLayer | (1 << LayerMask.NameToLayer("Ledge")))))
                ObstacleLayer += 1 << LayerMask.NameToLayer("Ledge");
        }

        ParkourController parkourController;
        private void Start()
        {
            parkourController = GetComponent<ParkourController>();

        }

        public Transform climbLedgeCheck(Vector3 jumpDir)
        {
            if (jumpDir == Vector3.zero) jumpDir = transform.forward;
            RaycastHit ledgeHit;
            Physics.BoxCast(transform.position + Vector3.up * 2f, new Vector3(0.3f, 1f, 0.01f), jumpDir, out ledgeHit, Quaternion.LookRotation(jumpDir), 0.6f, LedgeLayer);
            //GizmosExtend.drawBoxCastBox(transform.position + Vector3.up * 2f, new Vector3(0.3f, 1f, 0.01f), Quaternion.LookRotation(jumpDir), jumpDir, 0.6f, Color.black);
            return ledgeHit.transform;
        }


        public ObstacleHitData ObstacleCheck(bool performHeightCheck = true , float forwardOriginOffset = 1f)
        {
            var hitData = new ObstacleHitData();

            var forwardOrigin = transform.position + Vector3.up * forwardOriginOffset;
            hitData.forwardHitFound = Physics.BoxCast(forwardOrigin, new Vector3(0.1f, 0.7f, 0.01f), transform.forward,
                out hitData.forwardHit, Quaternion.LookRotation(transform.forward), obstacleCheckRange, ObstacleLayer);

            //Debug.DrawRay(forwardOrigin, transform.forward * obstacleCheckRange, (hitData.forwardHitFound) ? Color.red : Color.white);
            //BoxCastDebug.DrawBoxCastBox(forwardOrigin, new Vector3(0.1f, 0.7f, 0.01f), Quaternion.LookRotation(transform.forward), transform.forward, obstacleCheckRange, Color.green);


            //DrawAxis(hitData.forwardHit.point, 0.1f, Color.blue);

            if (hitData.forwardHitFound && performHeightCheck)
            {
                var heightOrigin = hitData.forwardHit.point + Vector3.up * heightRayLength;

                //GizmosExtend.drawSphereCast(hitData.forwardHit.point, 0.2f, Vector3.up, heightRayLength, Color.black);
                //var spaceCheckOrigin = transform.position + Vector3.up;
                //if (Physics.Raycast(spaceCheckOrigin, Vector3.up, out hitData.heightHit, heightRayLength, ObstacleLayer) && hitData.heightHit.point.y > hitData.forwardHit.point.y)
                //    heightOrigin.y = hitData.heightHit.point.y;
                var spaceCheckOrigin = transform.position + Vector3.up * heightRayLength;
                spaceCheckOrigin.y = heightOrigin.y;
                if (Physics.Raycast(spaceCheckOrigin, Vector3.down, out hitData.heightHit, heightRayLength, ObstacleLayer) && hitData.heightHit.point.y > hitData.forwardHit.point.y)
                    heightOrigin.y = hitData.heightHit.point.y;

                for (int i = 0; i < 4; ++i)
                {
                    //hitData.heightHitFound = Physics.BoxCast(heightOrigin, obstacleBoxCastHalf, Vector3.down, out hitData.heightHit,
                    //    Quaternion.LookRotation(Vector3.down), heightRayLength, ObstacleLayer);
                    //BoxCastDebug.DrawBoxCastBox(heightOrigin, obstacleBoxCastHalf,
                    //    Quaternion.LookRotation(Vector3.down), Vector3.down, heightRayLength, Color.green);

                    hitData.heightHitFound = Physics.SphereCast(heightOrigin, 0.2f, Vector3.down, out hitData.heightHit, heightRayLength, ObstacleLayer);

                    //GizmosExtend.drawSphereCast(heightOrigin, 0.2f, Vector3.down, heightRayLength, Color.magenta);


                    if (hitData.heightHitFound && Vector3.Angle(Vector3.up, hitData.heightHit.normal) < 45f)
                        break;

                    hitData.heightHitFound = false;
                    heightOrigin += transform.forward * 0.15f;
                }
                // Debug.DrawRay(hitData.heightHit.point, hitData.heightHit.normal, Color.blue);

                if (hitData.heightHitFound)
                {
                    float length = 0.8f;
                    //forwardOrigin = transform.position + transform.forward * length / 2; ;
                    forwardOrigin = hitData.heightHit.point;
                    forwardOrigin.y = hitData.heightHit.point.y + 0.2f + length * 1.5f / 2;
                    hitData.hasSpace = !Physics.CheckBox(forwardOrigin, new Vector3(0.5f, 1.5f, 0.7f) * length / 2, Quaternion.LookRotation(transform.forward), ObstacleLayer);

                    //BoxCastDebug.DrawBoxCastBox(forwardOrigin, new Vector3(0.5f, 1.5f, 0.7f) * length / 2, Quaternion.LookRotation(transform.forward), transform.forward, 0f, Color.green);

                    var spaceOrigin = hitData.heightHit.point + transform.forward * 0.5f + Vector3.up * 0.6f;

                    RaycastHit spaceHit;
                    hitData.hasSpaceToVault = Physics.SphereCast(spaceOrigin, 0.1f, Vector3.down, out spaceHit, 1f, ObstacleLayer);

                    //GizmosExtend.drawSphereCast(spaceOrigin, 0.1f, Vector3.down, 1f, Color.magenta);


                }

            }

            return hitData;
        }

        public bool ClimbLedgeCheck(Vector3 dir, out ClimbLedgeData climbLedgeData)
        {
            climbLedgeData = new ClimbLedgeData();

            if (dir == Vector3.zero)
                return false;

            var origin = transform.position + Vector3.up;

            for (int i = 0; i < 15; ++i)
            {
                if (Physics.Raycast(origin + new Vector3(0, 0.15f, 0) * i, dir, out RaycastHit hit, 1f, LedgeLayer))
                {
                    climbLedgeData.ledgeHit = hit;
                    return true;
                }
            }

            return false;
        }

        public bool DropLedgeCheck(Vector3 moveDir, out ClimbLedgeData climbLedgeData)
        {
            climbLedgeData = new ClimbLedgeData();

            var origin = transform.position + transform.forward * 1 + Vector3.down * 0.1f;

            //Debug.DrawRay(origin, -transform.forward * 1f, Color.cyan);
            if (Physics.SphereCast(origin, 0.1f, -transform.forward, out RaycastHit hit, 1f + 0.4f, LedgeLayer))
            {
                climbLedgeData.ledgeHit = hit;
                return true;
            }

            return false;
        }

        public bool LedgeCheck(Vector3 moveDir, out LedgeData ledgeData)
        {
            ledgeData = new LedgeData();

            if (moveDir == Vector3.zero)
                moveDir = transform.forward;

            var rigthVec = Vector3.Cross(Vector3.up, moveDir);

            var origin = transform.position + moveDir * 0.6f + Vector3.up;
            //PhysicsUtil.ThreeRayCasts(origin, Vector3.down, 0.25f, rigthVec,
            //    out List<RaycastHit> hits, 50f, ObstacleLayer, false, false);

            //    var validHits = hits.Where(h => transform.position.y - h.point.y > ledgeHeightThreshold).ToList();

            //    if (validHits.Count > 0)
            //    {
            //        var hit = validHits.First();

            //        float hitHeight = transform.position.y - hit.point.y;
            //        if (hitHeight > ledgeHeightThreshold)
            //        {
            var surfaceOrgin = transform.position + moveDir.normalized * 0.8f + Vector3.down * 0.05f;
            //var surfaceRayDir = transform.position + Vector3.down * 0.05f - surfaceOrgin;

            if (ledgeData.surfaceHitFound = Physics.Raycast(surfaceOrgin, -moveDir, out ledgeData.surfaceHit, 1f, ObstacleLayer))
            {
                //Debug.DrawRay(surfaceOrgin, -moveDir, Color.cyan);
                //Debug.DrawRay(ledgeData.surfaceHit.point, ledgeData.surfaceHit.normal, Color.blue);

                //ledgeData.height = hitHeight;
                ledgeData.angle = Vector3.Angle(transform.forward, ledgeData.surfaceHit.normal);

                var distPoint = ledgeData.surfaceHit.point;
                distPoint.y = transform.position.y;
                var distVec = distPoint - transform.position;

                ledgeData.distance = Vector3.Dot(distVec, ledgeData.surfaceHit.normal);

                surfaceOrgin = transform.position + Vector3.up * ledgeHeightThreshold + moveDir.normalized * 1f;
                //surfaceOrgin = ledgeData.surfaceHit.point + moveDir.normalized * 0.05f;
                RaycastHit heightHit;
                if (Physics.Raycast(surfaceOrgin, Vector3.down, out heightHit, 2f, ObstacleLayer))
                    if ((ledgeData.height = heightHit.distance) < ledgeHeightThreshold * 2)
                    {
                        return false;
                    }

                return true;
            }
            //else
            //    return true;
            //    }
            //}


            return false;
        }

        public class LandableTarget
        {
            public LandableTarget(Vector3 target, RaycastHit hit, float dispFromParabola)
            {
                this.position = target;
                this.hit = hit;
                this.distanceFromStart = dispFromParabola;
            }

            public Vector3 position;
            public float distanceFromStart;
            public RaycastHit hit;
        }

        private void Update()
        {
            //if (lastPrabola.Count > 1)
            //    ShowParabola(lastPrabola, lastMoveDir);
        }

        void ShowParabola(List<Vector3> parabola, Vector3 moveDir)
        {
            float time = 0.1f;
            Vector3 startPos = parabola[0];

            for (int i = 1; i < parabola.Count; ++i)
            {
                var newPos = parabola[i];
                var lastPos = parabola[i - 1];

                var disp = newPos - lastPos;
                float dispLen = disp.magnitude;
                var dispDir = disp.normalized;

                var origin = lastPos - dispDir * parabolaBoxCastHalf.y;

                //BoxCastDebug.DrawBoxCastBox(origin, parabolaBoxCastHalf, Quaternion.LookRotation(dispDir), dispDir, dispLen, Color.blue);

                origin = newPos;
                var distToNewPos = Vector3.Distance(newPos, startPos);

                if (distToNewPos <= parkourController.MinJumpDistance)
                    origin += Vector3.up * 2f;

                //BoxCastDebug.DrawBoxCastBox(origin + Vector3.up * downwardBoxCastHalf.y, downwardBoxCastHalf, Quaternion.LookRotation(moveDir), Vector3.down, 5f, Color.blue);

                time += 0.05f;
            }

            foreach (var point in lastDetectedPoints)
            {
                DrawAxis(point, 0.1f, Color.red);
            }

        }


        Vector3 lastMoveDir = Vector3.zero;
        List<Vector3> lastPrabola = new List<Vector3>();
        List<Vector3> lastDetectedPoints = new List<Vector3>();
        Vector3 lastTargetPos = Vector3.zero;
        RaycastHit lastHit;

        Transform jumpTarget;

        public List<Action> actions = new List<Action>();

        public JumpData FindPointToJump(Vector3 jumpDir, float forwardSpeed = 4.5f, float jumpHeight = 1.5f, bool checkClimb = true)
        {
            if(parkourController.alwaysUsePlayerForward)
            jumpDir = transform.forward;
            var jumpPoint = new JumpData();
            jumpDir = (jumpDir == Vector3.zero) ? transform.forward : jumpDir.normalized;

            float linePoints = 0.05f * 40f;

            var defaultVelocity = jumpDir * forwardSpeed + Vector3.up * Mathf.Sqrt(-2 * player.Gravity * jumpHeight);
            var startPos = transform.position;// + Vector3.up * 0.1f;
            var lastPos = startPos;
            var newPos = startPos;

            Vector3 targetPos = Vector3.zero;

            List<Vector3> parabolaPoints = new List<Vector3>();
            RaycastHit hit = new RaycastHit();
            RaycastHit hitVarying = new RaycastHit();

            var landablePoints = new List<LandableTarget>();

            bool hasHeightVariation = false;

            Vector3 lastHitPoint = transform.position;
            jumpPoint.pointBeforeledge = transform.position;

            jumpPoint.isClimbable = false;

            actions = new List<Action>();

            for (float time = 0.05f; time < linePoints; time += (0.4f / Mathf.Abs(forwardSpeed)))
            {
                newPos = startPos + defaultVelocity * time;
                newPos.y = startPos.y + defaultVelocity.y * time + 0.5f * player.Gravity * time * time;

                var disp = newPos - lastPos;
                float dispLen = disp.magnitude + 0.2f;
                var dispDir = disp.normalized;

                var parabolaVaryingBoxHalf = parabolaBoxCastHalf;
                var downwardVaryingBoxHalf = downwardBoxCastHalf;

                parabolaVaryingBoxHalf.x = Mathf.Lerp(boxCastRange.x, boxCastRange.y, time);
                downwardVaryingBoxHalf.x = Mathf.Lerp(boxCastRange.x, boxCastRange.y, time);

                bool hitFound = false;

                var origin = lastPos - dispDir * parabolaBoxCastHalf.y;

                hitFound = Physics.BoxCast(origin, parabolaVaryingBoxHalf, dispDir, out hitVarying, Quaternion.LookRotation(dispDir), dispLen, ObstacleLayer);
                BoxCastDebug.DrawBoxCastBox(origin, parabolaVaryingBoxHalf, Quaternion.LookRotation(dispDir), dispDir, dispLen, Color.yellow);

                //GizmosExtend.drawBoxCastBox(origin, parabolaVaryingBoxHalf, Quaternion.LookRotation(dispDir), dispDir, dispLen, Color.green);
                //var babu = origin;
                //actions.Add(()=> GizmosExtend.DrawBoxCastBox(babu, parabolaVaryingBoxHalf, Quaternion.LookRotation(dispDir), dispDir, dispLen, Color.green));

                var ledgeBoxCast = parabolaVaryingBoxHalf;

                float yOffset;
                if (checkClimb)
                {
                    yOffset = Mathf.Lerp(1.5f + disp.y, 0.7f, time * 4f);
                    ledgeBoxCast.y = Mathf.Lerp(1f + disp.y, 0.7f, time * 4f);
                }
                else
                {
                    yOffset = Mathf.Lerp(disp.y, 0.7f, time * 4f);
                    ledgeBoxCast.y = Mathf.Lerp(0, 0.7f, time * 4f);
                }

                if (!jumpPoint.isClimbable) // && time < 0.20f)
                {
                    var ledgeHit = new RaycastHit();
                    var ledgeHitFound = Physics.BoxCast(origin + Vector3.up * yOffset, ledgeBoxCast, dispDir, out ledgeHit, Quaternion.LookRotation(jumpDir), dispLen + 0.2f, LedgeLayer);
                    //BoxCastDebug.DrawBoxCastBox(origin + Vector3.up * yOffset, ledgeBoxCast, Quaternion.LookRotation(jumpDir), dispDir, dispLen + 0.2f, Color.blue);
                    //actions.Add(() => { GizmosExtend.DrawBoxCastBox((babu + Vector3.up * yOffset), ledgeBoxCast, Quaternion.LookRotation(jumpDir), dispDir, dispLen + 0.2f, Color.blue); });

                    //var ledgeHitFound = Physics.SphereCast(origin + Vector3.up * yOffset, ledgeBoxCast.y , dispDir, out ledgeHit, dispLen + 0.2f, LedgeLayer);
                    //actions.Add(() => { GizmosExtend.DrawSphereCast(origin + Vector3.up * yOffset, ledgeBoxCast.y , dispDir, dispLen + 0.2f, Color.blue); });

                    if (ledgeHitFound)
                    {
                        //DrawAxis(ledgeHit.point, 0.1f, Color.yellow);
                        jumpPoint.climbHitData = ledgeHit;
                        jumpPoint.isClimbable = true;
                    }
                }

                if (hitFound)
                    break;

                origin = newPos;
                var dispVec = newPos - startPos;

                var distToNewPos = dispVec.magnitude * Mathf.Cos(Vector3.Angle(dispVec, jumpDir) * Mathf.Deg2Rad);

                //BoxCastDebug.DrawBoxCastBox(origin + Vector3.up * downwardVaryingBoxHalf.y, downwardVaryingBoxHalf, Quaternion.LookRotation(jumpDir), Vector3.down, 5f, Color.green);
                //actions.Add(() => { GizmosExtend.DrawBoxCastBox(origin + Vector3.up * downwardVaryingBoxHalf.y, downwardVaryingBoxHalf, Quaternion.LookRotation(jumpDir), Vector3.down, 5f, Color.green); });
                //actions.Add(() => GizmosExtend.DrawSphereCast(origin + Vector3.up * downwardBoxCastHalf.y, 0.2f, Vector3.down, 5f, Color.green));

                bool centerHitFound = Physics.BoxCast(origin + Vector3.up * downwardBoxCastHalf.y, downwardBoxCastHalf,
                    Vector3.down, out hit, Quaternion.LookRotation(jumpDir), 5f, ObstacleLayer);



                hitFound = Physics.BoxCast(origin + Vector3.up * downwardVaryingBoxHalf.y, downwardVaryingBoxHalf,
                    Vector3.down, out hitVarying, Quaternion.LookRotation(jumpDir), 5f, ObstacleLayer);


                if (!centerHitFound || (hitFound && hitVarying.point.y > hit.point.y && Mathf.Abs(hitVarying.point.y - hit.point.y) > 0.2f))
                    hit = hitVarying;


                if (!hasHeightVariation)
                {
                    if (hit.point != Vector3.zero && Mathf.Abs(hit.point.y - lastHitPoint.y) <= 0.2f)
                    {
                        jumpPoint.pointBeforeledge = hit.point;
                        lastHitPoint = hit.point;
                    }
                    else
                    {
                        if (hit.point == Vector3.zero || transform.position.y - hit.point.y > ledgeHeightThreshold * 2)
                        {
                            jumpPoint.hasLedge = true;
                        }
                        hasHeightVariation = true;
                    }
                }

                if (hitFound)
                {
                    if (distToNewPos > parkourController.MinJumpDistance && hasHeightVariation && Vector3.Angle(hit.normal, Vector3.up) <= 45f)
                    {
                        float dispFromParabola = newPos.y - hit.point.y;
                        landablePoints.Add(new LandableTarget(hit.point, hit, distToNewPos));
                    }

                    jumpTarget = hit.transform;
                }


                parabolaPoints.Add(newPos);
                lastPos = newPos;
            }

            LandableTarget targetPoint = null;
            if (landablePoints.Count > 0)
            {
                // Find highest point
                Vector3 prevPoint = transform.position;
                List<List<LandableTarget>> priorityGroup = new List<List<LandableTarget>>();
                var group = new List<LandableTarget>();
                foreach (var p in landablePoints)
                {
                    var diff = p.position.y - prevPoint.y;
                    if (Mathf.Abs(diff) > 0.2f)
                        group = new List<LandableTarget>();
                    group.Insert(0, p);
                    if (group.Count > 0 && !priorityGroup.Contains(group))
                        priorityGroup.Add(group);
                    prevPoint = p.position;
                }
                var sortedGroup = priorityGroup.OrderByDescending(p => p[0].position.y).ToList();
                foreach (var item in sortedGroup)
                {
                    while (item.Count > 0)
                    {
                        targetPoint = item[Mathf.Clamp(item.Count / 2, 0, 3)];

                        var disXZ = targetPoint.position - transform.position;
                        var disY = disXZ.y;
                        disXZ.y = 0;

                        var upOffset = Vector3.up * 1.4f; // head Offset

                        var h = parkourController.getJumpHeight(disY, disXZ);
                        var heightestPoint = transform.position + disXZ / 2 + Vector3.up * h + upOffset;

                        //GizmosExtend.singleAction = () => GizmosExtend.DrawSphere(targetPoint.position + upOffset + disXZ.normalized * 0.3f, 0.1f, Color.grey);

                        var jumpXZdistance = transform.position - targetPoint.position;
                        jumpXZdistance.y = 0;

                        if (((transform.position.y - targetPoint.position.y) > 0f) && jumpXZdistance.magnitude < minJumpDistance ||
                            (Physics.Linecast(transform.position + upOffset, heightestPoint, ObstacleLayer) ||
                         Physics.Linecast(heightestPoint, targetPoint.position + upOffset + disXZ.normalized * 0.3f, ObstacleLayer)) || Physics.CheckSphere(targetPoint.position + Vector3.up * 0.5f, 0.2f, ObstacleLayer))
                            targetPoint = null;
                        else break;

                        item.RemoveRange(0, Mathf.Min(item.Count, 2));
                        //if (Physics.Linecast(transform.position+Vector3.up*jumpHeight, new Vector3(targetPoint.position.x, targetPoint.position.y + jumpHeight, targetPoint.position.z) + (targetPoint.position - transform.position).normalized * 0.5f, ObstacleLayer))
                        //    targetPoint = null;
                        //else break;
                        // use while for a special case
                    }
                    if (targetPoint != null) break;
                }
            }
            else
            {
                return jumpPoint;
            }

            if (targetPoint == null)
                return jumpPoint;

            hit = targetPoint.hit;
            targetPos = targetPoint.position;

            // Do sphere casts to check if there is space to land
            var dirToTarget = (targetPos - transform.position);
            dirToTarget.y = 0;
            dirToTarget = dirToTarget.normalized;

            var spaceOrigin = targetPos + dirToTarget * 0.5f + Vector3.up * 0.3f;

            RaycastHit spaceHit;
            jumpPoint.hasSpaceToLand = Physics.SphereCast(spaceOrigin, 0.1f, Vector3.down, out spaceHit, 0.4f, ObstacleLayer);

            float padding = 0.2f;
            var yOffsetPad = Vector3.up * 0.2f;

            var jumpDirRight = Vector3.Cross(Vector3.down, jumpDir).normalized;
            Vector3 offset = Vector3.zero;

            bool spaceFrontFound = Physics.Raycast(targetPos + jumpDir.normalized * padding + yOffsetPad, Vector3.down, out RaycastHit spaceFrontHit, 0.3f, ObstacleLayer);
            bool spaceBackFound = Physics.Raycast(targetPos - jumpDir.normalized * padding + yOffsetPad, Vector3.down, out RaycastHit spaceBackHit, 0.3f, ObstacleLayer);
            bool spaceRightFound = Physics.Raycast(targetPos + jumpDirRight * padding + yOffsetPad, Vector3.down, out RaycastHit spaceRightHit, 0.3f, ObstacleLayer);
            bool spaceLeftFound = Physics.Raycast(targetPos - jumpDirRight * padding + yOffsetPad, Vector3.down, out RaycastHit spaceLeftHit, 0.3f, ObstacleLayer);

            padding = 0.15f;

            if (spaceFrontFound) offset += jumpDir.normalized * padding;
            if (spaceBackFound) offset += jumpDir.normalized * -padding;
            if (!Physics.Linecast(targetPos + yOffsetPad, targetPos + jumpDirRight * padding + yOffsetPad, ObstacleLayer) && spaceRightFound) offset += jumpDirRight * padding;
            if (!Physics.Linecast(targetPos + yOffsetPad, targetPos - jumpDirRight * padding + yOffsetPad, ObstacleLayer) && spaceLeftFound) offset += jumpDirRight * -padding;

            //bool spaceFrontFound = Physics.SphereCast(targetPos + jumpDir.normalized * padding + yOffsetPad, 0.1f, Vector3.down, out RaycastHit spaceFrontHit, 0.3f, ObstacleLayer);
            //bool spaceBackFound = Physics.SphereCast(targetPos - jumpDir.normalized * padding + yOffsetPad, 0.1f, Vector3.down, out RaycastHit spaceBackHit, 0.3f, ObstacleLayer);
            //bool spaceRightFound = Physics.SphereCast(targetPos + jumpDirRight * padding + yOffsetPad, 0.1f, Vector3.down, out RaycastHit spaceRightHit, 0.3f, ObstacleLayer);
            //bool spaceLeftFound = Physics.SphereCast(targetPos - jumpDirRight * padding + yOffsetPad, 0.1f, Vector3.down, out RaycastHit spaceLeftHit, 0.3f, ObstacleLayer);
            //if (spaceFrontFound) offset += spaceFrontHit.point - targetPos;
            //if (spaceBackFound) offset += spaceBackHit.point - targetPos;
            //if (!Physics.Linecast(targetPos + yOffsetPad, targetPos + jumpDirRight * padding + yOffsetPad, ObstacleLayer) && spaceRightFound) offset += spaceRightHit.point - targetPos;
            //if (!Physics.Linecast(targetPos + yOffsetPad, targetPos - jumpDirRight * padding + yOffsetPad, ObstacleLayer) && spaceLeftFound) offset += spaceLeftHit.point - targetPos;

            offset.y = 0;

            targetPos += offset;


            jumpPoint.footPosition = targetPos;
            jumpPoint.rootPosition = targetPos;
            jumpPoint.jumpPointFound = true;

            // Used for showing jump trajectory after the jump for debugging
            lastTargetPos = jumpPoint.footPosition;
            lastHit = hit;
            lastDetectedPoints = landablePoints.GetRange(0, landablePoints.Count).Select(x => x.position).ToList();
            lastPrabola = parabolaPoints.GetRange(0, parabolaPoints.Count);
            lastMoveDir = jumpDir;

            return jumpPoint;
        }

        void DrawBounds(Bounds b, float delay = 0)
        {
            // bottom
            var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
            var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
            var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
            var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

            Debug.DrawLine(p1, p2, Color.blue, delay);
            Debug.DrawLine(p2, p3, Color.red, delay);
            Debug.DrawLine(p3, p4, Color.yellow, delay);
            Debug.DrawLine(p4, p1, Color.magenta, delay);

            // top
            var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
            var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
            var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
            var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

            Debug.DrawLine(p5, p6, Color.blue, delay);
            Debug.DrawLine(p6, p7, Color.red, delay);
            Debug.DrawLine(p7, p8, Color.yellow, delay);
            Debug.DrawLine(p8, p5, Color.magenta, delay);

            // sides
            Debug.DrawLine(p1, p5, Color.white, delay);
            Debug.DrawLine(p2, p6, Color.gray, delay);
            Debug.DrawLine(p3, p7, Color.green, delay);
            Debug.DrawLine(p4, p8, Color.cyan, delay);
        }

        public void DrawAxis(Vector3 pos, float r, Color color)
        {
            Debug.DrawLine(pos - new Vector3(r, 0, 0), pos + new Vector3(r, 0, 0), color);
            Debug.DrawLine(pos - new Vector3(0, r, 0), pos + new Vector3(0, r, 0), color);
            Debug.DrawLine(pos - new Vector3(0, 0, r), pos + new Vector3(0, 0, r), color);
        }
        public dynamic clone(dynamic d)
        {
            dynamic local = d;
            return d;
        }
    }
    public struct ObstacleHitData
    {
        public bool forwardHitFound;
        public bool heightHitFound;
        public RaycastHit forwardHit;
        public RaycastHit heightHit;
        public bool hasSpaceToVault;
        public bool hasSpace;
    }

    public struct LedgeData
    {
        public float height;
        public float angle;
        public float distance;
        public RaycastHit surfaceHit;
        public bool surfaceHitFound;
    }

    public struct ClimbLedgeData
    {
        public RaycastHit ledgeHit;
    }

    public class JumpData
    {
        public Vector3 rootPosition;
        public Vector3 footPosition;
        public bool hasSpaceToLand;
        public bool hasLedge;
        public bool jumpPointFound;

        public bool isClimbable;
        public RaycastHit climbHitData;
        public Vector3 pointBeforeledge;
    }
}

