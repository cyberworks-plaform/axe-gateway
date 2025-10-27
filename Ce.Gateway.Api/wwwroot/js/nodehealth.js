document.addEventListener('DOMContentLoaded', () => {
    const nodeHealthTableBody = document.getElementById('nodeHealthTableBody');
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
        return date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: true });
    }

    async function fetchNodeHealth() {
        startCountdown(); // Reset and start countdown on each fetch
        try {
            const response = await fetch('/api/nodehealth');
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
            mainRow.setAttribute('data-bs-toggle', 'collapse');
            mainRow.setAttribute('data-bs-target', `#collapse-${i}`);
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

            // Event listener for expand/collapse icon and row highlighting
            mainRow.addEventListener('click', function() {
                const isExpanded = this.getAttribute('aria-expanded') === 'true';
                const icon = this.querySelector('.expand-collapse-icon');
                if (isExpanded) {
                    icon.classList.remove('fa-minus-circle');
                    icon.classList.add('fa-plus-circle');
                    this.classList.remove('expanded');
                } else {
                    icon.classList.remove('fa-plus-circle');
                    icon.classList.add('fa-minus-circle');
                    this.classList.add('expanded');
                }
            });

            // Collapsible detail row
            const detailRow = nodeHealthTableBody.insertRow();
            detailRow.className = 'detail-row collapse';
            detailRow.id = `collapse-${i}`;
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
    fetchNodeHealth();

    // Set up polling
    setInterval(fetchNodeHealth, evaluationTimeInSeconds * 1000);
});