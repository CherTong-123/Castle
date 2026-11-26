using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public Npc DialogueTarget { get; private set; }
    public event Action<Npc> DialogueTargetChanged;

    // 按碰撞体记录，避免同一个 NPC 的多个触发区互相覆盖。
    private readonly List<Collider2D> npcColliders = new List<Collider2D>();

    private void Update()
    {
        // NPC 被隐藏、禁用或销毁时，也需要清理交互目标。
        RefreshDialogueTarget();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger && other.GetComponentInParent<Npc>() != null)
        {
            if (!npcColliders.Contains(other)) npcColliders.Add(other);
            RefreshDialogueTarget();
        }
        if (other.CompareTag("Item")) Debug.Log("进入拾取区域");
        if (other.CompareTag("Portal")) Debug.Log("进入传送门");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (npcColliders.Remove(other)) RefreshDialogueTarget();
        if (other.CompareTag("Item")) Debug.Log("离开拾取区域");
        if (other.CompareTag("Portal")) Debug.Log("离开传送门");
    }

    private void RefreshDialogueTarget()
    {
        npcColliders.RemoveAll(collider => collider == null ||
            !collider.enabled || !collider.gameObject.activeInHierarchy);

        Npc nextTarget = null;
        foreach (Collider2D collider in npcColliders)
        {
            Npc npc = collider.GetComponentInParent<Npc>();
            if (npc != null && npc.isActiveAndEnabled)
            {
                nextTarget = npc;
                break;
            }
        }

        // ReferenceEquals 也能识别已经被 Unity 销毁的旧目标。
        if (ReferenceEquals(DialogueTarget, nextTarget)) return;
        DialogueTarget = nextTarget;
        DialogueTargetChanged?.Invoke(DialogueTarget);
    }

    private void OnDisable()
    {
        npcColliders.Clear();
        RefreshDialogueTarget();
    }
}
