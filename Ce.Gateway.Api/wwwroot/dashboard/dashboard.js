let requestTimelineChart;
let latencyTimelineChart;
let httpStatusDonutChart;

// Register Chart.js Datalabels Plugin
Chart.register(ChartDataLabels);

const API_BASE_URL = '/api/Dashboard';
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

// Fetch and update all dashboard data
async function fetchDashboardData() {
    const { startTime, endTime } = getTimeRange();

    try {
        const [overview, routeSummary, nodeSummary, recentErrors] = await Promise.all([
            fetch(`${API_BASE_URL}/overview?startTime=${startTime}&endTime=${endTime}`).then(res => res.json()),
            fetch(`${API_BASE_URL}/routesummary?startTime=${startTime}&endTime=${endTime}`).then(res => res.json()),
            fetch(`${API_BASE_URL}/nodesummary?startTime=${startTime}&endTime=${endTime}`).then(res => res.json()),
            fetch(`${API_BASE_URL}/recenterrors?startTime=${startTime}&endTime=${endTime}`).then(res => res.json())
        ]);

        updateOverview(overview);
        renderRequestTimelineChart(overview.requestTimeline);
        renderLatencyTimelineChart(overview.latencyTimeline);
        renderHttpStatusDonutChart(overview.httpStatusDistribution);
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
    // Load saved time filter from localStorage
    const savedTimeFilter = localStorage.getItem('dashboardTimeFilter');
    if (savedTimeFilter) {
        document.getElementById('timeFilter').value = savedTimeFilter;
    }

    document.getElementById('timeFilter').addEventListener('change', (event) => {
        // Save selected time filter to localStorage
        localStorage.setItem('dashboardTimeFilter', event.target.value);
        fetchDashboardData();
    });

    // Initial data fetch
    fetchDashboardData();

    // Set up auto-reload
    setInterval(fetchDashboardData, RELOAD_INTERVAL_MS);
});
