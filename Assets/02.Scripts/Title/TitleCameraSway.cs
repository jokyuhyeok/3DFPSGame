using UnityEngine;

// 숨어서 들고 찍는 느낌을 주는 카메라 흔들림
// 시작 위치·회전을 기준으로 펄린 노이즈만큼 아주 조금씩 움직인다. (숨 쉬듯 느리게)
// Shake()를 부르면 잠깐 세게 흔들린다. (타이틀 등장 진동)
public class TitleCameraSway : MonoBehaviour
{
    [SerializeField] private float _positionAmount = 0.03f; // 위치 흔들림 (미터)
    [SerializeField] private float _rotationAmount = 0.6f; // 회전 흔들림 (도)
    [SerializeField] private float _speed = 0.35f;

    private Vector3 _startPosition;
    private Quaternion _startRotation;

    private float _shakeAmount = 0f;
    private float _shakeDuration = 0f;
    private float _shakeTimer = 0f;

    private void Start()
    {
        _startPosition = transform.localPosition;
        _startRotation = transform.localRotation;
    }

    public void Shake(float amount, float duration)
    {
        _shakeAmount = amount;
        _shakeDuration = duration;
        _shakeTimer = duration;
    }

    private void LateUpdate()
    {
        float time = Time.time * _speed;

        // 축마다 다른 시드를 줘서 서로 다르게 흔들리게 한다. (PerlinNoise는 0~1 → -1~1로 바꾼다)
        Vector3 offset = new Vector3(
            Mathf.PerlinNoise(time, 0f) * 2f - 1f,
            Mathf.PerlinNoise(0f, time + 10f) * 2f - 1f,
            0f) * _positionAmount;

        Vector3 angles = new Vector3(
            Mathf.PerlinNoise(time + 20f, 5f) * 2f - 1f,
            Mathf.PerlinNoise(5f, time + 30f) * 2f - 1f,
            (Mathf.PerlinNoise(time + 40f, 15f) * 2f - 1f) * 0.5f) * _rotationAmount;

        // 진동: 남은 시간에 비례해 잦아드는 무작위 흔들림
        if (_shakeTimer > 0f)
        {
            _shakeTimer -= Time.deltaTime;
            float strength = Mathf.Max(0f, _shakeTimer / _shakeDuration);
            offset += Random.insideUnitSphere * (_shakeAmount * strength);
        }

        transform.localPosition = _startPosition + offset;
        transform.localRotation = _startRotation * Quaternion.Euler(angles);
    }
}
