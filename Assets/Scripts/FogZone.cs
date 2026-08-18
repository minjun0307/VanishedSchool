using UnityEngine;

/// <summary>
/// 소화기를 뿌려서 생긴 연막 하나.
/// 눈에 보이는 연기는 파티클 이펙트 프리팹이 맡고, 이 스크립트는 '판정 범위와 수명'만 관리합니다.
///
/// 흐름: 켜지면 이펙트가 재생됨
///     → m_HoldTime초 뒤 연기 뿜기를 멈춤 (이미 나온 연기는 자기 수명대로 서서히 흩어짐)
///     → 다 흩어질 즈음 오브젝트를 꺼서 풀로 돌아감
///
/// 콜라이더는 일부러 붙이지 않습니다.
/// 몬스터의 시선 검사(MonsterMoves.DetectPlayer의 Physics2D.RaycastAll)가 레이어 마스크 없이
/// 모든 콜라이더를 훑기 때문에, 연막에 콜라이더가 있으면 벽처럼 시야를 가로막아 버립니다.
/// "몬스터가 연막 안에 있는가" 판정은 FogMgr가 위치와 반지름만으로 계산합니다.
/// </summary>
public class FogZone : MonoBehaviour
{
    float m_Radius;     // 감지 차단 판정에 쓰는 반지름 (월드 단위)
    float m_HoldTime;   // 연기를 계속 뿜는 시간
    float m_FadeTime;   // 뿜기를 멈춘 뒤 남은 연기가 흩어지기를 기다리는 시간
    float m_Elapsed;    // 켜진 뒤 지난 시간
    bool m_Stopped;     // 뿜기를 이미 멈췄는지

    GameObject m_Fx;                 // 재생 중인 연기 이펙트 (FogMgr가 만들어 넘겨줍니다)
    ParticleSystem[] m_Particles;    // 그 이펙트 안의 파티클들 (뿜기를 멈출 때 사용)

    public float Radius { get { return m_Radius; } }

    // 풀에서 꺼내 켤 때마다 호출합니다.
    // 위치는 FogMgr가 이 오브젝트에 먼저 잡아두고, 이펙트도 자식으로 붙여서 넘겨줍니다.
    public void Activate(float radius, float holdTime, float fadeTime, GameObject fx)
    {
        m_Radius = Mathf.Max(0.01f, radius);
        m_HoldTime = Mathf.Max(0f, holdTime);
        m_FadeTime = Mathf.Max(0f, fadeTime);
        m_Elapsed = 0f;
        m_Stopped = false;

        m_Fx = fx;
        // 이펙트가 없어도(프리팹 연결 실패) 감지 차단은 그대로 동작해야 하므로 null을 허용합니다.
        m_Particles = fx != null ? fx.GetComponentsInChildren<ParticleSystem>(true) : null;

        gameObject.SetActive(true);   // 여기서 켜지면서 파티클이 재생을 시작합니다
    }

    void Update()
    {
        m_Elapsed += Time.deltaTime;

        // ① 유지 시간이 끝나면 연기 뿜기를 멈춥니다.
        //    이미 뿜어져 나온 연기는 각자 수명이 다할 때까지 옅어지며 흩어집니다 = "서서히 제거"
        if (!m_Stopped && m_Elapsed >= m_HoldTime)
        {
            m_Stopped = true;
            StopEmitting();
        }

        // ② 남은 연기까지 흩어질 시간이 지나면 감지 차단을 끝내고 풀로 돌아갑니다.
        if (m_Elapsed >= m_HoldTime + m_FadeTime)
            gameObject.SetActive(false);   // OnDisable에서 이펙트까지 정리됩니다
    }

    // 새 연기를 더 뿜지 않게만 하고, 이미 나온 연기는 건드리지 않습니다.
    void StopEmitting()
    {
        if (m_Particles == null)
            return;

        for (int i = 0; i < m_Particles.Length; i++)
        {
            // 이펙트 프리팹이 스스로 사라졌을 수 있으므로 매번 확인합니다.
            if (m_Particles[i] != null)
                m_Particles[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    // 꺼질 때는 어떤 경로로 꺼졌든(수명 종료, FogMgr.ClearAll 등) 이펙트를 함께 치웁니다.
    // 이펙트는 켤 때마다 새로 만들어 붙이므로, 여기서 지우지 않으면 계속 쌓입니다.
    void OnDisable()
    {
        if (m_Fx != null)
            Destroy(m_Fx);

        m_Fx = null;
        m_Particles = null;
    }

    // 씬 뷰에서 이 오브젝트를 선택하면 실제 감지 차단 범위가 원으로 보입니다.
    // (보이는 연기 크기와 이 원을 맞추려면 FogMgr의 이펙트 크기 배율을 조절하세요)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, m_Radius);
    }
}
