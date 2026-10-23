using System.Collections.Generic;
using UnityEngine;

namespace NKK.Rats
{
    // 증강이 쥐에 남기는 것 (2026-10-23 기획서 Ver.04, 기획서/증강 기획서 Ver.04 반영 결정.md)
    // · 쥐 탑: 5마리를 합친 1마리 (능력치 합산, 특성 = 1층 쥐). 위 4층 = 합쳐진 쥐 리그를 쌓아 그림 (요리사 쥐도 사람 없이 쥐만). 판 끝까지
    // · 공 굴리기 (등급 통일 골든 햄스터 · 판다): 늘 몸을 만 공. 움직임 고르기는 그대로, 특수 액션·묘기 없음
    // · 다다다다 잔상: 쥐 뒤에 반투명 잔상 하나
    public partial class Rat
    {
        // ── 쥐 탑 ──
        readonly List<RatRig> towerRigs = new();
        readonly List<float> towerScales = new();
        float towerMul = 1, towerT = -1;
        bool towerThud;
        public bool IsTower => towerRigs.Count > 0;
        public float TowerMul => towerMul;

        // arts·lengths·scales = 위층 쥐 그림 · 리그 길이 · 크기 (아래 → 위). addDamage = 합친 쥐들 공격력 합
        public void MakeTower(List<RatArtLibrary.Entry> arts, List<float> lengths, List<float> scales, float addDamage)
        {
            if (!rig || arts.Count == 0) return;
            float baseD = Mathf.Max(1, Damage);
            for (int i = 0; i < arts.Count; i++)
            {
                if (arts[i] == null) continue;
                // rig 은 쥐 본체(Rat)와 같은 오브젝트 → 복제하면 쥐가 통째로 생겨 매니저 밖에서 멈춰 있었음 (벽에 낀 탑, 2026-10-23).
                // 이 쥐 자식으로 붙이고 그림(Visual)만 남김: Rat 컴포넌트 · 그림자(원래 쥐 것을 가리킴) · 탈것 · 다른 층은 지움
                var r = Instantiate(rig, transform);
                r.name = "Tower" + (towerRigs.Count + 1);
                var body = r.GetComponent<Rat>(); if (body) { body.shadow = null; DestroyImmediate(body); }
                for (int c = r.transform.childCount - 1; c >= 0; c--) { var ch = r.transform.GetChild(c); if (ch.name != "Visual") DestroyImmediate(ch.gameObject); }
                r.transform.localRotation = Quaternion.identity; r.transform.localScale = Vector3.one;
                r.Build(arts[i], lengths[i]);
                towerRigs.Add(r); towerScales.Add(scales[i]);
            }
            towerMul = 1 + addDamage / baseD;
            towerT = 0; towerThud = false;
            if (mRoot) mRoot.gameObject.SetActive(false);          // 슈퍼 요리사 쥐: 사람 없이 쥐만
        }

        // 탑 그리기 (LateUpdate, 아래 쥐 rig.Apply 뒤): 0.5초 동안 차례로 떨어져 쌓임 → 1초 흔들흔들 → 쿵
        void TowerApply(in RatRig.Pose p, int sortOrder, float lift, float rot)
        {
            if (towerRigs.Count == 0) return;
            if (towerT >= 0) towerT += Time.deltaTime;
            float step = Manager.RigLength(Data) * GradeData.size * Manager.ratScale * 0.55f;       // 한 층 높이 (게임 단위)
            float wob = towerT >= 0 && towerT < 1.5f ? Mathf.Sin(towerT * 14) * Mathf.Clamp01(1.5f - towerT) : Mathf.Sin(Time.time * 2 + seed) * 0.15f;
            for (int k = 0; k < towerRigs.Count; k++)
            {
                var r = towerRigs[k]; if (!r) continue;
                int lv = k + 1;
                float appear = towerT < 0 ? 1 : Mathf.Clamp01((towerT - lv * 0.1f) / 0.12f);
                r.gameObject.SetActive(appear > 0);
                if (appear <= 0) continue;
                float drop = (1 - appear) * 260;                        // 위에서 떨어져 쌓임
                r.transform.localPosition = new Vector3(wob * lv * 3 * World.U, 0, 0);
                r.Apply(p, Manager.ratScale * towerScales[k], face, sq, sortOrder + lv, lift + step * lv + drop, rot + wob * 0.02f * lv);
            }
            if (towerT > 1.5f && !towerThud)
            {
                towerThud = true; towerT = -1;
                FxManager.I?.Shake(0.4f); FxManager.I?.Dust(x, y, 10, 1.6f); FxManager.I?.Ring(x, y, 90, Color.white, 0.4f);
                SfxManager.PlayAt("boss_slam", x, y, 0.6f);
            }
        }

        // ── 공 굴리기 (등급 통일) ──
        public bool BallForm => AugmentSpecials.BallForm(this);
        float ballRot;
        void BallPose(ref RatRig.Pose p)
        {
            p.front = 1.9f; p.farFront = 1.9f; p.back = -1.9f; p.farBack = -1.9f; p.head = 0.8f; p.headX = 4; p.tail = -1.6f; p.sx = p.sy = 0.9f;
        }
        void BallTransform(ref float rot, ref float sx, ref float sy, ref float lift)
        {
            ballRot += Time.deltaTime * Speed / Mathf.Max(8, Radius) * face;
            rot = ballRot; sx = sy = 0.8f; lift = Speed > 40 ? Mathf.Abs(Mathf.Sin(ballRot * 2)) * 5 : 0;
        }

        // ── 다다다다 잔상: 몸통 그림 하나를 조금 늦게 따라오게 ──
        SpriteRenderer ghostImg;
        Vector3 ghostPos;
        void AfterimageApply()
        {
            bool on = AugmentSpecials.Afterimage && !HideBody && temp <= 0 && rig && (rig.IsSingle ? rig.single : rig.torso) && OnScreen(0.1f);
            if (!on) { if (ghostImg) ghostImg.enabled = false; return; }
            var src = rig.IsSingle ? rig.single : rig.torso;
            if (!ghostImg)
            {
                ghostImg = new GameObject("Afterimage").AddComponent<SpriteRenderer>();
                ghostImg.transform.SetParent(transform.parent, false);
                ghostPos = src.transform.position;
            }
            ghostImg.enabled = true;
            ghostImg.sprite = src.sprite; ghostImg.sortingLayerID = src.sortingLayerID; ghostImg.sortingOrder = World.SortOrder(y) - 1;
            ghostImg.flipX = src.flipX;
            ghostPos = Vector3.Lerp(ghostPos, src.transform.position, Mathf.Min(1, Time.deltaTime * 9));
            ghostImg.transform.position = ghostPos;
            ghostImg.transform.rotation = src.transform.rotation;
            ghostImg.transform.localScale = src.transform.lossyScale;
            ghostImg.color = new Color(1, 1, 1, 0.3f);
        }
        void OnDisable() { if (ghostImg) Destroy(ghostImg.gameObject); }
    }
}
