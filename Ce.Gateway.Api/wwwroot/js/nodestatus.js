document.addEventListener('DOMContentLoaded', () => {
    const nodeStatusTableBody = document.getElementById('nodeHealthTableBody');
    const refreshIntervalDisplay = document.getElementById('refreshIntervalDisplay');
    const nextCheckDisplay = document.getElementById('nextCheckDisplay');
    const evaluationTimeInSeconds = parseInt(document.getElementById('evaluationTimeInSeconds').value || '60');

    refreshIntervalDisplay.textContent = `Refresh every ${evaluationTimeInSeconds} seconds`;

    let countdownInterval;
    let remainingTime = evaluationTimeInSeconds;

    function startCountdown() {
        clearInterval(countdownInterval);
        remainingTime = evaluationTimeInSeconds;
        updateCountdownDisplay();

        countdownInterval = setInterval(() => {
            remainingTime--;
            updateCountdownDisplay();
            if (remainingTime <= 0) {
                clearInterval(countdownInterval);
            }
        }, 1000);
    }

    function updateCountdownDisplay() {
        nextCheckDisplay.textContent = `Next check in: ${remainingTime}s`;
    }

    function getHealthIconHtml(status) {
        switch (status.toLowerCase()) {
            case 'healthy': return '<i class="fas fa-check-circle fa-fw status-icon-fa text-success"></i>';
            case 'unhealthy': return '<i class="fas fa-exclamation-circle fa-fw status-icon-fa text-danger"></i>'; // Changed to exclamation for Unhealthy
            case 'degraded': return '<i class="fas fa-times-circle fa-fw status-icon-fa text-warning"></i>'; // Changed to times for Degraded
            default: return '';
        }
    }

    function getHealthBadgeClass(status) {
        switch (status.toLowerCase()) {
            case 'healthy': return 'status-badge healthy';
            case 'unhealthy': return 'status-badge unhealthy';
            case 'degraded': return 'status-badge degraded';
            default: return '';
        }
    }

    function formatTime(dateTimeString) {
        if (!dateTimeString) return '-';
        const date = new Date(dateTimeString);
        
        // Get UTC time components and add 7 hours for UTC+7
        let utcHours = date.getUTCHours() + 7;
        const utcMinutes = String(date.getUTCMinutes()).padStart(2, '0');
        const utcSeconds = String(date.getUTCSeconds()).padStart(2, '0');
        
        // Handle hour overflow (if >= 24, wrap around)
        if (utcHours >= 24) {
            utcHours -= 24;
        }
        const formattedHours = String(utcHours).padStart(2, '0');
        
        return `${formattedHours}:${utcMinutes}:${utcSeconds}`;
    }

    async function fetchNodeStatus() {
        startCountdown(); // Reset and start countdown on each fetch
        try {
            const response = await fetch('/api/nodestatus');
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            const healthData = await response.json();
            renderNodeHealthTable(healthData);
        } catch (error) {
            console.error('Error fetching node health:', error);
            nodeHealthTableBody.innerHTML = `<tr><td colspan="6" class="text-center text-danger">Error loading node health data: ${error.message}</td></tr>`; // Colspan updated to 6
        }
    }

    function renderNodeHealthTable(healthData) {
        nodeHealthTableBody.innerHTML = '';
        if (healthData.length === 0) {
            nodeHealthTableBody.innerHTML = '<tr><td colspan="6" class="text-center text-info">No downstream services configured for health monitoring.</td></tr>'; // Colspan updated to 6
            return;
        }

        healthData.forEach((service, i) => {
            const mainRow = nodeHealthTableBody.insertRow();
            mainRow.className = 'main-row';
            mainRow.setAttribute('data-toggle', 'collapse');
            mainRow.setAttribute('data-target', `#collapse-${i}`);
            mainRow.setAttribute('aria-expanded', 'false');
            mainRow.setAttribute('aria-controls', `collapse-${i}`);

            const healthIcon = getHealthIconHtml(service.status);
            const healthBadgeClass = getHealthBadgeClass(service.status);

            mainRow.innerHTML = `
                <td><i class="fas fa-plus-circle fa-fw expand-collapse-icon"></i></td>
                <td>${service.host}:${service.port} (${service.scheme.toUpperCase()})</td>
                <td>
                    <span class="${healthBadgeClass}">
                        ${healthIcon}
                        <span class="status-text">${service.status}</span>
                    </span>
                </td>
                <td>${service.statusMessage || 'N/A'}</td>
                <td>${formatTime(service.lastChecked)}</td>
                <td>${service.totalDuration || '-'}</td>
            `;

            // The expand/collapse functionality is handled by Bootstrap's collapse.js
            // The icon and row highlighting are updated by Bootstrap's events or CSS.
            // No custom click listener is needed here.

            // Collapsible detail row
            const detailRow = nodeStatusTableBody.insertRow();
            detailRow.className = 'detail-row collapse';
            detailRow.id = `collapse-${i}`;

            // Add event listeners for Bootstrap collapse events to change the icon
            $(detailRow).on('shown.bs.collapse', function () {
                $(mainRow).find('.expand-collapse-icon').removeClass('fa-plus-circle').addClass('fa-minus-circle');
            });

            $(detailRow).on('hidden.bs.collapse', function () {
                $(mainRow).find('.expand-collapse-icon').removeClass('fa-minus-circle').addClass('fa-plus-circle');
            });
            detailRow.innerHTML = `
                <td colspan="6">
                    <div class="detail-content">
                        ${service.entries && Object.keys(service.entries).length > 0 ? `
                            <table class="table table-sm sub-table table-bordered">
                                <thead>
                                    <tr>
                                        <th style="width: 25%;">NAME</th>
                                        <th style="width: 15%;">STATUS</th>
                                        <th style="width: 30%;">DESCRIPTION</th>
                                        <th style="width: 15%;">DURATION</th>
                                        <th style="width: 15%;">TAGS</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${Object.entries(service.entries).map(([key, entry]) => {
                                        const entryHealthIcon = getHealthIconHtml(entry.status);
                                        const entryHealthBadgeClass = getHealthBadgeClass(entry.status);
                                        return `
                                            <tr>
                                                <td>${key}</td>
                                                <td>
                                                    <span class="${entryHealthBadgeClass}">
                                                        ${entryHealthIcon}
                                                        <span class="status-text">${entry.status}</span>
                                                    </span>
                                                </td>
                                                <td>${entry.description || 'N/A'}</td>
                                                <td>${entry.duration || '-'}</td>
                                                <td>${entry.tags && entry.tags.length > 0 ? entry.tags.join(', ') : 'N/A'}</td>
                                            </tr>
                                        `;
                                    }).join('')}
                                </tbody>
                            </table>
                        ` : '<p class="text-muted">No detailed health check entries.</p>'}
                    </div>
                </td>
            `;
        });
    }

    // Initial fetch
    fetchNodeStatus();

    // Set up polling
    setInterval(fetchNodeStatus, evaluationTimeInSeconds * 1000);
});

