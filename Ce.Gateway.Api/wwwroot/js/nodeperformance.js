document.addEventListener('DOMContentLoaded', () => {
    const timeFilter = document.getElementById('timeFilter');
    const nodePerformanceSummaryTableBody = document.getElementById('nodePerformanceSummaryTableBody');
    const topSlowestErrorsTableBody = document.getElementById('topSlowestErrorsTableBody');

    let requestsPerNodeChart, avgLatencyPerNodeChart, errorRatePerNodeChart;

    // Initialize Chart.js defaults
    Chart.defaults.font.family = 'Inter';
    Chart.defaults.font.size = 12;
    Chart.defaults.color = '#666';

    // Function to get time range from filter
    function getTimeRange() {
        const selectedValue = timeFilter.value;
        let from = null;
        let to = new Date();

        switch (selectedValue) {
            case '5m': from = new Date(to.getTime() - 5 * 60 * 1000); break;
            case '15m': from = new Date(to.getTime() - 15 * 60 * 1000); break;
            case '30m': from = new Date(to.getTime() - 30 * 60 * 1000); break;
            case '1h': from = new Date(to.getTime() - 1 * 60 * 60 * 1000); break;
            case '3h': from = new Date(to.getTime() - 3 * 60 * 60 * 1000); break;
            case '6h': from = new Date(to.getTime() - 6 * 60 * 60 * 1000); break;
            case '12h': from = new Date(to.getTime() - 12 * 60 * 60 * 1000); break;
            case '24h': from = new Date(to.getTime() - 24 * 60 * 60 * 1000); break;
            case '7d': from = new Date(to.getTime() - 7 * 24 * 60 * 60 * 1000); break;
            case '30d': from = new Date(to.getTime() - 30 * 24 * 60 * 60 * 1000); break;
            case '60d': from = new Date(to.getTime() - 60 * 24 * 60 * 60 * 1000); break;
            case '90d': from = new Date(to.getTime() - 90 * 24 * 60 * 60 * 1000); break;
            default: from = new Date(to.getTime() - 1 * 60 * 60 * 1000); break; // Default to 1 hour
        }
        return { from: from.toISOString(), to: to.toISOString() };
    }

    async function fetchDataAndRender() {
        const { from, to } = getTimeRange();
        const queryParams = new URLSearchParams({ from, to });

        try {
            // Fetch Summary
            const summaryResponse = await fetch(`/api/nodeperformance/summary?${queryParams.toString()}`);
            const summaryData = await summaryResponse.json();
            renderNodePerformanceSummaryTable(summaryData);

            // Fetch Requests Per Node
            const requestsPerNodeResponse = await fetch(`/api/nodeperformance/requestspernode?${queryParams.toString()}`);
            const requestsPerNodeData = await requestsPerNodeResponse.json();
            renderRequestsPerNodeChart(requestsPerNodeData);

            // Fetch Average Latency Per Node
            const avgLatencyPerNodeResponse = await fetch(`/api/nodeperformance/averagelatencypernode?${queryParams.toString()}`);
            const avgLatencyPerNodeData = await avgLatencyPerNodeResponse.json();
            renderAverageLatencyPerNodeChart(avgLatencyPerNodeData);

            // Fetch Error Rate Per Node
            const errorRatePerNodeResponse = await fetch(`/api/nodeperformance/errorratepernode?${queryParams.toString()}`);
            const errorRatePerNodeData = await errorRatePerNodeResponse.json();
            renderErrorRatePerNodeChart(errorRatePerNodeData);

            // Fetch Top N Slowest Error Requests
            const topSlowestErrorsResponse = await fetch(`/api/nodeperformance/topslownessrequests?${queryParams.toString()}&n=10`);
            const topSlowestErrorsData = await topSlowestErrorsResponse.json();
            renderTopSlowestErrorsTable(topSlowestErrorsData);

        } catch (error) {
            console.error('Error fetching node performance data:', error);
            // Optionally display error messages on the UI
        }
    }

    function renderNodePerformanceSummaryTable(summaryData) {
        nodePerformanceSummaryTableBody.innerHTML = '';
        if (summaryData.length === 0) {
            nodePerformanceSummaryTableBody.innerHTML = '<tr><td colspan="8" class="text-center">No data available for the selected period.</td></tr>';
            return;
        }

        summaryData.forEach(node => {
            const row = nodePerformanceSummaryTableBody.insertRow();
            row.innerHTML = `
                <td>${node.nodeIdentifier}</td>
                <td class="text-right">${node.totalRequests}</td>
                <td class="text-right">${node.successfulRequests}</td>
                <td class="text-right">${node.errorRequests}</td>
                <td class="text-right">${node.errorRate.toFixed(2)}%</td>
                <td class="text-right">${node.minLatencyMs.toFixed(2)}</td>
                <td class="text-right">${node.maxLatencyMs.toFixed(2)}</td>
                <td class="text-right">${node.avgLatencyMs.toFixed(2)}</td>
            `;
        });
    }

    function renderTopSlowestErrorsTable(errorData) {
        topSlowestErrorsTableBody.innerHTML = '';
        if (errorData.length === 0) {
            topSlowestErrorsTableBody.innerHTML = '<tr><td colspan="7" class="text-center">No slowest error requests found for the selected period.</td></tr>';
            return;
        }

        errorData.forEach(error => {
            const row = topSlowestErrorsTableBody.insertRow();
            row.innerHTML = `
                <td>${formatUtcDate(error.createdAtUtc)}</td>
                <td>${error.route}</td>
                <td>${error.node}</td>
                <td class="text-right">${error.gatewayLatencyMs}</td>
                <td class="text-center">${error.statusCode || 'N/A'}</td>
                <td>${error.errorMessage || 'N/A'}</td>
                <td>${error.requestBody || 'N/A'}</td>
            `;
        });
    }

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

    function createBarChart(chartId, data, title, yAxisLabel) {
        const ctx = document.getElementById(chartId).getContext('2d');
        const labels = data.map(d => d.label);
        const values = data.map(d => d.value);

        return new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: yAxisLabel,
                    data: values,
                    backgroundColor: 'rgba(60,141,188,0.8)',
                    borderColor: 'rgba(60,141,188,0.8)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: title
                    },
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: yAxisLabel
                        }
                    },
                    x: {
                        title: {
                            display: true,
                            text: 'Node'
                        }
                    }
                }
            }
        });
    }

    function renderRequestsPerNodeChart(data) {
        if (requestsPerNodeChart) requestsPerNodeChart.destroy();
        requestsPerNodeChart = createBarChart('requestsPerNodeChart', data, 'Requests Per Node', 'Total Requests');
    }

    function renderAverageLatencyPerNodeChart(data) {
        if (avgLatencyPerNodeChart) avgLatencyPerNodeChart.destroy();
        avgLatencyPerNodeChart = createBarChart('avgLatencyPerNodeChart', data, 'Average Latency Per Node', 'Latency (ms)');
    }

    function renderErrorRatePerNodeChart(data) {
        if (errorRatePerNodeChart) errorRatePerNodeChart.destroy();
        errorRatePerNodeChart = createBarChart('errorRatePerNodeChart', data, 'Error Rate Per Node', 'Error Rate (%)');
    }

    // Load saved time filter from localStorage
    const savedTimeFilter = localStorage.getItem('dashboardTimeFilter');
    if (savedTimeFilter) {
        timeFilter.value = savedTimeFilter;
    }

    // Event listener for time filter change
    timeFilter.addEventListener('change', (event) => {
        // Save selected time filter to localStorage
        localStorage.setItem('dashboardTimeFilter', event.target.value);
        fetchDataAndRender();
    });

    // Initial data fetch
    fetchDataAndRender();

    // Auto-refresh every 10 seconds
    setInterval(fetchDataAndRender, 10000);
});
