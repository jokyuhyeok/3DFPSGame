using UnityEngine;
using System.Collections;

public class CoroutineTest : MonoBehaviour
{
    // 매 초마다 시작!, 1초, 2초, 3초를 센 다음에 "나로호 발사"라는 로그를 출력하고 싶다.

    // 유니티의 코루틴이란
    // 비동기 실행과 제어를 효율적으로 제공하는 기능이다.

    // 1. 비동기 실행
    // 2. 시간/프레임에 따른 실행 제어
    // - IEnumerator: 코루틴 함수의 반환형
    // - StartCoroutine(함수()): 유니티에서 코루틴 함수를 실행하는 방법
    // - yield: 정해진 시간/프레임동안 쉼.

    private void Start()
    {
        Debug.Log("시작");

        // Countdown이라는 메서드를 비동기 방식으로 실행한다.
        // 유니티에서는 비동기로 처리해야 하는 로직을 위해 코루틴을 제공한다. Co + Routine: 협력 동작 (멀티스레드야? 가 오늘의 과제)
        StartCoroutine(Countdown_Coroutine());
        Debug.Log("끝!");
    }

    private IEnumerator Countdown_Coroutine()
    {
        yield return new WaitForSeconds(1f);
        yield return null;
        Debug.Log("1초");
        yield return new WaitForSeconds(1f);
        Debug.Log("2초");
        yield return new WaitForSeconds(1f);
        Debug.Log("3초");
        yield return new WaitForSeconds(1f);
        Debug.Log("나로호 발사!");
    }
}