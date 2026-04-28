import apiClient from './apiClient.js'

export const notificationsService = {
    async getNotifications({ page = 1, pageSize = 20, unreadOnly = false } = {}) {
        const params = new URLSearchParams({ page, pageSize, unreadOnly })
        const response = await apiClient.get(`/Notifications?${params}`)
        return response.data
    },

    async getUnreadCount() {
        const response = await apiClient.get('/Notifications/unread-count')
        return response.data
    },

    async markAsRead(id) {
        await apiClient.put(`/Notifications/${id}/read`)
        return true
    },

    async markAllAsRead() {
        const response = await apiClient.put('/Notifications/read-all')
        return response.data
    },

    async dismiss(id) {
        await apiClient.put(`/Notifications/${id}/dismiss`)
        return true
    },

    async sendGlobal(data) {
        const response = await apiClient.post('/AdminNotifications/global', data)
        return response.data
    },

    async getGlobalNotifications() {
        const response = await apiClient.get('/AdminNotifications/global')
        return response.data
    },

    async deleteGlobal(id) {
        const response = await apiClient.delete(`/AdminNotifications/global/${id}`)
        return response.data
    },

    async updateGlobal(id, data) {
        const response = await apiClient.put(`/AdminNotifications/global/${id}`, data)
        return response.data
    }
}

export default notificationsService
