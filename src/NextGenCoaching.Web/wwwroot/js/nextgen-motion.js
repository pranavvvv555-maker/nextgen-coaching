(() => {
  'use strict';
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)');
  const finePointer = window.matchMedia('(hover: hover) and (pointer: fine)');
  const revealed = new WeakSet();
  const bound = new WeakSet();
  const artworkBound = new WeakSet();
  const artworkObserver = new IntersectionObserver(entries => {
    entries.forEach(entry => entry.target.classList.toggle('art-visible', entry.isIntersecting));
  }, { threshold: 0.01 });
  let queued = false;
  let progress;
  const revealObserver = new IntersectionObserver(entries => {
    entries.forEach(entry => {
      if (!entry.isIntersecting) return;
      entry.target.classList.remove('motion-pending');
      entry.target.classList.add('motion-visible');
      revealObserver.unobserve(entry.target);
    });
  }, { threshold: 0.04, rootMargin: '0px 0px -20px 0px' });
  function reveal(el) {
    if (revealed.has(el)) return;
    revealed.add(el);
    if (reduced.matches || el.getBoundingClientRect().top < window.innerHeight - 30) { el.classList.add('motion-visible'); return; }
    el.style.transitionDelay = `${Math.min(Number(el.dataset.delay) || 0, 240)}ms`;
    el.classList.add('motion-pending');
    revealObserver.observe(el);
  }
  function bindPointer(el) {
    if (bound.has(el)) return;
    bound.add(el);
    let frame = 0;
    el.addEventListener('pointermove', event => {
      if (reduced.matches || !finePointer.matches || frame) return;
      frame = requestAnimationFrame(() => {
        frame = 0;
        const rect = el.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const y = event.clientY - rect.top;
        el.style.setProperty('--spotlight-x', `${x}px`);
        el.style.setProperty('--spotlight-y', `${y}px`);
        if (el.matches('[data-scene-tilt]')) {
          el.style.setProperty('--scene-x', `${(x / rect.width - .5) * 9}deg`);
          el.style.setProperty('--scene-y', `${(.5 - y / rect.height) * 7}deg`);
        } else if (el.matches('[data-tilt]')) {
          el.classList.add('tilt-enabled');
          el.style.setProperty('--tilt-x', `${(x / rect.width - .5) * 3}deg`);
          el.style.setProperty('--tilt-y', `${(.5 - y / rect.height) * 3}deg`);
        }
      });
    }, { passive: true });
    el.addEventListener('pointerleave', () => {
      cancelAnimationFrame(frame); frame = 0;
      ['--scene-x', '--scene-y', '--tilt-x', '--tilt-y'].forEach(key => el.style.setProperty(key, '0deg'));
    });
  }
  function updateChrome() {
    const max = document.documentElement.scrollHeight - window.innerHeight;
    if (progress) progress.style.transform = `scaleX(${max > 0 ? Math.min(window.scrollY / max, 1) : 0})`;
    document.querySelector('.site-nav')?.classList.toggle('is-scrolled', window.scrollY > 15);
  }
  // Motion's JavaScript runtime works with Blazor-rendered elements and internal navigation.
  const careerHeroes = new Map();
  function syncCareerHero(state) {
    const playing = state.inView && !document.hidden && !reduced.matches;
    state.floats.forEach(control => playing ? control.play() : control.pause());
  }
  function initCareerHeroes() {
    careerHeroes.forEach((state, root) => {
      if (root.isConnected) return;
      state.observer.disconnect();
      state.controls.forEach(control => control.stop());
      state.hoverCleanup.forEach(cleanup => cleanup());
      careerHeroes.delete(root);
    });
    if (!window.Motion) return;
    const { animate, hover } = window.Motion;
    document.querySelectorAll('[data-career-hero], [data-learning-hero]').forEach(root => {
      if (careerHeroes.has(root)) return;
      const state = { inView: false, floats: [], controls: [], hoverCleanup: [] };
      careerHeroes.set(root, state);
      if (!reduced.matches) {
        root.querySelectorAll('[data-hero-enter]').forEach((element, index) => {
          const control = animate(element, { opacity: [0, 1], y: [24, 0] }, {
            type: 'spring', duration: 0.85, bounce: 0.14, delay: Math.min(index * 0.055, 0.33)
          });
          state.controls.push(control);
        });
      }
      root.querySelectorAll('[data-hero-float]').forEach((element, index) => {
        const distance = Number(element.dataset.heroFloat) || 6;
        const control = animate(element, { y: [0, -distance, 0] }, {
          duration: 4.8 + index * 0.65, ease: 'easeInOut', repeat: Infinity
        });
        control.pause();
        state.floats.push(control);
        state.controls.push(control);
      });
      root.querySelectorAll('[data-hero-hover]').forEach(element => {
        state.hoverCleanup.push(hover(element, () => {
          if (reduced.matches) return;
          animate(element, { scale: 1.035 }, { type: 'spring', stiffness: 400, damping: 26 });
          return () => animate(element, { scale: 1 }, { type: 'spring', stiffness: 400, damping: 26 });
        }));
      });
      state.observer = new IntersectionObserver(entries => {
        state.inView = entries[0].isIntersecting;
        syncCareerHero(state);
      }, { threshold: 0.01, rootMargin: '-100px 0px 0px 0px' });
      state.observer.observe(root.querySelector('.career-hero-scene, .learning-universe') || root);
    });
  }
  document.addEventListener('visibilitychange', () => careerHeroes.forEach(syncCareerHero));
  reduced.addEventListener('change', () => {
    careerHeroes.forEach((state, root) => {
      if (reduced.matches) {
        state.controls.filter(control => !state.floats.includes(control)).forEach(control => control.stop());
        root.querySelectorAll('[data-hero-enter]').forEach(element => {
          element.style.opacity = '1';
          element.style.transform = 'none';
        });
      }
      syncCareerHero(state);
    });
  });
  // One Motion timeline synchronizes SQL input, AI response and chart output.
  const dataAiScenes = new Map();
  function syncDataAiScene(state) {
    const playing = state.inView && !document.hidden && !reduced.matches && !state.manualPause;
    playing ? state.timeline.play() : state.timeline.pause();
    state.root.dataset.playing = String(playing);
    if (reduced.matches) state.render(1, true);
  }
  function initDataAiScenes() {
    dataAiScenes.forEach((state, root) => {
      if (root.isConnected) return;
      state.timeline.stop(); state.observer.disconnect();
      state.toggle?.removeEventListener('click', state.onToggle);
      dataAiScenes.delete(root);
    });
    if (!window.Motion) return;
    document.querySelectorAll('[data-data-ai-scene]').forEach(root => {
      if (dataAiScenes.has(root)) return;
      const find = name => root.querySelector('[data-' + name + ']');
      const input = find('sql-input-path'), cloud = find('cloud-output-path');
      const output = find('chart-output-path'), arrow = find('chart-arrow-path');
      const trail = find('chart-arrow-trail'), head = find('chart-arrow-head');
      const glow = find('ai-chip-glow'), outline = find('ai-chip-outline');
      const sqlPackets = [...root.querySelectorAll('[data-sql-packet]')];
      const neural = [...root.querySelectorAll('[data-neural-node]')];
      const stage = root.querySelector('.data-ai-stage');
      const networkLayer=find('ai-network-layer'),cloudLayer=find('ai-cloud-layer'),sqlLayer=find('ai-sql-layer');
      const bars=[...root.querySelectorAll('[data-ai-bar-layer]')];
      const signals=[...root.querySelectorAll('[data-neural-signal]')];
      const signalPaths=[...root.querySelectorAll('[data-neural-signal-path]')];
      const leds=[...root.querySelectorAll('[data-sql-led]')];
      const sparkles=[...root.querySelectorAll('[data-ai-sparkle]')];
      const scan=find('ai-scan'),scanSoft=find('ai-scan-soft'),cloudRing=find('cloud-ring'),cloudCheck=find('cloud-check');
      const seal=root.querySelector('.data-ai-seal>span');
      root.dataset.motionReady='true';
      const paths = new Map([input, cloud, output, arrow,...signalPaths].map(path => [path, path.getTotalLength()]));
      const clamp = v => Math.max(0, Math.min(1, v));
      const range = (p, a, b) => clamp((p-a)/(b-a));
      const smooth = v => v*v*(3-2*v);
      const packet = (element, path, amount, visible) => {
        const point = path.getPointAtLength(paths.get(path)*clamp(amount));
        element.setAttribute('transform', 'translate(' + point.x.toFixed(2) + ' ' + point.y.toFixed(2) + ')');
        element.setAttribute('opacity', visible ? String(Math.min(1, amount*12, (1-amount)*14)) : '0');
      };
      const state = {root, inView:false, manualPause:false, timeline:null, toggle:root.parentElement.querySelector('[data-ai-motion-toggle]')};
      state.render = (p, still=false) => {
        if (!root.isConnected) return;
        packet(sqlPackets[0],input,range(p,.02,.23),!still&&p>.02&&p<.23);
        packet(sqlPackets[1],input,range(p,.08,.29),!still&&p>.08&&p<.29);
        packet(sqlPackets[2],input,range(p,.14,.35),!still&&p>.14&&p<.35);
        const response = p>.22&&p<.59 ? Math.sin(range(p,.22,.59)*Math.PI) : 0;
        const glowLevel = still ? .12 : response*(.78+.18*Math.sin(p*65));
        glow.setAttribute('opacity',glowLevel.toFixed(3));
        outline.setAttribute('opacity',(still?0:response*.74).toFixed(3));
        neural.forEach((node,index)=>{
          const t=range(p,.25+index*.008,.57+index*.008);
          node.setAttribute('opacity',!still&&t>0&&t<1?(.62*Math.sin(t*Math.PI)*(.65+.35*Math.sin(p*55-index))).toFixed(3):'0');
          node.setAttribute('r',String(26+(!still?response*4:0)));
        });
        packet(find('cloud-packet'),cloud,range(p,.46,.67),!still&&p>.46&&p<.67);
        packet(find('chart-packet'),output,range(p,.5,.64),!still&&p>.5&&p<.64);
        const climb=still?1:smooth(range(p,.62,.94));
        const breathe=still?0:Math.sin(p*Math.PI*2);
        networkLayer.setAttribute('transform',still?'':'translate(500 422) scale(1 '+(1+.014*breathe).toFixed(4)+') rotate('+(.55*breathe).toFixed(2)+') translate(-500 -422)');
        sqlLayer.setAttribute('transform',still?'':'translate(0 '+(-2.5*Math.sin(p*Math.PI*4)).toFixed(2)+')');
        cloudLayer.setAttribute('transform',still?'':'translate(830 487) scale('+(1+.022*Math.sin(p*Math.PI*2+.7)).toFixed(4)+') translate(-830 -487)');
        leds.forEach((led,i)=>led.setAttribute('opacity',still?'0':(.15+.7*Math.max(0,Math.sin(p*Math.PI*8-i*.65))).toFixed(3)));
        signals.forEach((signal,i)=>{
          const t=range(p,.25+(i%3)*.015+(i>=3?.12:0),.51+(i%3)*.015+(i>=3?.12:0));
          packet(signal,signalPaths[+signal.dataset.neuralSignal],t,!still&&t>0&&t<1);
        });
        const scanProgress=range(p,.24,.58),scanOpacity=!still&&scanProgress>0&&scanProgress<1?Math.sin(scanProgress*Math.PI)*.4:0;
        [scan,scanSoft].forEach(line=>{line.setAttribute('transform','translate(0 '+(scanProgress*295).toFixed(2)+')');line.setAttribute('opacity',scanOpacity.toFixed(3));});
        const cloudResponse=!still&&p>.65&&p<.93?Math.sin(range(p,.65,.93)*Math.PI):0;
        cloudRing.setAttribute('r',(36+cloudResponse*30).toFixed(2));cloudRing.setAttribute('opacity',(cloudResponse*.55).toFixed(3));
        cloudCheck.setAttribute('opacity',(cloudResponse*.9).toFixed(3));
        bars.forEach((bar,i)=>{
          const rise=still?1:smooth(range(p,.58+i*.045,.79+i*.045));
          const reset=still?0:smooth(range(p,.95,1));
          const height=.73+.27*rise*(1-reset),anchor=890+i*7;
          bar.setAttribute('transform','translate(0 '+(anchor*(1-height)).toFixed(2)+') scale(1 '+height.toFixed(4)+')');
        });
        const positions=[[220,430],[772,248],[958,280],[308,505],[915,555],[83,527],[745,112]];
        sparkles.forEach((star,i)=>{const t=p*Math.PI*2+i*.8;star.setAttribute('transform','translate('+(positions[i][0]+Math.sin(t)*5).toFixed(2)+' '+(positions[i][1]+Math.cos(t)*10).toFixed(2)+') rotate('+(still?0:Math.sin(t)*22).toFixed(2)+')');star.setAttribute('opacity',still?'.35':(.18+.42*(1+Math.sin(t))/2).toFixed(3));});
        seal.style.transform=still?'none':'rotate('+(p*360).toFixed(2)+'deg)';
        const length=paths.get(arrow),point=arrow.getPointAtLength(length*climb);
        const ahead=arrow.getPointAtLength(Math.min(length,length*climb+2));
        const behind=arrow.getPointAtLength(Math.max(0,length*climb-2));
        const angle=Math.atan2(ahead.y-behind.y,ahead.x-behind.x)*180/Math.PI;
        trail.style.strokeDasharray=String(length);trail.style.strokeDashoffset=String(length*(1-climb));
        trail.style.opacity=still||p>.61?'1':'0';
        head.setAttribute('transform','translate('+point.x.toFixed(2)+' '+point.y.toFixed(2)+') rotate('+angle.toFixed(2)+')');
        head.setAttribute('opacity',still||p>.62?'1':'0');
        stage.style.transform=still?'none':'translateY('+(-6*Math.sin(p*Math.PI*2)).toFixed(2)+'px)';
        const phase=still?'still':p<.23?'sql':p<.61?'ai':'insights';
        if(root.dataset.phase!==phase){root.dataset.phase=phase;root.querySelectorAll('[data-ai-step]').forEach(el=>el.classList.toggle('is-active',el.dataset.aiStep===phase));}
      };
      state.render(0);
      state.timeline=window.Motion.animate(0,1,{duration:8.6,ease:'linear',repeat:Infinity,onUpdate:p=>state.render(p)});
      state.timeline.pause();
      state.onToggle=()=>{
        state.manualPause=!state.manualPause;
        state.toggle.setAttribute('aria-pressed',String(state.manualPause));
        const label=state.manualPause?state.toggle.dataset.playLabel:state.toggle.dataset.pauseLabel;
        state.toggle.setAttribute('aria-label',label);
        state.toggle.querySelector('[data-ai-toggle-label]').textContent=label;
        state.toggle.querySelector('[data-ai-pause-icon]').hidden=state.manualPause;
        state.toggle.querySelector('[data-ai-play-icon]').hidden=!state.manualPause;
        syncDataAiScene(state);
      };
      state.toggle?.addEventListener('click',state.onToggle);
      state.observer=new IntersectionObserver(entries=>{state.inView=entries[0].isIntersecting;syncDataAiScene(state)},{threshold:.03,rootMargin:'-100px 0px 0px 0px'});
      dataAiScenes.set(root,state);state.observer.observe(root);syncDataAiScene(state);
    });
  }
  document.addEventListener('visibilitychange',()=>dataAiScenes.forEach(syncDataAiScene));
  reduced.addEventListener('change',()=>dataAiScenes.forEach(syncDataAiScene));
  // A shared clock carries learning input into AI and on to the achievement milestone.
  const careerJourneys = new Map();
  function syncCareerJourney(state) {
    const playing=state.inView&&!document.hidden&&!reduced.matches&&!state.manualPause;
    playing?state.timeline.play():state.timeline.pause();
    state.root.dataset.playing=String(playing);
    if(reduced.matches)state.render(1,true);
  }
  function initCareerJourneys() {
    careerJourneys.forEach((state,root)=>{
      if(root.isConnected)return;
      state.timeline.stop();state.observer.disconnect();state.toggle.removeEventListener('click',state.onToggle);
      careerJourneys.delete(root);
    });
    if(!window.Motion)return;
    document.querySelectorAll('[data-career-journey]').forEach(root=>{
      if(careerJourneys.has(root))return;
      const find=name=>root.querySelector('[data-journey-'+name+']');
      const input=find('input'),output=find('output'),inputLength=input.getTotalLength(),outputLength=output.getTotalLength();
      const inputs=[...root.querySelectorAll('[data-journey-input-packet]')],outputs=[...root.querySelectorAll('[data-journey-output-packet]')];
      const glow=find('ai-glow'),ring=find('ai-ring'),achievement=find('achievement-glow'),goldRing=find('achievement-ring'),mentor=find('mentor-ring'),climber=find('climber');
      const stage=root.querySelector('.journey-scene-stage'),sparkles=[...root.querySelectorAll('[data-journey-sparkle]')];
      const clamp=n=>Math.max(0,Math.min(1,n)),range=(p,a,b)=>clamp((p-a)/(b-a)),smooth=n=>n*n*(3-2*n);
      const pulse=(p,a,b)=>p>a&&p<b?Math.sin(range(p,a,b)*Math.PI):0;
      const packet=(el,path,length,t,still)=>{
        const point=path.getPointAtLength(length*t);
        el.setAttribute('transform','translate('+point.x.toFixed(2)+' '+point.y.toFixed(2)+')');
        el.setAttribute('opacity',still?'0':Math.min(1,t*14,(1-t)*14).toFixed(3));
      };
      const state={root,inView:false,manualPause:false,timeline:null,toggle:find('toggle')};
      state.render=(p,still=false)=>{
        if(!root.isConnected)return;
        inputs.forEach((el,i)=>packet(el,input,inputLength,range(p,.025+i*.055,.43+i*.055),still));
        outputs.forEach((el,i)=>packet(el,output,outputLength,range(p,.64+i*.05,.9+i*.05),still));
        const ai=pulse(p,.43,.73),success=pulse(p,.86,1),learning=pulse(p,.08,.41);
        glow.setAttribute('opacity',still?'.12':(ai*(.8+.18*Math.sin(p*75))).toFixed(3));
        ring.setAttribute('opacity',still?'0':(ai*.85).toFixed(3));
        achievement.setAttribute('opacity',still?'.2':success.toFixed(3));
        goldRing.setAttribute('opacity',still?'0':(success*.7).toFixed(3));
        goldRing.setAttribute('r',(63+success*18).toFixed(2));
        mentor.setAttribute('opacity',still?'0':(learning*.6).toFixed(3));
        mentor.setAttribute('r',(30+learning*15).toFixed(2));
        const climb=smooth(range(p,.64,.94)),point=output.getPointAtLength(outputLength*climb);
        climber.setAttribute('transform','translate('+point.x.toFixed(2)+' '+point.y.toFixed(2)+')');
        climber.setAttribute('opacity',still?'0':pulse(p,.64,.98).toFixed(3));
        input.style.strokeDashoffset=still?'0':String(-p*90);
        output.style.strokeDashoffset=still?'0':String(-p*90);
        const positions=[[262,373],[736,120],[955,510],[267,650],[750,840]];
        sparkles.forEach((el,i)=>{
          const t=p*Math.PI*2+i;
          el.setAttribute('transform','translate('+(positions[i][0]+(still?0:Math.sin(t)*7)).toFixed(2)+' '+(positions[i][1]+(still?0:Math.cos(t)*11)).toFixed(2)+') rotate('+(still?0:Math.sin(t)*25).toFixed(2)+')');
          el.setAttribute('opacity',still?'.4':(.3+.5*(1+Math.sin(t))/2).toFixed(3));
        });
        stage.style.transform=still?'none':'translateY('+(-5*Math.sin(p*Math.PI*2)).toFixed(2)+'px)';
        const phase=still?'still':p<.43?'learn':p<.73?'build':'grow';
        if(root.dataset.phase!==phase){root.dataset.phase=phase;root.querySelectorAll('[data-journey-step]').forEach(el=>el.classList.toggle('is-active',still||el.dataset.journeyStep===phase));}
      };
      state.render(0);
      state.timeline=window.Motion.animate(0,1,{duration:10,ease:'linear',repeat:Infinity,onUpdate:p=>{if(reduced.matches){syncCareerJourney(state)}else{state.render(p)}}});
      state.timeline.pause();
      state.onToggle=()=>{
        state.manualPause=!state.manualPause;
        state.toggle.setAttribute('aria-pressed',String(state.manualPause));
        const label=state.manualPause?state.toggle.dataset.playLabel:state.toggle.dataset.pauseLabel;
        state.toggle.setAttribute('aria-label',label);find('toggle-label').textContent=label;find('toggle-icon').textContent=state.manualPause?'▶':'Ⅱ';
        syncCareerJourney(state);
      };
      state.toggle.addEventListener('click',state.onToggle);
      state.observer=new IntersectionObserver(entries=>{state.inView=entries[0].isIntersecting;syncCareerJourney(state)},{threshold:.03,rootMargin:'-100px 0px 0px 0px'});
      careerJourneys.set(root,state);state.observer.observe(root);syncCareerJourney(state);
    });
  }
  document.addEventListener('visibilitychange',()=>careerJourneys.forEach(syncCareerJourney));
  reduced.addEventListener('change',()=>careerJourneys.forEach(syncCareerJourney));

  // CSS runs the two seamless ribbons; this controller owns pause and lifecycle state.
  const companyRibbons = new Map();
  function syncCompanyRibbon(state) {
    state.root.dataset.playing = String(state.inView && !state.paused && !state.hovered && !document.hidden && !reduced.matches);
    state.toggle.setAttribute('aria-pressed', String(state.paused));
  }
  function initCompanyRibbons() {
    companyRibbons.forEach((state, root) => {
      if (root.isConnected) return;
      state.observer.disconnect();
      state.toggle.removeEventListener('click', state.onToggle);
      state.rails.removeEventListener('pointerenter', state.onEnter);
      state.rails.removeEventListener('pointerleave', state.onLeave);
      companyRibbons.delete(root);
    });
    document.querySelectorAll('[data-company-ribbons]').forEach(root => {
      if (companyRibbons.has(root)) return;
      const state = { root, rails:root.querySelector('.company-ribbons'), toggle:root.querySelector('[data-company-toggle]'), paused:false, hovered:false, inView:false };
      state.onToggle = () => { state.paused = !state.paused; syncCompanyRibbon(state); };
      state.onEnter = event => { if (event.pointerType !== 'mouse' || !finePointer.matches) return; state.hovered = true; syncCompanyRibbon(state); };
      state.onLeave = () => { state.hovered = false; syncCompanyRibbon(state); };
      state.toggle.addEventListener('click', state.onToggle);
      state.rails.addEventListener('pointerenter', state.onEnter);
      state.rails.addEventListener('pointerleave', state.onLeave);
      state.observer = new IntersectionObserver(entries => { state.inView = entries[0].isIntersecting; syncCompanyRibbon(state); }, { threshold:.03 });
      companyRibbons.set(root, state);
      state.observer.observe(root);
    });
  }
  document.addEventListener('visibilitychange', () => companyRibbons.forEach(syncCompanyRibbon));
  reduced.addEventListener('change', () => companyRibbons.forEach(syncCompanyRibbon));


  const skillGalaxies = new Map();
  function syncSkillGalaxy(state) {
    const playing = state.inView && !state.paused && !document.hidden && !reduced.matches;
    state.root.dataset.playing = String(playing);
    state.toggle.setAttribute('aria-pressed', String(state.paused));
    state.animations.forEach(animation => playing ? animation.play() : animation.pause());
  }
  function initSkillGalaxies() {
    skillGalaxies.forEach((state, root) => {
      if (root.isConnected) return;
      state.observer.disconnect();
      state.toggle.removeEventListener('click', state.onToggle);
      state.animations.forEach(animation => animation.cancel());
      skillGalaxies.delete(root);
    });
    document.querySelectorAll('[data-skill-galaxy]').forEach(root => {
      if (skillGalaxies.has(root)) return;
      const state = {root, toggle:root.querySelector('[data-galaxy-toggle]'), paused:false, inView:false, animations:[]};
      if (window.Motion) root.querySelectorAll('[data-galaxy-node]').forEach((node, i) => {
        const animation = window.Motion.animate(node, {y:[0,-8,0], rotate:[0,i%2?2:-2,0]}, {duration:4.5+i*.3, delay:i*.15, repeat:Infinity, ease:'easeInOut'});
        animation.pause();state.animations.push(animation);
      });
      state.onToggle = () => {state.paused=!state.paused;syncSkillGalaxy(state);};
      state.toggle.addEventListener('click',state.onToggle);
      state.observer=new IntersectionObserver(entries=>{state.inView=entries[0].isIntersecting;syncSkillGalaxy(state);},{threshold:.05});
      skillGalaxies.set(root,state);state.observer.observe(root);
    });
  }
  document.addEventListener('visibilitychange',()=>skillGalaxies.forEach(syncSkillGalaxy));
  reduced.addEventListener('change',()=>skillGalaxies.forEach(syncSkillGalaxy));

  // The collage stays still; Motion drives its data flow and small paper accents.
  const masterclassArt = new Map();
  function syncMasterclassArt(state) {
    const playing = state.inView && !state.paused && !document.hidden && !reduced.matches;
    state.root.dataset.playing = String(playing);
    state.animations.forEach(animation => playing ? animation.play() : animation.pause());
    if (!playing) state.root.dataset.stage = 'still';
  }
  function initMasterclassArt() {
    masterclassArt.forEach((state, root) => {
      if (root.isConnected) return;
      state.observer.disconnect();
      state.toggle.removeEventListener('click', state.onToggle);
      state.animations.forEach(animation => animation.cancel());
      masterclassArt.delete(root);
    });
    document.querySelectorAll('[data-masterclass-art]').forEach(root => {
      if (masterclassArt.has(root)) return;
      const state = {root, toggle:root.querySelector('[data-mc-toggle]'), paused:false, inView:false, animations:[]};
      if (window.Motion) {
        const path = root.querySelector('[data-mc-path]'), length = path.getTotalLength();
        const packets = [...root.querySelectorAll('[data-mc-packet]')];
        const flow = window.Motion.animate(0, 1, {duration:7, repeat:Infinity, ease:'linear', onUpdate:p => {
          packets.forEach((packet,i) => {
            const t = (p + i * .13) % 1, point = path.getPointAtLength(length * t);
            packet.setAttribute('transform', `translate(${point.x} ${point.y})`);
          });
          const stage = p > .19 && p < .45 ? 'ai' : 'flow';
          if (root.dataset.stage !== stage) root.dataset.stage = stage;
        }});
        flow.pause();state.animations.push(flow);
        root.querySelectorAll('[data-mc-float]').forEach((node,i) => {
          const animation = window.Motion.animate(node,{y:[0,-7,0],rotate:[0,i?10:-10,0]}, {duration:5+i,repeat:Infinity,ease:'easeInOut'});
          animation.pause();state.animations.push(animation);
        });
      }
      state.onToggle = () => {
        state.paused = !state.paused;
        state.toggle.setAttribute('aria-pressed',String(state.paused));
        state.toggle.setAttribute('aria-label',state.paused ? state.toggle.dataset.playLabel : state.toggle.dataset.pauseLabel);
        syncMasterclassArt(state);
      };
      state.toggle.addEventListener('click',state.onToggle);
      state.observer = new IntersectionObserver(entries => {state.inView=entries[0].isIntersecting;syncMasterclassArt(state);},{threshold:.05});
      masterclassArt.set(root,state);state.observer.observe(root);
    });
  }
  document.addEventListener('visibilitychange',() => masterclassArt.forEach(syncMasterclassArt));
  reduced.addEventListener('change',() => masterclassArt.forEach(syncMasterclassArt));

  function init() {
    initMasterclassArt();
    initSkillGalaxies();
    initCompanyRibbons();
    initCareerJourneys();
    initDataAiScenes();
    initCareerHeroes();
    if (!progress) { progress = document.createElement('div'); progress.id = 'nextgen-scroll-progress'; progress.setAttribute('aria-hidden', 'true'); document.body.appendChild(progress); }
    document.querySelectorAll('.course-art, .learning-universe, .contact-advisor-scene, .project-demo').forEach(el => {
      if (artworkBound.has(el)) return;
      artworkBound.add(el);
      artworkObserver.observe(el);
    });
    document.querySelectorAll('[data-motion]').forEach(reveal);
    document.querySelectorAll('.spotlight-card, [data-tilt], [data-scene-tilt]').forEach(bindPointer);
    document.querySelectorAll('.nav-links a').forEach(link => {
      if (link.pathname === location.pathname || (link.pathname !== '/' && location.pathname.startsWith(link.pathname + '/'))) link.setAttribute('aria-current', 'page');
      else link.removeAttribute('aria-current');
    });
    updateChrome();
  }
  function scheduleInit() { if (queued) return; queued = true; requestAnimationFrame(() => { queued = false; init(); }); }
  let scrollFrame = 0;
  window.addEventListener('scroll', () => { if (scrollFrame) return; scrollFrame = requestAnimationFrame(() => { scrollFrame = 0; updateChrome(); }); }, { passive: true });
  window.addEventListener('resize', updateChrome, { passive: true });
  reduced.addEventListener('change', () => { document.querySelectorAll('.motion-pending').forEach(el => { el.classList.remove('motion-pending'); el.classList.add('motion-visible'); revealObserver.unobserve(el); }); });
  window.initNextGenMotion = scheduleInit;
  window.fireConfetti = () => { if (!reduced.matches && typeof window.confetti === 'function') window.confetti({ particleCount: 35, spread: 45, colors: ['#4263f5', '#9bb1ff', '#51b999'], disableForReducedMotion: true }); };
  function start() { init(); const observer = new MutationObserver(records => { if (records.some(record => [...record.addedNodes, ...record.removedNodes].some(node => node.nodeType === 1))) scheduleInit(); }); observer.observe(document.body, { childList: true, subtree: true }); }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start, { once: true }); else start();
})();


// Keep the selected recommendation visible beneath the sticky navigation.
window.revealNextGenDirection = () => {
  const card = document.querySelector('#recommended-direction');
  const layout = document.querySelector('.direction-layout');
  const header = document.querySelector('.site-nav');
  if (!card || !layout) return;
  const target = innerWidth < 768 ? card : layout;
  const inset = (header?.getBoundingClientRect().bottom || 90) + 20;
  const top = target.getBoundingClientRect().top;
  if (innerWidth < 768 || top < inset || top > innerHeight * .65) {
    window.scrollTo({top: Math.max(0, scrollY + top - inset), behavior: 'instant'});
  }
};
