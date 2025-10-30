let requestTimelineChart;
let latencyTimelineChart;
let httpStatusDonutChart;

// Register Chart.js Datalabels Plugin
Chart.register(ChartDataLabels);

const API_BASE_URL = '/api/dashboard';
const RELOAD_INTERVAL_MS = 30000; // 30 seconds

// Function to get start and end time based on selected filter
function getTimeRange() {
    const timeFilter = document.getElementById('timeFilter').value;
    let endTime = new Date();
    let startTime = new Date();

    switch (timeFilter) {
        case '5m': startTime.setMinutes(endTime.getMinutes() - 5); break;
        case '15m': startTime.setMinutes(endTime.getMinutes() - 15); break;
        case '30m': startTime.setMinutes(endTime.getMinutes() - 30); break;
        case '1h': startTime.setHours(endTime.getHours() - 1); break;
        case '3h': startTime.setHours(endTime.getHours() - 3); break;
        case '6h': startTime.setHours(endTime.getHours() - 6); break;
        case '12h': startTime.setHours(endTime.getHours() - 12); break;
        case '24h': startTime.setHours(endTime.getHours() - 24); break;
        case '7d': startTime.setDate(endTime.getDate() - 7); break;
        case '30d': startTime.setDate(endTime.getDate() - 30); break;
        case '60d': startTime.setDate(endTime.getDate() - 60); break;
        case '90d': startTime.setDate(endTime.getDate() - 90); break;
        default: startTime.setHours(endTime.getHours() - 1); break; // Default to 1 hour
    }

    return {
        startTime: startTime.toISOString(),
        endTime: endTime.toISOString()
    };
}

// Helper to format UTC date for display
function formatUtcDate(dateString) {
    const date = new Date(dateString);
    // Add 7 hours for UTC+7
    date.setUTCHours(date.getUTCHours() + 7);
    return date.toLocaleString('en-US', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        hour12: false // Use 24-hour format
    });
}

// Helper to format datetime for node last checked (UTC+7) - Full date and time
function formatNodeLastChecked(dateTimeString) {
    if (!dateTimeString) return 'N/A';
    const date = new Date(dateTimeString);
    if (isNaN(date.getTime())) return 'N/A';
    
    // Get UTC components and add 7 hours manually for UTC+7
    const utcYear = date.getUTCFullYear();
    const utcMonth = String(date.getUTCMonth() + 1).padStart(2, '0');
    const utcDay = String(date.getUTCDate()).padStart(2, '0');
    let utcHours = date.getUTCHours() + 7;
    const utcMinutes = String(date.getUTCMinutes()).padStart(2, '0');
    const utcSeconds = String(date.getUTCSeconds()).padStart(2, '0');
    
    // Handle day overflow when adding 7 hours
    let adjustedDay = utcDay;
    if (utcHours >= 24) {
        utcHours -= 24;
        adjustedDay = String(parseInt(utcDay) + 1).padStart(2, '0');
    }
    const formattedHours = String(utcHours).padStart(2, '0');
    
    return `${formattedHours}:${utcMinutes}:${utcSeconds}`;
}

// Fetch and update all dashboard data
async function fetchDashboardData() {
    const { startTime, endTime } = getTimeRange();

    try {
        const [overview, routeSummary, nodeSummary, recentErrors, nodeStatusWithMetrics] = await Promise.all([
            fetch(`${API_BASE_URL}/overview?startTime=${startTime}&endTime=${endTime}`).then(res => res.json()),
            fetch(`${API_BASE_URL}/routesummary?startTime=${startTime}&endTime=${endTime}`).then(res => res.json()),
            fetch(`${API_BASE_URL}/nodesummary?startTime=${startTime}&endTime=${endTime}`).then(res => res.json()),
            fetch(`${API_BASE_URL}/recenterrors?startTime=${startTime}&endTime=${endTime}`).then(res => res.json()),
            fetch(`${API_BASE_URL}/nodestatuswithmetrics?startTime=${startTime}&endTime=${endTime}`).then(res => {
                if (!res.ok) {
                    console.error('Failed to fetch node status:', res.status, res.statusText);
                    return [];
                }
                return res.json();
            })
        ]);

        updateOverview(overview);
        renderRequestTimelineChart(overview.requestTimeline);
        renderLatencyTimelineChart(overview.latencyTimeline);
        renderHttpStatusDonutChart(overview.httpStatusDistribution);
        updateCurrentNodeStatusTable(nodeStatusWithMetrics || []);
        updateRouteSummaryTable(routeSummary);
        updateNodeSummaryTable(nodeSummary);
        updateRecentErrorsTable(recentErrors);

    } catch (error) {
        console.error('Error fetching dashboard data:', error);
        // Optionally display an error message on the dashboard
    }
}

