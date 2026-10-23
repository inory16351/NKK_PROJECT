using System.Collections.Generic;
using NKK.Hazards;
using UnityEditor;
using UnityEngine;
using M = NKK.Hazards.BossPartArt.Move;
using Sh = NKK.Hazards.BossPartArt.Show;

// Assets/Art/Rats/BossParts/<보스>/*.png (Tools/slice_boss_rework.py) → Assets/Data/BossPartArt.asset
// 붙는 자리·관절·움직임 기본값 (보스 개편 2026-10-22). 다시 만들면 에셋에서 고친 값은 덮어써짐 → 고친 값은 여기에도 옮길 것.
public static class BossPartArtBuilder
{
    const string AssetPath = "Assets/Data/BossPartArt.asset";
    const string Dir = "Assets/Art/Rats/BossParts/";

    static Sprite S(string p) => AssetDatabase.LoadAssetAtPath<Sprite>(Dir + p + ".png");
    static Sprite R(string p) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Rats/Parts/" + p + ".png");

    static BossPartArt.Part P(string name, Sprite sp, string parent, float ax, float ay, float px, float py, int order,
        M move = M.None, float amp = 0, float phase = 0, float speed = 1, float angle = 0, float scale = 1, bool flip = false, bool far = false, Sprite alt = null,
        Sh show = Sh.Always, float open = 0, int neck = 0, float wind = 0, float strike = 0) =>
        new() { name = name, sprite = sp, alt = alt, parent = parent, at = new Vector2(ax, ay), pivot = new Vector2(px, py), order = order,
                move = move, amp = amp, phase = phase, speed = speed, angle = angle, scale = scale, flipX = flip, far = far, show = show, open = open,
                neck = neck, wind = wind, strike = strike };

