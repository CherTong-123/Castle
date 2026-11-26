using System.Collections.Generic;
using UnityEngine;

/// <summary>按 E 开始对话，使用数字键或鼠标选择回答，按 nextNodeId 跳转。</summary>
public class DialogueSystem : MonoBehaviour
{
    [SerializeField] private PlayerInteract playerInteract;
    [SerializeField] private DialogueView dialoguePrefab;
    [SerializeField] private RectTransform dialogueParent;
    [SerializeField] private GameObject identityPrefab;

    public bool IsInDialogue { get; private set; }
    public bool CanStartDialogue => target != null && target.Data != null && !IsInDialogue;

    private Npc target;
    private DialogueConfig currentDialogue;
    private int nodeIndex;
    private DialogueView dialogueView;
    private PlayerMove playerMovement;
    private bool waitingForStep;
    private GameObject identityView;
    private readonly List<DialogueOption> availableOptions = new List<DialogueOption>();
    private static readonly KeyCode[] numberKeys = { KeyCode.Alpha1, KeyCode.Alpha2,
        KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
        KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9 };
    private static readonly KeyCode[] keypadKeys = { KeyCode.Keypad1, KeyCode.Keypad2,
        KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6,
        KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9 };

    private void OnEnable()
    {
        if (playerInteract == null)
        {
            Debug.LogError("DialogueSystem 尚未绑定 PlayerInteract。", this);
            return;
        }

        playerMovement = playerInteract.GetComponent<PlayerMove>();
        playerInteract.DialogueTargetChanged += SetDialogueTarget;
        SetDialogueTarget(playerInteract.DialogueTarget);
    }

    private void Update()
    {
        RefreshIdentity();
        if (waitingForStep)
        {
            if (target == null || !target.isActiveAndEnabled ||
                playerMovement == null || !playerMovement.isActiveAndEnabled)
                CancelPendingDialogue();
            else if (!playerMovement.IsMoving)
                StartDialogue();
            return;
        }
        if (!IsInDialogue)
        {
            if (target != null && Input.GetKeyDown(KeyCode.E)) StartDialogue();
            return;
        }
        if (target == null || !target.isActiveAndEnabled)
        {
            EndDialogue("对话目标已失效");
            return;
        }
        if (availableOptions.Count > 0)
        {
            for (int i = 0; i < availableOptions.Count && i < numberKeys.Length; i++)
            {
                if (Input.GetKeyDown(numberKeys[i]) || Input.GetKeyDown(keypadKeys[i]))
                {
                    SelectChoice(i + 1);
                    return;
                }
            }
            return; // 有选项时等待选择，E 不跳过分支。
        }
        if (Input.GetKeyDown(KeyCode.E)) PrintNextLine();
    }

    private void SetDialogueTarget(Npc npc)
    {
        CancelPendingDialogue();
        if (IsInDialogue) EndDialogue("离开对话区域");
        target = npc;
        if (CanStartDialogue)
            Debug.Log($"进入 {target.Data.name} 的对话区域，按 E 开始对话。", this);
        RefreshIdentity();
    }

    private void RefreshIdentity()
    {
        bool visible = CanStartDialogue && target.isActiveAndEnabled && !waitingForStep;
        if (visible && identityView == null && identityPrefab != null && dialogueParent != null)
            identityView = Instantiate(identityPrefab, dialogueParent, false);
        if (identityView != null && identityView.activeSelf != visible)
            identityView.SetActive(visible);
    }

