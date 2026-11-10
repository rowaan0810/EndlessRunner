import fs from 'node:fs/promises';
import {Presentation,PresentationFile} from '@oai/artifact-tool';
const p=Presentation.create({slideSize:{width:1280,height:720}});
const base='D:/TEEP-subwaySurfers/EndlessRunner/Assets/ActiveMotion3D/Art/UI/';
async function img(s,file,pos){let b=await fs.readFile(base+file); s.images.add({blob:b.buffer.slice(b.byteOffset,b.byteOffset+b.byteLength),contentType:'image/png',alt:file,fit:'contain',position:pos});}
function tx(s,t,x,y,w,h,size,color='#F9F6FF',bold=false){let a=s.shapes.add({geometry:'textbox',position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});a.text=t;a.text.style={fontFamily:'Arial',fontSize:size,color,bold};}
function slide(title,body,n){let s=p.slides.add();s.background.fill='#18102D';tx(s,'STEEL BALL RUN',70,38,600,30,16,'#F2B85B',true);tx(s,title,70,105,680,130,48,'#FFFFFF',true);tx(s,body,74,270,620,330,27,'#E0D9EB');tx(s,String(n).padStart(2,'0'),1130,650,80,30,16,'#B9A9C8');return s;}
let s=slide('NO END IN SIGHT','A body-controlled endless runner\n\nCreated by Rowan & Rohan',1);await img(s,'SBR_title_last.png',{left:730,top:170,width:490,height:310});
s=slide('Run as far as you can','Move between three lanes, avoid obstacles and collect coins.\n\nA collision ends the run. Try again and beat your score.',2);await img(s,'run_button_v9.png',{left:750,top:200,width:420,height:300});
s=slide('Choose how to play','KEYBOARD\nChange lanes with A / D or the arrow keys. Jump with W, Up or Space; duck with S or Down.\n\nWEBCAM\nUse your body to move, jump and duck.',3);await img(s,'ModeChangeImage.png',{left:770,top:190,width:390,height:340});
s=slide('More than dodging','PUNCH TARGETS\nRaise the matching arm when a target lights up to earn a coin.\n\nHUMAN TETRIS\nMatch the pose shown by an approaching wall to pass through.',4);await img(s,'d4cPose.png',{left:790,top:160,width:380,height:400});
s=slide('Your run','Choose a character and input mode in the menu.\n\nStart running, collect coins and react to obstacles.\n\nAfter a collision, play again or return to the menu.',5);await img(s,'CharacterChangeImage.png',{left:760,top:190,width:420,height:350});
s=slide('Where the project stands','The core game flow and activities are in place.\n\nNext up: pause and settings, clearer webcam guidance, visual polish and release testing.',6);await img(s,'KillerQueenPeek.png',{left:780,top:170,width:400,height:400});
s=slide('Ready to run?','Pick a character, choose your controls and see how long you can survive.',7);await img(s,'LOGO_v2.png',{left:750,top:190,width:440,height:310});
await fs.mkdir('deck_build/render',{recursive:true});for(let i=0;i<p.slides.items.length;i++){let b=await p.export({slide:p.slides.items[i],format:'png',scale:1});await fs.writeFile(`deck_build/render/${i+1}.png`,new Uint8Array(await b.arrayBuffer()));}
await (await PresentationFile.exportPptx(p)).save('deck_output/Steel-Ball-Run-Updated.pptx');
