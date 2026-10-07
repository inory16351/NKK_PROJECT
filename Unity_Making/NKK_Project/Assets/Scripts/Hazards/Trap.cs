using NKK.Rats;
using UnityEngine;

namespace NKK.Hazards
{
    // 쥐덫 (웹게임 hazards.js 이식): 밟은 쥐는 기절하고 덫에 붙잡힘, 일정 시간 뒤 다시 장전.
    public class Trap : MonoBehaviour
    {
        public SpriteRenderer sprite;
        public Sprite armedSprite, snapSprite;

        [Header("상태 (실행 중 확인용)")]
        public float x, y;
        public bool Armed = true;

        float reloadT, snapT;
        Rat victim;

        public void Init(float px, float py, float width)
        {
            x = px; y = py; Armed = true;
            transform.position = World.ToUnity(x, y);
            sprite.sprite = armedSprite;
            float s = armedSprite ? width * World.U / armedSprite.bounds.size.x : 1;
            sprite.transform.localScale = Vector3.one * s;
            sprite.transform.localPosition = new Vector3(0, armedSprite ? armedSprite.bounds.size.y * s * 0.35f : 0, 0);
            sprite.sortingOrder = World.SortOrder(y - 8);
        }

        public void Tick(float dt, RatManager rats, float radius, float stunTime, float reloadTime)
        {
            snapT = Mathf.Max(0, snapT - dt);
            if (!Armed)
            {
                reloadT -= dt;
                if (victim) { if (victim.stun > 0) { victim.x = x; victim.y = y; } else { victim.Held = false; victim = null; } }
                if (reloadT <= 0) { Armed = true; if (victim) victim.Held = false; victim = null; sprite.sprite = armedSprite; }
                return;
            }
            foreach (var r in rats.Rats)
            {
                if (r.z > 4 || r.stun > 0 || Vector2.Distance(new Vector2(r.x, r.y), new Vector2(x, y)) > radius + r.Radius * 0.6f) continue;
                Armed = false; reloadT = reloadTime; snapT = 0.3f;
                sprite.sprite = snapSprite;
                var fx = FxManager.I;
                if (Random.value < CommonSkill.TrapCheeseChance)        // 덫 해체 전문가: 치즈만 쏙 빼 먹고 빠져나감
                {
                    if (fx) { fx.Popup(x, y, "치즈만 쏙!", new Color(1, 0.95f, 0.75f), 20, 0.9f, 40); fx.Ring(x, y, 40, Color.white, 0.2f); }
                    break;
                }
                victim = r; r.Stun(stunTime); r.Held = true; r.vx = r.vy = 0;
                if (fx) { fx.Popup(x, y, "딸깍!!", Color.white, 22, 0.9f, 40); fx.Ring(x, y, 40, Color.white, 0.2f); fx.Shake(0.08f); }
                break;
            }
        }

        void LateUpdate()
        {
            // 닫힐 때 살짝 튀어오름
            float k = snapT > 0 ? Mathf.Sin(snapT / 0.3f * Mathf.PI) * 0.15f : 0;
            transform.localScale = new Vector3(1 + k, 1 - k, 1);
        }

        void OnDestroy() { if (victim) victim.Held = false; }
    }
}
