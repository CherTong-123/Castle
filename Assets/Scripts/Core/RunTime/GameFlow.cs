using System;
using UnityEngine;
/// <summary>
/// 管理游戏状态。
/// </summary>
[DisallowMultipleComponent]
public class GameFlow : MonoBehaviour
{
    public GameState CurrentState { get; private set; } = GameState.Playing;
    public bool IsPlaying => CurrentState == GameState.Playing;
    /// <summary>
    /// 状态切换完成后触发，参数依次为旧状态、新状态。
    /// 初始状态为 Playing，初始化时不触发此事件。
    /// </summary>
    public event Action<GameState, GameState> StateChanged;
    private void ChangeState(GameState nextState)
    {
        if (CurrentState == nextState) return;
        GameState previousState = CurrentState;
        CurrentState = nextState;
        switch (CurrentState)
        {
            case GameState.Playing:
                Time.timeScale = 1f; break;
            case GameState.Paused:
                Time.timeScale = 0f; break;
            case GameState.GameOver:
                Time.timeScale = 1f; break;
            default:
                break;
        }
        StateChanged?.Invoke(previousState, nextState);
    }
    public void Pause()
    {
        if (CurrentState == GameState.Playing) ChangeState(GameState.Paused);
    }
    public void Resume()
    {
        if (CurrentState == GameState.Paused) ChangeState(GameState.Playing);
    }
    public void EndGame()
    {
        if (CurrentState == GameState.Playing || CurrentState == GameState.Paused)
            ChangeState(GameState.GameOver);
    }
}