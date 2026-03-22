(function () {
  'use strict';

  // ---------- 工艺概览数据（设备 + 文字流程）----------
  var devices = [
    { id: 'IiDKO4wh8ftm1ieObsXz-6', name: '除臭设备', x: 179, y: 120, w: 120, h: 60 },
    { id: 'IiDKO4wh8ftm1ieObsXz-10', name: '电磁阀', x: 179, y: 300, w: 70, h: 40 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-22', name: '自来水补水', x: 179, y: 230, w: 70, h: 40 },
    { id: 'IiDKO4wh8ftm1ieObsXz-11', name: '格栅机', x: 49, y: 430, w: 70, h: 40 },
    { id: 'IiDKO4wh8ftm1ieObsXz-12', name: '调节池', x: 179, y: 420, w: 120, h: 110 },
    { id: 'IiDKO4wh8ftm1ieObsXz-14', name: '搅拌机', x: 379, y: 160, w: 120, h: 60 },
    { id: 'IiDKO4wh8ftm1ieObsXz-15', name: '碳源投加', x: 499, y: 160, w: 120, h: 60 },
    { id: 'IiDKO4wh8ftm1ieObsXz-16', name: '加药装置', x: 379, y: 220, w: 240, h: 60 },
    { id: 'IiDKO4wh8ftm1ieObsXz-17', name: '1#调节池提升泵', x: 369, y: 420, w: 210, h: 60 },
    { id: 'IiDKO4wh8ftm1ieObsXz-18', name: '2#调节池提升泵', x: 369, y: 500, w: 210, h: 60 },
    { id: 'IiDKO4wh8ftm1ieObsXz-20', name: '缺氧池', x: 659, y: 420, w: 120, h: 110 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-1', name: '1#鼓风机', x: 789, y: 160, w: 90, h: 40 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-2', name: '2#鼓风机', x: 789, y: 230, w: 90, h: 40 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-3', name: '缺氧气搅拌机', x: 709, y: 330, w: 90, h: 40 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-4', name: 'MBR膜池', x: 849, y: 420, w: 120, h: 110 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-5', name: '1#回流泵', x: 789, y: 560, w: 90, h: 40 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-6', name: '2#回流泵', x: 789, y: 610, w: 90, h: 40 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-7', name: '反洗罐', x: 1089, y: 170, w: 90, h: 40 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-8', name: '1#反洗泵', x: 989, y: 240, w: 90, h: 40 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-9', name: '2#反洗泵', x: 989, y: 290, w: 90, h: 40 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-10', name: '2#产水泵', x: 1089, y: 480, w: 90, h: 40 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-11', name: '1#产水泵', x: 1089, y: 420, w: 90, h: 40 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-12', name: '电磁阀', x: 1259, y: 320, w: 70, h: 40 },
    { id: 'IiDKO4wh8ftm1ieObsXz-9', name: '自来水补水', x: 1259, y: 190, w: 120, h: 60 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-13', name: '中间水池', x: 1279, y: 410, w: 120, h: 110 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-14', name: '1#回用泵', x: 1329, y: 585, w: 90, h: 40 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-15', name: '2#回用泵', x: 1329, y: 625, w: 90, h: 40 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-16', name: '3#回用泵', x: 1329, y: 666, w: 90, h: 40 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-17', name: '变频模块', x: 1329, y: 700, w: 90, h: 40 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-46', name: '次氯酸钠投加', x: 1439, y: 160, w: 90, h: 30 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-47', name: '加药装置', x: 1439, y: 190, w: 90, h: 30 },
    { id: 'h1sOd0kz_K9NjbGk3U_x-55', name: '至用水单元', x: 1509, y: 385, w: 120, h: 110 }
  ];

  var flowText = [
    '[格栅机] ──────────────────┐',
    '                            │',
    '[自来水补水] → [电磁阀] → [调节池] ──┬──→ [1#调节池提升泵] ──→ [缺氧池]',
    '     （自来水补水经电磁阀接入调节池）  │         ↑',
    '                            └──→ [2#调节池提升泵] ──→ [缺氧池]',
    '[除臭设备] ────────────────────────────┘',
    '[加药装置] ─────────────────────────────┘',
    '',
    '[1#/2#鼓风机] ──→ [MBR膜池] ←── [缺氧池]',
    '     │                  │',
    '     │                  ├──→ [1#/2#产水泵] ──→ [中间水池]',
    '     │                  │',
    '     │                  ←── [1#/2#反洗泵] ←── [反洗罐]',
    '     │',
    '     └── [缺氧气搅拌机]（缺氧池搅拌）',
    '',
    '[中间水池] ←── [电磁阀] ←── [自来水补水]',
    '     ↑',
    '[加药装置/次氯酸钠投加]',
    '     │',
    '     └──→ [1#/2#/3#回用泵]（变频模块）──→ [至用水单元]'
  ].join('\n');

  // ---------- 工艺简图 SVG ----------
  function renderOverviewSvg() {
    var svg = document.getElementById('overviewSvg');
    if (!svg) return;
    var devLayer = document.getElementById('devices-layer');
    var pipeLayer = document.getElementById('pipes-layer');
    if (!devLayer || !pipeLayer) return;

    pipeLayer.innerHTML = '';
    devLayer.innerHTML = '';

    devices.forEach(function (d) {
      var g = document.createElementNS('http://www.w3.org/2000/svg', 'g');
      g.setAttribute('class', 'diagram-unit');
      g.setAttribute('data-name', d.name);
      var rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
      rect.setAttribute('x', d.x);
      rect.setAttribute('y', d.y);
      rect.setAttribute('width', d.w);
      rect.setAttribute('height', d.h);
      rect.setAttribute('rx', '4');
      rect.setAttribute('fill', 'url(#tankGrad)');
      rect.setAttribute('stroke', '#4a90b0');
      rect.setAttribute('stroke-width', '2');
      g.appendChild(rect);
      var text = document.createElementNS('http://www.w3.org/2000/svg', 'text');
      text.setAttribute('x', d.x + d.w / 2);
      text.setAttribute('y', d.y + d.h / 2 + 4);
      text.setAttribute('text-anchor', 'middle');
      text.setAttribute('fill', '#e0e8f0');
      text.setAttribute('font-size', d.h < 50 ? '10' : '12');
      text.textContent = d.name.length > 8 ? d.name.slice(0, 6) + '…' : d.name;
      g.appendChild(text);
      devLayer.appendChild(g);
    });
  }

  function setFlowText() {
    var el = document.getElementById('flowText');
    if (el) el.textContent = flowText;
  }

  function initClock() {
    var el = document.getElementById('sysTime');
    if (!el) return;
    function update() {
      var d = new Date();
      el.textContent = d.getHours().toString().padStart(2, '0') + ':' +
        d.getMinutes().toString().padStart(2, '0') + ':' +
        d.getSeconds().toString().padStart(2, '0');
    }
    update();
    setInterval(update, 1000);
  }

  function init() {
    renderOverviewSvg();
    setFlowText();
    initClock();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
