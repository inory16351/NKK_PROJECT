using System;
using System.Collections.Generic;
using NKK.Hazards;
using NKK.Humans;
using NKK.Rats;
using UnityEngine;

// 고치는 중인 값 (창이 들고 있음, Undo 대상)
public class MonsterRigWork : ScriptableObject
{
    public string kind, code;
    public BossPartArt.Entry part = new();
    public RatArtLibrary.Entry cat = new();
    public HumanArtLibrary.Entry human = new();
    [Serializable] public class Pivot { public string part; public Vector2 tl; }
    public List<Pivot> pivots = new();
}
