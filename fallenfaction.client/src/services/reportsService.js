import apiClient from './apiClient.js'

export const reportsService = {
    async createReport(data) {
        const response = await apiClient.post('/Reports', data)
        return response.data
    },

    async getMyReports() {
        const response = await apiClient.get('/Reports/my')
        return response.data
    },

    async getAdminReports({ status, targetType, reason, searchQuery, page = 1, pageSize = 20 } = {}) {
        const params = new URLSearchParams()
        if (status !== undefined && status !== null && status !== '') params.append('status', status)
        if (targetType !== undefined && targetType !== null && targetType !== '') params.append('targetType', targetType)
        if (reason !== undefined && reason !== null && reason !== '') params.append('reason', reason)
        if (searchQuery) params.append('searchQuery', searchQuery)
        params.append('page', page)
        params.append('pageSize', pageSize)
        const response = await apiClient.get(`/AdminReports?${params}`)
        return response.data
    },

    async getReportCounts() {
        const response = await apiClient.get('/AdminReports/counts')
        return response.data
    },

    async getReport(id) {
        const response = await apiClient.get(`/AdminReports/${id}`)
        return response.data
    },

    async reviewReport(id, data) {
        const response = await apiClient.put(`/AdminReports/${id}/review`, data)
        return response.data
    },

    async bulkReviewReports(data) {
        const response = await apiClient.put('/AdminReports/bulk-review', data)
        return response.data
    }
}

export default reportsService
