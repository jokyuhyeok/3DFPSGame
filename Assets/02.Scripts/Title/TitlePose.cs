using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// 타이틀 연출용 캐릭터 포즈
// 애니메이터 컨트롤러 없이 클립 하나를 바로 재생한다. (speed가 0이면 그 자리에서 멈춘 포즈가 된다)
// 반복 클립은 계속 돌고, 반복이 아닌 클립은 끝 동작에서 멈춰 있는다.
[RequireComponent(typeof(Animator))]
public class TitlePose : MonoBehaviour
{
    [Header("시작 포즈")]
    [SerializeField] private AnimationClip _clip;

    [SerializeField, Range(0f, 1f)] private float _normalizedTime = 0f; // 클립의 어느 지점에서 시작할지
    [SerializeField] private float _speed = 1f;

    private Animator _animator;
    private PlayableGraph _graph;
    private AnimationPlayableOutput _output;
    private AnimationClipPlayable _playable;
    private bool _hasPlayed = false;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _animator.applyRootMotion = false; // 제자리에서 포즈만 취한다.
        _animator.fireEvents = false; // 클립에 붙은 공격 판정 이벤트(HitEvent 등)는 연출에 필요 없다.

        _graph = PlayableGraph.Create($"{name}_TitlePose");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        _output = AnimationPlayableOutput.Create(_graph, "Pose", _animator);
    }

    private void Start()
    {
        // 다른 스크립트가 먼저 Play를 불렀으면 시작 포즈로 덮어쓰지 않는다.
        if (!_hasPlayed && _clip != null) Play(_clip, _normalizedTime, _speed);
    }

    public void Play(AnimationClip clip, float normalizedTime = 0f, float speed = 1f)
    {
        if (_playable.IsValid()) _playable.Destroy();

        _playable = AnimationClipPlayable.Create(_graph, clip);
        _playable.SetTime(clip.length * normalizedTime);
        _playable.SetSpeed(speed);
        _output.SetSourcePlayable(_playable);

        if (!_graph.IsPlaying()) _graph.Play();
        _graph.Evaluate(); // 다음 프레임을 기다리지 않고 바로 포즈를 적용한다. (컷이 바뀌는 순간 T포즈가 보이지 않도록)
        _hasPlayed = true;
    }

    private void OnDestroy()
    {
        if (_graph.IsValid()) _graph.Destroy();
    }
}
