const API_BASE = import.meta.env.VITE_API_URL || '';

const getAuthHeaders = () => {
    const token = localStorage.getItem('authToken');
    return {
        'Content-Type': 'application/json',
        ...(token ? { 'Authorization': `Bearer ${token}` } : {})
    };
};

export const notificationsService = {
    // User: get notifications
    async getNotifications({ page = 1, pageSize = 20, unreadOnly = false } = {}) {
        const params = new URLSearchParams({ page, pageSize, unreadOnly });
        const response = await fetch(`${API_BASE}/api/Notifications?${params}`, {
            headers: getAuthHeaders()
        });
        if (!response.ok) throw new Error('Failed to fetch notifications');
        return response.json();
    },

    // User: get unread count
    async getUnreadCount() {
        const response = await fetch(`${API_BASE}/api/Notifications/unread-count`, {
            headers: getAuthHeaders()
        });
        if (!response.ok) throw new Error('Failed to fetch unread count');
        return response.json();
    },

    // User: mark one as read
    async markAsRead(id) {
        const response = await fetch(`${API_BASE}/api/Notifications/${id}/read`, {
            method: 'PUT',
            headers: getAuthHeaders()
        });
        if (!response.ok) throw new Error('Failed to mark notification as read');
        return true;
    },

    // User: mark all as read
    async markAllAsRead() {
        const response = await fetch(`${API_BASE}/api/Notifications/read-all`, {
            method: 'PUT',
            headers: getAuthHeaders()
        });
        if (!response.ok) throw new Error('Failed to mark all as read');
        return response.json();
    },

    // User: dismiss notification
    async dismiss(id) {
        const response = await fetch(`${API_BASE}/api/Notifications/${id}/dismiss`, {
            method: 'PUT',
            headers: getAuthHeaders()
        });
        if (!response.ok) throw new Error('Failed to dismiss notification');
        return true;
    },

    // Admin: send global notification
    async sendGlobal(data) {
        const response = await fetch(`${API_BASE}/api/AdminNotifications/global`, {
            method: 'POST',
            headers: getAuthHeaders(),
            body: JSON.stringify(data)
        });
        if (!response.ok) throw new Error('Failed to send notification');
        return response.json();
    },

    // Admin: get all global notifications
    async getGlobalNotifications() {
        const response = await fetch(`${API_BASE}/api/AdminNotifications/global`, {
            headers: getAuthHeaders()
        });
        if (!response.ok) throw new Error('Failed to fetch global notifications');
        return response.json();
    },

    // Admin: delete global notification
    async deleteGlobal(id) {
        const response = await fetch(`${API_BASE}/api/AdminNotifications/global/${id}`, {
            method: 'DELETE',
            headers: getAuthHeaders()
        });
        if (!response.ok) throw new Error('Failed to delete notification');
        return response.json();
    },

    // Admin: update global notification
    async updateGlobal(id, data) {
        const response = await fetch(`${API_BASE}/api/AdminNotifications/global/${id}`, {
            method: 'PUT',
            headers: getAuthHeaders(),
            body: JSON.stringify(data)
        });
        if (!response.ok) throw new Error('Failed to update notification');
        return response.json();
    }
};

export default notificationsService;
