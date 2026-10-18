import fs from 'node:fs';import {fileURLToPath} from 'node:url';
const root=fileURLToPath(new URL('../',import.meta.url));const cfg=JSON.parse(fs.readFileSync(root+'.local/backend.json','utf8').replace(/^\uFEFF/,''));const base='http://localhost:5101';
const response=await fetch(base+'/api/v1/auth/login',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({email:cfg.Seed.AdminEmail,password:cfg.Seed.AdminPassword})});const login=(await response.json()).data;if(!login?.accessToken)throw Error('Admin login failed');
const headers={Authorization:'Bearer '+login.accessToken};const path=root+'.local/persistence-check.json';
if(process.argv[2]==='before'){
 const leads=await(await fetch(base+'/api/v1/admin/leads/?PageSize=1',{headers})).json();if(!leads.data.items.length)throw Error('No verification lead to check');fs.writeFileSync(path,JSON.stringify({leadId:leads.data.items[0].id,checkedBeforeRestart:true},null,2));console.log('Saved a lead ID for restart verification.');
}else{
 const saved=JSON.parse(fs.readFileSync(path,'utf8'));const result=await(await fetch(base+'/api/v1/admin/leads/'+saved.leadId,{headers})).json();if(result.data?.id!==saved.leadId)throw Error('Saved record did not survive restart');saved.survivedRestart=true;fs.writeFileSync(path,JSON.stringify(saved,null,2));console.log('The same database record is available after the API restart.');
}
