using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Talk : MonoBehaviour
{
    public Text talkUI;
    bool isTalking = false;
    public string[] dialogues;
    int index = 0;

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (!isTalking)
                {
                    talkUI.gameObject.SetActive(true);
                    talkUI.text = dialogues[index];
                    isTalking = true;
                }
            }
        }
        

        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            Debug.Log("2");
            if (isTalking)
            {
                if (index < dialogues.Length - 1)
                {
                    index++;
                    talkUI.text = dialogues[index];
                }
                else
                {
                    talkUI.gameObject.SetActive(false);
                    isTalking = false;
                    index = 0;
                }
            }
        }
    }
}
