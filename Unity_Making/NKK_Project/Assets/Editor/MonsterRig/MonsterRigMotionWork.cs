using NKK.Hazards;
using UnityEngine;

// 몬스터 리깅 툴 '공격 · 필살기 모션' 에서 고치는 중인 모션 하나 (Undo 대상)
public class MonsterRigMotionWork : ScriptableObject
{
    public BossMotion.Motion motion = new();
}
