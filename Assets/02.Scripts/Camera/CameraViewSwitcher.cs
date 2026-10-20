using System;
using UnityEngine;

// 역할: V 키를 누를 때마다 1인칭, 3인칭, 숄더뷰, 쿼터뷰, 탑다운, 사이드뷰 순서로 시점을 전환한다.
public class CameraViewSwitcher : MonoBehaviour
{
    // 카메라 시점 종류 정의
    public enum CameraViewType
    {
        FirstPerson, // 1인칭 (FPS)
        ThirdPerson, // 3인칭 (TPS)
        Shoulder, // 숄더뷰
        QuarterView, // 쿼터뷰
        TopDown, // 탑다운
        SideView // 사이드뷰
    }

    [Serializable]
    public struct CameraPreset
    {
        [Header("시점 이름")]
        public CameraViewType viewType;

        [Header("플레이어 기준 상대 위치 및 회전")]
        public Vector3 localPosition; // 플레이어 중심점 기준 카메라의 상대 위치

        public Vector3 localRotation; // 플레이어 기준 카메라의 회전 각도 (Euler Angles)

        [Header("카메라 회전 제어 여부")]
        public bool enableMouseRotate; // 마우스 회전(CameraRotate) 활성화 여부
    }

    [SerializeField] private Transform _target; // 따라갈 대상 (Player)
    [SerializeField] private CameraPreset[] _presets; // 인스펙터에서 설정할 시점 프리셋 목록

    private int _currentIndex = 0; // 현재 시점 인덱스
    private CameraRotate _cameraRotateScript; // 카메라 회전 스크립트 참조

    private void Awake()
    {
        _cameraRotateScript = GetComponent<CameraRotate>();

        if (_target == null)
        {
            GameLogger.LogWarning("CameraViewSwitcher: Target이 지정되지 않았습니다.");
        }
    }

    private void Update()
    {
        // V 키를 누를 때마다 다음 시점으로 순환 변경
        if (Input.GetKeyDown(KeyCode.V))
        {
            CycleNextView();
        }
    }

    private void LateUpdate()
    {
        if (_target == null || _presets == null || _presets.Length == 0)
        {
            return;
        }

        // 현재 선택된 프리셋 정보 가져오기
        CameraPreset currentPreset = _presets[_currentIndex];

        // 1. 위치 변경: 플레이어의 월드 위치 + 플레이어 회전을 반영한 상대 위치로 이동
        transform.position = _target.TransformPoint(currentPreset.localPosition);

        // 2. 회전 변경: 시점별 고정 각도 또는 플레이어 방향 연동
        // (1인칭이나 숄더뷰처럼 마우스 회전이 필요한 경우와 쿼터뷰/탑다운처럼 고정된 각도가 필요한 경우를 분기)
        if (currentPreset.enableMouseRotate)
        {
            if (_cameraRotateScript != null)
            {
                _cameraRotateScript.enabled = true;
            }
        }
        else
        {
            if (_cameraRotateScript != null)
            {
                _cameraRotateScript.enabled = false;
            }

            // 고정 시점인 경우 플레이어 방향을 기준으로 한 상대 회전 적용
            transform.rotation = _target.rotation * Quaternion.Euler(currentPreset.localRotation);
        }
    }

    private void CycleNextView()
    {
        if (_presets == null || _presets.Length == 0)
        {
            return;
        }

        // 인덱스를 1씩 증가시키며 전체 프리셋 개수 안에서 순환 (0 -> 1 -> 2 -> 3 -> 4 -> 5 -> 0)
        _currentIndex = (_currentIndex + 1) % _presets.Length;

        CameraPreset nextPreset = _presets[_currentIndex];
        GameLogger.Log($"시점 변경: {nextPreset.viewType}");
    }
}