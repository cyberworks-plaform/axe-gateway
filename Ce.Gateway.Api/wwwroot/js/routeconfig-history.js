// Route Configuration History Script

$(document).ready(function() {
    loadHistory();
    $('#btnConfirmRollback').on('click', performRollback);
});

// Load configuration history
function loadHistory() {
    console.log('Loading configuration history');
    $.ajax({
        url: '/api/routes/history?limit=50',
        method: 'GET',
        success: function(response) {
            console.log('History API response:', response);
            if (response.success) {
                renderHistory(response.data);
            } else {
                showError('Failed to load history: ' + response.message);
            }
        },
        error: function(xhr, status, error) {
            console.error('Error loading history:', status, error, xhr);
            showError('Failed to load configuration history: ' + (xhr.responseJSON?.message || error || 'Please try again.'));
        }
    });
}

// Render history table
function renderHistory(history) {
    const tbody = $('#historyTableBody');
    tbody.empty();
    
    if (history.length === 0) {
        tbody.html('<tr><td colspan="5" class="text-center">No configuration history found</td></tr>');
        return;
    }
    
    history.forEach(entry => {
        const row = createHistoryRow(entry);
        tbody.append(row);
    });
}

// Create history row
function createHistoryRow(entry) {
    const timestamp = new Date(entry.timestamp).toLocaleString();
    const statusBadge = entry.isActive ? 
        '<span class="badge badge-success"><i class="fas fa-check-circle"></i> Active</span>' : 
        '<span class="badge badge-secondary"><i class="fas fa-history"></i> Historical</span>';
    
    const rollbackButton = !entry.isActive ? 
        `<button type="button" class="btn btn-sm btn-warning" onclick="confirmRollback('${entry.id}')">
            <i class="fas fa-undo"></i> Rollback
        </button>` : 
        '<span class="text-muted">Current</span>';
    
    return $(`
        <tr>
            <td>${statusBadge}</td>
            <td>${escapeHtml(timestamp)}</td>
            <td>${escapeHtml(entry.changedBy)}</td>
            <td>${escapeHtml(entry.description)}</td>
            <td>${rollbackButton}</td>
        </tr>
    `);
}

// Confirm rollback
function confirmRollback(historyId) {
    $('#rollbackHistoryId').val(historyId);
    $('#rollbackModal').modal('show');
}

// Perform rollback
function performRollback() {
    const historyId = $('#rollbackHistoryId').val();
    
    $.ajax({
        url: `/api/routes/rollback/${historyId}`,
        method: 'POST',
        success: function(response) {
            if (response.success) {
                showSuccess(response.message);
                $('#rollbackModal').modal('hide');
                setTimeout(() => {
                    loadHistory();
                }, 1000);
            } else {
                showError(response.message);
            }
        },
        error: function(xhr) {
            showError('Failed to rollback configuration. Please try again.');
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
    $('.content').prepend(alert);
    // Auto-dismiss after 5 seconds
    setTimeout(() => $('.alert-success').fadeOut(), 5000);
}

function showError(message) {
    console.error('Error:', message);
    // Show Bootstrap alert
    const alert = `<div class="alert alert-danger alert-dismissible fade show" role="alert">
        <i class="fas fa-exclamation-circle"></i> ${escapeHtml(message)}
        <button type="button" class="close" data-dismiss="alert" aria-label="Close">
            <span aria-hidden="true">&times;</span>
        </button>
    </div>`;
    $('.content').prepend(alert);
}
