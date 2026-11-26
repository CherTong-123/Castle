using System;
using System.Collections.Generic;
/// <summary>
/// 存放每个NPC的数据，标识、名字、嫌疑值、狼人状态
/// </summary>
[Serializable]
public class NpcData 
{
    public string id;
    public string name;
    public int suspicion;
    public bool isWolf;
    public DialogueConfig dialogue;
}