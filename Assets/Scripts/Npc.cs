using UnityEngine;

/// <summary>
/// 把场景中的 NPC 物体与它的个体数据绑定起来。
/// </summary>
[DisallowMultipleComponent]
public class Npc : MonoBehaviour
{
    [SerializeField] private NpcData data = new NpcData();
    public NpcData Data => data;
}
