using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Presentation state switching for two independent Figma UI hierarchies.</summary>
public sealed class FlowerEscapeUiController : MonoBehaviour
{
    public enum UiState { Gear, Combat }
    public static FlowerEscapeUiController Instance { get; private set; }
    public UiState CurrentState { get; private set; }
    [SerializeField] private UiState initialState = UiState.Gear;
    [SerializeField] private GameObject gearRoot;
    [SerializeField] private GameObject combatRoot;
    [SerializeField] private Button startButton;

    private void Awake()
    {
        Instance = this;
        if (startButton != null) startButton.onClick.AddListener(ShowCombat);
        SetState(initialState);
    }

    private void OnDestroy()
    {
        if (startButton != null) startButton.onClick.RemoveListener(ShowCombat);
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.gKey.wasPressedThisFrame) ShowGear();
        if (keyboard.cKey.wasPressedThisFrame) ShowCombat();
#endif
    }

    public void ShowGear() => SetState(UiState.Gear);
    public void ShowCombat() => SetState(UiState.Combat);
    public void SetState(UiState state)
    {
        CurrentState = state;
        if (gearRoot != null) gearRoot.SetActive(state == UiState.Gear);
        if (combatRoot != null) combatRoot.SetActive(state == UiState.Combat);
    }

    [ContextMenu("Preview/Gear")]
    private void PreviewGear() => ShowGear();
    [ContextMenu("Preview/Combat")]
    private void PreviewCombat() => ShowCombat();
}
