using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace NKK.Lobby
{
    /// <summary>
    /// 뽑기 연출 · 쥐 고유 컷씬이 같이 쓰는 컷 전환 (사용자 2026-10-12: 줌 인 · 플래시 전환으로 컷끼리 최대한 이어지게).
    /// PushOut = 컷 끝에 앞으로 빨려 들며(줌 인) 하얗게 → PunchIn = 하얀 화면에서 다음 컷이 크게 시작해 자리 잡음.
    /// 배율은 컷을 담는 틀(target)에 줌 (컷 자체의 확대 애니메이션과 따로 놂). 시간은 unscaled.
    /// </summary>
    public static class CutFx
    {
        public static float Ease(float k) => 1 - (1 - k) * (1 - k);
        public static float In(float k) => k * k * k;

        public static void Alpha(Image im, float a)
        {
            if (!im) return;
            if (a > 0 && !im.gameObject.activeSelf) im.gameObject.SetActive(true);
            var c = im.color; c.a = a; im.color = c;
        }

        // 컷 끝: target 배율 1 → zoom (가속) + flash 0 → 1. 끝나면 하얀 화면 그대로 (다음 PunchIn 이 걷어 냄)
        public static IEnumerator PushOut(RectTransform target, Image flash, float time, float zoom, Func<bool> stop, Action<float> tick = null)
        {
            for (float x = 0; x < time && !stop(); x += Time.unscaledDeltaTime)
            {
                float k = x / time;
                if (target) target.localScale = Vector3.one * Mathf.Lerp(1, zoom, In(k));
                Alpha(flash, Mathf.Min(1, k * k * 1.3f));
                tick?.Invoke(Time.unscaledDeltaTime);
                yield return null;
            }
            Alpha(flash, 1);
        }

        // 컷 시작: target 배율 from → 1 (빨리 → 느리게) + flash 1 → 0
        public static IEnumerator PunchIn(RectTransform target, Image flash, float time, float from, Func<bool> stop)
        {
            for (float x = 0; x < time && !stop(); x += Time.unscaledDeltaTime)
            {
                float k = Ease(x / time);
                if (target) target.localScale = Vector3.one * Mathf.Lerp(from, 1, k);
                Alpha(flash, 1 - k);
                yield return null;
            }
            if (target) target.localScale = Vector3.one;
            Alpha(flash, 0);
            if (flash) flash.gameObject.SetActive(false);
        }
    }

    /// <summary>컷 전환 값 (인스펙터): 컷 끝 줌 인 시간·배율 · 다음 컷 펀치 인 시간·시작 배율</summary>
    [Serializable]
    public class CutTransition
    {
        public float pushTime = 0.16f, pushZoom = 1.15f;
        public float punchTime = 0.25f, punchFrom = 1.12f;
    }

    /// <summary>
    /// 벡터풍 입자 (눈보라 · 속도선 · 불티 등): 플랫한 둥근 점이 바람 방향으로 늘어난 줄무늬, 가까울수록 크고 빠르고 진함.
    /// 사용자 2026-10-12: 벡터로 찍은 눈보라 흩날림. 점·큰 입자는 씬의 템플릿 Image(꺼 둠)를 layer 안에 복제.
    /// Gust 로 돌풍 (속도·줄무늬가 잠깐 세짐 → 컷 전환과 맞춤).
    /// </summary>
    [Serializable]
    public class Blizzard
    {
        [Tooltip("입자를 담을 곳 (화면 크기 RectTransform)")] public RectTransform layer;
        [Tooltip("둥근 점 템플릿 (꺼 둠) · 가까운 큰 입자로 가끔 섞을 템플릿 (꺼 둠, 없어도 됨)")] public Image dot, flake;
        [Tooltip("바람 (px/초)")] public Vector2 wind = new(-1100, -320);
        [Tooltip("색 (palette 가 있으면 그중 아무거나)")] public Color color = Color.white;
        public Color[] palette;
        [Tooltip("점 크기 (먼 것 ~ 가까운 것, px) · 큰 입자 크기")] public Vector2 dotSize = new(5, 22), flakeSize = new(34, 70);
        [Tooltip("먼 입자 진하기 (가까운 것 = 1)")] public float farAlpha = 0.35f;
        [Tooltip("속도 1px/초 당 줄무늬 길이 배율")] public float streak = 0.0028f;
        [Tooltip("바람과 직각으로 흔들리는 폭 (px/초)")] public float sway = 140;
        [Tooltip("가까운 입자 중 큰 입자(flake) 비율")] public float flakeChance = 0.08f;
        [Tooltip("돌풍이 잦아드는 빠르기")] public float gustDecay = 2.8f;

        [NonSerialized] float gust;
        class P { public RectTransform rt; public float depth, size, ph, spin; public bool flake; }
        [NonSerialized] readonly List<P> ps = new();

        public void Add(int n)
        {
            if (!layer || !dot) return;
            var half = layer.rect.size * 0.5f;
            for (int i = 0; i < n; i++)
            {
                float d = Random.value; d = 0.25f + 0.75f * d * d;                    // 먼 것이 많게
                bool fl = flake && d > 0.8f && Random.value < flakeChance / 0.2f;
                var im = UnityEngine.Object.Instantiate(fl ? flake : dot, layer);
                im.gameObject.SetActive(true); im.raycastTarget = false;
                var c = palette != null && palette.Length > 0 ? palette[Random.Range(0, palette.Length)] : color;
                c.a *= Mathf.Lerp(farAlpha, 1f, d); im.color = c;
                var rt = im.rectTransform;
                float size = fl ? Mathf.Lerp(flakeSize.x, flakeSize.y, d) : Mathf.Lerp(dotSize.x, dotSize.y, d * d);
                rt.sizeDelta = Vector2.one * size;
                rt.anchoredPosition = new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
                ps.Add(new P { rt = rt, depth = d, size = size, ph = Random.value * 10, spin = Random.Range(-260f, 260f), flake = fl });
            }
        }

        public void Gust(float power) { gust = Mathf.Max(gust, power); }

        public void Tick(float dt)
        {
            if (!layer) return;
            gust *= Mathf.Exp(-gustDecay * dt);
            var half = layer.rect.size * 0.5f + Vector2.one * 120;
            float ang0 = Mathf.Atan2(wind.y, wind.x);
            foreach (var p in ps)
            {
                if (!p.rt) continue;
                p.ph += dt;
                float sw = Mathf.Sin(p.ph * (1.6f + p.depth)) * sway * p.depth;
                var v = wind * (0.35f + 0.8f * p.depth) * (1 + gust) + new Vector2(-Mathf.Sin(ang0), Mathf.Cos(ang0)) * sw;
                var pos = p.rt.anchoredPosition + v * dt;
                if (pos.x < -half.x) pos.x += half.x * 2; if (pos.x > half.x) pos.x -= half.x * 2;
                if (pos.y < -half.y) pos.y += half.y * 2; if (pos.y > half.y) pos.y -= half.y * 2;
                p.rt.anchoredPosition = pos;
                if (p.flake) p.rt.localRotation = Quaternion.Euler(0, 0, p.ph * p.spin);
                else
                {
                    // 속도 방향으로 늘어난 캡슐 = 바람 줄무늬
                    p.rt.sizeDelta = new Vector2(p.size * (1 + v.magnitude * streak * (0.5f + p.depth)), p.size);
                    p.rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg);
                }
            }
        }

        public void Clear()
        {
            foreach (var p in ps) if (p.rt) UnityEngine.Object.Destroy(p.rt.gameObject);
            ps.Clear(); gust = 0;
        }
    }
}
