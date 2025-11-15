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
    $('#btnConfirmCopy').on('click', confirmCopyRoute);
    
    // Event delegation for dynamically created node buttons (SECURITY FIX)
    $('#routesContainer').on('click', '[data-action="edit-node"]', function() {
        const routeId = $(this).data('route-id');
        const host = $(this).data('host');
        const port = $(this).data('port');
        editNode(routeId, host, port);
    });
    
    $('#routesContainer').on('click', '[data-action="delete-node"]', function() {
        const routeId = $(this).data('route-id');
        const host = $(this).data('host');
        const port = $(this).data('port');
        deleteNode(routeId, host, port);
    });
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
    const card = $('<div>').addClass('card route-card mb-3').attr('data-route-id', route.routeId);
    
    // Create header
    const header = $('<div>').addClass('card-header d-flex justify-content-between align-items-center flex-wrap');
    const title = $('<h5>').addClass('mb-0')
        .append($('<i>').addClass('fas fa-route text-primary'))
        .append(' ' + escapeHtml(route.upstreamPathTemplate));
    
    const buttonGroup = $('<div>').addClass('btn-group btn-group-sm mt-2 mt-md-0');
    
    // Configure button
    const configBtn = $('<button>').addClass('btn btn-outline-primary')
        .attr('type', 'button')
        .attr('title', 'Configure Route')
        .html('<i class="fas fa-cog"></i> Configure')
        .on('click', function() { editRoute(route.routeId); });
    
    // Copy button (NEW)
    const copyBtn = $('<button>').addClass('btn btn-outline-success')
        .attr('type', 'button')
        .attr('title', 'Copy Route')
        .html('<i class="fas fa-copy"></i> Copy')
        .on('click', function() { copyRoute(route.routeId); });
    
    buttonGroup.append(configBtn).append(copyBtn);
    header.append(title).append(buttonGroup);
    
    // Create body
    const body = $('<div>').addClass('card-body');
    const row = $('<div>').addClass('row');
    
    const col1 = $('<div>').addClass('col-md-6');
    col1.append($('<p>').html('<strong>Downstream:</strong> ' + escapeHtml(route.downstreamScheme) + '://' + escapeHtml(route.downstreamPathTemplate)));
    col1.append($('<p>').html('<strong>HTTP Methods:</strong> ' + route.upstreamHttpMethod.map(m => escapeHtml(m)).join(', ')));
    
    const col2 = $('<div>').addClass('col-md-6');
    const loadBalancerType = route.loadBalancerOptions ? escapeHtml(route.loadBalancerOptions.type) : 'None';
    col2.append($('<p>').html('<strong>Load Balancer:</strong> <span class="badge badge-secondary">' + loadBalancerType + '</span>'));
    
    const qosText = route.qoSOptions ? 
        `Timeout: ${route.qoSOptions.timeoutValue}ms, Max Errors: ${route.qoSOptions.exceptionsAllowedBeforeBreaking}` :
        'No QoS configured';
    col2.append($('<p>').html('<strong>QoS:</strong> <small>' + escapeHtml(qosText) + '</small>'));
    
    row.append(col1).append(col2);
    body.append(row);
    
    // Create nodes section with event delegation (SECURITY FIX)
    const nodesDiv = $('<div>').addClass('mt-2');
    nodesDiv.append($('<strong>').text(`Nodes (${route.downstreamHostAndPorts.length}):`)).append('<br>');
    
    if (route.downstreamHostAndPorts.length === 0) {
        nodesDiv.append($('<span>').addClass('text-muted').text('No nodes configured'));
    } else {
        route.downstreamHostAndPorts.forEach(node => {
            const badge = $('<span>').addClass('badge badge-info node-badge')
                .text(escapeHtml(node.host) + ':' + node.port);
            
            const editBtn = $('<button>').addClass('btn btn-xs btn-link text-white p-0 ml-1')
                .attr('type', 'button')
                .attr('title', 'Edit')
                .attr('data-action', 'edit-node')
                .attr('data-route-id', route.routeId)
                .attr('data-host', node.host)
                .attr('data-port', node.port)
                .html('<i class="fas fa-edit"></i>');
            
            const deleteBtn = $('<button>').addClass('btn btn-xs btn-link text-white p-0 ml-1')
                .attr('type', 'button')
                .attr('title', 'Delete')
                .attr('data-action', 'delete-node')
                .attr('data-route-id', route.routeId)
                .attr('data-host', node.host)
                .attr('data-port', node.port)
                .html('<i class="fas fa-times"></i>');
            
            badge.append(editBtn).append(deleteBtn);
            nodesDiv.append(badge);
        });
    }
    
    body.append(nodesDiv);
    card.append(header).append(body);
    
    return card;
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
    
    const routeNames = selectedRoutes.map(id => {
        const route = allRoutes.find(r => r.routeId === id);
        return route ? escapeHtml(route.upstreamPathTemplate) : id;
    }).join('<br>');
    
    showConfirmation(
        'Add Node',
        `Add node <strong>${escapeHtml(host)}:${port}</strong> to ${selectedRoutes.length} route(s):<br><small>${routeNames}</small><br><br><small class="text-muted">A backup will be created automatically before applying changes.</small>`,
        function() {
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
    );
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
    
    showConfirmation(
        'Update Node',
        `Update node from <strong>${escapeHtml(oldHost)}:${oldPort}</strong> to <strong>${escapeHtml(newHost)}:${newPort}</strong>?<br><small class="text-muted">A backup will be created automatically.</small>`,
        function() {
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
    );
}

// Delete node
function deleteNode(routeId, host, port) {
    showConfirmation(
        'Delete Node',
        `Are you sure you want to delete node <strong>${escapeHtml(host)}:${port}</strong>?<br><small class="text-muted">A backup will be created automatically.</small>`,
        function() {
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
    );
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
    const route = allRoutes.find(r => r.routeId === routeId);
    if (!route) {
        showError('Route not found');
        return;
    }
    
    const scheme = $('#downstreamScheme').val();
    const pathTemplate = $('#downstreamPathTemplate').val();
    const loadBalancerType = $('#loadBalancerType').val();
    const timeout = $('#qosTimeout').val();
    const exceptions = $('#qosExceptions').val();
    const duration = $('#qosDuration').val();
    const dangerousAccept = $('#dangerousAccept').is(':checked');
    
    showConfirmation(
        'Update Route Configuration',
        `Update configuration for route:<br><strong>${escapeHtml(route.upstreamPathTemplate)}</strong><br><br><small class="text-muted">A backup will be created automatically before applying changes.</small>`,
        function() {
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
    );
}

// Copy route functionality (NEW FEATURE)
function copyRoute(routeId) {
    const route = allRoutes.find(r => r.routeId === routeId);
    if (!route) {
        showError('Route not found');
        return;
    }
    
    // Populate copy modal with current route data
    $('#copyRouteId').val(routeId);
    $('#copyUpstreamPath').val(route.upstreamPathTemplate);
    $('#copyDownstreamScheme').val(route.downstreamScheme);
    $('#copyDownstreamPath').val(route.downstreamPathTemplate);
    $('#copyHttpMethods').val(route.upstreamHttpMethod.join(', '));
    $('#copyLoadBalancerType').val(route.loadBalancerOptions?.type || '');
    $('#copyPriority').val(route.priority || '');
    $('#copyDangerousAccept').prop('checked', route.dangerousAcceptAnyServerCertificateValidator || false);
    
    if (route.qoSOptions) {
        $('#copyQosTimeout').val(route.qoSOptions.timeoutValue || '');
        $('#copyQosExceptions').val(route.qoSOptions.exceptionsAllowedBeforeBreaking || '');
        $('#copyQosDuration').val(route.qoSOptions.durationOfBreak || '');
    } else {
        $('#copyQosTimeout').val('');
        $('#copyQosExceptions').val('');
        $('#copyQosDuration').val('');
    }
    
    // Copy nodes
    $('#copyNodesDisplay').empty();
    route.downstreamHostAndPorts.forEach(node => {
        $('#copyNodesDisplay').append(
            $('<div>').addClass('badge badge-info mr-1')
                .text(`${node.host}:${node.port}`)
        );
    });
    
    $('#copyRouteModal').modal('show');
}

function confirmCopyRoute() {
    const upstreamPath = $('#copyUpstreamPath').val().trim();
    
    if (!upstreamPath) {
        showError('Upstream path is required');
        return;
    }
    
    // Check if route already exists
    if (allRoutes.some(r => r.upstreamPathTemplate === upstreamPath)) {
        showError('A route with this upstream path already exists');
        return;
    }
    
    const originalRouteId = $('#copyRouteId').val();
    const originalRoute = allRoutes.find(r => r.routeId === originalRouteId);
    
    const newRoute = {
        upstreamPathTemplate: upstreamPath,
        downstreamPathTemplate: $('#copyDownstreamPath').val().trim(),
        downstreamScheme: $('#copyDownstreamScheme').val(),
        upstreamHttpMethod: $('#copyHttpMethods').val().split(',').map(m => m.trim()).filter(m => m),
        downstreamHostAndPorts: originalRoute.downstreamHostAndPorts.map(n => ({host: n.host, port: n.port})),
        loadBalancerOptions: $('#copyLoadBalancerType').val() ? {type: $('#copyLoadBalancerType').val()} : null,
        priority: $('#copyPriority').val() ? parseInt($('#copyPriority').val()) : null,
        dangerousAcceptAnyServerCertificateValidator: $('#copyDangerousAccept').is(':checked'),
        qoSOptions: null
    };
    
    const qosTimeout = $('#copyQosTimeout').val();
    const qosExceptions = $('#copyQosExceptions').val();
    const qosDuration = $('#copyQosDuration').val();
    
    if (qosTimeout || qosExceptions || qosDuration) {
        newRoute.qoSOptions = {
            timeoutValue: qosTimeout ? parseInt(qosTimeout) : null,
            exceptionsAllowedBeforeBreaking: qosExceptions ? parseInt(qosExceptions) : null,
            durationOfBreak: qosDuration ? parseInt(qosDuration) : null
        };
    }
    
    // Show confirmation dialog
    showConfirmation(
        'Copy Route',
        `Create new route: ${escapeHtml(upstreamPath)}?`,
        function() {
            // Call API to create route
            $.ajax({
                url: '/api/routes',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(newRoute),
                success: function(response) {
                    if (response.success) {
                        $('#copyRouteModal').modal('hide');
                        showSuccess(response.message || 'Route copied successfully');
                        loadRoutes();
                    } else {
                        showError(response.message || 'Failed to copy route');
                    }
                },
                error: function(xhr) {
                    showError('Failed to copy route: ' + (xhr.responseJSON?.message || 'Please try again'));
                }
            });
        }
    );
}

// Utility functions
function escapeHtml(text) {
    if (!text) return '';
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return String(text).replace(/[&<>"']/g, m => map[m]);
}

function showSuccess(message) {
    console.log('Success:', message);
    // Show Bootstrap alert (SECURITY FIX: store reference to specific alert)
    const alert = $(`<div class="alert alert-success alert-dismissible fade show" role="alert">
        <i class="fas fa-check-circle"></i> ${escapeHtml(message)}
        <button type="button" class="close" data-dismiss="alert" aria-label="Close">
            <span aria-hidden="true">&times;</span>
        </button>
    </div>`);
    $('#routesContainer').prepend(alert);
    // Auto-dismiss after 5 seconds (fade only this specific alert)
    setTimeout(() => alert.fadeOut(function() { $(this).remove(); }), 5000);
}

function showError(message) {
    console.error('Error:', message);
    // Hide loading indicator
    $('#loadingIndicator').hide();
    // Show Bootstrap alert
    const alert = $(`<div class="alert alert-danger alert-dismissible fade show" role="alert">
        <i class="fas fa-exclamation-circle"></i> ${escapeHtml(message)}
        <button type="button" class="close" data-dismiss="alert" aria-label="Close">
            <span aria-hidden="true">&times;</span>
        </button>
    </div>`);
    $('#routesContainer').prepend(alert);
}

// Show confirmation dialog (NEW)
function showConfirmation(title, message, onConfirm) {
    $('#confirmModalTitle').text(title);
    $('#confirmModalMessage').html(message);
    $('#confirmModalBtn').off('click').on('click', function() {
        $('#confirmModal').modal('hide');
        if (onConfirm) onConfirm();
    });
    $('#confirmModal').modal('show');
}
