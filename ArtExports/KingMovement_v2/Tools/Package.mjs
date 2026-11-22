import fs from 'node:fs/promises';
import path from 'node:path';
const root=path.resolve(import.meta.dirname,'..');
function blocks(b,start) {let p=start;while(b[p]!==0){p+=b[p]+1;}return p+1;}
function imageBlock(b) {
 let packed=b[10],p=13,table;
 if(packed&128){table=b.subarray(p,p+3*(2**((packed&7)+1)));p+=table.length;}
 while(p<b.length) {
  if(b[p]===0x21){p=blocks(b,p+2);continue;}
  if(b[p]===0x2c) {
   let desc=Buffer.from(b.subarray(p,p+10));p+=10;
   if(desc[9]&128){let n=3*2**((desc[9]&7)+1);table=b.subarray(p,p+n);p+=n;}
   else desc[9]=(desc[9]&0x40)|0x80|(packed&7);
   let end=blocks(b,p+1);
   return Buffer.concat([desc,table,b.subarray(p,end)]);
  }
  throw Error('Unexpected GIF block');
 }
 throw Error('Missing image');
}
for(const action of ['Walk','Run']) {
 const parts=[];
 for(let i=0;i<8;i++) {
  let b=await fs.readFile(path.join(root,'Preview',`${action}_${i}.gif`));
  if(i===0){
   let header=Buffer.from(b.subarray(0,13+((b[10]&128)?3*2**((b[10]&7)+1):0)));
   header.write('GIF89a',0,'ascii');parts.push(header,Buffer.from([0x21,0xff,11,...Buffer.from('NETSCAPE2.0'),3,1,0,0,0]));
  }
  parts.push(Buffer.from([0x21,0xf9,4,8,action==='Walk'?10:7,0,0,0]),imageBlock(b));
 }
 parts.push(Buffer.from([0x3b]));
 await fs.writeFile(path.join(root,'Preview',`King_${action}.gif`),Buffer.concat(parts));
}
const manifest={version:2,character:'King',frameSize:{width:160,height:160},alpha:'binary',filter:'point',pixelsPerUnit:128,pivot:{x:0.6,y:0.1},directions:{right:'authored',left:'SpriteRenderer.flipX',up:null,down:null},animations:{Walk:{frames:8,fps:10,loop:true,strip:'Frames/King_Walk_Strip.png'},Run:{frames:8,fps:14,loop:true,strip:'Frames/King_Run_Strip.png'}},idle:'Static hold of Walk_01, not a separate authored idle cycle',motion:'in-place; no root motion'};
await fs.writeFile(path.join(root,'manifest.json'),JSON.stringify(manifest,null,2));
await fs.writeFile(path.join(root,'Preview','index.html'),`<!doctype html><html lang="zh-CN"><meta charset="utf-8"><title>King 动作预览</title><style>body{background:#202530;color:#eee;font:16px system-ui;padding:32px}main{display:flex;gap:32px;flex-wrap:wrap}article{border:1px solid #465168;border-radius:12px;padding:20px}img{image-rendering:pixelated;width:320px;height:320px}h1{font-size:24px}p{color:#bbc6d8}</style><h1>King · 走路 / 奔跑</h1><p>160 × 160 / 透明 PNG / 固定锚点 / 原地循环</p><main><article><h2>走路 · 8 帧 · 10 fps</h2><img src="King_Walk.gif"></article><article><h2>奔跑 · 8 帧 · 14 fps</h2><img src="King_Run.gif"></article></main><p>原作朝右，朝左使用镜像。GIF 使用深色背景便于检查轮廓，游戏用 PNG 为透明背景。</p></html>`);
console.log('Animated GIFs, preview page and manifest packaged.');
