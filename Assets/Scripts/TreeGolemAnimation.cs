using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeGolemAnimation : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            GetComponent<Animator>().SetTrigger("Activate");
        }
    }
}
