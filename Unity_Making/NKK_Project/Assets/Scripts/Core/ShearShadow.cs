using UnityEngine;

namespace NKK
{
    // 형태 그림자: 물체 그림을 그대로 써서 바닥에 납작하게 눌러 비스듬히 드리움.
    // 트랜스폼만으로 기울이기(전단)를 하려고 회전 → 크기 → 회전 3단으로 나눔 (2×2 행렬 SVD).
    // 구조: ShearShadow(이 컴포넌트, 회전 φ) > Scale(크기) > Sprite(회전 θ, SpriteRenderer)
    public class ShearShadow : MonoBehaviour
    {
        public Transform scaleNode;
        public SpriteRenderer sprite;
        [Tooltip("눌린 높이 비율 (1 = 원래 높이)")] public float squash = 0.32f;
        [Tooltip("옆으로 기우는 정도 (+ = 오른쪽)")] public float shear = 0.55f;
        public Color color = new(0.12f, 0.07f, 0.04f, 0.2f);

        // 바닥 기준점(그림 아래 가운데)을 원점으로 두고, 그림 폭이 width(유닛)가 되게
        public void Setup(Sprite s, float width)
        {
            if (!s || !sprite || !scaleNode) return;
            sprite.sprite = s; sprite.color = color;
            float k = width / s.bounds.size.x;
            // M = [[k, k·shear], [0, k·squash]]  (u: 가로, v: 바닥에서 위로)
            float a = k, b = k * shear, c = 0, d = k * squash;
            float E = (a + d) / 2, F = (a - d) / 2, G = (c + b) / 2, H = (c - b) / 2;
            float Q = Mathf.Sqrt(E * E + H * H), R = Mathf.Sqrt(F * F + G * G);
            float sx = Q + R, sy = Q - R;
            float a1 = Mathf.Atan2(G, F), a2 = Mathf.Atan2(H, E);
            float theta = (a2 - a1) / 2, phi = (a2 + a1) / 2;
            transform.localRotation = Quaternion.Euler(0, 0, phi * Mathf.Rad2Deg);
            scaleNode.localScale = new Vector3(sx, sy, 1);
            scaleNode.localRotation = Quaternion.identity;
            sprite.transform.localRotation = Quaternion.Euler(0, 0, theta * Mathf.Rad2Deg);
            // 스프라이트 피벗(가운데) → 바닥이 원점에 오게: Rθ·(0, 반높이)
            float hh = s.bounds.size.y / 2 - s.bounds.center.y;
            sprite.transform.localPosition = new Vector3(-Mathf.Sin(theta) * hh, Mathf.Cos(theta) * hh, 0);
        }
    }
}
