// Node Network Graph Visualization
// Reusable component for displaying node network topology

class NodeNetworkGraph {
    constructor(containerId, options = {}) {
        this.containerId = containerId;
        this.container = document.getElementById(containerId);
        this.network = null;
        this.nodes = new vis.DataSet([]);
        this.edges = new vis.DataSet([]);
        this.infoPanel = null;
        this.options = {
            showInfoPanel: options.showInfoPanel !== false,
            ...options
        };
        
        this.init();
    }

    init() {
        if (!this.container) {
            console.error(`Container ${this.containerId} not found`);
            return;
        }

        // Create network container
        const networkContainer = document.createElement('div');
        networkContainer.id = `${this.containerId}-network`;
        networkContainer.style.width = '100%';
        networkContainer.style.height = '500px';
        networkContainer.style.border = '1px solid #ddd';
        networkContainer.style.borderRadius = '4px';
        this.container.appendChild(networkContainer);

        // Create info panel if enabled
        if (this.options.showInfoPanel) {
            this.createInfoPanel();
        }

        // Initialize vis-network
        const data = {
            nodes: this.nodes,
            edges: this.edges
        };

        const networkOptions = {
            nodes: {
                shape: 'dot',
                size: 30,
                font: {
                    size: 14,
                    color: '#343a40',
                    face: 'Arial',
                    bold: {
                        size: 16
                    }
                },
                borderWidth: 3,
                borderWidthSelected: 4,
                shadow: {
                    enabled: true,
                    color: 'rgba(0,0,0,0.15)',
                    size: 10,
                    x: 2,
                    y: 2
                }
            },
            edges: {
                width: 4,
                smooth: {
                    type: 'continuous',
                    roundness: 0.5
                },
                arrows: {
                    to: {
                        enabled: false
                    }
                },
                shadow: {
                    enabled: true,
                    color: 'rgba(0,0,0,0.1)',
                    size: 5,
                    x: 1,
                    y: 1
                }
            },
            physics: {
                enabled: true,
                stabilization: {
                    enabled: true,
                    iterations: 100
                },
                barnesHut: {
                    gravitationalConstant: -2000,
                    centralGravity: 0.3,
                    springLength: 200,
                    springConstant: 0.04,
                    damping: 0.09,
                    avoidOverlap: 0.5
                }
            },
            interaction: {
                hover: true,
                tooltipDelay: 100,
                zoomView: true,
                dragView: true
            }
        };

        this.network = new vis.Network(networkContainer, data, networkOptions);

        // Event listeners
        this.network.on('click', (params) => {
            if (params.nodes.length > 0) {
                const nodeId = params.nodes[0];
                this.showNodeInfo(nodeId);
            }
        });

        this.network.on('hoverNode', (params) => {
            this.container.style.cursor = 'pointer';
        });

        this.network.on('blurNode', () => {
            this.container.style.cursor = 'default';
        });
    }

    createInfoPanel() {
        this.infoPanel = document.createElement('div');
        this.infoPanel.id = `${this.containerId}-info`;
        this.infoPanel.className = 'node-info-panel';
        this.infoPanel.style.display = 'none';
        this.container.appendChild(this.infoPanel);
    }

