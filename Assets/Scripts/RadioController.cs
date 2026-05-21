using UnityEngine;

/// <summary>
/// 收音机控制器 —— 玩家按 E 交互时开始播放音频，退出交互后继续播放。
/// 挂载到 Radio 物体上（需同时挂载 InteractableObject 和 AudioSource）。
/// </summary>
public class RadioController : InteractableObject
{
    [Header("Audio")]
    [Tooltip("AudioSource 组件，若为空则自动从同一物体获取")]
    public AudioSource audioSource;

    [Tooltip("退出交互后音频是否循环播放")]
    public bool loopAfterInteract = true;

    // 重新声明 description 以在 Inspector 中显示
    [TextArea]
    public new string description;

    private bool hasPlayed;

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public override void OnStartInteract()
    {
        if (audioSource == null || hasPlayed)
            return;

        // 交互开始时播放音频
        audioSource.loop = loopAfterInteract;
        audioSource.Play();
        hasPlayed = true;
    }

    public override void OnStopInteract()
    {
        // 退出交互时不做任何事 —— 音频继续播放
        // 如果你想退出时停止，取消下面的注释：
        // if (audioSource != null) audioSource.Stop();
    }
}
