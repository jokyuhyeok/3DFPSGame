using UnityEngine;

// 플레이어 몸의 좌우 회전 (좌우 회전의 유일한 기준. 카메라는 이 값을 따라간다)
// [변경] - 오른쪽 버튼 없이 항상 회전 (오른쪽 버튼은 폭탄 전용)
//        - 커서 조준 시점에서는 커서 쪽을 바라본다.
public class PlayerRotate : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 200f;
    [SerializeField] private CameraViewSwitcher _cameraView; // [추가]

    private float _accumulationX = 0f;

    private void Start()
    {
        // [추가] 씬에 놓인 시작 방향에서 출발 (기존: 0에서 출발해서 첫 회전 때 몸이 튀거나 카메라와 어긋날 수 있었음)
        _accumulationX = transform.eulerAngles.y;
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (CursorManager.Instance.IsUIMode) return; // [추가] Alt 누르는 동안은 회전 안 함

        // [추가] 커서 조준 시점: 커서를 바라본다.
        if (_cameraView.IsCursorAim)
        {
            FaceCursor();
            return;
        }

        // [삭제] if (!Input.GetMouseButton(1)) return; → 항상 회전
        float mouseX = Input.GetAxis("Mouse X");
        _accumulationX += mouseX * _rotationSpeed * Time.deltaTime;

        transform.eulerAngles = new Vector3(0, _accumulationX, 0);
    }

    // [추가] 커서가 가리키는 곳으로 몸을 돌린다. (좌우만)
    private void FaceCursor()
    {
        Vector3 aimPoint = _cameraView.GetCursorAimPoint(transform.position);
        Vector3 direction = aimPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(direction);

        // 중앙 조준 시점으로 돌아갔을 때 지금 바라보는 방향에서 이어지도록 동기화
        _accumulationX = transform.eulerAngles.y;
    }
}