    updateData(nodeStatusData) {
        // Clear existing data
        this.nodes.clear();
        this.edges.clear();

        // Add gateway node (center)
        this.nodes.add({
            id: 'gateway',
            label: '⚡ Gateway',
            shape: 'box',
            size: 50,
            color: {
                background: '#5a67d8',
                border: '#4c51bf',
                highlight: {
                    background: '#6b7fd7',
                    border: '#5a67d8'
                }
            },
            font: {
                size: 18,
                color: '#ffffff',
                bold: true,
                face: 'Arial'
            },
            borderWidth: 4,
            margin: 15,
            widthConstraint: {
                minimum: 120,
                maximum: 120
            },
            fixed: {
                x: false,
                y: false
            },
            physics: false,
            x: 0,
            y: 0,
            nodeData: {
                type: 'gateway',
                name: 'Gateway',
                status: 'Healthy'
            }
        });

        // Add downstream nodes
        nodeStatusData.forEach((node, index) => {
            const nodeId = `node-${index}`;
            const isHealthy = node.isHealthy;
            
            // Format label (hide port 80)
            let label = node.node;
            if (label.endsWith(':80')) {
                label = label.substring(0, label.length - 3);
            }

            // Professional muted color palette
            const nodeColor = isHealthy 
                ? {
                    background: '#52b788',
                    border: '#40916c',
                    highlight: {
                        background: '#74c69d',
                        border: '#52b788'
                    }
                }
                : {
                    background: '#e76f51',
                    border: '#d84a2f',
                    highlight: {
                        background: '#ee8769',
                        border: '#e76f51'
                    }
                };

            // Format metrics for display
            const reqCount = node.totalRequests || 0;
            const reqDisplay = reqCount > 999 ? `${(reqCount/1000).toFixed(1)}k` : reqCount;
            const avgLatency = node.avgLatencyMs || 0;
            
            // Build multi-line label with metrics
            const nodeLabel = `🖥️ ${label}\n${reqDisplay} req | ${avgLatency}ms`;

            this.nodes.add({
                id: nodeId,
                label: nodeLabel,
                shape: 'dot',
                size: 40,
                color: nodeColor,
                font: {
                    size: 13,
                    color: '#1f2937',
                    face: 'Arial',
                    bold: false,
                    multi: true,
                    align: 'center'
                },
                borderWidth: 3,
                title: this.buildTooltipHtml(node, isHealthy),
                nodeData: {
                    type: 'downstream',
                    name: node.node,
                    isHealthy: isHealthy,
                    status: node.status || (isHealthy ? 'Healthy' : 'Unhealthy'),
                    totalRequests: node.totalRequests || 0,
                    minLatencyMs: node.minLatencyMs || 0,
                    maxLatencyMs: node.maxLatencyMs || 0,
                    avgLatencyMs: node.avgLatencyMs || 0,
                    lastChecked: node.lastChecked,
                    statusMessage: node.statusMessage,
                    totalDuration: node.totalDuration,
                    entries: node.entries
                }
            });

            // Professional edge styling with dashed line for unhealthy
            const edgeColor = isHealthy 
                ? {
                    color: '#52b788',
                    highlight: '#74c69d',
                    hover: '#74c69d',
                    opacity: 0.8
                }
                : {
                    color: '#e76f51',
                    highlight: '#ee8769',
                    hover: '#ee8769',
                    opacity: 0.7
                };
            
            const edgeStyle = isHealthy ? false : [5, 5]; // Dashed line for unhealthy

            this.edges.add({
                id: `edge-${index}`,
                from: 'gateway',
                to: nodeId,
                color: edgeColor,
                width: 4,
                dashes: edgeStyle
            });
        });

        // Stabilize network
        if (this.network) {
            this.network.stabilize();
        }
    }

    buildTooltipHtml(node, isHealthy) {
        const status = isHealthy ? 'Healthy' : 'Unhealthy';
        const statusIcon = isHealthy ? '✓' : '✗';
        const reqCount = node.totalRequests || 0;
        
        return `
            <div style="padding: 8px; font-size: 13px; line-height: 1.6;">
                <strong>${statusIcon} ${status}</strong><br/>
                Requests: <strong>${reqCount.toLocaleString()}</strong><br/>
                Avg: <strong>${node.avgLatencyMs || 0}ms</strong> | 
                Min: ${node.minLatencyMs || 0}ms | 
                Max: ${node.maxLatencyMs || 0}ms<br/>
                <em style="font-size: 11px; color: #888;">Click for details</em>
            </div>
        `;
    }

