using UnityEngine;
using UnityEngine.EventSystems;

namespace NKK.Lobby
{
    // 쥐돌깨기 누르기 입력 → JudolGame. id: board = 판 (끌었다 놓아 발사) · buy = 쥐 뽑기 버튼 (Shift = 10마리, 꾹 = 연속)
    public class JudolPress : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public JudolGame game;
        public string id = "board";

        public void OnPointerDown(PointerEventData e) { if (game) game.OnPress(id, e, 0); }
        public void OnDrag(PointerEventData e) { if (game) game.OnPress(id, e, 1); }
        public void OnPointerUp(PointerEventData e) { if (game) game.OnPress(id, e, 2); }
    }
}
