import authApi from './authApi';

const API_BASE = import.meta.env.VITE_API_URL || '';

const getAuthHeaders = () => {
    const token = localStorage.getItem('authToken');
    return {
        'Content-Type': 'application/json',
        ...(token ? { 'Authorization': `Bearer ${token}` } : {})
    };
};

export const reportsService = {
    // User: submit a report
    async createReport(data) {
        const response = await fetch(`${API_BASE}/api/Reports`, {
            method: 'POST',
            headers: getAuthHeaders(),
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            const err = await response.text();
            throw new Error(err || 'Failed to submit report');
        }
        return response.json();
    },

    // User: get my reports
    async getMyReports() {
        const response = await fetch(`${API_BASE}/api/Reports/my`, {
            headers: getAuthHeaders()
        });
        if (!response.ok) throw new Error('Failed to fetch reports');
        return response.json();
    },

    // Admin: get all reports (paginated, filterable)
    async getAdminReports({ status, targetType, reason, searchQuery, page = 1, pageSize = 20 } = {}) {
        const params = new URLSearchParams();
        if (status !== undefined && status !== null && status !== '') params.append('status', status);
        if (targetType !== undefined && targetType !== null && targetType !== '') params.append('targetType', targetType);
        if (reason !== undefined && reason !== null && reason !== '') params.append('reason', reason);
        if (searchQuery) params.append('searchQuery', searchQuery);
        params.append('page', page);
        params.append('pageSize', pageSize);

        const response = await fetch(`${API_BASE}/api/AdminReports?${params}`, {
            headers: getAuthHeaders()
        });
        if (!response.ok) throw new Error('Failed to fetch reports');
        return response.json();
    },

    // Admin: get report counts
    async getReportCounts() {
        const response = await fetch(`${API_BASE}/api/AdminReports/counts`, {
            headers: getAuthHeaders()
        });
        if (!response.ok) throw new Error('Failed to fetch report counts');
        return response.json();
    },

    // Admin: get single report
    async getReport(id) {
        const response = await fetch(`${API_BASE}/api/AdminReports/${id}`, {
            headers: getAuthHeaders()
        });
        if (!response.ok) throw new Error('Failed to fetch report');
        return response.json();
    },

    // Admin: review/resolve a report
    async reviewReport(id, data) {
        const response = await fetch(`${API_BASE}/api/AdminReports/${id}/review`, {
            method: 'PUT',
            headers: getAuthHeaders(),
            body: JSON.stringify(data)
        });
        if (!response.ok) throw new Error('Failed to update report');
        return response.json();
    },

    // Admin: bulk review
    async bulkReviewReports(data) {
        const response = await fetch(`${API_BASE}/api/AdminReports/bulk-review`, {
            method: 'PUT',
            headers: getAuthHeaders(),
            body: JSON.stringify(data)
        });
        if (!response.ok) throw new Error('Failed to bulk update reports');
        return response.json();
    }
};

export default reportsService;