function updateOverview(data) {
    // Combined Node Status Widget
    const nodeStatusValueElem = document.getElementById('nodeStatusValue');
    const nodeStatusTitleElem = document.getElementById('nodeStatusTitle');
    const nodeStatusBox = nodeStatusValueElem.closest('.small-box');

    if (data.nodesDown === 0) {
        nodeStatusValueElem.textContent = data.totalNodes;
        nodeStatusTitleElem.textContent = 'All nodes Up';
        nodeStatusBox.classList.remove('bg-danger');
        nodeStatusBox.classList.add('bg-info');
    } else {
        nodeStatusValueElem.textContent = `${data.nodesDown} / ${data.totalNodes}`;
        nodeStatusTitleElem.textContent = 'Nodes Down / Total';
        nodeStatusBox.classList.remove('bg-info');
        nodeStatusBox.classList.add('bg-danger');
    }

    document.getElementById('totalRequests').textContent = data.totalRequests;
    document.getElementById('errorRate').textContent = data.errorRate.toFixed(2) + '%';
    document.getElementById('avgLatency').textContent = data.avgLatencyMs + 'ms';
}

function renderRequestTimelineChart(data) {
    const ctx = document.getElementById('requestTimelineChart').getContext('2d');
    const labels = data.map(item => item.timestamp);
    const requestCounts = data.map(item => item.requestCount);

    if (requestTimelineChart) {
        requestTimelineChart.data.labels = labels;
        requestTimelineChart.data.datasets[0].data = requestCounts;
        requestTimelineChart.update();
    } else {
        requestTimelineChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Total Requests',
                    data: requestCounts,
                    backgroundColor: 'rgba(60,141,188,0.9)',
                    borderColor: 'rgba(60,141,188,0.8)',
                    pointRadius: false,
                    pointColor: '#3b8bba',
                    pointStrokeColor: 'rgba(60,141,188,1)',
                    pointHighlightFill: '#fff',
                    pointHighlightStroke: 'rgba(60,141,188,1)',
                    fill: true,
                }]
            },
            options: {
                maintainAspectRatio: false,
                responsive: true,
                scales: {
                    x: {
                        type: 'category',
                        labels: labels,
                        grid: {
                            display: false
                        }
                    },
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) { if (value % 1 === 0) return value; }
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: false
                    },
                    datalabels: {
                        display: false
                    }
                }
            }
        });
    }
}

function renderLatencyTimelineChart(data) {
    const ctx = document.getElementById('latencyTimelineChart').getContext('2d');
    const labels = data.map(item => item.timestamp);
    const avgLatencies = data.map(item => item.requestCount); // requestCount is avg latency here

    if (latencyTimelineChart) {
        latencyTimelineChart.data.labels = labels;
        latencyTimelineChart.data.datasets[0].data = avgLatencies;
        latencyTimelineChart.update();
    } else {
        latencyTimelineChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Avg Latency (ms)',
                    data: avgLatencies,
                    backgroundColor: 'rgba(255,193,7,0.9)', // Warning yellow
                    borderColor: 'rgba(255,193,7,0.8)',
                    pointRadius: false,
                    pointColor: '#ffc107',
                    pointStrokeColor: 'rgba(255,193,7,1)',
                    pointHighlightFill: '#fff',
                    pointHighlightStroke: 'rgba(255,193,7,1)',
                    fill: true,
                }]
            },
            options: {
                maintainAspectRatio: false,
                responsive: true,
                plugins: {
                    legend: {
                        display: false
                    },
                    datalabels: {
                        display: false
                    }
                },
                scales: {
                    x: {
                        type: 'category',
                        labels: labels,
                        grid: {
                            display: false
                        }
                    },
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) { if (value % 1 === 0) return value; }
                        }
                    }
                }
            }
        });
    }
}

