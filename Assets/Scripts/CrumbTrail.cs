using System.Collections.Generic;
using UnityEngine;

// 플레이어가 지나간 자리에 남는 '과자' 흔적 (헨젤과 그레텔 방식)
// - 플레이어 오브젝트에 붙여서 사용합니다.
// - 일정 거리마다 현재 좌표를 하나씩 기록해두고, 추격 중인 몬스터가 이 좌표들을
//   순서대로 밟으며 플레이어가 실제로 걸었던 경로를 되짚어 옵니다.
// - 방 안에 있을 때·숨어 있을 때·조작이 잠겨 있을 때는 기록하지 않고,
//   캐비넷에 숨거나 순간이동(방/계단)이 일어나면 흔적을 전부 지웁니다.
public class CrumbTrail : MonoBehaviour
{
    // 과자 한 개 = 좌표 + 그 좌표가 있던 층 + 떨어뜨린 시각
    struct Crumb
    {
        public Vector2 m_Pos;
        public int m_Floor;
        public float m_Time;
    }

    [Tooltip("이 거리만큼 움직일 때마다 과자를 하나 떨어뜨립니다. 몬스터의 도착 판정보다 커야 합니다.")]
    public float m_DropDistance = 0.6f;
    [Tooltip("과자가 남아 있는 시간(초). 이 시간이 지나면 냄새가 사라지듯 오래된 것부터 지워집니다.")]
    public float m_Lifetime = 12f;
    [Tooltip("동시에 남길 수 있는 과자의 최대 개수입니다.")]
    public int m_MaxCount = 60;
    [Tooltip("Scene 뷰에 과자 위치를 점으로 그려 확인합니다. (게임 화면에는 보이지 않습니다)")]
    public bool m_DrawGizmos = true;

    readonly List<Crumb> m_Crumbs = new List<Crumb>();   // index 0 = 가장 오래된 과자
    PlayerMove m_PlayerMove;

    void Awake()
    {
        m_PlayerMove = GetComponent<PlayerMove>();
    }

    void Update()
    {
        RemoveExpired();

        if (!CanDrop())
            return;

        Vector2 pos = transform.position;

        // 마지막 과자에서 m_DropDistance 이상 움직였을 때만 새로 떨어뜨립니다.
        if (m_Crumbs.Count > 0 && Vector2.Distance(m_Crumbs[m_Crumbs.Count - 1].m_Pos, pos) < m_DropDistance)
            return;

        m_Crumbs.Add(new Crumb { m_Pos = pos, m_Floor = GetPlayerFloor(), m_Time = Time.time });

        // 개수 제한 — 넘치면 가장 오래된 것부터 버립니다.
        while (m_Crumbs.Count > m_MaxCount)
            m_Crumbs.RemoveAt(0);
    }

    // 지금 과자를 떨어뜨려도 되는 상황인지 확인합니다.
    // 방 안(별도 맵)·캐비넷 안·조작 잠금(페이드 중) 상태에서는 기록하지 않습니다.
    bool CanDrop()
    {
        if (m_PlayerMove == null || !m_PlayerMove.m_IsActive)
            return false;
        if (Cabinet.Hiding != null)
            return false;
        if (m_PlayerMove.CurrentRoom != Room.None)
            return false;

        return true;
    }

    // 수명이 다한 과자를 앞에서부터 제거합니다. (앞쪽이 항상 더 오래된 것이라 정렬이 필요 없습니다)
    void RemoveExpired()
    {
        while (m_Crumbs.Count > 0 && Time.time - m_Crumbs[0].m_Time > m_Lifetime)
            m_Crumbs.RemoveAt(0);
    }

    int GetPlayerFloor()
    {
        return m_PlayerMove.CurrentFloor == Floor.F1 ? 1 : m_PlayerMove.CurrentFloor == Floor.F3 ? 3 : 2;
    }

    // 흔적을 전부 지웁니다. (캐비넷에 숨었을 때, 방/계단으로 순간이동했을 때)
    public void Clear()
    {
        m_Crumbs.Clear();
    }

    /// <summary>
    /// 몬스터가 다음으로 향해야 할 과자 좌표를 돌려줍니다.
    /// 몬스터와 같은 층의 과자 중 가장 가까운 것을 찾고, 그보다 오래된 과자는 이미 지나온
    /// 구간이므로 버립니다. (그러지 않으면 몬스터가 줄의 맨 뒤로 되돌아가 버립니다)
    /// </summary>
    public bool TryGetTarget(Vector2 monsterPos, int monsterFloor, out Vector2 target)
    {
        target = Vector2.zero;

        int nearest = -1;
        float nearestDist = float.MaxValue;
        for (int i = 0; i < m_Crumbs.Count; i++)
        {
            if (m_Crumbs[i].m_Floor != monsterFloor)
                continue;

            float dist = Vector2.SqrMagnitude(m_Crumbs[i].m_Pos - monsterPos);
            if (dist >= nearestDist)
                continue;

            nearestDist = dist;
            nearest = i;
        }

        if (nearest < 0)
            return false;   // 같은 층에 남은 과자가 없음

        m_Crumbs.RemoveRange(0, nearest);   // 지나온 구간(더 오래된 과자) 폐기
        target = m_Crumbs[0].m_Pos;
        return true;
    }

    // 몬스터가 맨 앞 과자에 도달했을 때 호출 — 그 과자를 먹어 치우고 다음 과자로 넘어갑니다.
    public void Consume()
    {
        if (m_Crumbs.Count > 0)
            m_Crumbs.RemoveAt(0);
    }

    void OnDrawGizmos()
    {
        if (!m_DrawGizmos)
            return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < m_Crumbs.Count; i++)
            Gizmos.DrawSphere(m_Crumbs[i].m_Pos, 0.08f);
    }
}
