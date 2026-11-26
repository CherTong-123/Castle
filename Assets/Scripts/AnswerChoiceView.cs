using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>显示回答文字，并把鼠标选择通知给对话视图。</summary>
public class AnswerChoiceView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text choiceText;
    public event Action Selected;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            Selected?.Invoke();
    }

    public float Show(string text, float width)
    {
        choiceText.text = text;
        gameObject.SetActive(true);
        return Mathf.Max(64f, choiceText.GetPreferredValues(text, width - 32f, Mathf.Infinity).y + 24f);
    }
}
