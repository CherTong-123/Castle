using System;
using System.Collections.Generic;
using UnityEngine;

// 一整段对话，保存为一个资源文件
[CreateAssetMenu(
    fileName = "NewDialogue",
    menuName = "Dialogue/对话配置")]
public class DialogueConfig : ScriptableObject
{
    public int startNodeId = 0; // 从哪个节点开始
    public List<DialogueNode> nodes = new List<DialogueNode>();
}

// 对话节点
[Serializable]
public class DialogueNode
{
    public int id; // 节点编号，同一份配置内不能重复
    [TextArea]
    [Tooltip("NPC说的话")] public string npcText; 
    public List<DialogueOption> options = new List<DialogueOption>();
}

// 玩家可以选择的一个回答
[Serializable]
public class DialogueOption
{
    [Tooltip("玩家看到的选项的文字")] public string text; 
    [Tooltip("跳转的结点(-1)为结束")]public int nextNodeId = -1;
}