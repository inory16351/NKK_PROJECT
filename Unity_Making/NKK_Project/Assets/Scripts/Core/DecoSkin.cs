using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NKK
{
    // 도토리 상점 장식 그림 (2026-11-04).
    // DecoArt.Ready = 그림이 준비된 장식 id (상점이 '적용하기' 버튼을 낼지 정함). 장식을 바꾸는 곳마다 자기 id 를 넣음.
    public static class DecoArt
    {
        public static readonly HashSet<int> Ready = new();
        public static int Equipped(string cat) => Progress.I ? Progress.I.EquippedDeco(cat) : 0;
    }

    // 그림 하나를 장식 종류에 따라 바꿈 (SpriteRenderer 또는 UI Image). 적용한 장식이 없으면 씬에 놓인 처음 그림.
    // 예: 훈장 8개 · 쳇바퀴 링/받침 · 뒷골목 건물 · 뒷골목 배경. 꺼져 있는 오브젝트의 id 도 Register 로 미리 넣음 (LobbyHome).
    public class DecoSkin : MonoBehaviour
    {
        [Tooltip("장식 종류 (도토리 테이블 Deco_Category: Badge · Wheel · Arcade · Market · Lab · AugLab · AlleyBg …)")] public string category;
        [Serializable] public class Entry { public int decoId; public Sprite sprite; }
        [Tooltip("장식 id → 그림 (같은 크기·피벗)")] public Entry[] entries;

        SpriteRenderer sr; Image img; Sprite baseSprite; bool init, hooked; int shown = -1;

        void Init()
        {
            if (init) return;
            init = true;
            sr = GetComponent<SpriteRenderer>(); img = GetComponent<Image>();
            baseSprite = sr ? sr.sprite : img ? img.sprite : null;
            Register();
        }
        public void Register() { if (entries != null) foreach (var e in entries) if (e != null && e.sprite) DecoArt.Ready.Add(e.decoId); }

        void OnEnable() { Init(); Hook(true); Apply(); }
        void Start() { Hook(true); Apply(); }
        void OnDisable() => Hook(false);
        void Hook(bool on)
        {
            var p = Progress.I; if (!p) return;
            if (on && !hooked) { p.DecoEquipChanged += Apply; hooked = true; }
            else if (!on && hooked) { p.DecoEquipChanged -= Apply; hooked = false; }
        }

        public void Apply()
        {
            Init();
            int id = DecoArt.Equipped(category);
            if (id == shown) return;
            shown = id;
            Sprite s = baseSprite;
            if (id != 0 && entries != null) foreach (var e in entries) if (e != null && e.sprite && e.decoId == id) { s = e.sprite; break; }
            if (sr) sr.sprite = s; else if (img) img.sprite = s;
        }
    }
}
