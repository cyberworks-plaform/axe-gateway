// Route Configuration Management - Upload Mode
// Simplified version focusing on view and upload functionality

$(document).ready(function () {
    let allRoutes = [];
    let filteredRoutes = [];
    let currentVersion = null;
    let uploadFileContent = null;

    // Load routes on page load
    loadRoutes();

    // Event handlers
    $('#searchFilter').on('keyup', filterRoutes);
    $('#schemeFilter').on('change', filterRoutes);
    $('#btnUploadConfig').on('click', openUploadModal);
    $('#configFileInput').on('change', handleFileSelect);
    $('#btnAnalyzeConfig').on('click', analyzeConfiguration);
    $('#btnUploadConfig').on('click', uploadConfiguration);
    $('#uploadVersion').on('input', function() {
        // Reset analysis when version changes
        $('#versionComparisonResult').hide();
        $('#riskWarningsSection').hide();
        $('#btnUploadConfig').hide();
    });

    // Custom file input label update
    $('#configFileInput').on('change', function() {
        const fileName = $(this).val().split('\\').pop();
        $(this).next('.custom-file-label').html(fileName || 'Choose file...');
    });

    /**
     * Load all routes from API
     */
    function loadRoutes() {
        console.log('[LOG] Loading routes from /api/routes');
        
        $('#routesContainer').html('<div class="text-center"><i class="fas fa-spinner fa-spin fa-3x"></i><p class="mt-2">Loading routes...</p></div>');

        $.ajax({
            url: '/api/routes',
            method: 'GET',
            success: function (response) {
                console.log('[LOG] Routes API response:', response);
                if (response.success && response.data) {
                    allRoutes = response.data;
                    filteredRoutes = allRoutes;
                    renderRoutes();
                } else {
                    showError('Failed to load routes: ' + (response.message || 'Unknown error'));
                }
            },
            error: function (xhr, status, error) {
                console.error('[ERROR] Failed to load routes:', error);
                console.error('[ERROR] Response:', xhr.responseText);
                showError('Error loading routes: ' + error);
            }
        });
    }

    /**
     * Render routes to the page
     */
    function renderRoutes() {
        if (filteredRoutes.length === 0) {
            $('#routesContainer').html('<div class="alert alert-info"><i class="fas fa-info-circle"></i> No routes found matching your criteria.</div>');
            return;
        }

        let html = '';
        filteredRoutes.forEach(route => {
            html += generateRouteCard(route);
        });

        $('#routesContainer').html(html);

        // Attach event handlers using event delegation
        $('#routesContainer').off('click').on('click', '[data-action="view-route"]', function() {
            const routeId = $(this).data('route-id');
            viewRouteDetails(routeId);
        });
    }

    /**
     * Generate HTML for a route card
     */
    function generateRouteCard(route) {
        const nodesHtml = route.downstreamHostAndPorts.map(node => 
            `<span class="badge badge-info mr-1">${escapeHtml(node.host)}:${node.port}</span>`
        ).join('');

        const loadBalancer = route.loadBalancerOptions ? 
            `<span class="badge badge-secondary">${escapeHtml(route.loadBalancerOptions.type)}</span>` : 
            '<span class="badge badge-secondary">None</span>';

        const qos = route.qoSOptions ? 
            `<span class="badge badge-warning">Timeout: ${route.qoSOptions.timeoutValue}ms, Max Errors: ${route.qoSOptions.exceptionsAllowedBeforeBreaking}</span>` : 
            '<span class="badge badge-secondary">None</span>';

        return `
            <div class="card mb-3">
                <div class="card-header bg-light">
                    <div class="d-flex justify-content-between align-items-center flex-wrap">
                        <h5 class="mb-0">
                            <i class="fas fa-route text-primary"></i> ${escapeHtml(route.upstreamPathTemplate)}
                        </h5>
                        <button class="btn btn-sm btn-info" data-action="view-route" data-route-id="${escapeHtml(route.routeId)}">
                            <i class="fas fa-eye"></i> View Details
                        </button>
                    </div>
                </div>
                <div class="card-body">
                    <div class="row">
                        <div class="col-md-6 mb-2">
                            <strong>Downstream:</strong> ${escapeHtml(route.downstreamScheme)}://${escapeHtml(route.downstreamPathTemplate)}
                        </div>
                        <div class="col-md-6 mb-2">
                            <strong>HTTP Methods:</strong> ${route.upstreamHttpMethod.map(m => `<span class="badge badge-primary mr-1">${escapeHtml(m)}</span>`).join('')}
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-4 mb-2">
                            <strong>Load Balancer:</strong> ${loadBalancer}
                        </div>
                        <div class="col-md-4 mb-2">
                            <strong>QoS:</strong> ${qos}
                        </div>
                        <div class="col-md-4 mb-2">
                            <strong>Nodes (${route.downstreamHostAndPorts.length}):</strong> ${nodesHtml || '<span class="text-muted">None</span>'}
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * View route details in modal
     */
    function viewRouteDetails(routeId) {
        const route = allRoutes.find(r => r.routeId === routeId);
        if (!route) {
            showError('Route not found');
            return;
        }

        let html = `
            <h6><i class="fas fa-info-circle"></i> Route Information</h6>
            <table class="table table-bordered table-sm">
                <tr><th width="30%">Upstream Path</th><td>${escapeHtml(route.upstreamPathTemplate)}</td></tr>
                <tr><th>Downstream Scheme</th><td>${escapeHtml(route.downstreamScheme)}</td></tr>
                <tr><th>Downstream Path</th><td>${escapeHtml(route.downstreamPathTemplate)}</td></tr>
                <tr><th>HTTP Methods</th><td>${route.upstreamHttpMethod.map(m => escapeHtml(m)).join(', ')}</td></tr>
                <tr><th>Priority</th><td>${route.priority || 'None'}</td></tr>
                <tr><th>Accept Any Cert</th><td>${route.dangerousAcceptAnyServerCertificateValidator ? 'Yes' : 'No'}</td></tr>
            </table>

            <h6 class="mt-3"><i class="fas fa-balance-scale"></i> Load Balancer</h6>
            <table class="table table-bordered table-sm">
                ${route.loadBalancerOptions ? `
                    <tr><th width="30%">Type</th><td>${escapeHtml(route.loadBalancerOptions.type)}</td></tr>
                    <tr><th>Key</th><td>${escapeHtml(route.loadBalancerOptions.key || 'None')}</td></tr>
                ` : '<tr><td colspan="2" class="text-muted">No load balancer configured</td></tr>'}
            </table>

            <h6 class="mt-3"><i class="fas fa-shield-alt"></i> Quality of Service (QoS)</h6>
            <table class="table table-bordered table-sm">
                ${route.qoSOptions ? `
                    <tr><th width="30%">Timeout</th><td>${route.qoSOptions.timeoutValue}ms</td></tr>
                    <tr><th>Max Errors</th><td>${route.qoSOptions.exceptionsAllowedBeforeBreaking}</td></tr>
                    <tr><th>Break Duration</th><td>${route.qoSOptions.durationOfBreak}ms</td></tr>
                ` : '<tr><td colspan="2" class="text-muted">No QoS configured</td></tr>'}
            </table>

            <h6 class="mt-3"><i class="fas fa-server"></i> Downstream Nodes</h6>
            <table class="table table-bordered table-sm">
                <thead><tr><th>Host</th><th>Port</th></tr></thead>
                <tbody>
                    ${route.downstreamHostAndPorts.length > 0 ? 
                        route.downstreamHostAndPorts.map(node => 
                            `<tr><td>${escapeHtml(node.host)}</td><td>${node.port}</td></tr>`
                        ).join('') :
                        '<tr><td colspan="2" class="text-muted">No nodes configured</td></tr>'
                    }
                </tbody>
            </table>
        `;

        $('#viewRouteContent').html(html);
        $('#viewRouteModal').modal('show');
    }

    /**
     * Filter routes based on search and scheme
     */
    function filterRoutes() {
        const searchText = $('#searchFilter').val().toLowerCase();
        const schemeFilter = $('#schemeFilter').val();

        filteredRoutes = allRoutes.filter(route => {
            const matchesSearch = !searchText || 
                route.upstreamPathTemplate.toLowerCase().includes(searchText) ||
                route.downstreamPathTemplate.toLowerCase().includes(searchText);

            const matchesScheme = !schemeFilter || route.downstreamScheme === schemeFilter;

            return matchesSearch && matchesScheme;
        });

        renderRoutes();
    }

    /**
     * Open upload modal and load current version
     */
    function openUploadModal() {
        // Reset form
        $('#configFileInput').val('');
        $('.custom-file-label').html('Choose file...');
        $('#uploadVersion').val('');
        $('#uploadDescription').val('');
        $('#confirmRisks').prop('checked', false);
        $('#versionComparisonResult').hide();
        $('#riskWarningsSection').hide();
        $('#btnAnalyzeConfig').prop('disabled', true);
        $('#btnUploadConfig').hide();
        uploadFileContent = null;

        // Load current version
        $.ajax({
            url: '/api/routes/version',
            method: 'GET',
            success: function (response) {
                if (response.success && response.data) {
                    currentVersion = response.data;
                    $('#currentVersionDisplay').text('v' + currentVersion.version);
                    $('#currentGitHashDisplay').text(currentVersion.gitHash || 'N/A');
                } else {
                    $('#currentVersionDisplay').text('Unknown');
                    $('#currentGitHashDisplay').text('N/A');
                }
            },
            error: function () {
                $('#currentVersionDisplay').text('Error loading version');
                $('#currentGitHashDisplay').text('N/A');
            }
        });

        $('#uploadConfigModal').modal('show');
    }

    /**
     * Handle file selection
     */
    function handleFileSelect(event) {
        const file = event.target.files[0];
        if (!file) {
            uploadFileContent = null;
            $('#btnAnalyzeConfig').prop('disabled', true);
            return;
        }

        const reader = new FileReader();
        reader.onload = function(e) {
            try {
                const content = e.target.result;
                JSON.parse(content); // Validate JSON
                uploadFileContent = content;
                $('#btnAnalyzeConfig').prop('disabled', false);
            } catch (error) {
                showError('Invalid JSON file: ' + error.message);
                uploadFileContent = null;
                $('#btnAnalyzeConfig').prop('disabled', true);
            }
        };
        reader.readAsText(file);
    }

    /**
     * Analyze configuration and compare versions
     */
    function analyzeConfiguration() {
        if (!uploadFileContent) {
            showError('Please select a configuration file first');
            return;
        }

        const uploadVersion = $('#uploadVersion').val().trim() || null;

        $.ajax({
            url: '/api/routes/version/compare',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(uploadVersion),
            success: function (response) {
                if (response.success && response.data) {
                    displayVersionComparison(response.data);
                } else {
                    showError('Failed to compare versions: ' + (response.message || 'Unknown error'));
                }
            },
            error: function (xhr, status, error) {
                showError('Error comparing versions: ' + error);
            }
        });
    }

    /**
     * Display version comparison results
     */
    function displayVersionComparison(comparison) {
        let alertClass = 'alert-info';
        let icon = 'fa-info-circle';

        if (comparison.isDowngrade) {
            alertClass = 'alert-danger';
            icon = 'fa-exclamation-triangle';
        } else if (comparison.isUpgrade) {
            alertClass = 'alert-success';
            icon = 'fa-arrow-up';
        }

        $('#versionComparisonResult')
            .removeClass('alert-info alert-warning alert-danger alert-success')
            .addClass(alertClass)
            .html(`
                <h6><i class="fas ${icon}"></i> Version Comparison</h6>
                <p class="mb-0"><strong>${escapeHtml(comparison.message)}</strong></p>
                <p class="mb-0 mt-2">
                    <strong>Current:</strong> v${escapeHtml(comparison.currentVersion.version)}<br>
                    <strong>Upload:</strong> v${escapeHtml(comparison.uploadVersion.version)}
                </p>
            `)
            .show();

        // Show warnings
        const warningsHtml = comparison.warnings.map(w => `<li>${escapeHtml(w)}</li>`).join('');
        $('#riskWarningsList').html(warningsHtml);
        $('#riskWarningsSection').show();
        
        // Reset confirmation
        $('#confirmRisks').prop('checked', false);
    }

    /**
     * Upload configuration
     */
    function uploadConfiguration() {
        if (!uploadFileContent) {
            showError('Please select a configuration file first');
            return;
        }

        if (!$('#confirmRisks').is(':checked')) {
            showError('Please confirm that you understand the risks');
            return;
        }

        const description = $('#uploadDescription').val().trim();
        if (!description) {
            showError('Please provide a description');
            return;
        }

        const request = {
            configurationContent: uploadFileContent,
            version: $('#uploadVersion').val().trim() || null,
            description: description,
            confirmRisks: true
        };

        $('#btnUploadConfig').prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Uploading...');

        $.ajax({
            url: '/api/routes/upload',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(request),
            success: function (response) {
                if (response.success) {
                    showSuccess('Configuration uploaded successfully! Reloading routes...');
                    $('#uploadConfigModal').modal('hide');
                    setTimeout(loadRoutes, 1000);
                } else {
                    showError('Upload failed: ' + (response.message || 'Unknown error'));
                }
            },
            error: function (xhr, status, error) {
                const errorMsg = xhr.responseJSON && xhr.responseJSON.message ? 
                    xhr.responseJSON.message : error;
                showError('Error uploading configuration: ' + errorMsg);
            },
            complete: function() {
                $('#btnUploadConfig').prop('disabled', false).html('<i class="fas fa-upload"></i> Upload & Apply');
            }
        });
    }

    /**
     * Show success alert
     */
    function showSuccess(message) {
        const alert = $(`
            <div class="alert alert-success alert-dismissible fade show" role="alert">
                <i class="fas fa-check-circle"></i> ${escapeHtml(message)}
                <button type="button" class="close" data-dismiss="alert">
                    <span>&times;</span>
                </button>
            </div>
        `);
        $('#routesContainer').prepend(alert);
        setTimeout(() => alert.fadeOut(), 5000);
    }

    /**
     * Show error alert
     */
    function showError(message) {
        const alert = $(`
            <div class="alert alert-danger alert-dismissible fade show" role="alert">
                <i class="fas fa-exclamation-triangle"></i> ${escapeHtml(message)}
                <button type="button" class="close" data-dismiss="alert">
                    <span>&times;</span>
                </button>
            </div>
        `);
        $('#routesContainer').prepend(alert);
        setTimeout(() => alert.fadeOut(), 8000);
    }

    /**
     * Escape HTML to prevent XSS
     */
    function escapeHtml(text) {
        if (text == null) return '';
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return String(text).replace(/[&<>"']/g, m => map[m]);
    }

    // Enable upload button when risks are confirmed
    $('#confirmRisks').on('change', function() {
        $('#btnUploadConfig').toggle($(this).is(':checked'));
    });
});
