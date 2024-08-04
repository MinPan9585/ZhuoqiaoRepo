using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FC_ParkourSystem {

    public class SetLayersForDemoscene : MonoBehaviour
    {
        public List<Transform> ledgeParent;

        void Start()
        {
            foreach (var ledges in ledgeParent)
            {
                foreach (Transform ledge in ledges)
                    ledge.gameObject.layer = LayerMask.NameToLayer("Ledge");
            }
        }
    }
}