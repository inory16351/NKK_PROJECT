import fs from 'node:fs/promises';
import {createRequire} from 'node:module';
import {Presentation,PresentationFile} from 'file:///C:/Users/user/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const req=createRequire('C:/Users/user/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/package.json');
const sharp=req('sharp');
const root='C:/Project/NKK_PROJECT', build=root+'/.codex-build/icequeen-ppt';
const art=root+'/UnityResources/Rats/SuperUlt/icequeen/';
const skill='C:/Users/user/.codex/plugins/cache/openai-primary-runtime/presentations/26.1015.12040/skills/presentations';
const p=Presentation.create({slideSize:{width:1280,height:720}});
p.theme.defaultFont='Malgun Gothic';
const C={bg:'#F3F8FC',ink:'#15354D',blue:'#237DA9',muted:'#526E82'};
function text(s,t,x,y,w,h,size=25,color=C.ink,bold=false){let a=s.shapes.add({geometry:'textbox',position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});a.text=t;a.text.style={typeface:'Malgun Gothic',fontSize:size,color,bold};return a;}
function slide(title,sub){let s=p.slides.add();s.background.fill=C.bg;text(s,title,56,35,1168,65,40,C.ink,true);if(sub)text(s,sub,58,106,1160,45,21,C.blue);text(s,String(p.slides.items.length).padStart(2,'0'),1172,675,50,24,16,C.muted);return s;}
async function img(s,file,x,y,w,h){let b=await fs.readFile(file);s.images.add({blob:b.buffer.slice(b.byteOffset,b.byteOffset+b.byteLength),contentType:'image/png',alt:file.split('/').at(-1),fit:'contain',position:{left:x,top:y,width:w,height:h}});}
function notes(s,extra=''){s.speakerNotes.text='출처: '+root+'/기획서/얼음쥐_초필살기_현재연출_스토리보드.md\n'+root+'/Tools/superult_story.py icequeen(), WORLD, IMAGE_FX\n'+root+'/Unity_Making/NKK_Project/Assets/Scenes/Game.unity code: icequeen\n'+root+'/Unity_Making/NKK_Project/Assets/Scripts/Ults/SuperUltManager.cs\n'+extra;}
let s=slide('얼음쥐 초필살기 스토리보드','아이스 퀸 쥐  /  영원한 겨울  /  현재 구현 기준');
text(s,'얼음성을 소환하고\n전장에 우박을 쏟는다',60,223,640,150,46,C.ink,true);
text(s,'2026.10.19\n6컷 컷신과 전장 공격 단계',62,489,660,90,25,C.muted);await img(s,art+'pose_intro.png',790,169,390,440);notes(s);
s=slide('전체 연출 순서','발동부터 종료까지 약 8.37초');
const rows=[['0.00–0.53초','진입','전장에서 초필살기 컷신으로 전환'],['0.53–2.53초','컷 1–3','오로라 하늘, 소환 동작, 얼굴 클로즈업'],['2.53–4.53초','컷 4–6','얼음성 낙하, 눈보라 상승, 우박과 화이트아웃'],['4.53–5.07초','컷신 걷힘','원래 전장이 드러남'],['5.07–8.07초','전장 우박','정상 속도 3초 동안 다단 10타'],['8.07–8.37초','마지막 일격','큰 충돌과 마무리 문구, 연출 종료']];
rows.forEach((r,i)=>{let y=181+i*67;text(s,r[0],60,y,235,47,25,C.blue,true);text(s,r[1],310,y,220,47,25,C.ink,true);text(s,r[2],540,y,680,48,23);});text(s,'시간은 설정 환산값. 컷신은 0.75배, 전장 공격은 정상 속도.',60,615,1130,42,21,C.muted);notes(s,'컷신 3.8/0.75 + 전장 3.0 + 종료 0.3 = 약 8.37초.');
const cuts=[
['01  겨울의 전조','0.53–1.13초  /  내부 0.40–0.85','pano_on.png','파노라마 원본 아트',[
['화면','오로라가 흐르는 하늘에 큰 눈송이가 모여든다.'],['카메라','하늘에서 아래로 빠르게 틸트. 줌 1.25에서 1.10.'],['이펙트','세로 속도선과 회전하는 눈송이 6개.'],['공격·소리','피해 없음. 컷인음 superjump_charge.']]],
['02  얼음성 소환','1.13–1.87초  /  내부 0.85–1.40','anim_dash_2.png','소환 동작 프레임 원본',[
['화면','언 호수 위 여왕이 웅크렸다 두 앞발을 하늘로 든다.'],['카메라','여왕을 확대. 줌 1.70에서 1.90, 짧은 흔들림.'],['이펙트','서리 오라가 커지고 눈송이가 위로 솟는다.'],['제목·판정','약 1.27초에 제목 띠가 빠짐. 아직 피해 없음.']]],
['03  여왕의 얼굴','1.87–2.53초  /  내부 1.40–1.90','face_close.png','얼굴 레이어 원본',[
['화면','얼굴을 크게 잡아 소환의 주체를 강조한다.'],['카메라','배경과 얼굴을 분리한 클로즈업 줌.'],['이펙트','전환 순간 흰 플래시, 얼굴 주변 눈송이 회전.'],['레이어','face_close와 face_bg를 따로 합성. 피해 없음.']]],
['04  얼음성 강림','2.53–3.33초  /  내부 1.90–2.50','prop_castle.png','얼음성 소품 원본',[
['화면','거대한 성이 빈 호수에 낙하. 여왕은 오른쪽 아래.'],['카메라','호수 하단을 잡고 착지 순간 강하게 흔들림.'],['핵심 시점','실제 약 2.96초에 착지. 충격광, 원형 파동, 파편.'],['문구·판정','“세상이 얼어붙는다…”\n성 착지에는 별도 공격 판정 없음.']]],
['05  눈보라 상승','3.33–4.00초  /  내부 2.50–3.00','pano_alt.png','얼어붙은 파노라마 원본',[
['화면','얼음성에서 눈보라가 하늘로 솟구친다.'],['카메라','성에서 하늘로 위쪽 틸트. 줌 1.50에서 1.25.'],['이펙트','상승하는 얼음 줄기와 눈송이.'],['밈','상단에 “얼어 죽어도 아이스 아메리카노”.']]],
['06  하늘을 덮는 우박','4.00–5.07초  /  내부 3.00–3.80','fx_shard3.png','우박에 사용하는 얼음 조각 원본',[
['화면','우박이 비스듬히 떨어지고 화면이 하얗게 덮인다.'],['카메라','하늘로 줌 인하며 짧게 흔들림.'],['이펙트','얼음 조각 22개와 낙하 궤적. 화이트아웃 강화.'],['전장 연결','4.53초부터 걷힘. 5.07초에 전장 공격 시작.']]]
];
for(const [title,time,file,label,blocks] of cuts){s=slide(title,time);await img(s,art+file,55,174,570,435);text(s,label,58,625,570,32,18,C.muted);blocks.forEach(([a,b],i)=>{let y=174+i*116;text(s,a,675,y,540,35,23,C.blue,true);text(s,b,675,y+37,540,74,24);});notes(s,'이미지는 현재 사용 중인 원본 에셋이며 해당 컷의 완성 프레임 재현은 아니다.');}
const src=root+'/UnityResources/Rats/SuperUlt/preview/icequeen_v3_super.png';
await sharp(src).extract({left:980,top:308,width:480,height:270}).png().toFile(build+'/world.png');
s=slide('전장 우박과 다단 10타','5.07–8.07초  /  내부 3.80–6.80');await img(s,build+'/world.png',55,188,704,396);text(s,'기존 플레이 캡처에서 전장 구간 발췌',58,608,710,38,18,C.muted);
text(s,'3초간 전장 전체로 우박 낙하\n\n0.30초 간격으로 10타\n첫 타는 구간 시작 후 0.15초\n\n대상별 충돌 그림과 피해 숫자\n\n화면 안 생명체 빙결 반복 갱신',800,181,420,430,25);notes(s,'플레이 캡처: '+src+'\n새 재생 확인이 아닌 기존 저장 캡처.');
s=slide('마지막 일격과 빙결의 여운','8.07–8.37초  /  내부 6.80–7.10');await img(s,art+'fx_hail_splash3.png',70,200,530,360);text(s,'마지막 일격에 쓰는 충돌 애니메이션의 마지막 프레임',58,609,590,45,18,C.muted);
text(s,'“전부 꽁꽁!!”',676,182,540,70,41,C.blue,true);text(s,'대상 위치에 더 큰 우박 충돌\n중앙 충돌 효과와 흰 플래시 0.45\n\nwall_blast 재생 후 0.30초 뒤 종료\n\n별도 피니시 포즈 컷 없이 전장에서 마무리\n빙결은 종료 뒤 약 1.5초 여운을 주도록 갱신',675,283,540,330,25);notes(s,'freezeAll은 0.5초 주기로 남은 시간 + 1.5초를 갱신. 실제 잔여 시간은 대상과 갱신 시점에 따라 달라짐.');
s=slide('공격 처리와 연출 검토 기준','컷신이 걷힌 뒤 실제 전장에서 공격');
text(s,'현재 처리',60,181,520,45,29,C.blue,true);text(s,'다단 10타: 기본 피해 ×4씩\n마지막 일격: 기본 피해 ×20\n\n피해 숫자는 대상별 월드 팝업\n보스 상한과 대상 상태에 따라 실제 피해 변동\n\n우박 충돌은 3프레임, 프레임 간격 0.07초',60,246,540,350,25);
text(s,'검토할 지점',695,181,520,45,29,C.blue,true);text(s,'성 착지가 충분히 무겁게 느껴지는가\n\n성에서 하늘, 전장으로 공격이 이어지는가\n\n화이트아웃 뒤 피해 숫자가 읽히는가\n\n제목과 밈이 여왕의 동작을 가리지 않는가\n\n마지막 일격 문구가 충분히 보이는가',695,246,530,390,24);notes(s,'RatTable.json Super_Config hit_count=10, hit_mul=4, finish_mul=20.');
s=slide('문서 기준과 구현 근거','2026.10.19 현재 저장 상태');
text(s,'확인 범위',60,177,1130,44,29,C.blue,true);text(s,'저장된 씬과 실행 코드, 원본 아트, 기존 플레이 캡처를 대조했다.\n시간은 설정을 환산한 값이며 이번 문서화에서 새 Play 재생이나 소리 청취는 하지 않았다.',60,239,1145,100,25);
text(s,'현재 버전 판별',60,365,1130,44,29,C.blue,true);text(s,'6컷 소환 컷신과 전장 우박 단계. 전체 약 8.37초.\n활강 중심 8컷과 화면 균열·산산조각 피니시는 이전 버전이다.',60,425,1145,93,25);
text(s,'근거 파일',60,551,190,38,23,C.blue,true);text(s,'Tools/superult_story.py, Game.unity, SuperUltManager.cs\nRatTable.json, SuperUlt/preview/icequeen_v3_super.png\n기획서/얼음쥐_초필살기_현재연출_스토리보드.md',260,549,950,100,21,C.muted);notes(s,'각 슬라이드 노트에 관련 로컬 소스 경로 기록.');
await (await PresentationFile.exportPptx(p)).save(build+'/candidate.pptx');
for(let i=0;i<p.slides.items.length;i++){let b=await p.export({slide:p.slides.items[i],format:'png',scale:1});await fs.writeFile(build+`/slide-${i+1}.png`,new Uint8Array(await b.arrayBuffer()));}
const {finalizePresentation}=await import('file:///'+skill+'/container_tools/artifact_tool_utils.mjs');
let result=await finalizePresentation({workspaceDir:root,candidatePath:build+'/candidate.pptx',finalPath:root+'/기획서/PPT/얼음쥐_초필살기_스토리보드.pptx',pythonExecutable:'C:/Users/user/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe',integrityValidatorPath:skill+'/container_tools/inspect_presentation_package_integrity.py',layoutValidatorPath:skill+'/container_tools/inspect_presentation_layout_geometry.py',layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-bullet-geometry','--validate-heading-fit'],fontPolicy:{basis:'design',families:['Malgun Gothic']},verifyArtifactToolImport:true,receiptPath:build+'/validation.json'});console.log(JSON.stringify(result));


