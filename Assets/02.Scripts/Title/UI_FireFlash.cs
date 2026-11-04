using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 검기가 화면을 덮는 불꽃 연출
// 길게 늘인 노이즈로 불꽃 결 텍스처를 만들어 여러 겹으로 흘려보내고, 검기가 지나간 대각선 자리가 하얗게 타오르게 한다.
// Cover로 화면을 덮고 Reveal로 걷어낸다. 평소에는 꺼져 있어 화면 입력을 막지 않는다.
[RequireComponent(typeof(CanvasGroup))]
public class UI_FireFlash : MonoBehaviour
{
    [Header("구성")]
    [SerializeField] private RawImage[] _fireLayers; // 위로 갈수록 빠르게 흐른다.

    [SerializeField] private RawImage _glow; // 가운데를 가로지르는 하얀 빛줄기

    [Header("불꽃 결")]
    [SerializeField] private Vector2 _scrollSpeed = new Vector2(1.4f, 0.35f); // 초당 UV 이동량

    [SerializeField] private float _streakStretch = 6f; // 클수록 가로로 길게 늘어진 결이 된다.
    [SerializeField] private int _seed = 7;

    [Header("빛줄기")]
    [SerializeField] private float _glowMax = 0.9f; // 가장 밝을 때의 불투명도

    [SerializeField] private float _glowWidth = 0.22f; // 띠 두께 (텍스처 높이 대비)

    private const int TextureWidth = 512;
    private const int TextureHeight = 256;
    private const int GlowTextureHeight = 128;

    // 짙은 빨강 → 주홍 → 주황 → 노랑 → 흰빛
    private static readonly Color[] FireColors =
    {
        new Color(0.70f, 0.02f, 0.00f),
        new Color(0.98f, 0.18f, 0.02f),
        new Color(1.00f, 0.50f, 0.04f),
        new Color(1.00f, 0.86f, 0.22f),
        new Color(1.00f, 0.98f, 0.82f),
    };

    private CanvasGroup _group;
    private Texture2D _fireTexture;
    private Texture2D _glowTexture;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _fireTexture = CreateFireTexture();
        _glowTexture = CreateGlowTexture();

        for (int i = 0; i < _fireLayers.Length; i++)
        {
            _fireLayers[i].texture = _fireTexture;
            // 겹마다 시작 위치를 다르게 해서 같은 무늬가 겹쳐 보이지 않게 한다.
            _fireLayers[i].uvRect = new Rect(i * 0.37f, i * 0.21f, 1f, 1f);
        }

        _glow.texture = _glowTexture;

        SetVisible(0f, 0f);
    }

    private void Update()
    {
        if (_group.alpha <= 0f) return;

        for (int i = 0; i < _fireLayers.Length; i++)
        {
            Rect rect = _fireLayers[i].uvRect;
            rect.position += _scrollSpeed * ((1f + i * 0.7f) * Time.deltaTime);
            _fireLayers[i].uvRect = rect;
        }
    }

    // 불꽃이 확 번지며 화면을 덮는다. 끝날 즈음 빛줄기가 하얗게 타오른다.
    public IEnumerator Cover_Coroutine(float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            SetVisible(1f - (1f - t) * (1f - t), t * t * _glowMax); // 불꽃은 빠르게, 빛줄기는 늦게 차오른다.
            yield return null;
        }

        SetVisible(1f, _glowMax);
    }

    // 빛줄기가 먼저 빠지고, 불꽃이 천천히 걷힌다.
    public IEnumerator Reveal_Coroutine(float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            SetVisible(1f - t * t, _glowMax * Mathf.Clamp01(1f - t * 2.5f));
            yield return null;
        }

        SetVisible(0f, 0f);
    }

    public void Hide()
    {
        SetVisible(0f, 0f);
    }

    private void SetVisible(float alpha, float glowAlpha)
    {
        _group.alpha = alpha;
        _group.blocksRaycasts = alpha > 0f;

        Color color = _glow.color;
        color.a = glowAlpha;
        _glow.color = color;
    }

    // 이음매 없이 반복되는 불꽃 결 텍스처
    private Texture2D CreateFireTexture()
    {
        Texture2D texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        Random.State oldState = Random.state;
        Random.InitState(_seed);
        Vector2 offset = new Vector2(Random.Range(0f, 100f), Random.Range(0f, 100f));
        Random.state = oldState;

        Color[] pixels = new Color[TextureWidth * TextureHeight];
        for (int y = 0; y < TextureHeight; y++)
        {
            for (int x = 0; x < TextureWidth; x++)
            {
                float value = TileableNoise(x, y, offset);
                pixels[y * TextureWidth + x] = EvaluateFire(value);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    // 가운데가 가장 밝고 위아래로 부드럽게 사라지는 가로 띠
    private Texture2D CreateGlowTexture()
    {
        Texture2D texture = new Texture2D(1, GlowTextureHeight, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[GlowTextureHeight];
        for (int y = 0; y < GlowTextureHeight; y++)
        {
            float distance = ((float)y / (GlowTextureHeight - 1) - 0.5f) / _glowWidth;
            float alpha = Mathf.Exp(-distance * distance * 2f);
            pixels[y] = new Color(1f, 0.97f, 0.85f, alpha);
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    // 네 귀퉁이 방향으로 어긋난 노이즈를 거리에 따라 섞으면 가장자리가 서로 이어진다.
    private float TileableNoise(int x, int y, Vector2 offset)
    {
        float u = (float)x / TextureWidth;
        float v = (float)y / TextureHeight;

        float a = Fbm(u, v, offset);
        float b = Fbm(u - 1f, v, offset);
        float c = Fbm(u, v - 1f, offset);
        float d = Fbm(u - 1f, v - 1f, offset);

        float value = Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);

        // 섞인 부분은 대비가 줄어드므로 크게 펼쳐서 진한 결과 밝은 결이 뚜렷하게 갈리게 한다.
        return Mathf.Clamp01((value - 0.5f) * 3f + 0.5f);
    }

    private float Fbm(float u, float v, Vector2 offset)
    {
        float sum = 0f;
        float amplitude = 0.5f;
        float frequency = 4f;
        for (int i = 0; i < 4; i++)
        {
            // 세로 주파수만 크게 해서 가로로 길게 늘어진 결을 만든다.
            sum += amplitude * Mathf.PerlinNoise(offset.x + u * frequency, offset.y + v * frequency * _streakStretch);
            amplitude *= 0.5f;
            frequency *= 2f;
        }

        return sum / 0.9375f; // 진폭 합(0.5+0.25+0.125+0.0625)으로 나눠 0~1로 맞춘다.
    }

    private static Color EvaluateFire(float value)
    {
        float scaled = value * (FireColors.Length - 1);
        int index = Mathf.Min(Mathf.FloorToInt(scaled), FireColors.Length - 2);
        return Color.Lerp(FireColors[index], FireColors[index + 1], scaled - index);
    }

    private void OnDestroy()
    {
        if (_fireTexture != null) Destroy(_fireTexture);
        if (_glowTexture != null) Destroy(_glowTexture);
    }
}
