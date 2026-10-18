// Explicit one-time synchronization for the isolated local development database.
// Normal application startup never overwrites admin-edited course records.
import fs from 'node:fs';
import {fileURLToPath} from 'node:url';
const root=fileURLToPath(new URL('../',import.meta.url));
const cfg=JSON.parse(fs.readFileSync(root+'.local/backend.json','utf8').replace(/^\uFEFF/,''));
if(!cfg.ConnectionStrings.DefaultConnection.includes('Port=55432;')||!cfg.Seed.IncludeDemoData)throw Error('This helper only supports the isolated development database.');
const source=fs.readFileSync(root+'src/NextGenCoaching.Shared/Catalog/ProgramCatalog.cs','utf8');
const programs=[...source.matchAll(/new CourseSummaryDto\s*\{([\s\S]*?)\n        \}/g)].map(([,block])=>{
 const obj={};for(const key of ['Slug','Title','Tagline','DurationMonths','Price','DiscountPrice','Currency','Mode','Level','IsFlagship','Category','SortOrder']){
  const value=block.match(new RegExp('\\b'+key+'\\s*=\\s*("(?:[^"\\\\]|\\\\.)*"|true|false|[0-9]+m?)'))?.[1];if(value===undefined)throw Error('Missing catalogue value '+key);obj[key[0].toLowerCase()+key.slice(1)]=value.startsWith('"')?JSON.parse(value):JSON.parse(value.replace(/m$/,''));
 }return obj;
});if(programs.length!==6)throw Error('Expected the six approved programmes');
if(!process.argv.includes('--apply')){console.log('Use --apply to synchronize the isolated development catalogue. Slugs:',programs.map(p=>p.slug).join(', '));process.exit(0)}
const base='http://localhost:5101';
async function req(path,options={}){const r=await fetch(base+path,options);const j=await r.json();if(!r.ok||!j.success)throw Error(path+': '+(j.message||r.status));return j.data}
const login=await req('/api/v1/auth/login',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({email:cfg.Seed.AdminEmail,password:cfg.Seed.AdminPassword})});
const previous=[];for(const initial of programs){const detail=await req('/api/v1/public/courses/'+initial.slug);previous.push(detail);await req('/api/v1/admin/courses/'+detail.id,{method:'PUT',headers:{'Content-Type':'application/json',Authorization:'Bearer '+login.accessToken},body:JSON.stringify({...detail,...initial,isPublished:true})});}
if(!fs.existsSync(root+'.local/catalogue-before-sync.json'))fs.writeFileSync(root+'.local/catalogue-before-sync.json',JSON.stringify(previous,null,2));
console.log('Synchronized six development programmes with the approved website catalogue. Existing IDs and curriculum were preserved.');
