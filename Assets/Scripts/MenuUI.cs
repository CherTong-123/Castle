using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主菜单：点“开始游戏”从 Menu（场景 0）切到 SampleScene（场景 1）。
///
/// 场景必须已经加进 File > Build Settings（File > Build Profiles），
/// 并且顺序是 0 = Menu、1 = SampleScene。
/// </summary>
public class MenuUI : MonoBehaviour
{
    [Header("按钮")]
    [Tooltip("“开始游戏”按钮。挂在按钮物体上时可以留空，会自动 GetComponent<Button>()")]
    [SerializeField] private Button startButton;

    [Header("目标场景")]
    [Tooltip("要加载的场景名，必须和 Build Settings 里的名字完全一致（大小写、空格都算）")]
    [SerializeField] private string gameSceneName = "SampleScene";

    [Tooltip("按名字找不到场景时的兜底：用 Build Settings 里的索引加载。0 = Menu，1 = SampleScene")]
    [SerializeField] private int gameSceneBuildIndex = 1;

    private bool isLoading;

    private void Awake()
    {
        // 脚本直接挂在按钮上时，不需要在 Inspector 里拖引用
        if (startButton == null)
        {
            startButton = GetComponent<Button>();
        }
    }

    private void OnEnable()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
        else
        {
            Debug.LogError("[MenuUI] 没有找到“开始游戏”按钮：请把本脚本挂在按钮上，或把按钮拖到 Start Button 字段。");
        }
    }

    private void OnDisable()
    {
        // 和 OnEnable 成对移除，避免按钮被重复注册、点一次跑两遍
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }
    }

    private void OnStartButtonClicked()
    {
        // 防连点：加载过程中再点也不重复切场景
        if (isLoading)
        {
            return;
        }

        isLoading = true;

        // 如果是从暂停状态回来的，先恢复时间，否则新场景是静止的
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(gameSceneName) && Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        if (gameSceneBuildIndex >= 0 && gameSceneBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("[MenuUI] 按名字「" + gameSceneName + "」找不到场景，改用 Build Settings 索引 " + gameSceneBuildIndex + " 加载。");
            SceneManager.LoadScene(gameSceneBuildIndex);
            return;
        }

        // 最常见的原因：场景没加进 Build Settings，或者名字拼错
        Debug.LogError("[MenuUI] 无法加载场景「" + gameSceneName + "」。请检查 File > Build Settings 里是否已添加 SampleScene，且名字完全一致。");
        isLoading = false;
    }
}
