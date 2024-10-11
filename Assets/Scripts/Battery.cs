using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Battery : MonoBehaviour
{
    public Transform ballPos;
    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                this.transform.SetParent(ballPos);
                this.transform.localPosition = Vector3.zero;
            }
        }
    }
}
