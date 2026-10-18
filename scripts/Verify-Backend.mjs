import fs from 'node:fs';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
const root=fileURLToPath(new URL('../',import.meta.url));
const config=JSON.parse(fs.readFileSync(root+'.local/backend.json','utf8').replace(/^\uFEFF/,''));
const base=process.env.NEXTGEN_API_URL||'http://localhost:5101';
const checks=[];let admin,webinarId;const webinarFixtures=[];
function check(ok,name,detail){checks.push({name,passed:!!ok,...(!ok?{detail}: {})});if(!ok)throw Error(name+': '+JSON.stringify(detail));}
async function request(path,{method='GET',body,token,expected=200}={}){
 const response=await fetch(base+path,{method,headers:{...(body?{'Content-Type':'application/json'}:{}),...(token?{Authorization:'Bearer '+token}:{})},...(body?{body:JSON.stringify(body)}:{})});
 const text=await response.text();let data;try{data=JSON.parse(text)}catch{data=text}
 check(response.status===expected,method+' '+path,{status:response.status,expected,message:data?.message||data?.title});return data;
}
async function login(role){const result=await request('/api/v1/auth/login',{method:'POST',body:{email:config.Seed[role+'Email'],password:config.Seed[role+'Password']}});check(!!result.data?.accessToken,role+' login');return result.data;}
try{
 check((await request('/health/ready')).status==='ready','Database ready');
 const courses=(await request('/api/v1/public/courses')).data;check(courses.length===6,'Six database courses');
 for(const c of courses){const detail=(await request('/api/v1/public/courses/'+c.slug)).data;check(detail.id===c.id&&detail.journeySteps.length>0,'Course content '+c.slug)}
 await request('/api/v1/public/courses/not-a-real-course',{expected:404});await request('/api/v1/admin/leads/',{expected:401});
 admin=await login('Admin');const emailSettings=await request('/api/v1/admin/settings/email',{token:admin.accessToken});check(emailSettings.data.enabled===false,'Outbound email disabled for verification');
 for(const role of ['Student','Instructor','Hr']){
  const user=await login(role);const prefix={Student:'student',Instructor:'instructor',Hr:'hr'}[role];await request('/api/v1/'+prefix+'/dashboard',{token:user.accessToken});if(role!=='Admin')await request('/api/v1/admin/users/',{token:user.accessToken,expected:403});
 }
 // Sweep all read-only routes without mandatory path/query parameters, using each role's own profile.
 const swagger=await request('/swagger/v1/swagger.json');const roleTokens={admin:admin.accessToken};
 for(const role of ['Student','Instructor','Hr'])roleTokens[{Student:'student',Instructor:'instructor',Hr:'hr'}[role]]=(await login(role)).accessToken;
 for(const [path,methods]of Object.entries(swagger.paths)){
  if(!methods.get||path.includes('{')||(methods.get.parameters||[]).some(p=>p.required))continue;
  const area=path.split('/')[3];if(!roleTokens[area])continue;
  await request(path,{token:roleTokens[area]});
 }
 await request('/api/v1/public/enquiries',{method:'POST',body:{name:'',email:'bad',message:''},expected:400});
 await request('/api/v1/auth/register',{method:'POST',body:{fullName:'Invalid fixture',email:'bad',phone:'123',password:'StrongPassword123!',confirmPassword:'different'},expected:400});
 const unique=crypto.randomUUID().slice(0,8),email='backend-'+unique+'@nextgen.invalid';
 const enquiry=await request('/api/v1/public/enquiries',{method:'POST',body:{name:'Backend verification '+unique,email,message:'Local backend verification fixture; not a customer enquiry.',sourcePage:'/backend-verification'}});
 await request('/api/v1/public/enquiries',{method:'POST',body:{name:'Optional phone check',email:'optional-'+unique+'@nextgen.invalid',phone:' ',message:'Optional phone should be accepted.'}});
 await request('/api/v1/public/enquiries',{method:'POST',body:{name:'Invalid source check',email,message:'Validation should reject oversized source pages.',sourcePage:'x'.repeat(201)},expected:400});
 const saved=(await request('/api/v1/admin/leads/'+enquiry.data.leadId,{token:admin.accessToken})).data;check(saved.email===email&&saved.source==='ContactForm','Enquiry persisted as lead');
 check((await request('/api/v1/admin/leads/?search='+email,{token:admin.accessToken})).data.items.some(l=>l.email===email),'Lead search works without pagination parameters');
 await request('/api/v1/admin/leads/?Page=not-a-number',{token:admin.accessToken,expected:400});
 const signup={fullName:'Backend test student '+unique,email:'student-'+unique+'@nextgen.invalid',phone:'+919999999999',password:'BackendTest!'+unique+'9',confirmPassword:'BackendTest!'+unique+'9',interestedInCourseSlug:'data-science'};
 const signed=(await request('/api/v1/auth/register',{method:'POST',body:signup})).data;check(signed.roles.length===1&&signed.roles[0]==='Student','Sign-up creates student role only');
 await request('/api/v1/student/profile',{token:signed.accessToken});await request('/api/v1/student/dashboard',{token:signed.accessToken});await request('/api/v1/admin/users/',{token:signed.accessToken,expected:403});
 await request('/api/v1/auth/register',{method:'POST',body:signup,expected:409});
 const refreshed=(await request('/api/v1/auth/refresh',{method:'POST',body:{refreshToken:signed.refreshToken}})).data;
 check(refreshed.refreshToken!==signed.refreshToken,'Refresh token rotates');await request('/api/v1/auth/refresh',{method:'POST',body:{refreshToken:signed.refreshToken},expected:401});
 await request('/api/v1/auth/logout',{method:'POST',token:refreshed.accessToken,body:{},expected:204});await request('/api/v1/auth/refresh',{method:'POST',body:{refreshToken:refreshed.refreshToken},expected:401});
 webinarId=(await request('/api/v1/admin/webinars/',{method:'POST',token:admin.accessToken,body:{title:'Backend verification '+unique,description:'Temporary automated verification session.',courseId:courses[0].id,scheduledAtUtc:new Date(Date.now()+86400000).toISOString(),durationMinutes:60,capacity:2}})).data;
 webinarFixtures.push(webinarId);
 const registration={fullName:'Backend verification '+unique,email,phone:'+919999999999'};
 const first=await request('/api/v1/public/webinars/'+webinarId+'/register',{method:'POST',body:registration});const second=await request('/api/v1/public/webinars/'+webinarId+'/register',{method:'POST',body:registration});check(first.data.duplicate===false&&second.data.duplicate===true,'Webinar registration deduplication');
 const registrations=(await request('/api/v1/admin/webinars/'+webinarId+'/registrations',{token:admin.accessToken})).data;check(registrations.length===1,'Registration saved exactly once');
 const race=await Promise.all([1,2].map(async i=>{const response=await fetch(base+'/api/v1/public/webinars/'+webinarId+'/register',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({...registration,email:'capacity-'+unique+'-'+i+'@nextgen.invalid'})});return response.status}));
 check(race.sort().join(',')==='200,400','Concurrent registrations respect remaining capacity',race);
 check((await request('/api/v1/admin/webinars/'+webinarId+'/registrations',{token:admin.accessToken})).data.length===2,'Session capacity never exceeded');
 const expired=(await request('/api/v1/admin/webinars/',{method:'POST',token:admin.accessToken,body:{title:'Expired verification fixture',scheduledAtUtc:new Date(Date.now()-86400000).toISOString(),durationMinutes:60}})).data;webinarFixtures.push(expired);await request('/api/v1/public/webinars/'+expired+'/register',{method:'POST',body:registration,expected:400});
 for(let i=0;i<5;i++)await request('/api/v1/auth/login',{method:'POST',body:{email:signup.email,password:'IncorrectPassword999'},expected:401});
 await request('/api/v1/auth/login',{method:'POST',body:{email:signup.email,password:signup.password},expected:401});check(true,'Account lockout enforced after repeated failures');
}catch(error){checks.push({name:'Verification interrupted',passed:false,detail:error.message});process.exitCode=1;console.error(error.message)}
finally{
 for(const fixture of webinarFixtures)if(admin){try{await request('/api/v1/admin/webinars/'+fixture,{method:'DELETE',token:admin.accessToken})}catch(error){process.exitCode=1;console.error('Fixture cleanup failed')}}
 fs.mkdirSync(root+'.local',{recursive:true});fs.writeFileSync(root+'.local/backend-checks.json',JSON.stringify({checkedAt:new Date().toISOString(),base,checks},null,2));console.log('Backend checks:',checks.filter(c=>c.passed).length,'passed,',checks.filter(c=>!c.passed).length,'failed. Report: .local/backend-checks.json');
}
