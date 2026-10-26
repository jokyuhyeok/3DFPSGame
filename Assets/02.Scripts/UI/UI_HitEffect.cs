using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 플레이어가 맞으면 화면을 붉게 번쩍였다가 서서히 사라지게 한다.
[RequireComponent(typeof(Image))]
public class UI_HitEffect : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private float _maxAlpha = 0.4f; // 번쩍일 때의 진하기 (0~1)
    [SerializeField] private float _fadeTime = 0.3f; // 사라지는 데 걸리는 시간 (초)

    private Image _image;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        _image = GetComponent<Image>();
        SetAlpha(0f); // 처음에는 보이지 않는다.
    }

    private void OnEnable()
    {
        if (_player != null)
        {
            _player.OnDamaged += Play;
        }
    }

    private void OnDisable()
    {
        if (_player != null)
        {
            _player.OnDamaged -= Play;
        }
    }

    // 번쩍인다. 번쩍이는 중에 또 부르면 처음부터 다시 번쩍인다.
    public void Play()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        _fadeCoroutine = StartCoroutine(Fade_Coroutine());
    }

    private IEnumerator Fade_Coroutine()
    {
        float elapsed = 0f;
        while (elapsed < _fadeTime)
        {
            // 진하게 시작해서 시간이 지날수록 옅어진다.
            SetAlpha(Mathf.Lerp(_maxAlpha, 0f, elapsed / _fadeTime));
            elapsed += Time.deltaTime;
            yield return null; // 다음 프레임까지 쉰다.
        }

        SetAlpha(0f);
        _fadeCoroutine = null;
    }

    private void SetAlpha(float alpha)
    {
        Color color = _image.color; // 색을 꺼내서
        color.a = alpha; // 알파만 바꾸고
        _image.color = color; // 다시 넣는다.
    }
}