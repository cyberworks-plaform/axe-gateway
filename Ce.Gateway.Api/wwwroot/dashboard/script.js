function nowIso(){return new Date().toISOString().replace('T',' ').slice(0,19);}
function randInt(min,max){return Math.floor(Math.random()*(max-min+1))+min;}

const ROUTES = ['/api/ocr/hopdong','/api/ocr/giaykhaisinh','/api/ocr/summary','/api/ocr/spellcheck'];
const NODES = [
  {id:'ocr-node-1','addr':'10.0.0.11:5000','routes':['/api/ocr/hopdong','/api/ocr/summary'],status:'up'},
  {id:'ocr-node-2','addr':'10.0.0.12:5000','routes':['/api/ocr/giaykhaisinh','/api/ocr/spellcheck'],status:'up'},
  {id:'ocr-node-3','addr':'10.0.0.13:5000','routes':['/api/ocr/hopdong'],status:'down'}
];

// Removed fake data generation functions

let rpsChart, latChart, statusChart;

function initCharts(){
  const rpsCtx = document.getElementById('rpsChart').getContext('2d');
  rpsChart = new Chart(rpsCtx, {type:'line', data:{labels:[],datasets:[{label:'RPS',data:[],tension:0.3, fill:true}]}, options:{scales:{y:{beginAtZero:true}}}});

  const latCtx = document.getElementById('latencyChart').getContext('2d');
  latChart = new Chart(latCtx, {type:'line', data:{labels:[],datasets:[{label:'Latency ms',data:[],tension:0.3}]}, options:{scales:{y:{beginAtZero:true}}}});

  const stCtx = document.getElementById('statusChart').getContext('2d');
  statusChart = new Chart(stCtx, {type:'doughnut', data:{labels:['2xx','4xx','5xx'],datasets:[{data:[0,0,0],label:'Status'}]}} );
}

async function refreshAll(){
  document.getElementById('server-time').innerText = new Date().toLocaleString();
  const windowKey = document.getElementById('overview-window').value;

  // Fetch real data from /api/monitor/logs
  try {
    const response = await fetch(`/api/monitor/logs?page=1&pageSize=1000`); // Fetch a larger set of logs for analysis
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    const data = await response.json();
    const logs = data.data;

    // Process logs to generate overview metrics
    const overviewMetrics = processLogsForOverview(logs, windowKey);
    document.getElementById('total-rps').innerText = overviewMetrics.totals.rps + ' /s';
      document.getElementById('total-requests').innerText = overviewMetrics.totals.requests;
      document.getElementById('error-rate').innerText = overviewMetrics.totals.errorRate + '%';
      //document.getElementById('avgLatency').innerText = overviewMetrics.totals.avgLatency;
    rpsChart.data.labels = overviewMetrics.labels; rpsChart.data.datasets[0].data = overviewMetrics.rps; rpsChart.update();
    latChart.data.labels = overviewMetrics.labels; latChart.data.datasets[0].data = overviewMetrics.latency; latChart.update();
    statusChart.data.datasets[0].data = [overviewMetrics.status['2xx'], overviewMetrics.status['4xx'], overviewMetrics.status['5xx']]; statusChart.update();

    // Process logs for nodes and errors
    const nodesData = processLogsForNodes(logs);
    const errorsData = processLogsForErrors(logs);

    // routes chips (dynamic from logs)
    const uniqueRoutes = [...new Set(logs.map(log => log.upstreamPath))];
    const rc = document.getElementById('routes-chips'); rc.innerHTML='';
    uniqueRoutes.forEach(r=>{ const el = document.createElement('div'); el.className='chip'; el.innerText=r; rc.appendChild(el); });

    // nodes list & table
    const nl = document.getElementById('nodes-list'); nl.innerHTML='';
    const tb = document.getElementById('nodes-table-body'); tb.innerHTML='';
    nodesData.forEach(n=>{
      const d = document.createElement('div');
      const st = n.status==='up' ? '<span class="status-dot up"></span>UP' : (n.status==='down' ? '<span class="status-dot down"></span>DOWN' : '<span class="status-dot deg"></span>DEG');
      d.innerHTML = `<div style="margin-bottom:8px"><b>${n.addr}</b><div class="small muted">${n.routes.join(', ')}</div><div class="small">status: ${st} • latency ${n.avgLatency}ms • ${n.reqPerHour}/hr</div></div>`;
      nl.appendChild(d);

      const tr = document.createElement('tr');
      tr.innerHTML = `<td>${n.addr}</td><td>${n.routes.join('<br>')}</td><td>${n.status==='up' ? '<span class="status-dot up"></span>UP' : '<span class="status-dot down"></span>DOWN'}</td><td>${n.avgLatency}</td><td>${n.reqPerHour}</td>`;
      tb.appendChild(tr);
    });

    // populate filters routes select
    const sel = document.getElementById('filter-route'); sel.innerHTML = '<option value="">All routes</option>';
    uniqueRoutes.forEach(r=> sel.innerHTML += `<option value="${r}">${r}</option>`);

    // errors
    const eb = document.getElementById('errors-body'); eb.innerHTML='';
    errorsData.forEach(e=>{
      const tr = document.createElement('tr');
      tr.innerHTML = `<td>${new Date(e.time).toLocaleString()}</td><td>${e.route}</td><td>${e.node}</td><td style="color:${e.status>=500? '#ff6b6b':'#f59e0b'}">${e.status}</td><td>${e.latency}</td><td>${e.cid}</td>`;
      tr.onclick = ()=> openDetail(e);
      eb.appendChild(tr);
    });

  } catch (error) {
    console.error('Error fetching dashboard data:', error);
    // Display error message on dashboard
  }
}

