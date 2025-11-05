let requestReportChart = null;

function initChart() {
    const ctx = document.getElementById('requestReportChart');
    if (!ctx) return;

    requestReportChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: [],
            datasets: [
                {
                    label: 'Success (2xx)',
                    data: [],
                    backgroundColor: '#28a745',
                    borderColor: '#28a745',
                    borderWidth: 1
                },
                {
                    label: 'Client Error (4xx)',
                    data: [],
                    backgroundColor: '#ffc107',
                    borderColor: '#ffc107',
                    borderWidth: 1
                },
                {
                    label: 'Server Error (5xx)',
                    data: [],
                    backgroundColor: '#dc3545',
                    borderColor: '#dc3545',
                    borderWidth: 1
                },
                {
                    label: 'Other',
                    data: [],
                    backgroundColor: '#6c757d',
                    borderColor: '#6c757d',
                    borderWidth: 1
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: {
                    stacked: true,
                    grid: {
                        display: false
                    }
                },
                y: {
                    stacked: true,
                    beginAtZero: true,
                    ticks: {
                        stepSize: 1
                    }
                }
            },
            plugins: {
                legend: {
                    display: true,
                    position: 'top'
                },
                tooltip: {
                    mode: 'index',
                    intersect: false,
                    callbacks: {
                        footer: function(tooltipItems) {
                            let total = 0;
                            tooltipItems.forEach(function(tooltipItem) {
                                total += tooltipItem.parsed.y;
                            });
                            return 'Total: ' + total;
                        }
                    }
                }
            }
        }
    });
}

// Convert label to UTC+7 display format
function convertLabelToUTC7(label, timeFormat) {
    // For hour format (HH:00), add 7 hours
    if (timeFormat === 'HH:00') {
        const match = label.match(/(\d+):00/);
        if (match) {
            let hour = parseInt(match[1]);
            hour = (hour + 7) % 24;
            return hour.toString().padStart(2, '0') + ':00';
        }
    }
    // For month format (MMM yyyy) and day format (MM/dd), no conversion needed
    // as they represent date boundaries, not specific times
    return label;
}

async function loadReportData() {
    const reportGeneratedInfo = $('#generated-time-info');
    const period = document.getElementById('periodFilter').value;
    const loadingOverlay = document.getElementById('loadingOverlay');
    const startTime = performance.now(); // Record start time
    const startGeneratedDateTime = new Date();
    try {
        reportGeneratedInfo.text('Generating...');
        document.getElementById('totalRequests').textContent = '...';
        document.getElementById('successRequests').textContent = '...';
        document.getElementById('clientErrorRequests').textContent = '...';
        document.getElementById('serverErrorRequests').textContent = '...';

        loadingOverlay.style.display = 'flex';
        
        const response = await fetch(`/api/requestreport/data?period=${period}`);
        if (!response.ok) {
            throw new Error('Failed to load report data');
        }
        
        const data = await response.json();
        
        // Update summary cards
        document.getElementById('totalRequests').textContent = data.totalRequests.toLocaleString();
        document.getElementById('successRequests').textContent = data.successRequests.toLocaleString();
        document.getElementById('clientErrorRequests').textContent = data.clientErrorRequests.toLocaleString();
        document.getElementById('serverErrorRequests').textContent = data.serverErrorRequests.toLocaleString();
        
        // Update chart with UTC+7 labels
        if (requestReportChart) {
            // Convert labels to UTC+7 if needed
            const labels = data.timeSlots.map(slot => 
                convertLabelToUTC7(slot.label, data.timeFormat)
            );
            const successData = data.timeSlots.map(slot => slot.successCount);
            const clientErrorData = data.timeSlots.map(slot => slot.clientErrorCount);
            const serverErrorData = data.timeSlots.map(slot => slot.serverErrorCount);
            const otherData = data.timeSlots.map(slot => slot.otherCount);
            
            requestReportChart.data.labels = labels;
            requestReportChart.data.datasets[0].data = successData;
            requestReportChart.data.datasets[1].data = clientErrorData;
            requestReportChart.data.datasets[2].data = serverErrorData;
            requestReportChart.data.datasets[3].data = otherData;
            requestReportChart.update();
        }
        
    } catch (error) {
        console.error('Error loading report data:', error);
        alert('Failed to load report data. Please try again.');
    } finally {
        loadingOverlay.style.display = 'none';

        const endTime = performance.now(); // Record start time
        var totalTimeInMs = endTime - startTime;
        var generateTimeText = "";
        if (totalTimeInMs < 1000) {
            generateTimeText = " (Generated in " + totalTimeInMs.toFixed(0) + " ms)"
        }
        else {
            generateTimeText = " (Generated in " + (totalTimeInMs/1000).toFixed(0) + " s)"
        }
        reportGeneratedInfo.text("Report at " + startGeneratedDateTime.toISOString() + generateTimeText)
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    initChart();
    loadReportData();
    
    // Add event listener for period filter
    document.getElementById('periodFilter').addEventListener('change', loadReportData);
});
