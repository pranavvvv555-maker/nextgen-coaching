// Vendor published brand artwork locally; no external logo requests are made by visitors.
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=fileURLToPath(new URL('../',import.meta.url));
const output=path.join(root,'src/NextGenCoaching.Web/wwwroot/images/tools');
await fs.mkdir(output,{recursive:true});
const sources=[];
for(const name of ['azure','postgresql','mysql','pandas','numpy','scikitlearn','tensorflow','pytorch','docker','fastapi','apachespark','matplotlib'])
 sources.push({name,file:name+'.svg',url:`https://raw.githubusercontent.com/devicons/devicon/master/icons/${name}/${name}-original.svg`,collection:'Devicon (MIT; marks belong to their owners)'});
for(const name of ['snowflake','databricks','langchain','langgraph','crewai','huggingface','mlflow','modelcontextprotocol'])
 sources.push({name,file:name+'.svg',url:'https://cdn.simpleicons.org/'+name,collection:'Simple Icons (CC0; marks belong to their owners)'});
sources.push({name:'power-bi',file:'power-bi.svg',url:'https://raw.githubusercontent.com/microsoft/PowerBI-Icons/main/SVG/Power-BI.svg',collection:'Microsoft Power BI Icons (CC BY 4.0)'});
sources.push({name:'power-query',file:'power-query.svg',url:'https://raw.githubusercontent.com/microsoft/PowerBI-Icons/main/SVG/Power-Query-Colored.svg',collection:'Microsoft Power BI Icons (CC BY 4.0)'});
sources.push({name:'delta-lake',file:'delta-lake.svg',url:'https://delta.io/_astro/delta-lake-logo.Dx7tzbyv_1OsUys.svg',collection:'Delta Lake official website'});
sources.push({name:'unity-catalog',file:'unity-catalog.png',url:'https://raw.githubusercontent.com/unitycatalog/unitycatalog/main/docs/assets/images/uc-logo.png',collection:'Unity Catalog project repository'});
const results=await Promise.allSettled(sources.map(async source=>{
 const r=await fetch(source.url);if(!r.ok)throw Error(source.name+': '+r.status);
 const bytes=Buffer.from(await r.arrayBuffer());
 if(source.file.endsWith('.svg')){
  const svg=bytes.toString('utf8');
  if(!svg.includes('<svg')||/<script|<foreignObject|\bonload\s*=|\bonerror\s*=|javascript:/i.test(svg))throw Error('Unexpected SVG content: '+source.name);
 }
 await fs.writeFile(path.join(output,source.file),bytes);
 return {...source,bytes:bytes.length};
}));
const failed=results.filter(r=>r.status==='rejected');
for(const failure of failed)console.error(failure.reason.message);
const records=results.filter(r=>r.status==='fulfilled').map(r=>r.value);
// Retain the user-provided Python image and the Azure service icons prepared separately.
const previous=await fs.readFile(path.join(output,'sources.json'),'utf8').then(JSON.parse).catch(()=>({assets:[]}));
for(const asset of previous.assets)if(!records.some(record=>record.file===asset.file))records.push(asset);
await fs.writeFile(path.join(output,'sources.json'),JSON.stringify({purpose:'Identify technologies taught in NextGen programs. Logos retain their original artwork and colors.',assets:records},null,2));
const license=await fetch('https://raw.githubusercontent.com/devicons/devicon/master/LICENSE');
if(!license.ok)throw Error('Devicon license: '+license.status);
await fs.writeFile(path.join(output,'DEVICON-LICENSE.txt'),await license.text());
const cache=path.join(root,'.local/brand-assets');await fs.mkdir(cache,{recursive:true});
const azure=await fetch('https://arch-center.azureedge.net/icons/Azure_Public_Service_Icons_V24.zip');
if(!azure.ok)throw Error('Azure icon archive: '+azure.status);
await fs.writeFile(path.join(cache,'azure-icons.zip'),Buffer.from(await azure.arrayBuffer()));
console.log(`Saved ${records.length} logo assets and the official Azure icon archive.`);
if(failed.length)process.exitCode=1;