function processLogsForOverview(logs, windowKey) {
  const now = Date.now();
  const timeWindowMs = {
    '1m': 60 * 1000,
    '5m': 5 * 60 * 1000,
    '30m': 30 * 60 * 1000,
    '1h': 60 * 60 * 1000,
    '1d': 24 * 60 * 60 * 1000,
  }[windowKey] || (5 * 60 * 1000); // Default to 5m

  const filteredLogs = logs.filter(log => (now - new Date(log.createdAtUtc).getTime()) < timeWindowMs);

  const totalRequests = filteredLogs.length;
  const errorCount = filteredLogs.filter(log => log.isError).length;
  const errorRate = totalRequests > 0 ? ((errorCount / totalRequests) * 100).toFixed(1) : 0;

  // Calculate RPS more accurately based on the actual time span of filtered logs
  let rps = 0;
  if (totalRequests > 0) {
    const minTime = Math.min(...filteredLogs.map(log => new Date(log.createdAtUtc).getTime()));
    const maxTime = Math.max(...filteredLogs.map(log => new Date(log.createdAtUtc).getTime()));
    const actualTimeSpanSeconds = (maxTime - minTime) / 1000;
    if (actualTimeSpanSeconds > 0) {
      rps = (totalRequests / actualTimeSpanSeconds).toFixed(1);
    }
  }

  const totalLatency = filteredLogs.reduce((sum, log) => sum + (log.gatewayLatencyMs || 0), 0);
  const avgLatency = totalRequests > 0 ? (totalLatency / totalRequests).toFixed(0) : 0;

  const statusDistribution = { '2xx': 0, '4xx': 0, '5xx': 0 };
  filteredLogs.forEach(log => {
    if (log.downstreamStatusCode >= 200 && log.downstreamStatusCode < 300) statusDistribution['2xx']++;
    else if (log.downstreamStatusCode >= 400 && log.downstreamStatusCode < 500) statusDistribution['4xx']++;
    else if (log.downstreamStatusCode >= 500) statusDistribution['5xx']++;
  });

  // For charts, we need time series data. This is a simplified placeholder for now.
  // In a real scenario, you'd aggregate logs into time buckets.
  const labels = Array.from({length:12}).map((_,i)=> (new Date(now - (11-i)*timeWindowMs/12)).toLocaleTimeString());
  const rpsSeries = labels.map(()=> Math.max(0, Math.round(Math.random()*50))); // Still fake for series
  const latencySeries = labels.map(()=> Math.round(50 + Math.random()*400)); // Still fake for series

  return {
    labels: labels,
    rps: rpsSeries,
    latency: latencySeries,
    status: statusDistribution,
    totals: { rps: rps, requests: totalRequests, errorRate: errorRate, avgLatency: avgLatency }
  };
}

