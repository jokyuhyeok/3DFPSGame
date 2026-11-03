using System.Collections;
using UnityEngine;

// 버튼을 누르면 젤리처럼 일렁이게 한다.
// 가로와 세로가 서로 반대로 늘었다 줄었다 하면서 점점 잦아든다.
public class UI_ButtonWobble : MonoBehaviour
{
    [SerializeField] private float _duration = 0.6f;
    [SerializeField] private float _amount = 0.15f; // 처음 일렁임 크기 (0.15 = 15%)
    [SerializeField] private float _frequency = 18f; // 초당 흔들리는 빠르기 (라디안)

    private Vector3 _baseScale;
    private Coroutine _wobbleCoroutine = null;

    public float Duration => _duration;

    private void Awake()
    {
        _baseScale = transform.localScale;
    }

    public void Play()
    {
        if (_wobbleCoroutine != null) StopCoroutine(_wobbleCoroutine);
        _wobbleCoroutine = StartCoroutine(Wobble_Coroutine());
    }

    private IEnumerator Wobble_Coroutine()
    {
        float time = 0f;
        while (time < _duration)
        {
            time += Time.unscaledDeltaTime;

            float damping = 1f - time / _duration; // 시간이 지날수록 0으로 잦아든다.
            float wave = Mathf.Sin(time * _frequency) * _amount * damping;
            transform.localScale = new Vector3(_baseScale.x * (1f + wave), _baseScale.y * (1f - wave), _baseScale.z);

            yield return null;
        }

        transform.localScale = _baseScale;
        _wobbleCoroutine = null;
    }
}
