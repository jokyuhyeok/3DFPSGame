using UnityEngine;

// 마우스 커서 상태 관리
// [변경] 시점에 따라 커서 모드가 달라진다.
// - 중앙 조준 시점: 화면 가운데에 잠그고 숨김 (기존과 동일)
// - 커서 조준 시점: 화면 안에 가두고 보이게 (조준에 사용)
// - LeftAlt 누르는 동안(UI 모드): 자유롭게 보이게 (기존과 동일) → 이때는 사격/회전하지 않는다.
public class CursorManager : MonoBehaviour
{
    private static CursorManager _instance = null; // [추가] 다른 스크립트에서 IsUIMode, SetCursorAimMode 사용
    public static CursorManager Instance => _instance;

    private bool _isCursorAimMode = false; // [추가]
    private bool _isUIMode = false; // [추가] LeftAlt 누르는 중

    public bool IsUIMode => _isUIMode; // [추가] 기존 PlayerGunFire의 Cursor.visible 검사를 대체

    private void Awake()
    {
        _instance = this;
        ApplyCursorState();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            _isUIMode = true;
            ApplyCursorState();
        }
        else if (Input.GetKeyUp(KeyCode.LeftAlt))
        {
            _isUIMode = false;
            ApplyCursorState();
        }
    }

    // [추가] CameraViewSwitcher가 시점을 바꿀 때 호출
    public void SetCursorAimMode(bool isCursorAim)
    {
        _isCursorAimMode = isCursorAim;
        ApplyCursorState();
    }

    // [변경] LockCursor/UnlockCursor → 현재 상태에 맞게 한 곳에서 결정
    private void ApplyCursorState()
    {
        if (_isUIMode)
        {
            Cursor.lockState = CursorLockMode.None; // 자유
            Cursor.visible = true;
        }
        else if (_isCursorAimMode)
        {
            Cursor.lockState = CursorLockMode.Confined; // 게임 화면 밖으로 못 나감
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; // 화면 가운데에 고정
            Cursor.visible = false;
        }
    }
}