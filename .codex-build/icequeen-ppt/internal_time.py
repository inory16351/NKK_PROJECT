from pathlib import Path
import re
root=Path('C:/Project/NKK_PROJECT')
md=root/'기획서/얼음쥐_초필살기_현재연출_스토리보드.md'
s=md.read_text(encoding='utf-8-sig')
s=s.replace('아래 시간은 현재 설정에서 환산한 이론값이며, 캡처 좌상단 시간은 캡처 당시의 표기이므로 일치한다고 보장하지 않는다.', '모든 시간은 발동 순간을 0초로 둔 연출 내부 타임라인 기준이다. 캡처는 장면 확인용으로 사용한다.')
s=s.replace('시간 기준은 발동 순간 0초. 컷신 내부 시각은 0.75배로 재생하고, 전장 공격은 정상 속도로 진행한다.', '시간 기준은 발동 순간 0.00초부터 연출 종료 7.10초까지의 내부 타임라인이다. 모든 컷, 전환, 공격 시점을 이 기준으로 표기한다.')
s=s.replace('| 구간 | 내부 시각 | 실제 경과 시간(약) | 역할 |','| 구간 | 내부 시간 | 역할 |').replace('|---|---:|---:|---|','|---|---:|---|')
s=re.sub(r'(?m)^(\| (?:진입|컷 1–6|컷신 걷힘|전장 우박|마지막 일격·종료) \| [^|]+) \| [^|]+ \|',r'\1초 |',s)
s=s.replace('전체 약 **8.37초**. 프레임 지연 등 실제 환경에 따라 달라진다. 과거의 ‘전체 5초’ 버전과 다르며, 별도의 피니시 포즈 컷은 없다.', '내부 타임라인 전체 길이는 **7.10초**다. 별도의 피니시 포즈 컷은 없다.')
s=s.replace('실제 시간(약) / 내부 시각','내부 시간')
s=re.sub(r'(?m)^(\| \d\. [^|]+ \| )[^|]+ / ([^|]+)( \|)',r'\1\2초\3',s)
s=s.replace('내부 0.95초(실제 약 1.27초)','0.95초').replace('내부 2.22초 / 실제 약 2.96초 착지.', '2.22초 착지.').replace('정상 속도 기준 0.30초 간격','0.30초 간격')
s=s.replace('우박, 재생 속도','우박, 내부 시각').replace('actEnd 3.4, playRate 0.75, cutFade 0.4','actEnd 3.4, cutFade 0.4').replace('재생 속도 환산, 전장 단계 전환','내부 타임라인, 전장 단계 전환')
# Cropped panel omits capture timestamps while retaining the visual evidence.
s=s.replace('../UnityResources/Rats/SuperUlt/preview/icequeen_v3_super.png','스토리보드_자료/얼음쥐_현재연출_장면모음.png')
md.write_text(s,encoding='utf-8')
b=root/'.codex-build/icequeen-ppt'
t=(b/'restyle.mjs').read_text(encoding='utf-8-sig')
t=t.replace("['길이','설정 환산 약 8.37초']", "['내부 시간','0.00~7.10초']")
needle="s=deck.slides.items[3];table"
insertion="""const timeline=[['0.00–0.40초','진입','전장에서 초필살기 컷신으로 전환'],['0.40–1.90초','컷 1–3','오로라 하늘, 소환 동작, 얼굴 클로즈업'],['1.90–3.40초','컷 4–6','얼음성 낙하, 눈보라 상승, 우박과 화이트아웃'],['3.40–3.80초','컷신 걷힘','원래 전장이 드러남'],['3.80–6.80초','전장 우박','3초 동안 다단 10타'],['6.80–7.10초','마지막 일격','큰 충돌과 마무리 문구, 연출 종료']];
rows=timeline;
const ranges=['0.40–0.85초','0.85–1.40초','1.40–1.90초','1.90–2.50초','2.50–3.00초','3.00–3.80초'];
for(let [i,c]of cuts.entries()){c[1]='내부 시간 '+ranges[i];for(let block of c[4]){block[1]=block[1].replace('약 1.27초','0.95초').replace('실제 약 2.96초','2.22초').replace('4.53초부터 걷힘. 5.07초에 전장 공격 시작.','3.40초부터 걷힘. 3.80초에 전장 공격 시작.');}}
s=deck.slides.items[3];table"""
t=t.replace(needle,insertion).replace("['실제 경과 시간','구간','내용']", "['내부 시간','구간','내용']")
t=t.replace('컷신 내부 시각은 0.75배로 재생하고 전장 우박은 정상 속도로 진행한다.\\n3.80 ÷ 0.75 + 3.00 + 0.30 = 약 8.37초. 실제 재생 시간은 환경에 따라 달라진다.', '모든 시간은 발동 순간 0.00초를 기준으로 한 연출 내부 타임라인이다.\\n컷신 0.00~3.40초, 걷힘 3.40~3.80초, 전장 우박 3.80~6.80초, 종료 7.10초.')
t=t.replace("c[1]='내부 시간 '","c[1]='내부 시간 '")
t=t.replace("cuts[i][1].split('  /')[0]", "cuts[i][1]")
t=t.replace("['5.07~8.07초'", "['3.80~6.80초'").replace("['8.07초'", "['6.80초'").replace("['8.37초'", "['7.10초'")
t=t.replace('restyled-candidate','internal-candidate').replace('/restyled-${i+1}', '/internal-${i+1}').replace('restyled-validation','internal-validation')
t=t.replace('얼음쥐_초필살기_스토리보드_기획서양식.pptx','얼음쥐_초필살기_스토리보드_내부시간.pptx')
t=t.replace("t=''){s.speakerNotes.text=", "t=''){s.speakerNotes.text='시간 기준: 연출 내부 시간 0.00~7.10초.\\n'+")
(b/'internal.mjs').write_text(t,encoding='utf-8')
