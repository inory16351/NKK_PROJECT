# 운석 이펙트 (2026-10-06)

공룡 잠옷 쥐 필살기 "대멸종"의 운석 비 + 모든 운석 낙하(메테오·궤도 폭격 액션, 공용 스킬 치즈 운석)에 쓰는 이펙트.
코드: `Assets/Scripts/Items/ItemManager.Meteor.cs` (ItemManager 인스펙터 "운석 이펙트"에 연결)

| 파일 | 쓰임 |
|---|---|
| `meteor_fire.png` | 불꼬리 달린 운석 (기본 운석이 비스듬히 떨어질 때) |
| `meteor_rock.png` | 불 없는 운석 돌 (여분 · 굴리기용) |
| `meteor_flame.png` | 불꼬리만 (치즈 운석처럼 다른 그림 운석 뒤에 붙임, 진행 반대 방향으로 돌림) |
| `meteor_burst.png` | 착지 폭발 (확 커졌다 사라짐) |
| `meteor_crater.png` | 크레이터 자국 (바닥에 몇 초 남았다 옅어짐) |
| `meteor_smoke.png` | 연기 (떠오르며 커짐) |
| `meteor_debris.png` | 튀는 돌 파편 |
| `meteor_target.png` | 낙하 지점 표시 (떨어지기 전 바닥에서 깜빡) |
| `Sheets/fx_meteor.png` | Codex 원본 시트 (자홍 배경 4×2) · `prompt_meteor.txt` · `log_meteor.txt` · 참고 이미지 `ref_*.png` |
| `_미리보기.png` | 잘라낸 결과 모아 보기 |

- 자르기: 시트 전체에서 그림 덩어리를 찾아 칸 중심으로 배정 (칸 경계를 넘어 그려진 불꼬리도 안 잘림)
- Unity 사본: `Assets/Art/Rats/FX_Meteor/`