function renderHttpStatusDonutChart(data) {
    const ctx = document.getElementById('httpStatusDonutChart').getContext('2d');
    const labels = data.map(item => item.label);
    const counts = data.map(item => item.count);

    const backgroundColors = [
        '#28a745', // 2xx (Green)
        '#ffc107', // 3xx (Yellow/Orange)
        '#fd7e14', // 4xx (Orange)
        '#dc3545'  // 5xx (Red)
    ];

    if (httpStatusDonutChart) {
        httpStatusDonutChart.data.labels = labels;
        httpStatusDonutChart.data.datasets[0].data = counts;
        httpStatusDonutChart.update();
    } else {
        httpStatusDonutChart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [
                    {
                        data: counts,
                        backgroundColor: backgroundColors,
                    }
                ]
            },
            options: {
                maintainAspectRatio: false,
                responsive: true,
                plugins: {
                    legend: {
                        position: 'bottom',
                    },
                    datalabels: {
                        color: '#fff',
                        formatter: (value, ctx) => {
                            return value;
                        },
                        font: {
                            weight: 'bold'
                        }
                    }
                }
            }
        });
    }
}

let allNodeStatusData = [];
let nodeNetworkGraph = null;

// Initialize network graph on page load
document.addEventListener('DOMContentLoaded', () => {
    nodeNetworkGraph = new NodeNetworkGraph('nodeNetworkGraph', {
        showInfoPanel: true
    });
});

function updateCurrentNodeStatusTable(data) {
    allNodeStatusData = data;
    applyNodeStatusFilter();
    updateNodeNetworkGraph(data);
}

function updateNodeNetworkGraph(data) {
    if (nodeNetworkGraph) {
        nodeNetworkGraph.updateData(data);
        
        // Update counters
        const healthyCount = data.filter(item => item.isHealthy).length;
        const unhealthyCount = data.filter(item => !item.isHealthy).length;
        const totalRequests = data.reduce((sum, item) => sum + (item.totalRequests || 0), 0);
        
        document.getElementById('networkHealthyNodeCount').textContent = healthyCount;
        document.getElementById('networkUnhealthyNodeCount').textContent = unhealthyCount;
        
        // Update stats bar
        document.getElementById('totalNodesCount').textContent = data.length;
        document.getElementById('healthyNodesCount').textContent = healthyCount;
        document.getElementById('unhealthyNodesCount').textContent = unhealthyCount;
        document.getElementById('totalRequestsAll').textContent = totalRequests.toLocaleString();
    }
}

function applyNodeStatusFilter() {
    const filter = document.getElementById('nodeStatusFilter').value;
    const tableBody = document.querySelector('#currentNodeStatusTable tbody');
    tableBody.innerHTML = '';
    
    let filteredData = allNodeStatusData;
    if (filter === 'up') {
        filteredData = allNodeStatusData.filter(item => item.isHealthy);
    } else if (filter === 'down') {
        filteredData = allNodeStatusData.filter(item => !item.isHealthy);
    }
    
    let healthyCount = allNodeStatusData.filter(item => item.isHealthy).length;
    let unhealthyCount = allNodeStatusData.filter(item => !item.isHealthy).length;
    
    filteredData.forEach((item, index) => {
        const row = tableBody.insertRow();
        row.className = 'main-node-row';
        row.dataset.index = index;
        row.dataset.nodeData = JSON.stringify(item);
        
        const expandCell = row.insertCell();
        expandCell.innerHTML = '<i class="fas fa-chevron-right expand-icon"></i>';
        expandCell.style.cursor = 'pointer';
        expandCell.style.textAlign = 'center';
        
        row.insertCell().textContent = item.node || 'N/A';
        
        const statusCell = row.insertCell();
        const statusIndicator = document.createElement('span');
        statusIndicator.className = 'status-indicator';
        statusIndicator.style.display = 'inline-block';
        statusIndicator.style.width = '12px';
        statusIndicator.style.height = '12px';
        statusIndicator.style.borderRadius = '50%';
        statusIndicator.style.backgroundColor = item.isHealthy ? '#28a745' : '#dc3545';
        statusIndicator.title = item.isHealthy ? 'Healthy' : 'Unhealthy';
        statusCell.appendChild(statusIndicator);
        
        const totalRequestsCell = row.insertCell();
        totalRequestsCell.classList.add('text-right');
        totalRequestsCell.textContent = item.totalRequests || 0;
        
        const avgLatencyCell = row.insertCell();
        avgLatencyCell.classList.add('text-right');
        avgLatencyCell.textContent = item.avgLatencyMs || 0;
        
        const minLatencyCell = row.insertCell();
        minLatencyCell.classList.add('text-right');
        minLatencyCell.textContent = item.minLatencyMs || 0;
        
        const maxLatencyCell = row.insertCell();
        maxLatencyCell.classList.add('text-right');
        maxLatencyCell.textContent = item.maxLatencyMs || 0;
        
        row.onclick = function() {
            toggleNodeDetails(this);
        };
    });
    
    document.getElementById('healthyNodeCount').textContent = healthyCount;
    document.getElementById('unhealthyNodeCount').textContent = unhealthyCount;
}

