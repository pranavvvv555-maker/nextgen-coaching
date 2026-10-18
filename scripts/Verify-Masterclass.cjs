const fs=require('fs');
const {chromium}=require('C:/Users/prana/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root='D:/NG website/NextGen-main/';
const checks=[];
function check(ok,name,details){checks.push({name,passed:!!ok,...(details?{details}: {})});if(!ok)throw Error(name+': '+JSON.stringify(details));}
(async()=>{const browser=await chromium.launch({headless:true,channel:'msedge'});let fixture,token;try{
 for(const language of ['en','mr','hi'])for(const theme of ['light','dark']){
  const context=await browser.newContext({viewport:{width:390,height:844},isMobile:true,hasTouch:true,reducedMotion:'reduce'});
  await context.addInitScript(p=>localStorage.setItem('nextgen.site.preferences.v1',JSON.stringify(p)),{theme,language});
  const page=await context.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://localhost:5102/webinar',{waitUntil:'domcontentloaded'});await page.locator('#mc-name').waitFor();
  await page.waitForFunction(p=>document.documentElement.lang===p.language&&document.documentElement.dataset.theme===p.theme,{theme,language});
  if(language!=='en')await page.waitForFunction(()=>!document.querySelector('#masterclass-title').textContent.includes('Big ideas.'));
  await page.evaluate(()=>Promise.race([document.fonts.ready,new Promise(resolve=>setTimeout(resolve,2000))]));
  for(const width of [320,360,390,430,768,1024,1440]){
   await page.setViewportSize({width,height:1000});await page.waitForTimeout(100);
   const layout=await page.evaluate(width=>{
    const all=[...document.querySelectorAll('.masterclass-page h1,.masterclass-page h2,.masterclass-page input,.masterclass-page button,.mc-art-paper')];
    const outside=all.filter(e=>{const r=e.getBoundingClientRect();return r.width&&(r.left < -1||r.right>width+1)}).map(e=>e.className||e.id);
    const img=document.querySelector('.mc-art-paper img'),r=img.getBoundingClientRect();
    return {width:innerWidth,scroll:document.documentElement.scrollWidth,visual:visualViewport.width,outside,imageLoaded:img.complete&&img.naturalWidth>0,imageRatio:r.width/r.height,fonts:[...document.querySelectorAll('.mc-field input')].map(e=>parseFloat(getComputedStyle(e).fontSize)),playing:document.querySelector('[data-masterclass-art]').dataset.playing};
   },width);
   check(layout.width===width&&layout.scroll<=width+1&&Math.abs(layout.visual-width)<1&&!layout.outside.length&&layout.imageLoaded&&Math.abs(layout.imageRatio-1448/1086)<.02&&(width>767||layout.fonts.every(n=>n>=16))&&layout.playing==='false',`${language}/${theme}/${width}: aligned, uncropped, readable and reduced motion`,layout);
  }
  check(!errors.length,`${language}/${theme}: no browser errors`,errors);
  if(language==='en'){
   await page.setViewportSize({width:1440,height:1000});await page.screenshot({path:root+'.local/masterclass-'+theme+'-desktop.png',fullPage:true});
   await page.setViewportSize({width:390,height:844});await page.screenshot({path:root+'.local/masterclass-'+theme+'-mobile.png',fullPage:true});
  }
  await context.close();
 }
 const context=await browser.newContext({viewport:{width:1440,height:1000},reducedMotion:'no-preference'});const page=await context.newPage();await page.goto('http://localhost:5102/webinar',{waitUntil:'domcontentloaded'});const art=page.locator('[data-masterclass-art]');await page.waitForFunction(()=>document.querySelector('[data-masterclass-art]')?.dataset.playing==='true');
 const packet=page.locator('[data-mc-packet]').first();const before=await packet.getAttribute('transform');await page.waitForTimeout(250);check(await packet.getAttribute('transform')!==before,'Data packets move');
 await page.locator('[data-mc-toggle]').click();await page.waitForFunction(()=>document.querySelector('[data-masterclass-art]').dataset.playing==='false');const paused=await packet.getAttribute('transform');await page.waitForTimeout(250);check(await packet.getAttribute('transform')===paused&&await page.locator('[data-mc-toggle]').getAttribute('aria-pressed')==='true','Pause freezes all Motion animation');
 await page.locator('[data-mc-toggle]').click();await page.waitForFunction(()=>document.querySelector('[data-masterclass-art]').dataset.playing==='true');check(true,'Animation resumes');
 await page.locator('#mc-background').scrollIntoViewIfNeeded();await page.waitForFunction(()=>document.querySelector('[data-masterclass-art]').dataset.playing==='false');check(true,'Animation pauses offscreen');
 await page.locator('.mc-hero-copy>.mc-primary').click();check(page.url().endsWith('#masterclass-registration'),'Hero link reaches registration');
 await page.goto('http://localhost:5102/contact',{waitUntil:'domcontentloaded'});await page.locator('#enquiry-name').waitFor();await page.goto('http://localhost:5102/webinar',{waitUntil:'domcontentloaded'});await page.waitForFunction(()=>document.querySelector('[data-masterclass-art]')?.dataset.playing==='true');check(await art.count()===1,'Animation initializes after page navigation');
 // Test the scheduled-session branch with a clearly labelled, temporary local fixture.
 const cfg=JSON.parse(fs.readFileSync(root+'.local/backend.json','utf8').replace(/^\uFEFF/,''));
 const api=async(path,method='GET',body)=>{const r=await fetch('http://localhost:5101/api/v1'+path,{method,headers:{'Content-Type':'application/json',...(token?{Authorization:'Bearer '+token}: {})},...(body?{body:JSON.stringify(body)}: {})});const data=await r.json();if(!r.ok)throw Error('API fixture '+r.status);return data.data;};
 token=(await api('/auth/login','POST',{email:cfg.Seed.AdminEmail,password:cfg.Seed.AdminPassword})).accessToken;
 check((await api('/admin/settings/email')).enabled===false,'Email disabled during local fixture test');
 const unique=Date.now();fixture=await api('/admin/webinars/','POST',{title:'Collage browser verification '+unique,description:'Temporary automated local verification; not a public event.',scheduledAtUtc:new Date(Date.now()+86400000).toISOString(),durationMinutes:45,capacity:2});
 await page.reload({waitUntil:'domcontentloaded'});await page.locator('.mc-session.is-selected').waitFor();check(await page.locator('.mc-session.is-selected').getAttribute('aria-pressed')==='true','Scheduled session selectable and selected state accessible');
 await page.locator('#mc-name').fill('Collage verification');await page.locator('#mc-email').fill('collage-'+unique+'@nextgen.invalid');await page.locator('#mc-phone').fill('+919999999999');await page.locator('.mc-submit').click();await page.locator('.mc-result--success').waitFor();check((await api('/admin/webinars/'+fixture+'/registrations')).some(x=>x.email==='collage-'+unique+'@nextgen.invalid'),'Redesigned scheduled registration persists in backend');
 await context.close();
}catch(e){checks.push({name:e.message,passed:false});console.error(e);process.exitCode=1;}finally{
 if(fixture&&token){const response=await fetch('http://localhost:5101/api/v1/admin/webinars/'+fixture,{method:'DELETE',headers:{Authorization:'Bearer '+token}});if(!response.ok){process.exitCode=1;checks.push({name:'Fixture cleanup',passed:false});}}
 await browser.close();fs.writeFileSync(root+'.local/masterclass-checks.json',JSON.stringify(checks,null,2));console.log(checks.filter(x=>x.passed).length+' checks passed; '+checks.filter(x=>!x.passed).length+' failed');
}})();
