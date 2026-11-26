using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("NPC")) Debug.Log("进入对话区域");
        if (other.CompareTag("Item")) Debug.Log("进入拾取区域");
        if (other.CompareTag("Portal")) Debug.Log("进入传送门");

    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("NPC")) Debug.Log("离开对话区域");
        if (other.CompareTag("Item")) Debug.Log("离开拾取区域");
        if (other.CompareTag("Portal")) Debug.Log("离开传送门");
    }
}