function toggleNodeDetails(row) {
    const expandIcon = row.querySelector('.expand-icon');
    const nextRow = row.nextElementSibling;
    
    if (nextRow && nextRow.classList.contains('detail-row')) {
        nextRow.remove();
        expandIcon.className = 'fas fa-chevron-right expand-icon';
        row.classList.remove('expanded');
    } else {
        const nodeData = JSON.parse(row.dataset.nodeData);
        const detailRow = document.createElement('tr');
        detailRow.className = 'detail-row';
        
        const detailCell = document.createElement('td');
        detailCell.colSpan = 7;
        detailCell.style.backgroundColor = '#f8f9fa';
        detailCell.style.padding = '15px';
        
        let entriesHtml = '';
        if (nodeData.entries && Object.keys(nodeData.entries).length > 0) {
            entriesHtml = '<div class="health-check-entries"><h6>Health Check Details:</h6><table class="table table-sm table-bordered">';
            entriesHtml += '<thead><tr><th>Check Name</th><th>Status</th><th>Description</th><th>Duration</th></tr></thead><tbody>';
            for (const [key, entry] of Object.entries(nodeData.entries)) {
                const statusColor = entry.status === 'Healthy' ? '#28a745' : '#dc3545';
                entriesHtml += `<tr>
                    <td>${key}</td>
                    <td><span style="color: ${statusColor}; font-weight: bold;">${entry.status}</span></td>
                    <td>${entry.description || '-'}</td>
                    <td>${entry.duration || '-'}</td>
                </tr>`;
            }
            entriesHtml += '</tbody></table></div>';
        }
        
        detailCell.innerHTML = `
            <div class="node-health-detail">
                <div class="row">
                    <div class="col-md-6">
                        <p><strong>Overall Status:</strong> <span style="color: ${nodeData.isHealthy ? '#28a745' : '#dc3545'}; font-weight: bold;">${nodeData.status || (nodeData.isHealthy ? 'Healthy' : 'Unhealthy')}</span></p>
                        <p><strong>Last Checked:</strong> ${formatNodeLastChecked(nodeData.lastChecked)}</p>
                    </div>
                    <div class="col-md-6">
                        <p><strong>Status Message:</strong> ${nodeData.statusMessage || 'N/A'}</p>
                        <p><strong>Total Duration:</strong> ${nodeData.totalDuration || 'N/A'}</p>
                    </div>
                </div>
                ${entriesHtml}
            </div>
        `;
        
        detailRow.appendChild(detailCell);
        row.parentNode.insertBefore(detailRow, row.nextSibling);
        expandIcon.className = 'fas fa-chevron-down expand-icon';
        row.classList.add('expanded');
    }
}

