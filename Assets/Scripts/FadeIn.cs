using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 전체를 덮는 검은 이미지의 투명도를 서서히 바꿔서 페이드 인/아웃을 만드는 스크립트.
///
/// 사용법: GameScene 안의 아무 오브젝트(빈 오브젝트여도 됨)에 붙인 뒤,
///        Timeline의 Signal Receiver나 버튼 OnClick, 다른 스크립트에서
///        FadeOutScreen() / FadeInScreen() 을 호출하면 됩니다.
///
/// - FadeOutScreen() : 화면이 점점 어두워집니다 (투명 → 검정)
/// - FadeInScreen()  : 화면이 점점 밝아집니다 (검정 → 투명)
///
/// (C#에서는 메서드 이름을 클래스 이름(FadeIn)과 똑같이 지을 수 없어 뒤에 Screen을 붙였습니다)
///
/// 각각 걸리는 시간은 인스펙터의 m_FadeOutTime / m_FadeInTime 으로 따로 조절합니다.
/// </summary>
public class FadeIn : MonoBehaviour
{
    // ── 시간 조절 (인스펙터에서 각각 따로 설정) ──
    [Header("페이드 시간 (초)")]
    public float m_FadeInTime = 1f;    // 밝아지는 데(검정 → 투명) 걸리는 시간
    public float m_FadeOutTime = 1f;   // 어두워지는 데(투명 → 검정) 걸리는 시간

    // 씬에 미리 만들어 둔 전체 화면 이미지를 쓰고 싶으면 여기에 연결하고,
    // 비워두면 Awake에서 검은 전체 화면 이미지를 자동으로 만들어 씁니다.
    [Header("덮개 이미지 (비워두면 자동 생성)")]
    public Image m_FadeImage;

    Color m_Color = Color.black;   // 색을 매번 새로 만들지 않고 알파(a)만 바꿔 재사용
    Coroutine m_Routine;           // 진행 중인 페이드 (새로 호출되면 이걸 멈추고 다시 시작)
    bool m_IsFading;

    public bool IsFading { get { return m_IsFading; } }

    void Awake()
    {
        if (m_FadeImage == null)
            CreateFadeImage();
        else
            m_Color = m_FadeImage.color;   // 인스펙터에서 정한 색(검정이 아닐 수도 있음)을 그대로 씁니다

        // 완전히 투명한 상태라면 굳이 그릴 필요가 없으므로 꺼 둡니다.
        // (전체 화면 이미지를 매 프레임 그리면 눈에 안 보여도 그리기 비용은 그대로 듭니다)
        m_FadeImage.enabled = m_Color.a > 0f;
    }

    // 화면이 점점 밝아짐 (검정 → 투명). Timeline Signal, 버튼 등에서 그대로 호출할 수 있습니다.
    public void FadeInScreen()
    {
        StartFade(0f, m_FadeInTime);
    }

    // 화면이 점점 어두워짐 (투명 → 검정)
    public void FadeOutScreen()
    {
        StartFade(1f, m_FadeOutTime);
    }

    /// <summary>
    /// 지금 알파에서 목표 알파(to)까지 time초 동안 서서히 바꿉니다.
    /// 페이드 도중에 다시 호출되면 이전 페이드를 멈추고 '지금 밝기'에서 새로 시작하므로,
    /// 어두워지는 중에 FadeInScreen()을 불러도 툭 끊기지 않고 자연스럽게 되돌아갑니다.
    /// </summary>
    void StartFade(float to, float time)
    {
        if (m_FadeImage == null)
            return;

        // 이미 목표 상태면 아무것도 하지 않습니다 (불필요한 코루틴 생성 방지)
        if (!m_IsFading && Mathf.Approximately(m_Color.a, to))
            return;

        if (m_Routine != null)
            StopCoroutine(m_Routine);

        m_Routine = StartCoroutine(FadeRoutine(to, time));
    }

    IEnumerator FadeRoutine(float to, float time)
    {
        m_IsFading = true;
        m_FadeImage.enabled = true;   // 페이드 중에는 그려야 하므로 켭니다

        float from = m_Color.a;

        // time이 0 이하면 나눗셈을 하지 않고 곧바로 목표값으로 넘어갑니다 (0으로 나누기 방지)
        if (time > 0f)
        {
            float invTime = 1f / time;   // 나눗셈은 루프 밖에서 한 번만 하고, 안에서는 곱셈만 씁니다
            float t = 0f;
            while (t < time)
            {
                t += Time.deltaTime;
                ApplyAlpha(Mathf.Lerp(from, to, t * invTime));
                yield return null;       // 다음 프레임까지 대기
            }
        }

        ApplyAlpha(to);   // 마지막 프레임 오차 없이 정확히 목표값으로 맞춤

        // 완전히 투명해졌으면 그리기를 꺼서 매 프레임 낭비되는 화면 그리기를 없앱니다.
        if (to <= 0f)
            m_FadeImage.enabled = false;

        m_IsFading = false;
        m_Routine = null;
    }

    // 색 구조체의 알파만 바꿔서 이미지에 넣습니다. (Color는 구조체라 쓰레기 메모리가 생기지 않습니다)
    void ApplyAlpha(float a)
    {
        m_Color.a = a;
        m_FadeImage.color = m_Color;
    }

    // 항상 가장 위에 그려지는 전체 화면 검은 이미지를 코드로 만듭니다.
    // (씬에 UI를 미리 배치하지 않아도 바로 쓸 수 있게 하기 위함 — HudUI의 페이드와 같은 방식)
    void CreateFadeImage()
    {
        GameObject canvasGo = new GameObject("FadeCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;   // HudUI의 페이드 캔버스(999)보다 위에 오도록

        GameObject imgGo = new GameObject("FadeImage");
        imgGo.transform.SetParent(canvasGo.transform, false);
        m_FadeImage = imgGo.AddComponent<Image>();
        m_Color.a = 0f;                      // 처음에는 투명 (화면이 보이는 상태)
        m_FadeImage.color = m_Color;
        m_FadeImage.raycastTarget = false;   // 클릭을 가로막지 않고, UI 클릭 검사 대상에서도 빠집니다

        // 화면 전체를 덮도록 네 모서리에 붙입니다 (해상도가 바뀌어도 항상 꽉 참)
        RectTransform rt = m_FadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
