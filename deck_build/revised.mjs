import fs from 'node:fs/promises';
import {Presentation,PresentationFile} from '@oai/artifact-tool';

const deck=Presentation.create({slideSize:{width:1280,height:720}});
const ink='#181322', plum='#442040', pink='#D54487', pale='#FAEBD8', gold='#FFE278', cyan='#A7E8F0', lilac='#E9D6F5';
const art='Assets/ActiveMotion3D/Art/UI/';
const build='deck_build/';
const doc='Docs/SheerHeartAttack/';
function block(s,x,y,w,h,fill){
  return s.shapes.add({geometry:'rect',position:{left:x,top:y,width:w,height:h},fill,line:{fill:'none',width:0}});
}
function txt(s,value,x,y,w,h,size,{color=pale,font='Arial',bold=false}={}){
  const shape=s.shapes.add({geometry:'textbox',position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});
  shape.text=value;
  shape.text.style={typeface:font,fontSize:size,bold,color,insets:{left:0,right:0,top:0,bottom:0},wrap:'none'};
  return shape;
}
function title(s,value,x,y,w,h,size,color=pale){return txt(s,value,x,y,w,h,size,{color,font:'Impact'});}
function label(s,value,x,y,w=650,color=gold){txt(s,value,x,y,w,28,17,{bold:true,color});}
function body(s,value,x,y,w,h,size=24,color=pale){txt(s,value,x,y,w,h,size,{color});}
function slide(bg,notes){const s=deck.slides.add();s.background.fill=bg;s.speakerNotes.text=notes;return s;}
async function pic(s,file,x,y,w,h,fit='cover',alt=file){
  const b=await fs.readFile(file);
  const contentType=b[0]===255?'image/jpeg':'image/png';
  s.images.add({blob:b.buffer.slice(b.byteOffset,b.byteOffset+b.byteLength),contentType,alt,fit,position:{left:x,top:y,width:w,height:h}});
}
function frame(s,x,y,w,h,color){block(s,x+9,y+9,w,h,color);block(s,x-4,y-4,w+8,h+8,ink);}
function footer(s,n,color=pale){label(s,n+' / 08',1150,669,110,color);}
const menuSource='Source: SteelBallRun_v5, local keyboard demo and game menu. Current scene routing: ProjectSettings/EditorBuildSettings.asset.';
let s=slide(ink,'Introduce this as an endless runner that players can control with a keyboard or by moving in front of a webcam. Cover art: project D4C menu artwork.');
block(s,786,0,494,720,plum);
block(s,758,0,28,720,pink);
await pic(s,art+'d4cPose.png',675,18,587,682,'contain','D4C menu illustration');
label(s,'A GAME BY ROWAN & ROHAN',64,75,680,cyan);
title(s,'STEEL',61,123,680,170,149);
title(s,'BALL RUN',61,270,680,165,135);
block(s,66,461,594,71,gold);
title(s,'NO END IN SIGHT',82,473,550,58,44,ink);
body(s,'A three-lane endless runner with\nkeyboard and webcam controls.',66,565,615,80,27);

s=slide(pale,'Show the illustrated start screen. The active menu offers D4C and Killer Queen. Pick an input mode and press Run. '+menuSource);
label(s,'01   BEFORE THE RUN',61,55,560,plum);
title(s,'PICK YOUR\nRUNNER',60,107,430,172,65,ink);
body(s,'Choose D4C or Killer Queen.\nThen pick how you want to play.',63,305,440,75,24,ink);
label(s,'KEYBOARD',63,422,330,plum);
body(s,'Use familiar movement keys.',63,452,440,42,23,ink);
label(s,'WEBCAM',63,513,330,plum);
body(s,'Lean, jump and duck to steer\nyour runner and clear obstacles.',63,543,440,75,23,ink);
frame(s,500,136,707,398,pink);
await pic(s,build+'menu.png',500,136,707,398,'contain','Current menu with D4C, Killer Queen and input mode selection');
label(s,'THE GAME’S START SCREEN',501,558,600,plum);
footer(s,'02',plum);

s=slide(ink,'Point out the three lanes and the changing street scenery. The character runs forward automatically and the pace increases. The HUD shows distance, coins, score and speed. '+menuSource);
await pic(s,build+'street.png',0,0,1280,488,'cover','The runner on Morioh’s three-lane street');
block(s,0,488,1280,232,ink);block(s,0,488,1280,7,gold);
label(s,'02   THE OPENING STRETCH',62,519,550,cyan);
title(s,'THE STREETS OF MORIOH',59,551,710,78,57);
body(s,'The runner moves forward on its own.\nYou pick a lane, dodge hazards and\ncollect coins as the pace picks up.',797,533,450,110,24);
footer(s,'03');