function updateRouteSummaryTable(data) {
    const tableBody = document.querySelector('#routeSummaryTable tbody');
    tableBody.innerHTML = ''; // Clear existing rows
    data.forEach(item => {
        const row = tableBody.insertRow();
        row.insertCell().textContent = item.route;
        row.insertCell().classList.add('text-right');
        row.cells[1].textContent = item.minLatencyMs;
        row.insertCell().classList.add('text-right');
        row.cells[2].textContent = item.maxLatencyMs;
        row.insertCell().classList.add('text-right');
        row.cells[3].textContent = item.avgLatencyMs;
        row.insertCell().classList.add('text-right');
        row.cells[4].textContent = item.totalRequests;
    });
}

function updateNodeSummaryTable(data) {
    const tableBody = document.querySelector('#nodeSummaryTable tbody');
    tableBody.innerHTML = ''; // Clear existing rows
    data.forEach(item => {
        const row = tableBody.insertRow();
        row.insertCell().textContent = item.node;
        row.insertCell().classList.add('text-right');
        row.cells[1].textContent = item.minLatencyMs;
        row.insertCell().classList.add('text-right');
        row.cells[2].textContent = item.maxLatencyMs;
        row.insertCell().classList.add('text-right');
        row.cells[3].textContent = item.avgLatencyMs;
        row.insertCell().classList.add('text-right');
        row.cells[4].textContent = item.totalRequests;
    });
}

function updateRecentErrorsTable(data) {
    const tableBody = document.querySelector('#recentErrorsTable tbody');
    tableBody.innerHTML = ''; // Clear existing rows
    data.forEach(item => {
        const row = tableBody.insertRow();
        row.insertCell().textContent = formatUtcDate(item.createdAtUtc);
        row.insertCell().textContent = item.upstreamPath || '';
        row.insertCell().textContent = item.upstreamHost || '';
        row.insertCell().classList.add('text-right');
        row.cells[3].textContent = item.gatewayLatencyMs;
        row.insertCell().classList.add('text-center');
        row.cells[4].textContent = item.downstreamStatusCode || '';
        row.insertCell().textContent = item.errorMessage || '';
        row.insertCell().textContent = item.requestBody || '';
    });
}

// Event listener for time filter change
    document.addEventListener('DOMContentLoaded', () => {
    const timeFilter = document.getElementById('timeFilter');
    const refreshIntervalSelect = document.getElementById('refreshInterval');
    const manualRefreshBtn = document.getElementById('manualRefreshBtn');
    let autoRefreshTimer;

    // Load saved time filter from localStorage
    const savedTimeFilter = localStorage.getItem('dashboardTimeFilter');
    if (savedTimeFilter) {
        timeFilter.value = savedTimeFilter;
    }

    // Load saved refresh interval from localStorage
    const savedRefreshInterval = localStorage.getItem('refreshIntervalSelection');
    if (savedRefreshInterval) {
        refreshIntervalSelect.value = savedRefreshInterval;
    }

    function startAutoRefresh() {
        if (autoRefreshTimer) clearInterval(autoRefreshTimer);

        const interval = refreshIntervalSelect.value;
        let intervalMs = 0;

        switch (interval) {
            case '10s': intervalMs = 10 * 1000; break;
            case '30s': intervalMs = 30 * 1000; break;
            case '1m': intervalMs = 60 * 1000; break;
            case 'off':
            default: intervalMs = 0; break;
        }

        if (intervalMs > 0) {
            autoRefreshTimer = setInterval(fetchDashboardData, intervalMs);
        }
    }

    timeFilter.addEventListener('change', (event) => {
        localStorage.setItem('dashboardTimeFilter', event.target.value);
        fetchDashboardData();
        startAutoRefresh(); // Restart timer with current settings
    });

    refreshIntervalSelect.addEventListener('change', (event) => {
        localStorage.setItem('refreshIntervalSelection', event.target.value);
        startAutoRefresh();
    });

    manualRefreshBtn.addEventListener('click', () => {
        fetchDashboardData();
        startAutoRefresh(); // Restart timer with current settings
    });

    // Node status filter
    const nodeStatusFilter = document.getElementById('nodeStatusFilter');
    if (nodeStatusFilter) {
        nodeStatusFilter.addEventListener('change', () => {
            applyNodeStatusFilter();
        });
    }

    // Initial data fetch and start auto-refresh
    fetchDashboardData();
    startAutoRefresh();
});
