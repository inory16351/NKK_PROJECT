import fs from 'node:fs/promises';
import {createRequire} from 'node:module';
const req=createRequire('C:/Users/user/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/package.json');const sharp=req('sharp');
const root='C:/Project/NKK_PROJECT';const dst=root+'/기획서/스토리보드_자료';await fs.mkdir(dst,{recursive:true});let panels=[];
for(let i=0;i<8;i++){let x=[0,490,980,1470][i%4];let y=i<4?18:308;panels.push({input:await sharp(root+'/UnityResources/Rats/SuperUlt/preview/icequeen_v3_super.png').extract({left:x,top:y,width:480,height:270}).toBuffer(),left:(i%4)*480,top:Math.floor(i/4)*270});}
await sharp({create:{width:1920,height:540,channels:4,background:'#FFFFFF'}}).composite(panels).png().toFile(dst+'/얼음쥐_현재연출_장면모음.png');
