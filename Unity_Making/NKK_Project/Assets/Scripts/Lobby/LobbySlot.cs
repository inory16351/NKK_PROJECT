using UnityEngine;

namespace NKK.Lobby
{
    // 아지트 활동 자리 (침대·의자·쳇바퀴·치즈 창고·아령). 하이라키에서 위치를 옮기면 그 자리로 감.
    // up = 높은 자리 (바닥에서 점프해서 올라감)
    public class LobbySlot : MonoBehaviour
    {
        public enum Act { Sleep, Tea, Wheel, Eat, Lift }
        public Act act;
        [Tooltip("앉았을 때 보는 방향 (1 = 오른쪽, -1 = 왼쪽)")] public int face = 1;
        [Tooltip("높은 자리: 바닥에서 점프해서 올라감")] public bool up;
        [HideInInspector] public LobbyRat user;

        void OnDrawGizmos() { Gizmos.color = new Color(1, 0.8f, 0.3f, 0.8f); Gizmos.DrawWireSphere(transform.position, 0.12f); }
    }
}
