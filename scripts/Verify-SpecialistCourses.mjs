// Runs only against the isolated local testing database; creates labelled admissions fixtures.
import fs from 'node:fs';
import {fileURLToPath} from 'node:url';
const root=fileURLToPath(new URL('../',import.meta.url));
const cfg=JSON.parse(fs.readFileSync(root+'.local/backend.json','utf8').replace(/^\uFEFF/,''));
if(!cfg.ConnectionStrings.DefaultConnection.includes('Port=55432;')||!cfg.Seed.IncludeDemoData)throw Error('Only the isolated development database is supported.');
const api='http://localhost:5101';
async function req(path,{method='GET',body,token}={}){
 const r=await fetch(api+path,{method,headers:{...(body?{'Content-Type':'application/json'}:{}),...(token?{Authorization:'Bearer '+token}:{})},...(body?{body:JSON.stringify(body)}:{})});
 const json=await r.json();if(!r.ok||!json.success)throw Error(path+': '+r.status);return json.data;
}
const login=await req('/api/v1/auth/login',{method:'POST',body:{email:cfg.Seed.AdminEmail,password:cfg.Seed.AdminPassword}});
const settings=await req('/api/v1/admin/settings/email',{token:login.accessToken});
if(settings.enabled)throw Error('Disable outbound email before running local fixture checks.');
const expected={'advanced-ai':6,'machine-learning':3,'generative-ai':3,'agentic-ai':5,'databricks':4};
const report=[];
for(const [slug,months] of Object.entries(expected)){
 const c=await req('/api/v1/public/courses/'+slug);
 if(c.durationMonths!==months||c.mode!=='live-online'||c.tools.length<5||c.features.length!==4||c.journeySteps.length!==6)throw Error('Incomplete specialist program: '+slug);
 const adminForm=await req('/api/v1/admin/courses/'+c.id,{token:login.accessToken});
 await req('/api/v1/admin/courses/'+c.id,{method:'PUT',body:adminForm,token:login.accessToken});
 const art=await fetch(`http://localhost:5102/images/courses/${slug}.svg`);
 if(!art.ok||!art.headers.get('content-type')?.includes('image/svg+xml'))throw Error('Missing vector artwork: '+slug);
 const fixture=await req('/api/v1/public/enquiries',{method:'POST',body:{name:'Specialist course verification',email:`course-check-${Date.now()}@nextgen.invalid`,phone:'+919999999999',courseInterestId:c.id,subject:'Local enrollment verification: '+c.title,message:'Automated local test fixture, not a customer enquiry.',sourcePage:'/enroll'}});
 const lead=await req('/api/v1/admin/leads/'+fixture.leadId,{token:login.accessToken});
 if(lead.courseInterestTitle!==c.title)throw Error('Enrollment linked to incorrect course: '+slug);
 report.push({slug,id:c.id,leadId:fixture.leadId,curriculum:true,adminEditSaved:true,responsiveImages:true,enrollmentStored:true});
}
fs.writeFileSync(root+'.local/specialist-course-checks.json',JSON.stringify({checkedAt:new Date().toISOString(),courses:report},null,2));
console.log('All five specialist programs: curriculum, admin editing, responsive image delivery and saved course-specific enrollment requests verified.');
