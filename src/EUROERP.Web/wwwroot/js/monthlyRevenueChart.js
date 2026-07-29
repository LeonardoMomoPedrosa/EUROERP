// Renders vertical bar and cumulative line charts for monthly revenue (Faturamento Mensal). Call from Blazor via IJSRuntime.
// chartId: id of the canvas element. config: { labels: string[], datasets: { label, data: number[], borderColor, backgroundColor }[] }
window.monthlyRevenueChart = {
    instance: null,
    instanceCumulative: null,
    render: function (chartId, config) {
        if (typeof Chart === 'undefined') return;
        if (this.instance) {
            this.instance.destroy();
            this.instance = null;
        }
        var canvas = document.getElementById(chartId);
        if (!canvas) return;
        var ctx = canvas.getContext('2d');
        this.instance = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: config.labels || [],
                datasets: (config.datasets || []).map(function (ds) {
                    return {
                        label: ds.label,
                        data: ds.data || [],
                        borderColor: ds.borderColor || '#000',
                        backgroundColor: (ds.backgroundColor || ds.borderColor || '#000').replace('0.1)', '0.7)'),
                        borderWidth: 1
                    };
                })
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                aspectRatio: 2,
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            usePointStyle: false,
                            pointStyle: 'rect',
                            generateLabels: function (chart) {
                                var datasets = chart.data.datasets;
                                return chart.data.labels ? datasets.map(function (ds, i) {
                                    return {
                                        text: ds.label,
                                        fillStyle: ds.borderColor || '#000',
                                        strokeStyle: ds.borderColor || '#000',
                                        lineWidth: 2,
                                        hidden: !chart.isDatasetVisible(i),
                                        index: i
                                    };
                                }) : [];
                            }
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                var value = context.parsed.y;
                                return context.dataset.label + ': R$ ' + value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function (value) {
                                return 'R$ ' + value.toLocaleString('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
                            }
                        }
                    }
                }
            }
        });
    },
    renderCumulative: function (chartId, config) {
        if (typeof Chart === 'undefined') return;
        if (this.instanceCumulative) {
            this.instanceCumulative.destroy();
            this.instanceCumulative = null;
        }
        var canvas = document.getElementById(chartId);
        if (!canvas) return;
        var ctx = canvas.getContext('2d');
        this.instanceCumulative = new Chart(ctx, {
            type: 'line',
            data: {
                labels: config.labels || [],
                datasets: (config.datasets || []).map(function (ds) {
                    return {
                        label: ds.label,
                        data: ds.data || [],
                        borderColor: ds.borderColor || '#000',
                        backgroundColor: (ds.backgroundColor || ds.borderColor || '#000').replace('1)', '0.1)'),
                        fill: false,
                        tension: 0.2,
                        borderWidth: 2,
                        pointRadius: 2,
                        pointHoverRadius: 4
                    };
                })
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                aspectRatio: 2,
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            usePointStyle: true,
                            generateLabels: function (chart) {
                                var datasets = chart.data.datasets;
                                return chart.data.labels ? datasets.map(function (ds, i) {
                                    return {
                                        text: ds.label,
                                        fillStyle: ds.borderColor || '#000',
                                        strokeStyle: ds.borderColor || '#000',
                                        lineWidth: 2,
                                        hidden: !chart.isDatasetVisible(i),
                                        index: i
                                    };
                                }) : [];
                            }
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                var value = context.parsed.y;
                                return context.dataset.label + ' (acum.): R$ ' + value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function (value) {
                                return 'R$ ' + value.toLocaleString('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
                            }
                        }
                    }
                }
            }
        });
    },
    destroy: function () {
        if (this.instance) {
            this.instance.destroy();
            this.instance = null;
        }
        if (this.instanceCumulative) {
            this.instanceCumulative.destroy();
            this.instanceCumulative = null;
        }
    }
};
