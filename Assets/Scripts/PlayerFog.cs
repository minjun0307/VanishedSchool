using UnityEngine;

/// <summary>
/// 플레이어 주변의 원형 범위만 보이고, 그 밖은 전부 검게 가리는 안개 효과.
/// 아무 오브젝트에나 붙여도 되고, 플레이어에 직접 붙여도 됩니다.
/// (m_Target을 비워두면 자동으로 PlayerMove가 붙은 오브젝트를 찾아 따라다닙니다)
/// </summary>
public class PlayerFog : MonoBehaviour
{
    [Header("시야 설정")]
    [Tooltip("플레이어 주변이 보이는 원의 반지름 (월드 단위)")]
    public float m_ViewRadius = 3f;

    [Tooltip("원 가장자리가 부드럽게 어두워지는 폭. 0에 가까울수록 경계가 딱 잘립니다")]
    public float m_EdgeSoftness = 1f;

    [Tooltip("따라다닐 대상. 비워두면 자동으로 PlayerMove를 찾습니다")]
    public Transform m_Target;

    [Header("렌더링 설정")]
    [Tooltip("검은 안개가 덮는 전체 크기 (월드 단위). 카메라 화면보다 충분히 크게")]
    public float m_FogSize = 60f;

    [Tooltip("안개를 그릴 소팅 레이어 이름")]
    public string m_SortingLayer = "Default";

    [Tooltip("소팅 순서. 다른 모든 스프라이트보다 위에 그려지도록 큰 값")]
    public int m_SortingOrder = 32767;

    const int TexSize = 1024;   // 안개 텍스처 해상도

    SpriteRenderer m_Renderer;
    Texture2D m_Texture;

    // 인스펙터 값이 바뀌었는지 감지용
    float m_LastRadius, m_LastSoftness, m_LastFogSize;

    void Start()
    {
        // if (m_Target == null)
        // {
        //     PlayerMove player = FindFirstObjectByType<PlayerMove>();
        //     m_Target = player != null ? player.transform : transform;
        // }

        // 안개 마스크 전용 자식 오브젝트 (플레이어의 SpriteRenderer와 충돌 방지)
        GameObject mask = new GameObject("FogMask");
        mask.transform.SetParent(transform, false);
        m_Renderer = mask.AddComponent<SpriteRenderer>();
        m_Renderer.sortingLayerName = m_SortingLayer;
        m_Renderer.sortingOrder = m_SortingOrder;

        RebuildFog();
    }

    void LateUpdate()
    {
        // 플레이 중 인스펙터에서 값을 바꾸면 즉시 다시 그림
        if (m_ViewRadius != m_LastRadius ||
            m_EdgeSoftness != m_LastSoftness ||
            m_FogSize != m_LastFogSize)
        {
            RebuildFog();
        }

        // 카메라(CameraManager)도 LateUpdate에서 움직이므로 여기서 위치를 맞춰줌
        m_Renderer.transform.position = m_Target.position;
    }

    void RebuildFog()
    {
        m_LastRadius = m_ViewRadius;
        m_LastSoftness = m_EdgeSoftness;
        m_LastFogSize = m_FogSize;

        if (m_Texture == null)
        {
            m_Texture = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            m_Texture.wrapMode = TextureWrapMode.Clamp;
        }

        float worldPerPixel = m_FogSize / TexSize;
        float half = TexSize * 0.5f;
        // softness가 0이어도 InverseLerp가 깨지지 않도록 최소 폭 보장
        float inner = Mathf.Max(0f, m_ViewRadius - Mathf.Max(0.01f, m_EdgeSoftness));

        Color32[] pixels = new Color32[TexSize * TexSize];
        for (int y = 0; y < TexSize; y++)
        {
            for (int x = 0; x < TexSize; x++)
            {
                float dx = (x - half) * worldPerPixel;
                float dy = (y - half) * worldPerPixel;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // 원 안 = 투명, 원 밖 = 검정, 경계는 부드럽게 보간
                float alpha = Mathf.InverseLerp(inner, m_ViewRadius, dist);
                pixels[y * TexSize + x] = new Color32(0, 0, 0, (byte)(alpha * 255f));
            }
        }
        m_Texture.SetPixels32(pixels);
        m_Texture.Apply();

        // 스프라이트가 월드에서 정확히 m_FogSize 크기가 되도록 PPU 계산
        float ppu = TexSize / m_FogSize;
        if (m_Renderer.sprite != null)
            Destroy(m_Renderer.sprite);
        m_Renderer.sprite = Sprite.Create(
            m_Texture, new Rect(0, 0, TexSize, TexSize), new Vector2(0.5f, 0.5f), ppu);
    }

    void OnDestroy()
    {
        if (m_Texture != null)
            Destroy(m_Texture);
    }
}
