import fs from 'node:fs/promises';
import {Presentation,PresentationFile,FileBlob} from 'file:///C:/Users/user/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const dir='C:/Project/NKK_PROJECT/기획서/';
for(const [name,tag] of [['20261016_프로젝트 NKK_초필살기 기획서.pptx','super'],['20261009_프로젝트 NKK_튜토리얼 기획서_정승운.pptx','tuto']]){
 let p=await PresentationFile.importPptx(await FileBlob.load(dir+name));await fs.writeFile(`.codex-build/icequeen-ppt/${tag}.inspect`,(await p.inspect({kind:'slide,textbox,shape,layout',maxChars:150000})).ndjson);
 console.log(tag,p.slides.items.length,p.masters.items.length);
 for(let [i,s] of p.slides.items.entries()){if(tag==='tuto'&&![0,6,7,9].includes(i))continue;let b=await s.export({format:'png',scale:1});await fs.writeFile(`.codex-build/icequeen-ppt/${tag}-${i+1}.png`,new Uint8Array(await b.arrayBuffer()));}
 await fs.writeFile(`.codex-build/icequeen-ppt/${tag}.proto.json`,JSON.stringify(p.toProto()));
}

