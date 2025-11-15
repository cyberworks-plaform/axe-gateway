// Route Configuration Management Script

let allRoutes = [];

$(document).ready(function() {
    loadRoutes();
    
    // Event handlers
    $('#searchFilter').on('input', filterRoutes);
    $('#schemeFilter').on('change', filterRoutes);
    $('#btnAddNode').on('click', showAddNodeModal);
    $('#btnSaveNode').on('click', addNode);
    $('#btnUpdateNode').on('click', updateNode);
    $('#btnUpdateRoute').on('click', updateRoute);
});

// Load all routes
function loadRoutes() {
    console.log('Loading routes from /api/routes');
    $.ajax({
        url: '/api/routes',
        method: 'GET',
        success: function(response) {
            console.log('Routes API response:', response);
            if (response.success) {
                allRoutes = response.data;
                renderRoutes(allRoutes);
            } else {
                showError('Failed to load routes: ' + response.message);
            }
        },
        error: function(xhr, status, error) {
            console.error('Error loading routes:', status, error, xhr);
            showError('Failed to load routes: ' + (xhr.responseJSON?.message || error || 'Please try again.'));
        }
    });
}

// Render routes
function renderRoutes(routes) {
    const container = $('#routesContainer');
    container.empty();
    
    // Hide loading indicator
    $('#loadingIndicator').hide();
    
    if (routes.length === 0) {
        container.html('<div class="alert alert-info">No routes found matching your criteria.</div>');
        return;
    }
    
    routes.forEach(route => {
        const card = createRouteCard(route);
        container.append(card);
    });
}

// Create route card HTML
function createRouteCard(route) {
    const nodes = route.downstreamHostAndPorts.map(node => 
        `<span class="badge badge-info node-badge">${node.host}:${node.port}
            <button type="button" class="btn btn-xs btn-link text-white p-0 ml-1" onclick="editNode('${route.routeId}', '${node.host}', ${node.port})" title="Edit">
                <i class="fas fa-edit"></i>
            </button>
            <button type="button" class="btn btn-xs btn-link text-white p-0 ml-1" onclick="deleteNode('${route.routeId}', '${node.host}', ${node.port})" title="Delete">
                <i class="fas fa-times"></i>
            </button>
        </span>`
    ).join('');
    
    const loadBalancer = route.loadBalancerOptions ? 
        `<span class="badge badge-secondary">${route.loadBalancerOptions.type}</span>` : 
        '<span class="badge badge-secondary">None</span>';
    
    const qos = route.qoSOptions ? 
        `<small>Timeout: ${route.qoSOptions.timeoutValue}ms, Max Errors: ${route.qoSOptions.exceptionsAllowedBeforeBreaking}</small>` :
        '<small>No QoS configured</small>';
    
    return $(`
        <div class="card route-card mb-3">
            <div class="card-header d-flex justify-content-between align-items-center">
                <h5 class="mb-0">
                    <i class="fas fa-route text-primary"></i> ${escapeHtml(route.upstreamPathTemplate)}
                </h5>
                <button type="button" class="btn btn-sm btn-outline-primary" onclick="editRoute('${route.routeId}')">
                    <i class="fas fa-cog"></i> Configure
                </button>
            </div>
            <div class="card-body">
                <div class="row">
                    <div class="col-md-6">
                        <p><strong>Downstream:</strong> ${route.downstreamScheme}://${escapeHtml(route.downstreamPathTemplate)}</p>
                        <p><strong>HTTP Methods:</strong> ${route.upstreamHttpMethod.join(', ')}</p>
                    </div>
                    <div class="col-md-6">
                        <p><strong>Load Balancer:</strong> ${loadBalancer}</p>
                        <p><strong>QoS:</strong> ${qos}</p>
                    </div>
                </div>
                <div class="mt-2">
                    <strong>Nodes (${route.downstreamHostAndPorts.length}):</strong><br>
                    ${nodes || '<span class="text-muted">No nodes configured</span>'}
                </div>
            </div>
        </div>
    `);
}

// Filter routes
function filterRoutes() {
    const search = $('#searchFilter').val().toLowerCase();
    const scheme = $('#schemeFilter').val();
    
    const filtered = allRoutes.filter(route => {
        const matchesSearch = !search || route.upstreamPathTemplate.toLowerCase().includes(search) || 
                             route.downstreamPathTemplate.toLowerCase().includes(search);
        const matchesScheme = !scheme || route.downstreamScheme === scheme;
        return matchesSearch && matchesScheme;
    });
    
    renderRoutes(filtered);
}

// Show add node modal
function showAddNodeModal() {
    const list = $('#routeSelectionList');
    list.empty();
    
    allRoutes.forEach(route => {
        list.append(`
            <div class="form-check">
                <input class="form-check-input route-checkbox" type="checkbox" value="${route.routeId}" id="route-${route.routeId}">
                <label class="form-check-label" for="route-${route.routeId}">
                    ${escapeHtml(route.upstreamPathTemplate)}
                </label>
            </div>
        `);
    });
    
    $('#addNodeForm')[0].reset();
    $('#addNodeModal').modal('show');
}

// Add node to routes
function addNode() {
    const selectedRoutes = $('.route-checkbox:checked').map(function() {
        return $(this).val();
    }).get();
    
    if (selectedRoutes.length === 0) {
        showError('Please select at least one route');
        return;
    }
    
    const host = $('#newHost').val().trim();
    const port = parseInt($('#newPort').val());
    
    if (!host || !port || port < 1 || port > 65535) {
        showError('Please enter valid host and port');
        return;
    }
    
    const request = {
        routeIds: selectedRoutes,
        host: host,
        port: port
    };
    
    $.ajax({
        url: '/api/routes/nodes',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(request),
        success: function(response) {
            if (response.success) {
                showSuccess(response.message);
                $('#addNodeModal').modal('hide');
                loadRoutes();
            } else {
                showError(response.message);
            }
        },
        error: function(xhr) {
            showError('Failed to add node. Please try again.');
        }
    });
}