    showNodeInfo(nodeId) {
        if (!this.infoPanel) return;

        const node = this.nodes.get(nodeId);
        if (!node || !node.nodeData) return;

        const data = node.nodeData;

        if (data.type === 'gateway') {
            this.infoPanel.innerHTML = `
                <div class="node-info-header gateway">
                    <i class="fas fa-network-wired"></i>
                    <span class="node-info-title">⚡ Gateway</span>
                    <button class="node-info-close" onclick="document.getElementById('${this.containerId}-info').style.display='none'">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
                <div class="node-info-body">
                    <div class="info-card">
                        <div class="info-icon gateway-icon">
                            <i class="fas fa-shield-alt"></i>
                        </div>
                        <div class="info-content">
                            <h6>API Gateway / Load Balancer</h6>
                            <p class="text-muted mb-0">Central routing and load balancing hub</p>
                        </div>
                    </div>
                    <div class="info-stats">
                        <div class="stat-item">
                            <div class="stat-label">Status</div>
                            <div class="stat-value">
                                <span class="badge badge-success"><i class="fas fa-check-circle"></i> Active</span>
                            </div>
                        </div>
                        <div class="stat-item">
                            <div class="stat-label">Function</div>
                            <div class="stat-value">Request Distribution</div>
                        </div>
                    </div>
                </div>
            `;
        } else {
            const statusClass = data.isHealthy ? 'success' : 'danger';
            const statusIcon = data.isHealthy ? 'fa-check-circle' : 'fa-exclamation-circle';

            let entriesHtml = '';
            if (data.entries && Object.keys(data.entries).length > 0) {
                entriesHtml = '<div class="health-check-entries">';
                entriesHtml += '<h6><i class="fas fa-heartbeat"></i> Health Check Details</h6>';
                entriesHtml += '<div class="health-checks-grid">';
                for (const [key, entry] of Object.entries(data.entries)) {
                    const entryStatusClass = entry.status === 'Healthy' ? 'success' : 'danger';
                    const entryIcon = entry.status === 'Healthy' ? 'fa-check-circle' : 'fa-times-circle';
                    entriesHtml += `
                        <div class="health-check-item ${entryStatusClass}">
                            <div class="health-check-header">
                                <i class="fas ${entryIcon}"></i>
                                <strong>${key}</strong>
                            </div>
                            <div class="health-check-meta">
                                <span class="badge badge-${entryStatusClass}">${entry.status}</span>
                                <span class="duration">${entry.duration || '-'}</span>
                            </div>
                            ${entry.description ? `<div class="health-check-desc">${entry.description}</div>` : ''}
                        </div>
                    `;
                }
                entriesHtml += '</div></div>';
            }

            this.infoPanel.innerHTML = `
                <div class="node-info-header ${statusClass}">
                    <i class="fas ${statusIcon}"></i>
                    <span class="node-info-title">🖥️ ${data.name}</span>
                    <button class="node-info-close" onclick="document.getElementById('${this.containerId}-info').style.display='none'">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
                <div class="node-info-body">
                    <div class="info-card">
                        <div class="info-icon ${statusClass}-icon">
                            <i class="fas fa-server"></i>
                        </div>
                        <div class="info-content">
                            <div class="status-badge-large">
                                <span class="badge badge-${statusClass}">
                                    <i class="fas ${statusIcon}"></i> ${data.status}
                                </span>
                            </div>
                            ${data.statusMessage ? `<p class="text-muted mb-0 mt-2">${data.statusMessage}</p>` : ''}
                        </div>
                    </div>
                    
                    <div class="info-stats-grid">
                        <div class="stat-card">
                            <div class="stat-icon requests">
                                <i class="fas fa-exchange-alt"></i>
                            </div>
                            <div class="stat-data">
                                <div class="stat-value">${data.totalRequests.toLocaleString()}</div>
                                <div class="stat-label">Total Requests</div>
                            </div>
                        </div>
                        
                        <div class="stat-card">
                            <div class="stat-icon latency">
                                <i class="fas fa-tachometer-alt"></i>
                            </div>
                            <div class="stat-data">
                                <div class="stat-value">${data.avgLatencyMs}ms</div>
                                <div class="stat-label">Avg Latency</div>
                            </div>
                        </div>
                        
                        <div class="stat-card">
                            <div class="stat-icon min">
                                <i class="fas fa-arrow-down"></i>
                            </div>
                            <div class="stat-data">
                                <div class="stat-value">${data.minLatencyMs}ms</div>
                                <div class="stat-label">Min Latency</div>
                            </div>
                        </div>
                        
                        <div class="stat-card">
                            <div class="stat-icon max">
                                <i class="fas fa-arrow-up"></i>
                            </div>
                            <div class="stat-data">
                                <div class="stat-value">${data.maxLatencyMs}ms</div>
                                <div class="stat-label">Max Latency</div>
                            </div>
                        </div>
                    </div>
                    
                    ${data.totalDuration ? `
                        <div class="info-meta">
                            <i class="fas fa-clock"></i> Check Duration: <strong>${data.totalDuration}</strong>
                        </div>
                    ` : ''}
                    
                    ${entriesHtml}
                </div>
            `;
        }

        this.infoPanel.style.display = 'block';
    }

    destroy() {
        if (this.network) {
            this.network.destroy();
        }
    }
}

// Export for use in other files
if (typeof module !== 'undefined' && module.exports) {
    module.exports = NodeNetworkGraph;
}
