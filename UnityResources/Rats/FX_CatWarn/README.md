# 고양이 무리 스킬 경고 · 이펙트

Codex 로 만든 그림 (투명 PNG, UI 키트와 같은 플랫). 유니티 `Assets/Art/FX_CatWarn/` 에 같은 파일 복사.
원본 시트·프롬프트·로그: `Sheets/` (`cat_warn.png` 3×2, `cat_fx.png` 2×2, 참고 그림 = `UI_Kit/_미리보기.png`). 자홍 배경 → 칸별로 자름.

| 파일 | 용도 (Game 씬 `CatManager/CatWarn` 자식) |
|---|---|
| cat_warn_rim.png | 바닥 경고 원 테두리 (최종 범위, 깜빡) `WarnRim` |
| cat_warn_fill.png | 바닥 경고 원 채움 (웅크리는 동안 커짐) `WarnFill` |
| cat_claw.png | 착지 자리 발톱 자국 `ClawMark` (clawSprite) |
| cat_alert.png | 고양이 머리 위 느낌표 `Alert` |
| cat_crack.png | 뱃살 프레스·지진·운석 착지 균열 (crackSprite) |
| cat_vortex.png | 블랙홀 소용돌이 (돌아감) `Vortex` |
| cat_laser_pillar.png | 스핑크스 하늘 레이저 기둥 `LaserTemplate` |
| cat_meteor.png | 마녀 고양이 운석 `MeteorTemplate` |
| cat_target.png · stun_stars.png | 아직 안 씀 (표적 화살표 · 기절 별) |