    [MenuItem("NKK/Build Boss Part Art")]
    public static void Build()
    {
        var lib = AssetDatabase.LoadAssetAtPath<BossPartArt>(AssetPath);
        if (!lib) { lib = ScriptableObject.CreateInstance<BossPartArt>(); AssetDatabase.CreateAsset(lib, AssetPath); }
        lib.entries.Clear();

        // 55층 거미 고양이 (토이스토리 '베이비 페이스' 패러디): 고양이 머리가 몸 가운데 위, 철근 다리 8개가 둘레로 뻗음 (무릎 위로, 끝은 바닥)
        // 다리 그림: leg = 무릎이 왼쪽 ('<') → 시계 방향으로 돌려 앞다리, leg2 = 무릎이 오른쪽 ('>') → 반시계로 돌려 뒷다리. 관절 = 철근 윗끝
        var spider = new BossPartArt.Entry { codeId = "boss_spider", length = 230, height = 330 };
        spider.parts.Add(P("body", S("boss_spider/body"), null, 0, 0.62f, 0.5f, 0.5f, 3, M.Bob, 5, 0, 2));
        float[] sx = { 0.3f, 0.42f, 0.58f, 0.7f }, sa = { -72, -52, 38, 58 }, ss = { 0.95f, 0.85f, 1.05f, 1.15f };
        for (int i = 0; i < 4; i++)
        {
            bool fwd = i < 2; var sp = S(fwd ? "boss_spider/leg" : "boss_spider/leg2");
            float px = fwd ? 0.7f : 0.2f;
            spider.parts.Add(P("legF" + i, sp, "body", sx[i] + (fwd ? 0.06f : -0.06f), 0.45f, px, 0.04f, 0, M.Walk, 12, i * 0.25f + 0.5f, 1, sa[i] * 0.85f, ss[i] * 0.92f, far: true));
            spider.parts.Add(P("leg" + i, sp, "body", sx[i], 0.55f, px, 0.04f, 4, M.Walk, 14, i * 0.25f, 1, sa[i], ss[i]));
        }
        // 용접 목 받침 (원판 + 파이프 + 철근 다리): 철근 발이 몸통 윗면에 박히고, 원판 위에 머리가 얹힘 (머리·몸통이 따로 놀아 보였음)
        spider.parts.Add(P("neck", S("boss_spider/neck"), "body", 0.5f, 0.62f, 0.5f, 0.99f, 5, M.None, 0, 0, 1, 0, 0.34f));
        spider.parts.Add(P("head", S("boss_spider/head"), "body", 0.5f, -0.24f, 0.55f, 0.97f, 6, M.Sway, 3, 0, 1.5f, 0, 0.95f, alt: S("boss_spider/head2")));
        lib.entries.Add(spider);

        // 65층 상어 고양이 (트랄랄레로, 참고 통 그림 = BossRework/Sheets/cx6_full.png): 고양이처럼 네 발 — 허벅지가 몸에서 이어지는 다리 (cx6),
        // 걸음 = 대각선 짝 (가까운 앞 + 먼 뒤 / 먼 앞 + 가까운 뒤). 값 = 통 그림에서 찾은 자리 (통 그림은 파츠의 1.3배, 다리 0.9배 → 몸 기준 0.692배)
        // 2026-10-22 사용자: 다리가 몸통 아래로 내려감 → 다리가 커서(1.1배) 몸이 떠 있었음. 가까운 다리는 몸통 위 (통 그림처럼 허벅지가 배에 겹침),
        // 앞다리는 윗끝 밝은 단면 원을 지운 legN, 먼 다리는 몸통 뒤.
        // 다리 관절 = 몸통 안쪽 (배 흰 부분 위), 먼 다리는 가까운 다리 바로 뒤에서 살짝 앞으로 내딛음 · 신발이 바닥에 닿게 다리마다 배율 (웹 미리보기 rig_web/giant.html?code=boss_sharkcat 에서 사용자 확인)
        var shark = new BossPartArt.Entry { codeId = "boss_sharkcat", length = 330, height = 260 };
        shark.parts.Add(P("body", S("boss_sharkcat/body"), null, 0, 0.834f, 0.5f, 0.5f, 3, M.Sway, 2, 0, 1));
        shark.parts.Add(P("legFarF", S("boss_sharkcat/leg"), "body", 0.31f, 0.6f, 0.76f, 0.12f, 1, M.Walk, 22, 0.5f, 1, -12, 0.952f, far: true));
        shark.parts.Add(P("legFarB", S("boss_sharkcat/leg2"), "body", 0.64f, 0.64f, 0.36f, 0.1f, 1, M.Walk, 22, 0, 1, -10, 0.815f, far: true));
        shark.parts.Add(P("legF", S("boss_sharkcat/legN"), "body", 0.29f, 0.62f, 0.76f, 0.12f, 5, M.Walk, 22, 0, 1, 12, 0.801f));
        shark.parts.Add(P("legB", S("boss_sharkcat/leg2"), "body", 0.62f, 0.66f, 0.36f, 0.1f, 5, M.Walk, 22, 0.5f, 1, 6, 0.843f));
        shark.parts.Add(P("head", S("boss_sharkcat/head"), "body", 0.05f, 0.5f, 0.85f, 0.6f, 6, M.Sway, 4, 0.2f, 1.5f, alt: S("boss_sharkcat/head2")));
        lib.entries.Add(shark);

        // 70층 물고기 고양이 → 머라이언 패러디 (참고 통 그림 = BossRework/Sheets/cx4_full.png): 똑바로 선 물고기 몸 위에 갈기 고양이 머리, 꼬리는 뒤로 말아 올림.
        // 다리 없음 → 통통 튐. 물줄기는 공격·맞을 때(두 번째 그림)만 입에서 나옴
        // 값 = Tools/match_parts.py 로 파츠를 통 그림 위에서 찾은 자리 (머리는 통 그림보다 작게 그려져 1.15배, 갈기가 몸 윗부분을 덮게 조금 내림)
        // 꼬리 이음새: 몸 오른쪽 끝 원은 잘라 냄(slice_boss_fix discs) + 꼬리 관절(아래끝 원 가운데)을 몸 안쪽에, 꼬리는 몸 뒤 → 원 안 보임. 얼굴은 몸에 비해 커서 0.85배
        var fish = new BossPartArt.Entry { codeId = "boss_fishcat", length = 252, height = 300 };
        fish.parts.Add(P("body", S("boss_fishcat/body"), null, 0, 0.532f, 0.5f, 0.5f, 3, M.Bob, 10, 0, 2, -8));
        fish.parts.Add(P("tail", S("boss_fishcat/tail"), "body", 0.92f, 0.62f, 0.19f, 0.86f, 2, M.Sway, 7, 0, 2, 20, 1.3f));      // 1.3배 = 꼬리 시작 굵기(약 100px)를 몸 끝(약 147px)에 맞춤
        fish.parts.Add(P("fin", S("boss_fishcat/fin"), "body", 0.483f, 0.45f, 0.15f, 0.2f, 4, M.Sway, 12, 0.3f, 3, -10, 0.8f));
        fish.parts.Add(P("head", S("boss_fishcat/head"), "body", 0.23f, 0.12f, 0.5f, 0.85f, 5, M.Sway, 4, 0, 1.5f, 15, 0.85f, alt: S("boss_fishcat/head2")));
        fish.parts.Add(P("water", null, "head", 0.28f, 0.42f, 0.97f, 0.03f, 7, M.Sway, 3, 0, 4, -7, alt: S("boss_fishcat/water")));
        lib.entries.Add(fish);

        // 85층 항공모함 고양이: 배 = 뿌리 (물결 따라 출렁), 관제탑 = 고양이 머리, 갑판 위 쥐 머리 전투기
        var carrier = new BossPartArt.Entry { codeId = "boss_carrier", length = 700, height = 260 };
        carrier.parts.Add(P("hull", S("boss_carrier/hull"), null, 0, 0.5f, 0.5f, 0.5f, 2, M.Bob, 5, 0, 0.6f));
        carrier.parts.Add(P("tower", S("boss_carrier/tower"), "hull", 0.62f, 0.12f, 0.5f, 0.97f, 3, M.Sway, 2, 0, 1, 0, 0.5f, alt: S("boss_carrier/tower2")));
        carrier.parts.Add(P("jet", S("boss_carrier/jet"), "hull", 0.25f, 0.06f, 0.5f, 0.95f, 4, scale: 0.55f));
        lib.entries.Add(carrier);

        // 80층 차력거인 (참고 통 그림 = BossRework/Sheets/tt4_full.png, 사용자 스케치 ref_cartgiant_pose.png 자세): 엉덩이가 가장 높고 팔은 땅까지 곧게,
        // 허벅지는 거의 수직 · 정강이는 뒤로 눕고 · 발끝으로 섬. 몸통 = 상반신(body) + 팬티 엉덩이(butt), 정강이·발 따로.
        // 값 = Tools/giant_rig_v3.py (웹 미리보기 BossRework/rig_web/giant.html 에서 사용자 확인, 2026-10-22): 몸통·엉덩이·안장·포탑 = 통 그림에서 찾은 자리,
        // 팔다리 = 통 그림 관절 방향 + 마디 배율은 가까운 쪽·먼 쪽 같게. 먼 팔·다리는 가까운 어깨·엉덩이 관절 옆에서 시작해 끝(주먹·발끝)이 통 그림 자리에 닿게 굽힘.
        // 파츠: tt9 상반신 · tt10b 엉덩이(앞에 허리 살을 길게 → 몸통 밑으로 겹침, 몸통이 위) · tt8 팔·허벅지 · tt10a 정강이 · tt10c 발 + 먼 쪽 전용(안쪽에서 본 주먹 uparmFar·forearmFar, 안쪽 발 footFar) (ChatGPT)
        // 허벅지 = 몸통에 붙임(엉덩이가 씰룩여도 다리 그대로) · 가까운 허벅지는 엉덩이·몸통 위 · 엉덩이 Wiggle = 멈출 때 한 번 출렁 + 공격 중 사격 박자로 툭툭
        var giant = new BossPartArt.Entry { codeId = "boss_cartgiant", length = 263, height = 360 };
        giant.parts.Add(P("body", S("boss_cartgiant/body"), null, 0, 1.784f, 0.5f, 0.5f, 4, M.Bob, 4, 0, 2, 6));
        giant.parts.Add(P("armFar", S("boss_cartgiant/uparmFar"), "body", 0.375f, 0.403f, 0.498f, 0.098f, -2, M.Walk, 14, 0.5f, 1, -37.4f, 0.695f, far: true));
        giant.parts.Add(P("foreFar", S("boss_cartgiant/forearmFar"), "armFar", 0.624f, 0.896f, 0.583f, 0.099f, -3, M.Walk, 8, 0.75f, 1, 16, 1.247f, far: true));
        giant.parts.Add(P("butt", S("boss_cartgiant/butt"), "body", 0.546f, 0.442f, 0.079f, 0.435f, 3, M.Wiggle, 4, 0, 2, -4));
        giant.parts.Add(P("thighFar", S("boss_cartgiant/thigh"), "body", 1.242f, 0.816f, 0.45f, 0.17f, -2, M.Walk, 14, 0, 1, -61, 1.029f, far: true));
        giant.parts.Add(P("shinFar", S("boss_cartgiant/shin"), "thighFar", 0.6f, 0.85f, 0.456f, 0.131f, -3, M.Walk, 8, 0.25f, 1, 114.3f, 0.838f, far: true));
        giant.parts.Add(P("footFar", S("boss_cartgiant/footFar"), "shinFar", 0.53f, 0.953f, 0.723f, 0.104f, -4, M.Walk, 6, 0.5f, 1, -60.3f, 0.694f, far: true));
        giant.parts.Add(P("thigh", S("boss_cartgiant/thigh"), "body", 1.304f, 0.825f, 0.45f, 0.17f, 5, M.Walk, 14, 0.5f, 1, -32.5f, 1.029f));
        giant.parts.Add(P("shin", S("boss_cartgiant/shin"), "thigh", 0.6f, 0.85f, 0.456f, 0.131f, 1, M.Walk, 8, 0.75f, 1, 99.9f, 0.838f));
        giant.parts.Add(P("foot", S("boss_cartgiant/foot"), "shin", 0.53f, 0.953f, 0.746f, 0.099f, 0, M.Walk, 6, 0, 1, -67.7f, 0.717f));
        giant.parts.Add(P("arm", S("boss_cartgiant/uparm"), "body", 0.323f, 0.395f, 0.475f, 0.22f, 6, M.Walk, 14, 0, 1, -10.5f, 0.992f));
        giant.parts.Add(P("fore", S("boss_cartgiant/forearm"), "arm", 0.63f, 0.912f, 0.54f, 0.12f, 5, M.Walk, 8, 0.25f, 1, 4, 1.137f));
        giant.parts.Add(P("head", S("boss_cartgiant/head"), "body", 0.12f, 0.26f, 0.821f, 0.92f, 7, M.Sway, 3, 0, 1, -12, alt: S("boss_cartgiant/head2")));      // 쥐 얼굴 (2026-10-22, 저작권) — 주둥이만큼 칸이 왼쪽으로 넓어져 피벗 0.76 → 0.821 (Tools/slice_cartgiant_ratface.py) · 원래 사람 머리 = head_human*.png
        // 등 위 안장 + 포탑 고양이 2마리: 공격 중엔 사격 그림(총구 불꽃)과 번갈아 깜빡이며 반동으로 흔들림 (Move.Recoil, 엇갈리게 phase)
        giant.parts.Add(P("saddle", S("boss_cartgiant/saddle"), "body", 0.747f, 0.376f, 0.5f, 0.5f, 8, M.None, 0, 0, 1, -8));
        giant.parts.Add(P("gunner1", S("boss_cartgiant/gunner"), "saddle", 0.32f, 0.089f, 0.773f, 0.95f, 9, M.Recoil, 6, 0, 1, 2, alt: S("boss_cartgiant/gunner2")));
        giant.parts.Add(P("gunner2", S("boss_cartgiant/gunner"), "saddle", 0.839f, 0.077f, 0.773f, 0.95f, 9, M.Recoil, 6, 0.5f, 1, 2, alt: S("boss_cartgiant/gunner2")));
        lib.entries.Add(giant);

        // 60층 굴착기 (발악 1 에 기사가 탐): 몸체 = 뿌리, 붐 팔은 늘 조금 까딱
        var exc = new BossPartArt.Entry { codeId = "boss_excavator", length = 300, height = 260 };
        exc.parts.Add(P("body", S("props/excavator_body"), null, 0, 0.5f, 0.5f, 0.5f, 3, M.Bob, 2, 0, 3));
        exc.parts.Add(P("arm", S("props/excavator_arm"), "body", 0.12f, 0.45f, 0.93f, 0.92f, 4, M.Sway, 8, 0, 1));
        lib.entries.Add(exc);

        // 97층 우주복 쥐 (보스 크기): 쥐 파츠 + 괴물 머리 (웹 미리보기 BossRework/rig_web/crewmate.html 에서 확인, 2026-10-23)
        // 머리 = 닫힘(쥐 머리) ↔ 열림(아래턱 + 위턱이 뒤쪽 경첩에서 open° 들림). 열림 = Boss.Crew.cs 가 정함 (등장·촉수 삼키기·덮치기·뱉기 때만)
        // 예전엔 머리를 alt(공격·맞을 때 그림)로 바꿔서 맞을 때마다 괴물 입이 깜빡였음
        var crew = new BossPartArt.Entry { codeId = "boss_crewmate", length = 260, height = 260 };
        crew.parts.Add(P("torso", R("crewmate/torso"), null, 0, 0.817f, 0.5f, 0.5f, 3, M.Bob, 3, 0, 2));      // 2026-10-23: 0.95 → 발이 바닥에서 떠 있었음 (뒷발 끝 = 바닥, 앞다리 1.1배)
        crew.parts.Add(P("tail", R("crewmate/tail"), "torso", 0.94f, 0.53f, 0.04f, 0.44f, 0, M.Sway, 12, 0, 1.5f));
        crew.parts.Add(P("backFar", R("crewmate/back"), "torso", 0.78f, 0.4f, 0.68f, 0.06f, 1, M.Walk, 20, 0.5f, 1, 0, 1, false, true));
        crew.parts.Add(P("frontFar", R("crewmate/front"), "torso", 0.28f, 0.5f, 0.77f, 0.06f, 1, M.Walk, 20, 0, 1, 0, 1.1f, false, true));
        crew.parts.Add(P("back", R("crewmate/back"), "torso", 0.72f, 0.4f, 0.68f, 0.06f, 4, M.Walk, 20, 0));
        crew.parts.Add(P("front", R("crewmate/front"), "torso", 0.22f, 0.5f, 0.77f, 0.06f, 4, M.Walk, 20, 0.5f, 1, 0, 1.1f));
        // 정체: 아래턱(혀) + 위턱(헬멧·바이저·윗니)을 따로 리깅 (사용자) — 위턱은 아래턱 뒤쪽 경첩에 붙음
        crew.parts.Add(P("head", R("crewmate/head"), "torso", 0.07f, 0.4f, 0.53f, 0.86f, 5, M.Sway, 4, 0, 1.5f, 0, 0.72f, show: Sh.Closed));
        crew.parts.Add(P("jawLow", S("boss_crewmate/lower"), "torso", 0.1f, 0.42f, 0.62f, 0.9f, 5, M.Sway, 4, 0, 1.5f, 0, 0.42f, show: Sh.Open, open: 6));
        crew.parts.Add(P("jawUp", S("boss_crewmate/upper"), "jawLow", 0.93f, 0.18f, 0.9f, 0.82f, 6, M.Sway, 5, 0, 4, 0, 1, show: Sh.Open, open: -40));
        lib.entries.Add(crew);

        lib.entries.Add(Hydra());

        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        var missing = new List<string>();
        foreach (var e in lib.entries) foreach (var p in e.parts) if (!p.sprite && !p.alt) missing.Add(e.codeId + "/" + p.name);
        Debug.Log($"[BossPartArt] {lib.entries.Count}종" + (missing.Count > 0 ? " · 그림 없음: " + string.Join(", ", missing) : ""));
    }

