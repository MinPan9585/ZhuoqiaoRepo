using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceToPutBattery : MonoBehaviour
{
    public Transform ballPos;
    public Animator anim;
    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                if(ballPos.childCount == 0)
                {
                    return;
                }
                else
                {
                    ballPos.GetChild(0).gameObject.SetActive(false);
                    GetComponent<MeshRenderer>().enabled = true;
                    anim.SetTrigger("OpenDoorTwo");
                }
            }
        }
    }
}