function processLogsForNodes(logs) {
  const nodesMap = new Map(); // addr -> { id, addr, routes, status, totalLatency, requestCount, errorCount, lastRequestTime }
  const now = Date.now();

  logs.forEach(log => {
    const addr = log.downstreamHost + ':' + log.downstreamPort;
    if (!nodesMap.has(addr)) {
      nodesMap.set(addr, { id: addr, addr: addr, routes: new Set(), status: 'up', totalLatency: 0, requestCount: 0, errorCount: 0, lastRequestTime: 0 });
    }
    const node = nodesMap.get(addr);
    node.routes.add(log.upstreamPath); // Using upstreamPath as route for simplicity
    node.totalLatency += log.gatewayLatencyMs || 0;
    node.requestCount++;
    if (log.isError) node.errorCount++;
    // Determine node status based on recent errors or status codes
    if (log.downstreamStatusCode >= 500) node.status = 'down'; // Simple status logic
    node.lastRequestTime = Math.max(node.lastRequestTime, new Date(log.createdAtUtc).getTime());
  });

  return Array.from(nodesMap.values()).map(node => {
    // Calculate requests per hour based on a fixed window (e.g., last hour of logs)
    // This is a simplification; a real system would aggregate over a sliding window.
    const oneHourAgo = now - (60 * 60 * 1000);
    const recentRequests = logs.filter(log => {
      const logTime = new Date(log.createdAtUtc).getTime();
      const logAddr = log.downstreamHost + ':' + log.downstreamPort;
      return logAddr === node.addr && logTime > oneHourAgo;
    }).length;
    const reqPerHour = recentRequests; // Simplified: count of requests in the last hour of available logs

    return {
      id: node.id,
      addr: node.addr,
      routes: Array.from(node.routes),
      status: node.status,
      avgLatency: node.requestCount > 0 ? (node.totalLatency / node.requestCount).toFixed(0) : 0,
      reqPerHour: reqPerHour
    };
  });
}

function processLogsForErrors(logs) {
  // Filter for errors and map to the expected format
  return logs.filter(log => log.isError).map(log => ({
    time: log.createdAtUtc,
    route: log.upstreamPath,
    node: log.downstreamHost + ':' + log.downstreamPort,
    status: log.downstreamStatusCode,
    latency: log.gatewayLatencyMs,
    cid: log.traceId,
    request: { method: log.upstreamHttpMethod, path: log.upstreamPath, body: log.requestBody || '' },
    response: { status: log.downstreamStatusCode, body: log.errorMessage || '' }
  }));
}

function openDetail(e){
  document.getElementById('detail-sub').innerText = `${new Date(e.time).toLocaleString()} • ${e.cid}`;
  document.getElementById('det-status').innerText = e.status;
  document.getElementById('det-lat').innerText = e.latency + ' ms';
  document.getElementById('det-node').innerText = e.node;
  document.getElementById('det-up').innerText = JSON.stringify(e.request,null,2);
  document.getElementById('det-down').innerText = JSON.stringify(e.response,null,2);
  document.getElementById('detailModal').style.display='flex';
}
function closeModal(){ document.getElementById('detailModal').style.display='none'; }

document.addEventListener('DOMContentLoaded', () => {
  initCharts();
  refreshAll();
  // auto-refresh every 15s
  setInterval(refreshAll, 15000);

  document.getElementById('refresh-btn').addEventListener('click', refreshAll);
  document.getElementById('btn-tail').addEventListener('click', ()=>alert('Tail mode (live) — in real deploy connect SignalR/WebSocket)'));
  document.getElementById('overview-window').addEventListener('change', refreshAll);
});