    public void StartDialogue()
    {
        if (!CanStartDialogue) return;

        if (playerMovement != null && playerMovement.isActiveAndEnabled && playerMovement.IsMoving)
        {
            waitingForStep = true;
            playerMovement.SetDialoguePending(true);
            RefreshIdentity();
            return; // 继续走完当前格，站定后再真正打开对话框。
        }
        CancelPendingDialogue();

        NpcData data = target.Data;
        currentDialogue = data.dialogue;
        if (currentDialogue == null || currentDialogue.nodes == null ||
            currentDialogue.nodes.Count == 0)
        {
            Debug.LogWarning("这个 NPC 没有可播放的对话配置。", this);
            currentDialogue = null;
            return;
        }

        // 编号是节点身份，与节点在列表中的位置无关。
        var nodeIds = new HashSet<int>();
        foreach (DialogueNode node in currentDialogue.nodes)
        {
            if (node != null && !nodeIds.Add(node.id))
            {
                Debug.LogWarning($"对话配置存在重复节点编号：{node.id}。", this);
                currentDialogue = null;
                return;
            }
        }
        nodeIndex = currentDialogue.nodes.FindIndex(node =>
            node != null && node.id == currentDialogue.startNodeId);
        if (nodeIndex < 0)
        {
            Debug.LogWarning("对话配置找不到 startNodeId 对应的节点。", this);
            currentDialogue = null;
            return;
        }

        if (dialogueView == null)
        {
            if (dialoguePrefab == null || dialogueParent == null)
            {
                Debug.LogError("DialogueSystem 尚未绑定对话预制体或 Canvas。", this);
                currentDialogue = null;
                return;
            }
            dialogueView = Instantiate(dialoguePrefab, dialogueParent, false);
            dialogueView.ChoiceSelected += SelectChoice;
            dialogueView.Hide();
        }

        IsInDialogue = true;
        RefreshIdentity();
        if (playerMovement != null) playerMovement.SetDialogueLocked(true);
        Debug.Log($"开始与 {data.name} 对话。", this);
        ShowNode(nodeIndex);
    }

    /// <summary>没有回答选项的节点，按 E 继续列表中的下一句。</summary>
    public void PrintNextLine()
    {
        if (!IsInDialogue || availableOptions.Count > 0) return;
        if (target == null || !target.isActiveAndEnabled || currentDialogue == null)
        {
            EndDialogue("对话目标已失效");
            return;
        }

        for (int nextIndex = nodeIndex + 1; nextIndex < currentDialogue.nodes.Count; nextIndex++)
        {
            if (currentDialogue.nodes[nextIndex] == null) continue;
            ShowNode(nextIndex);
            return;
        }
        EndDialogue("播放完毕");
    }

    /// <summary>选择屏幕上从 1 开始编号的回答。</summary>
    public void SelectChoice(int choiceNumber)
    {
        if (!IsInDialogue || choiceNumber < 1 || choiceNumber > availableOptions.Count) return;
        if (target == null || !target.isActiveAndEnabled || currentDialogue == null)
        {
            EndDialogue("对话目标已失效");
            return;
        }

        DialogueOption option = availableOptions[choiceNumber - 1];
        if (option.nextNodeId == -1)
        {
            Debug.Log($"Player：{option.text}", this);
            EndDialogue("选择结束对话");
            return;
        }

        int nextIndex = currentDialogue.nodes.FindIndex(node =>
            node != null && node.id == option.nextNodeId);
        if (nextIndex < 0)
        {
            Debug.LogWarning($"回答指向的节点不存在：{option.nextNodeId}。", this);
            return;
        }
        Debug.Log($"Player：{option.text}", this);
        ShowNode(nextIndex);
    }

    private void ShowNode(int index)
    {
        nodeIndex = index;
        DialogueNode node = currentDialogue.nodes[index];
        availableOptions.Clear();
        if (node.options != null)
        {
            foreach (DialogueOption option in node.options)
                if (option != null && !string.IsNullOrWhiteSpace(option.text))
                    availableOptions.Add(option);
        }
        dialogueView.Show(target.Data.name, node.npcText, availableOptions);
        Debug.Log($"{target.Data.name}：{node.npcText}", this);
    }

    private void CancelPendingDialogue()
    {
        waitingForStep = false;
        if (playerMovement != null) playerMovement.SetDialoguePending(false);
    }

    private void EndDialogue(string reason)
    {
        CancelPendingDialogue();
        IsInDialogue = false;
        if (playerMovement != null) playerMovement.SetDialogueLocked(false);
        currentDialogue = null;
        nodeIndex = 0;
        availableOptions.Clear();
        if (dialogueView != null) dialogueView.Hide();
        RefreshIdentity();
        Debug.Log($"对话结束：{reason}。", this);
    }

    private void OnDisable()
    {
        CancelPendingDialogue();
        if (playerInteract != null)
            playerInteract.DialogueTargetChanged -= SetDialogueTarget;
        if (IsInDialogue) EndDialogue("对话系统已关闭");
        if (identityView != null) identityView.SetActive(false);
        target = null;
    }

    private void OnDestroy()
    {
        CancelPendingDialogue();
        if (IsInDialogue) EndDialogue("对话系统已销毁");
        if (identityView != null) Destroy(identityView);
        // 视图挂在 Canvas 下，系统单独销毁时也要清理。
        if (dialogueView != null)
        {
            dialogueView.ChoiceSelected -= SelectChoice;
            Destroy(dialogueView.gameObject);
        }
    }
}