// Edit node
function editNode(routeId, host, port) {
    $('#editRouteId').val(routeId);
    $('#editOldHost').val(host);
    $('#editOldPort').val(port);
    $('#editHost').val(host);
    $('#editPort').val(port);
    $('#editNodeModal').modal('show');
}

// Update node
function updateNode() {
    const routeId = $('#editRouteId').val();
    const oldHost = $('#editOldHost').val();
    const oldPort = parseInt($('#editOldPort').val());
    const newHost = $('#editHost').val().trim();
    const newPort = parseInt($('#editPort').val());
    
    if (!newHost || !newPort || newPort < 1 || newPort > 65535) {
        showError('Please enter valid host and port');
        return;
    }
    
    const request = {
        routeId: routeId,
        oldHost: oldHost,
        oldPort: oldPort,
        newHost: newHost,
        newPort: newPort
    };
    
    $.ajax({
        url: '/api/routes/nodes',
        method: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(request),
        success: function(response) {
            if (response.success) {
                showSuccess(response.message);
                $('#editNodeModal').modal('hide');
                loadRoutes();
            } else {
                showError(response.message);
            }
        },
        error: function(xhr) {
            showError('Failed to update node. Please try again.');
        }
    });
}

// Delete node
function deleteNode(routeId, host, port) {
    if (!confirm(`Are you sure you want to delete node ${host}:${port}?`)) {
        return;
    }
    
    const request = {
        routeIds: [routeId],
        host: host,
        port: port
    };
    
    $.ajax({
        url: '/api/routes/nodes',
        method: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(request),
        success: function(response) {
            if (response.success) {
                showSuccess(response.message);
                loadRoutes();
            } else {
                showError(response.message);
            }
        },
        error: function(xhr) {
            showError('Failed to delete node. Please try again.');
        }
    });
}

// Edit route
function editRoute(routeId) {
    const route = allRoutes.find(r => r.routeId === routeId);
    if (!route) return;
    
    $('#routeId').val(routeId);
    $('#downstreamScheme').val(route.downstreamScheme);
    $('#downstreamPathTemplate').val(route.downstreamPathTemplate);
    $('#loadBalancerType').val(route.loadBalancerOptions ? route.loadBalancerOptions.type : '');
    
    if (route.qoSOptions) {
        $('#qosTimeout').val(route.qoSOptions.timeoutValue || '');
        $('#qosExceptions').val(route.qoSOptions.exceptionsAllowedBeforeBreaking || '');
        $('#qosDuration').val(route.qoSOptions.durationOfBreak || '');
    } else {
        $('#qosTimeout').val('');
        $('#qosExceptions').val('');
        $('#qosDuration').val('');
    }
    
    $('#dangerousAccept').prop('checked', route.dangerousAcceptAnyServerCertificateValidator || false);
    $('#editRouteModal').modal('show');
}

// Update route
function updateRoute() {
    const routeId = $('#routeId').val();
    const scheme = $('#downstreamScheme').val();
    const pathTemplate = $('#downstreamPathTemplate').val();
    const loadBalancerType = $('#loadBalancerType').val();
    const timeout = $('#qosTimeout').val();
    const exceptions = $('#qosExceptions').val();
    const duration = $('#qosDuration').val();
    const dangerousAccept = $('#dangerousAccept').is(':checked');
    
    const request = {
        routeId: routeId,
        downstreamScheme: scheme,
        downstreamPathTemplate: pathTemplate,
        loadBalancerOptions: loadBalancerType ? { type: loadBalancerType } : null,
        qoSOptions: (timeout || exceptions || duration) ? {
            timeoutValue: timeout ? parseInt(timeout) : null,
            exceptionsAllowedBeforeBreaking: exceptions ? parseInt(exceptions) : null,
            durationOfBreak: duration ? parseInt(duration) : null
        } : null,
        dangerousAcceptAnyServerCertificateValidator: dangerousAccept
    };
    
    $.ajax({
        url: `/api/routes/${routeId}`,
        method: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify(request),
        success: function(response) {
            if (response.success) {
                showSuccess(response.message);
                $('#editRouteModal').modal('hide');
                loadRoutes();
            } else {
                showError(response.message);
            }
        },
        error: function(xhr) {
            showError('Failed to update route. Please try again.');
        }
    });
}

// Utility functions
function escapeHtml(text) {
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
}

function showSuccess(message) {
    console.log('Success:', message);
    // Show Bootstrap alert
    const alert = `<div class="alert alert-success alert-dismissible fade show" role="alert">
        <i class="fas fa-check-circle"></i> ${escapeHtml(message)}
        <button type="button" class="close" data-dismiss="alert" aria-label="Close">
            <span aria-hidden="true">&times;</span>
        </button>
    </div>`;
    $('#routesContainer').prepend(alert);
    // Auto-dismiss after 5 seconds
    setTimeout(() => $('.alert-success').fadeOut(), 5000);
}

function showError(message) {
    console.error('Error:', message);
    // Hide loading indicator
    $('#loadingIndicator').hide();
    // Show Bootstrap alert
    const alert = `<div class="alert alert-danger alert-dismissible fade show" role="alert">
        <i class="fas fa-exclamation-circle"></i> ${escapeHtml(message)}
        <button type="button" class="close" data-dismiss="alert" aria-label="Close">
            <span aria-hidden="true">&times;</span>
        </button>
    </div>`;
    $('#routesContainer').prepend(alert);
}
