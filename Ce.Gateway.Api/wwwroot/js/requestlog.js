document.addEventListener('DOMContentLoaded', () => {
    const logTableBody = document.getElementById('logTableBody');
    const paginationControls = document.getElementById('paginationControls');
    const totalRequestsElem = document.getElementById('totalRequests');
    const errorRateElem = document.getElementById('errorRate');
    const avgLatencyElem = document.getElementById('avgLatency');

    const filterDownstreamHost = document.getElementById('filterDownstreamHost');
    const filterDownstreamStatusCode = document.getElementById('filterDownstreamStatusCode');
    const filterUpstreamClientIp = document.getElementById('filterUpstreamClientIp');
    const filterFrom = document.getElementById('filterFrom');
    const filterTo = document.getElementById('filterTo');
    const applyFiltersBtn = document.getElementById('applyFilters');
    const clearFiltersBtn = document.getElementById('clearFilters');

    let currentPage = 1;
    const pageSize = 50; // Fixed page size
    let currentFilters = {};
    let autoRefreshIntervalLogs;
    let autoRefreshIntervalHealth;

    async function fetchLogs() {
        const queryParams = new URLSearchParams();
        queryParams.set('page', currentPage);
        queryParams.set('pageSize', pageSize);

        for (const key in currentFilters) {
            if (currentFilters.hasOwnProperty(key)) {
                let value = currentFilters[key];
                if (key === 'from' || key === 'to') {
                    const date = new Date(value);
                    if (!isNaN(date.getTime())) {
                        value = date.toISOString();
                    } else {
                        continue; // Skip invalid dates
                    }
                }
                queryParams.set(key, value);
            }
        }

        try {
            const response = await fetch(`/api/requestlog/logs?${queryParams.toString()}`);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            const data = await response.json();
            renderLogs(data.data);
            renderPagination(data.page, data.totalPages);
            updateSummary(data);
        } catch (error) {
            console.error('Error fetching logs:', error);
            logTableBody.innerHTML = `<tr><td colspan="15" class="text-center text-danger">Error loading logs: ${error.message}</td></tr>`;
            updateSummary({ totalCount: 0, data: [] }); // Pass empty data array to avoid errors
        }
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

    function renderLogs(logs) {
        logTableBody.innerHTML = '';
        if (logs.length === 0) {
            logTableBody.innerHTML = '<tr><td colspan="15" class="text-center">No log entries found.</td></tr>';
            return;
        }

        logs.forEach(log => {
            const row = logTableBody.insertRow();
            row.innerHTML = `
                <td>${formatUtcDate(log.createdAtUtc)}</td>
                <td>${log.traceId || ''}</td>
                <td>${log.upstreamHost || ''}</td>
                <td class="text-right">${log.upstreamPort || ''}</td>
                <td>${log.upstreamHttpMethod || ''}</td>
                <td>${log.upstreamPath || ''}</td>
                <td>${log.downstreamHost || ''}</td>
                <td class="text-right">${log.downstreamPort || ''}</td>
                <td class="text-center"><span class="badge bg-${log.downstreamStatusCode >= 200 && log.downstreamStatusCode < 300 ? 'success' : log.downstreamStatusCode >= 400 && log.downstreamStatusCode < 500 ? 'warning' : 'danger'}">${log.downstreamStatusCode || ''}</span></td>
                <td class="text-right">${log.gatewayLatencyMs || ''}</td>
                <td>${log.upstreamClientIp || ''}</td>
                <td class="text-center">${log.isError ? 'Yes' : 'No'}</td>
                <td>${log.errorMessage || ''}</td>
                <td>${log.requestBody || ''}</td>
            `;
        });
    }

    function renderPagination(page, totalPages) {
        paginationControls.innerHTML = '';
        if (totalPages <= 1) return;

        const createPageItem = (pageNum, text, isDisabled = false, isActive = false) => {
            const li = document.createElement('li');
            li.className = `page-item ${isDisabled ? 'disabled' : ''} ${isActive ? 'active' : ''}`;
            const a = document.createElement('a');
            a.className = 'page-link';
            a.href = '#';
            a.textContent = text;
            a.addEventListener('click', (e) => {
                e.preventDefault();
                if (!isDisabled && !isActive) {
                    currentPage = pageNum;
                    fetchLogs();
                }
            });
            li.appendChild(a);
            return li;
        };

        paginationControls.appendChild(createPageItem(page - 1, 'Previous', page === 1));

        let startPage = Math.max(1, page - 2);
        let endPage = Math.min(totalPages, page + 2);

        if (startPage > 1) {
            paginationControls.appendChild(createPageItem(1, '1'));
            if (startPage > 2) {
                const li = document.createElement('li');
                li.className = 'page-item disabled';
                li.innerHTML = '<span class="page-link">...</span>';
                paginationControls.appendChild(li);
            }
        }

        for (let i = startPage; i <= endPage; i++) {
            paginationControls.appendChild(createPageItem(i, i, false, i === page));
        }

        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                const li = document.createElement('li');
                li.className = 'page-item disabled';
                li.innerHTML = '<span class="page-link">...</span>';
                paginationControls.appendChild(li);
            }
            paginationControls.appendChild(createPageItem(totalPages, totalPages));
        }

        paginationControls.appendChild(createPageItem(page + 1, 'Next', page === totalPages));
    }

    function updateSummary(data) {
        totalRequestsElem.textContent = data.totalCount;

        const errorCount = data.data.filter(log => log.isError).length;
        const errorRate = data.totalCount > 0 ? ((errorCount / data.totalCount) * 100).toFixed(2) : 0;
        errorRateElem.textContent = `${errorRate}%`;

        const totalLatency = data.data.reduce((sum, log) => sum + log.gatewayLatencyMs, 0);
        const avgLatency = data.data.length > 0 ? totalLatency / data.data.length : 0;
        avgLatencyElem.textContent = Math.round(avgLatency);
    }

    function applyCurrentFilters() {
        const filters = {
            downstreamHost: filterDownstreamHost.value.trim(),
            downstreamStatusCode: filterDownstreamStatusCode.value.trim(),
            upstreamClientIp: filterUpstreamClientIp.value.trim(),
            from: filterFrom.value,
            to: filterTo.value,
        };

        // Only include filters that have a non-empty value
        currentFilters = Object.fromEntries(Object.entries(filters).filter(([_, v]) => v !== '' && v !== undefined));

        currentPage = 1; // Reset to first page on filter change
        fetchLogs();
    }

    function clearAllFilters() {
        filterDownstreamHost.value = '';
        filterDownstreamStatusCode.value = '';
        filterUpstreamClientIp.value = '';
        filterFrom.value = '';
        filterTo.value = '';
        applyCurrentFilters(); // Apply empty filters
    }

    applyFiltersBtn.addEventListener('click', applyCurrentFilters);
    clearFiltersBtn.addEventListener('click', clearAllFilters);

    // Auto-refresh logs every 10 seconds
    function startAutoRefreshLogs() {
        if (autoRefreshIntervalLogs) clearInterval(autoRefreshIntervalLogs);
        autoRefreshIntervalLogs = setInterval(fetchLogs, 10000);
    }

    // Initial fetches and start auto-refresh
    fetchLogs();
    startAutoRefreshLogs();
});