    // 쥐 히드라 (새 보스, 층 미정 · 2026-10-23): 쥐 몸이 뭉친 덩어리 몸통(박힌 머리 각도 제각각) + 목 7 (마디 3 + 쥐 머리) · 다리 8 (먼 4) · 꼬리 4 · 박힌 머리 6.
    // 값 = Tools/hydra_rig.py --cs (웹 미리보기 BossRework/rig_web/hydra.html 과 같은 값, GIF = Tools/hydra_gif.py). 그림 = Art/Rats/BossParts/boss_hydra (BossRework/Parts/boss_hydra)
    // 목 = neck1s→neck2s→neck3s→head 사슬 (Neck: 머리 자리를 먼저 정하고 마디가 따라옴 = BossPartRig.NeckIK, 목마다 위상·빠르기 다르게 떠다님, 공격 중 biteOrder 차례로 물기, 목 모양 S/C·머리 방향 제각각 — 4·5·6 은 목 사슬째 첫 마디에서 뒤집어 뒤를 봄) · 박힌 머리 = Twitch (각도·좌우 제각각)
    // NKK/Build Boss Part Art 는 전부 다시 만듦 → 히드라만 넣을 땐 BossPartArtBuilder.AddHydra()
    public static BossPartArt.Entry Hydra()
    {
        var hydra = new BossPartArt.Entry { codeId = "boss_hydra", length = 300, height = 380 };
        hydra.parts.Add(P("body", S("boss_hydra/body_chaos"), null, 0, 0.691f, 0.5f, 0.5f, 50, M.Bob, 6, 0, 1.4f, 0, 1));
        hydra.parts.Add(P("n0_neck1", S("boss_hydra/neck1s"), "body", 0.15f, 0.45f, 0.835f, 0.479f, 37, M.Neck, 22, 0, 0.85f, -45, 0.977f, neck: 0, wind: 70, strike: 230));
        hydra.parts.Add(P("n0_neck2", S("boss_hydra/neck2s"), "n0_neck1", 0.165f, 0.479f, 0.818f, 0.479f, 36, M.Neck, 0, 0, 0.85f, 70, 1, neck: 0));
        hydra.parts.Add(P("n0_neck3", S("boss_hydra/neck3s"), "n0_neck2", 0.182f, 0.479f, 0.798f, 0.479f, 35, M.Neck, 0, 0, 0.85f, -15, 1, neck: 0));
        hydra.parts.Add(P("n0_head", S("boss_hydra/head"), "n0_neck3", 0.202f, 0.479f, 0.93f, 0.638f, 34, M.Neck, 4, 0, 0.85f, 30, 0.85f, alt: S("boss_hydra/head_bite"), neck: 0));
        hydra.parts.Add(P("n1_neck1", S("boss_hydra/neck1s"), "body", 0.22f, 0.32f, 0.835f, 0.479f, 29, M.Neck, 18, 0.37f, 1.1f, 30, 1.003f, neck: 1, wind: 70, strike: 230));
        hydra.parts.Add(P("n1_neck2", S("boss_hydra/neck2s"), "n1_neck1", 0.165f, 0.479f, 0.818f, 0.479f, 28, M.Neck, 0, 0.37f, 1.1f, -65, 1, neck: 1));
        hydra.parts.Add(P("n1_neck3", S("boss_hydra/neck3s"), "n1_neck2", 0.182f, 0.479f, 0.798f, 0.479f, 27, M.Neck, 0, 0.37f, 1.1f, -10, 1, neck: 1));
        hydra.parts.Add(P("n1_head", S("boss_hydra/head"), "n1_neck3", 0.202f, 0.479f, 0.93f, 0.638f, 26, M.Neck, 4, 0.37f, 1.1f, 15, 0.85f, alt: S("boss_hydra/head_bite"), neck: 1));
        hydra.parts.Add(P("n2_neck1", S("boss_hydra/neck1s"), "body", 0.32f, 0.25f, 0.835f, 0.479f, 21, M.Neck, 24, 0.71f, 0.7f, -69, 1.32f, neck: 2, wind: 70, strike: 230));
        hydra.parts.Add(P("n2_neck2", S("boss_hydra/neck2s"), "n2_neck1", 0.165f, 0.479f, 0.818f, 0.479f, 20, M.Neck, 0, 0.71f, 0.7f, -15, 1, neck: 2));
        hydra.parts.Add(P("n2_neck3", S("boss_hydra/neck3s"), "n2_neck2", 0.182f, 0.479f, 0.798f, 0.479f, 19, M.Neck, 0, 0.71f, 0.7f, 70, 1, neck: 2));
        hydra.parts.Add(P("n2_head", S("boss_hydra/head"), "n2_neck3", 0.202f, 0.479f, 0.93f, 0.638f, 18, M.Neck, 4, 0.71f, 0.7f, 49, 0.85f, alt: S("boss_hydra/head_bite"), neck: 2));
        hydra.parts.Add(P("n3_neck1", S("boss_hydra/neck1s"), "body", 0.46f, 0.18f, 0.835f, 0.479f, 13, M.Neck, 20, 0.15f, 0.95f, -93, 1.088f, neck: 3, wind: 70, strike: 230));
        hydra.parts.Add(P("n3_neck2", S("boss_hydra/neck2s"), "n3_neck1", 0.165f, 0.479f, 0.818f, 0.479f, 12, M.Neck, 0, 0.15f, 0.95f, 25, 1, neck: 3));
        hydra.parts.Add(P("n3_neck3", S("boss_hydra/neck3s"), "n3_neck2", 0.182f, 0.479f, 0.798f, 0.479f, 11, M.Neck, 0, 0.15f, 0.95f, 5, 1, neck: 3));
        hydra.parts.Add(P("n3_head", S("boss_hydra/head"), "n3_neck3", 0.202f, 0.479f, 0.93f, 0.638f, 10, M.Neck, 4, 0.15f, 0.95f, 8, 0.85f, alt: S("boss_hydra/head_bite"), neck: 3));
        hydra.parts.Add(P("n4_neck1", S("boss_hydra/neck1s"), "body", 0.6f, 0.24f, 0.835f, 0.479f, 17, M.Neck, 18, 0.58f, 1.25f, 90, 1.26f, flip: true, neck: 4, wind: 70, strike: 230));
        hydra.parts.Add(P("n4_neck2", S("boss_hydra/neck2s"), "n4_neck1", 0.165f, 0.479f, 0.818f, 0.479f, 16, M.Neck, 0, 0.58f, 1.25f, -15, 1, neck: 4));
        hydra.parts.Add(P("n4_neck3", S("boss_hydra/neck3s"), "n4_neck2", 0.182f, 0.479f, 0.798f, 0.479f, 15, M.Neck, 0, 0.58f, 1.25f, 70, 1, neck: 4));
        hydra.parts.Add(P("n4_head", S("boss_hydra/head"), "n4_neck3", 0.202f, 0.479f, 0.93f, 0.638f, 14, M.Neck, 4, 0.58f, 1.25f, 47, 0.85f, alt: S("boss_hydra/head_bite"), neck: 4));
        hydra.parts.Add(P("n5_neck1", S("boss_hydra/neck1s"), "body", 0.72f, 0.28f, 0.835f, 0.479f, 25, M.Neck, 24, 0.86f, 0.8f, 96, 1.323f, flip: true, neck: 5, wind: 70, strike: 230));
        hydra.parts.Add(P("n5_neck2", S("boss_hydra/neck2s"), "n5_neck1", 0.165f, 0.479f, 0.818f, 0.479f, 24, M.Neck, 0, 0.86f, 0.8f, 35, 1, neck: 5));
        hydra.parts.Add(P("n5_neck3", S("boss_hydra/neck3s"), "n5_neck2", 0.182f, 0.479f, 0.798f, 0.479f, 23, M.Neck, 0, 0.86f, 0.8f, 70, 1, neck: 5));
        hydra.parts.Add(P("n5_head", S("boss_hydra/head"), "n5_neck3", 0.202f, 0.479f, 0.93f, 0.638f, 22, M.Neck, 4, 0.86f, 0.8f, 31, 0.85f, alt: S("boss_hydra/head_bite"), neck: 5));
        hydra.parts.Add(P("n6_neck1", S("boss_hydra/neck1s"), "body", 0.84f, 0.4f, 0.835f, 0.479f, 33, M.Neck, 18, 0.27f, 1.05f, 60, 0.935f, flip: true, neck: 6, wind: 70, strike: 230));
        hydra.parts.Add(P("n6_neck2", S("boss_hydra/neck2s"), "n6_neck1", 0.165f, 0.479f, 0.818f, 0.479f, 32, M.Neck, 0, 0.27f, 1.05f, 70, 1, neck: 6));
        hydra.parts.Add(P("n6_neck3", S("boss_hydra/neck3s"), "n6_neck2", 0.182f, 0.479f, 0.798f, 0.479f, 31, M.Neck, 0, 0.27f, 1.05f, -15, 1, neck: 6));
        hydra.parts.Add(P("n6_head", S("boss_hydra/head"), "n6_neck3", 0.202f, 0.479f, 0.93f, 0.638f, 30, M.Neck, 4, 0.27f, 1.05f, -17, 0.85f, alt: S("boss_hydra/head_bite"), neck: 6));
        hydra.parts.Add(P("tail0", S("boss_hydra/tail"), "body", 0.93f, 0.55f, 0.08f, 0.38f, 4, M.Sway, 7, 0, 0.8f, 16, 1.1f));
        hydra.parts.Add(P("tail1", S("boss_hydra/tail"), "body", 0.93f, 0.62f, 0.08f, 0.38f, 3, M.Sway, 7, 0.3f, 0.9f, 4, 1.1f));
        hydra.parts.Add(P("tail2", S("boss_hydra/tail"), "body", 0.93f, 0.69f, 0.08f, 0.38f, 4, M.Sway, 7, 0.6f, 1, -8, 1.1f));
        hydra.parts.Add(P("tail3", S("boss_hydra/tail"), "body", 0.93f, 0.76f, 0.08f, 0.38f, 3, M.Sway, 7, 0.85f, 1.1f, -20, 1.1f));
        hydra.parts.Add(P("leg0", S("boss_hydra/legF1"), "body", 0.2f, 0.86f, 0.55f, 0.2f, 60, M.Walk, 20, 0, 1, 0, 1.25f));
        hydra.parts.Add(P("leg1", S("boss_hydra/legB1"), "body", 0.4f, 0.86f, 0.55f, 0.2f, 61, M.Walk, 20, 0.5f, 1, 0, 1.25f));
        hydra.parts.Add(P("leg2", S("boss_hydra/legF2"), "body", 0.6f, 0.86f, 0.55f, 0.2f, 62, M.Walk, 20, 0, 1, 0, 1.25f));
        hydra.parts.Add(P("leg3", S("boss_hydra/legB2"), "body", 0.79f, 0.86f, 0.55f, 0.2f, 63, M.Walk, 20, 0.5f, 1, 0, 1.25f));
        hydra.parts.Add(P("leg4Far", S("boss_hydra/legF2"), "body", 0.27f, 0.83f, 0.55f, 0.2f, 2, M.Walk, 20, 0.5f, 1, 0, 1.25f, far: true));
        hydra.parts.Add(P("leg5Far", S("boss_hydra/legB2"), "body", 0.47f, 0.83f, 0.55f, 0.2f, 2, M.Walk, 20, 0, 1, 0, 1.25f, far: true));
        hydra.parts.Add(P("leg6Far", S("boss_hydra/legF1"), "body", 0.67f, 0.83f, 0.55f, 0.2f, 2, M.Walk, 20, 0.5f, 1, 0, 1.25f, far: true));
        hydra.parts.Add(P("leg7Far", S("boss_hydra/legB1"), "body", 0.86f, 0.83f, 0.55f, 0.2f, 2, M.Walk, 20, 0, 1, 0, 1.25f, far: true));
        hydra.parts.Add(P("emb0", S("boss_hydra/emb_a"), "body", 0.24f, 0.62f, 0.55f, 0.55f, 51, M.Twitch, 9, 0.1f, 0.9f, 25, 0.85f, alt: S("boss_hydra/emb_c")));
        hydra.parts.Add(P("emb1", S("boss_hydra/emb_b"), "body", 0.45f, 0.42f, 0.55f, 0.55f, 52, M.Twitch, 9, 0.45f, 1.2f, -150, 0.8f, alt: S("boss_hydra/emb_d")));
        hydra.parts.Add(P("emb2", S("boss_hydra/emb_e"), "body", 0.62f, 0.62f, 0.55f, 0.55f, 53, M.Twitch, 9, 0.8f, 0.7f, 70, 0.8f, flip: true));
        hydra.parts.Add(P("emb3", S("boss_hydra/emb_d"), "body", 0.82f, 0.55f, 0.55f, 0.55f, 54, M.Twitch, 9, 0.3f, 1, -15, 0.85f, flip: true, alt: S("boss_hydra/emb_c")));
        hydra.parts.Add(P("emb4", S("boss_hydra/emb_f"), "body", 0.36f, 0.78f, 0.55f, 0.55f, 55, M.Twitch, 9, 0.65f, 1.3f, 160, 0.75f, flip: true));
        hydra.parts.Add(P("emb5", S("boss_hydra/emb_c"), "body", 0.7f, 0.8f, 0.55f, 0.55f, 56, M.Twitch, 9, 0.95f, 0.8f, -35, 0.75f, alt: S("boss_hydra/emb_b")));
        return hydra;
    }

    // 에셋의 다른 보스 값은 그대로 두고 히드라 줄만 넣거나 바꿈
    public static void AddHydra()
    {
        var lib = AssetDatabase.LoadAssetAtPath<BossPartArt>(AssetPath);
        lib.entries.RemoveAll(e => e.codeId == "boss_hydra");
        lib.entries.Add(Hydra());
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        var missing = new List<string>();
        foreach (var p in lib.Get("boss_hydra").parts) if (!p.sprite) missing.Add(p.name);
        Debug.Log("[BossPartArt] 히드라 " + lib.Get("boss_hydra").parts.Count + "파츠" + (missing.Count > 0 ? " · 그림 없음: " + string.Join(", ", missing) : ""));
    }
}