s=slide(lilac,'Describe the same actions in both modes. In webcam mode the main menu specifically describes leaning as continuous movement. Keyboard labels and webcam movement guidance come from the v5 menu and ActiveMotion3D README.');
label(s,'03   THE FIRST OBSTACLES',62,52,600,plum);
title(s,'THREE MOVES KEEP YOU RUNNING',60,91,1160,83,63,ink);
block(s,61,202,1158,4,plum);
label(s,'MOVE',62,224,270,plum);label(s,'KEYBOARD',397,224,300,plum);label(s,'WEBCAM',811,224,300,plum);
const rows=[
  ['CHANGE LANES','A / D or Left / Right','Lean left or right'],
  ['JUMP','W, Up or Space','Jump'],
  ['DUCK','S or Down','Crouch']
];
for(const [i,[move,keys,gesture]] of rows.entries()){
  const y=274+i*104;
  title(s,move,61,y,325,62,42,ink);
  body(s,keys,397,y+10,390,43,27,ink);
  body(s,gesture,811,y+10,390,43,27,ink);
  block(s,61,y+70,1158,2,'#BC9CCB');
}
body(s,'Jump over low hazards, duck under overhead barriers, or switch to a clear lane.',62,613,1156,42,22,ink);
footer(s,'04',plum);

s=slide(plum,'With pose tracking, raise the matching arm when a target lights up. A successful hit gives one coin. For Human Tetris, copy the wall’s silhouette before it reaches you. The runtime has six arm poses. Sources: Assets/ActiveMotion3D/README.md and local v5 screenshots.');
label(s,'04   WEBCAM CHALLENGES',62,46);
title(s,'THE TRACK ASKS YOU TO MOVE',60,84,1160,81,63);
frame(s,62,186,554,312,gold);frame(s,661,186,554,312,cyan);
await pic(s,build+'target.png',62,186,554,312,'contain','A right-arm punch target beside the road');
await pic(s,build+'wall.png',661,186,554,312,'contain','An approaching Human Tetris pose wall');
title(s,'PUNCH TARGETS',62,526,555,53,39,gold);
title(s,'HUMAN TETRIS',661,526,554,53,39,cyan);
body(s,'Raise the matching arm when\na target lights up to earn a coin.',62,580,550,72,23);
body(s,'Copy the pose on the approaching\nwall before it reaches you.',661,580,550,72,23);
footer(s,'05');

s=slide('#131620','The encounter clears previous hazards and commits to the player’s lane when it spawns. It can be jumped or dodged and detonates on impact. Render and behavior: Docs/SheerHeartAttack and PROJECT_HANDOFF.md, November 5 update.');
await pic(s,doc+'preview.png',644,19,620,620,'contain','Development render of the Sheer Heart Attack enemy');
label(s,'05   SPECIAL ENCOUNTER',62,61,580,cyan);
title(s,'SHEER HEART\nATTACK',59,112,610,195,91);
block(s,62,325,530,6,gold);
body(s,'A small tank locks onto your lane\nand rushes toward you faster\nthan the other hazards.',62,360,550,114,27);
body(s,'Jump over it or get out of its lane\nbefore it reaches you.',62,501,550,86,27);
label(s,'ENEMY MODEL',940,629,280,cyan);
footer(s,'06');

s=slide(pale,'The results screen shows score, distance, coins and time. Use Play Again for an immediate retry or Main Menu to change character or input mode. Source: v5 keyboard demo.');
label(s,'06   AFTER THE RUN',62,61,610,plum);
title(s,'ONE MORE TRY',60,106,500,90,70,ink);
body(s,'See how far you got, how many\ncoins you collected and how long\nyou lasted.',63,224,480,117,25,ink);
label(s,'PLAY AGAIN',63,392,400,plum);
body(s,'Start another run right away.',63,423,465,47,23,ink);
label(s,'MAIN MENU',63,500,400,plum);
body(s,'Change character or input mode.',63,531,470,48,23,ink);
frame(s,536,203,668,376,pink);
await pic(s,build+'finish.png',536,203,668,376,'contain','Results screen showing score, distance, coins, time and retry');
footer(s,'07',plum);

s=slide(ink,'Close by demoing the current local build. Start with a short keyboard run so the audience sees the lane changes and the results screen, then show webcam actions if the demo setup is ready. Art: project Killer Queen menu artwork.');
block(s,0,0,466,720,plum);block(s,466,0,21,720,pink);
await pic(s,art+'KillerQueenPeek.png',16,66,480,595,'contain','Killer Queen menu illustration');
label(s,'CURRENT DEMO',551,87,660,cyan);
title(s,'HOW FAR CAN\nYOU GET?',548,137,672,214,91);
block(s,552,380,640,5,gold);
body(s,'The current build includes both control modes,\nthe Morioh course and the tank encounter.\nYou can pause mid-run or retry from results.',551,421,675,117,25);
label(s,'STEEL BALL RUN  /  ROWAN & ROHAN',551,608,660,gold);
footer(s,'08');

await fs.mkdir(build+'revised_render',{recursive:true});
for(const [i,s] of deck.slides.items.entries()){
  const b=await deck.export({slide:s,format:'png',scale:0.75});
  await fs.writeFile(build+'revised_render/'+(i+1)+'.png',new Uint8Array(await b.arrayBuffer()));
}
await (await PresentationFile.exportPptx(deck)).save(build+'revised_draft.pptx');

