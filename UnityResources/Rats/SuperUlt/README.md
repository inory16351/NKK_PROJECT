# 초필살기 레이어 컷신 그림 (2026-10-17, Codex)

메이플 오리진 스킬식: 배경 · 캐릭터 포즈 · 이펙트 · 밈 소품을 **따로** 그려서 컷신에서 각각 움직임 (`Ults/SuperUltLayer.cs` 키프레임).
쥐마다 폴더 `<code>/` — 원본·프롬프트·로그·참고 그림 = `<code>/Sheets/`. 미리보기 GIF = `preview/`.

## 만드는 법
1. `<code>/Sheets/prompt_*.txt` (공통: 그림체 = 뽑기 컷 그림 `ref_style` · 캐릭터 = `ref_zapham`/`ref_parts`, 글자 없음)
   - `bg_*.png` 배경 1536×1024 (불 켜짐 → 같은 그림을 참고로 넣어 '불 꺼짐' 편집본을 뽑으면 구도가 같아 전환에 씀)
   - `*_pose_*.png` 캐릭터 한 포즈 1024×1536, 자홍 배경
   - `fx_bolts.png` 검은 배경 3×2 빛 효과 (밝기 = 투명도)
   - `props.png` 자홍 배경 소품 여러 개
2. `bash Tools/codex.sh exec --skip-git-repo-check --ephemeral -s workspace-write -C <Sheets> -i ref_style.png -i ref_zapham.png -i ref_parts.png < prompt_X.txt` (여러 개 동시 가능)
   - 안전 필터에 걸리면(예: '전투 함성·주먹') 귀엽고 밝은 쪽으로 문구를 바꿔 다시.
3. `python Tools/slice_superult.py <code>` → 키잉(자홍 + 분홍 번짐 걷기 · 검은 배경) · 자르기 · 소품 자동 분리(`PROPS` 표에 이름·대략 위치) → `Assets/Art/Rats/SuperUlt/<code>/` 복사
4. 씬: Game `HUD/SuperUlt/Cut/Frame/Scenes/<쥐>` 에 레이어 배치 (scratchpad 식 build2.cs 를 MCP execute_code 로), `Styles/<쥐>.scene` 연결

## 큰 배경 + 가상 카메라 (v3, 사용자: 카메라 무빙 · 배경을 크게)
- 세로 파노라마: `pano_a`(하늘) → `pano_b`(도시) → `pano_c`(지붕), 다음 장은 앞 장 아래 400px(`ref_prev_bottom.png`)를 보고 이어 그림 → `python Tools/stitch_vertical.py pano_on.png auto pano_a.png pano_b.png pano_c.png` (겹침 399 · 393).
- 대정전 버전: 조각마다 켜짐 그림을 참고로 '불만 끈' 편집본 `pano_?_off` → **같은 겹침 값**으로 이어 붙여 `pano_off` (구도가 같아 켜짐↔꺼짐 전환).
- 씬: 묶음마다 `Rig`(줌·기울기·충격 kicks) > `Track`(위치 = -보는 곳) 안에 파노라마·캐릭터·이펙트를 파노라마 좌표로 둠 → 카메라 키만 바꾸면 무빙. 속도선·배터리·클로즈업·밈 글은 화면 좌표.
- 클로즈업 컷 `zap_face_close` (1536×1024 전체 화면).

## zapham (찌릿 햄찌 · 도시 대정전)
- pano_on / pano_off (세로 파노라마, v3) · bg_city_on / bg_city_off (v2, 안 씀)
- zap_pose_absorb (플러그를 하늘로 · ②) · zap_pose_dash (날아오르기 · ③) · zap_pose_finish (하늘 가리키며 윙크 · ④)
- fx_bolt_big · fx_bolt_fork · fx_ring_shock · fx_streak · fx_starburst · fx_aura
- prop_bill (전기요금 고지서) · prop_battery (스마트폰 배터리 틀, 채우기는 UI) · prop_bulb (터지는 전구) · prop_socket · prop_roof (앞 지붕)
- 밈: 배터리 충전 1% → 100% (② 전기가 빨려 듦) · "※ 이번 달 전기세는 연구소가 냅니다" + 날아가는 고지서 (④)
