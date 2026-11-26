using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>负责显示一条对话，不处理对话推进逻辑。</summary>
public class DialogueView : MonoBehaviour
{
    private const float CharactersPerSecond = 30f;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private AnswerChoiceView choicePrefab;
    [SerializeField] private RectTransform choicesParent;
    private readonly List<AnswerChoiceView> choices = new List<AnswerChoiceView>();
    public event Action<int> ChoiceSelected;
    public bool IsTyping { get; private set; }
    private float typingElapsed;
    private int characterCount;

    public void Show(string speakerName, string content, IReadOnlyList<DialogueOption> options = null)
    {
        nameText.text = speakerName;
        // 保留完整文本的排版，只逐步增加可见字符，避免换行随着打字跳动。
        contentText.maxVisibleCharacters = 0;
        contentText.text = content;
        gameObject.SetActive(true);
        contentText.ForceMeshUpdate();
        characterCount = contentText.textInfo.characterCount;
        typingElapsed = 0f;
        IsTyping = characterCount > 0;
        ShowChoices(options);
    }

    private void Update()
    {
        AdvanceTyping(Time.unscaledDeltaTime);
    }

    private void AdvanceTyping(float deltaTime)
    {
        if (!IsTyping) return;
        // 累计时间而非每帧增加一个字，保证不同帧率下都为每秒 30 字。
        typingElapsed += deltaTime;
        int visibleCount = Mathf.Min(characterCount,
            Mathf.FloorToInt(typingElapsed * CharactersPerSecond + 0.0001f));
        contentText.maxVisibleCharacters = visibleCount;
        if (visibleCount >= characterCount) IsTyping = false;
    }

    private void OnDisable()
    {
        StopTyping();
    }

    private void StopTyping()
    {
        IsTyping = false;
        typingElapsed = 0f;
    }

    private void ShowChoices(IReadOnlyList<DialogueOption> options)
    {
        foreach (AnswerChoiceView choice in choices) choice.gameObject.SetActive(false);
        if (choicesParent == null || choicePrefab == null) return;

        int count = 0;
        float offset = 0f;
        if (options != null)
        {
            foreach (DialogueOption option in options)
            {
                if (option == null || string.IsNullOrWhiteSpace(option.text)) continue;
                if (count == choices.Count)
                {
                    AnswerChoiceView newChoice = Instantiate(choicePrefab, choicesParent, false);
                    int choiceNumber = count + 1;
                    newChoice.Selected += () => ChoiceSelected?.Invoke(choiceNumber);
                    choices.Add(newChoice);
                }

                AnswerChoiceView choice = choices[count++];
                RectTransform rect = (RectTransform)choice.transform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                float height = choice.Show($"{count}. {option.text}", choicesParent.rect.width);
                rect.sizeDelta = new Vector2(0f, height);
                rect.anchoredPosition = new Vector2(0f, -offset);
                offset += height + 12f;
            }
        }
        choicesParent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            count == 0 ? 0f : offset - 12f);
        choicesParent.gameObject.SetActive(count > 0);
    }

    public void Hide()
    {
        StopTyping();
        gameObject.SetActive(false);
    }
}
