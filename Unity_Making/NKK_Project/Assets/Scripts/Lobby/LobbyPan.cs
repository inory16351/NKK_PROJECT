using UnityEngine;
using UnityEngine.UI;

namespace NKK.Lobby
{
    // 아지트 아래 '지하 하수구' (2026-10-12, 사용자: 맨홀 뚜껑으로 들어감): 아지트 배경판 바로 아래 방 (유전자 추출기 · 쥐구멍 암시장).
    // 아지트 바닥의 맨홀 뚜껑(물건 버튼 id alley) → 카메라가 아래로 내려감, 하수구의 '올라가기' 버튼 → 아지트로. 16층 클리어(기능 alley)부터 열림
    public class LobbyPan : MonoBehaviour
    {
        public LobbyHome home;
        public LobbyManager manager;
        [Tooltip("하수구 방 (자식: 배경 · 물건 · 브로커 쥐). 실행하면 아지트 배경판 바로 아래에 붙임")] public Transform room;
        [Tooltip("뒷골목 배경판 (방 크기 기준)")] public SpriteRenderer roomBackground;
        [Tooltip("방과 같이 옮길 것 (아지트 물건 묶음 안의 뒷골목 물건 버튼 등)")] public Transform[] follow;
        [Tooltip("(선택) 아지트 화면의 '내려가기' 버튼 (잠겨 있으면 숨김) · 하수구 화면의 '올라가기' 버튼")] public Button toRoom, toHome;
        [Tooltip("맨홀 뚜껑 그림: 닫힘 · 열림 (내려갈 때 열림)")] public SpriteRenderer manhole;
        public Sprite manholeClosed, manholeOpen;
        [Tooltip("열림 그림일 때 맨홀 위치 보정 (부모 기준) — 열림 그림은 옆에 뚜껑이 있어서 구멍이 가운데가 아님")] public Vector2 manholeOpenOffset;
        Vector3 manholeBase;
        [Tooltip("뒷골목에 있을 때 숨길 아지트 화면 UI (로고 · 팁 등)")] public GameObject[] hideInRoom;
        [Tooltip("미끄러지는 시간 (초)")] public float panTime = 0.6f;

        public bool AtRoom { get; private set; }
        public static LobbyPan I { get; private set; }
        public event System.Action<bool> Moved;          // 도착 (true = 뒷골목)
        float k, homeY, roomY;
        bool placed;

        void Awake()
        {
            I = this;
            if (manhole) manholeBase = manhole.transform.localPosition;
            if (toRoom) toRoom.onClick.AddListener(() => Go(true));
            if (toHome) toHome.onClick.AddListener(() => Go(false));
        }

        void Place()
        {
            if (placed || !home || !home.background || !room) return;
            placed = true;
            var b = home.background.bounds;
                        float h = roomBackground ? roomBackground.bounds.size.y : b.size.y;
            var target = new Vector3(b.center.x, b.min.y - h / 2, room.position.z);
            var delta = target - room.position;
            room.position = target;
            if (follow != null) foreach (var f in follow) if (f) f.position += delta;
            homeY = b.center.y; roomY = room.position.y;
        }

        public void Go(bool toTheRoom)
        {
            if (toTheRoom && Progress.I && !Progress.I.IsUnlocked("alley")) return;
            if (manager && manager.PrepOpen) manager.CloseToHome();
            AtRoom = toTheRoom;
            SfxManager.Play("ui_open");
        }

        void LateUpdate()
        {
            Place();
            if (!home || !home.cam) return;
            float prev = k;
            k = Mathf.MoveTowards(k, AtRoom ? 1 : 0, Time.unscaledDeltaTime / Mathf.Max(0.05f, panTime));
            var p = home.cam.transform.position; p.y = Mathf.Lerp(homeY, roomY, Mathf.SmoothStep(0, 1, k)); home.cam.transform.position = p;
            if (manhole)
            {
                bool open = (AtRoom || k > 0) && manholeOpen;
                manhole.sprite = open ? manholeOpen : manholeClosed;
                manhole.transform.localPosition = manholeBase + (open ? (Vector3)manholeOpenOffset : Vector3.zero);
            }
            if (prev != k && (k == 0 || k == 1)) Moved?.Invoke(k == 1);
            bool unlocked = !Progress.I || Progress.I.IsUnlocked("alley");
            bool prep = manager && manager.PrepOpen;
            if (toRoom) toRoom.gameObject.SetActive(unlocked && !AtRoom && k < 0.01f && !prep);
            if (toHome) toHome.gameObject.SetActive(AtRoom && k > 0.99f && !prep && !GachaShow.Busy);      // 뽑기 연출 중엔 숨김
            if (hideInRoom != null) foreach (var g in hideInRoom) if (g) g.SetActive(k < 0.5f);
        }
    }
}
