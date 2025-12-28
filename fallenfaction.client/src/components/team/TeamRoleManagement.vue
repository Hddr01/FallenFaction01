<!-- components/team/TeamRoleManagement.vue -->
<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex justify-between items-center">
      <div>
        <h3 class="text-lg font-medium text-[var(--color-heading)]">Role Management</h3>
        <p class="text-sm text-[var(--color-text)] opacity-75">Configure what each role can do in your team</p>
      </div>
      <button @click="showCreateRoleModal = true"
              class="inline-flex items-center px-4 py-2 bg-[var(--color-accent)] text-white rounded-md hover:bg-[var(--color-accent-hover)] transition-colors duration-200">
        <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path>
        </svg>
        Create Custom Role
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="flex items-center justify-center py-8">
      <div class="inline-flex items-center">
        <svg class="animate-spin -ml-1 mr-3 h-6 w-6 text-[var(--color-accent)]" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <span class="text-[var(--color-text)]">Loading permissions...</span>
      </div>
    </div>

    <!-- Default Roles -->
    <div v-else class="space-y-4">
      <h4 class="text-md font-medium text-[var(--color-heading)]">Default Roles</h4>

      <div class="grid gap-4">
        <div v-for="role in defaultRoles" :key="role.value"
             class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-lg p-6">
          <div class="flex items-center justify-between mb-4">
            <div class="flex items-center">
              <div :class="getRoleIconClass(role.value)"
                   class="w-10 h-10 rounded-lg flex items-center justify-center text-white font-bold mr-3">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path v-if="role.value === 0" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.031 9-11.622 0-1.042-.133-2.052-.382-3.016z"></path>
                  <path v-else-if="role.value === 1" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path>
                  <path v-else stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path>
                </svg>
              </div>
              <div>
                <h5 class="text-lg font-semibold text-[var(--color-heading)]">{{ role.name }}</h5>
                <p class="text-sm text-[var(--color-text)] opacity-75">{{ role.description }}</p>
              </div>
            </div>
            <button v-if="role.value !== 0"
                    @click="editRole(role)"
                    class="px-3 py-1 text-sm border border-[var(--color-border)] rounded-md hover:bg-[var(--color-background-mute)] transition-colors duration-200">
              Edit Permissions
            </button>
            <span v-else class="px-3 py-1 text-sm text-[var(--color-text)] opacity-60 bg-[var(--color-background-mute)] rounded-md">
              All Permissions
            </span>
          </div>

          <!-- Permission Grid -->
          <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3">
            <div v-for="permission in availablePermissions" :key="permission.name"
                 class="flex items-center p-2 rounded-md border"
                 :class="hasPermission(role, permission.name) ? 'bg-green-50 border-green-200' : 'bg-gray-50 border-gray-200'">
              <svg class="w-4 h-4 mr-2"
                   :class="hasPermission(role, permission.name) ? 'text-green-600' : 'text-gray-400'"
                   fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path v-if="hasPermission(role, permission.name)"
                      stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
                <path v-else
                      stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
              </svg>
              <span class="text-sm"
                    :class="hasPermission(role, permission.name) ? 'text-green-800 font-medium' : 'text-gray-600'">
                {{ permission.displayName }}
              </span>
            </div>
          </div>
        </div>
      </div>

      <!-- Custom Roles -->
      <div v-if="customRoles.length > 0" class="space-y-4">
        <div class="flex items-center justify-between">
          <h4 class="text-md font-medium text-[var(--color-heading)]">Custom Roles</h4>
          <span class="text-sm text-[var(--color-text)] opacity-60">{{ customRoles.length }} custom role(s)</span>
        </div>

        <div class="grid gap-4">
          <div v-for="role in customRoles" :key="role.id"
               class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-lg p-6">
            <div class="flex items-center justify-between mb-4">
              <div class="flex items-center">
                <div class="w-10 h-10 bg-purple-500 rounded-lg flex items-center justify-center text-white font-bold mr-3">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z"></path>
                  </svg>
                </div>
                <div>
                  <h5 class="text-lg font-semibold text-[var(--color-heading)]">{{ role.name }}</h5>
                  <p class="text-sm text-[var(--color-text)] opacity-75">{{ role.description }}</p>
                </div>
              </div>
              <div class="flex space-x-2">
                <button @click="editCustomRole(role)"
                        class="px-3 py-1 text-sm border border-[var(--color-border)] rounded-md hover:bg-[var(--color-background-mute)] transition-colors duration-200">
                  Edit
                </button>
                <button @click="deleteCustomRole(role.id)"
                        class="px-3 py-1 text-sm border border-red-300 text-red-600 rounded-md hover:bg-red-50 transition-colors duration-200">
                  Delete
                </button>
              </div>
            </div>

            <!-- Permission Grid for Custom Role -->
            <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3">
              <div v-for="permission in availablePermissions" :key="permission.name"
                   class="flex items-center p-2 rounded-md border"
                   :class="role.permissions.includes(permission.name) ? 'bg-green-50 border-green-200' : 'bg-gray-50 border-gray-200'">
                <svg class="w-4 h-4 mr-2"
                     :class="role.permissions.includes(permission.name) ? 'text-green-600' : 'text-gray-400'"
                     fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path v-if="role.permissions.includes(permission.name)"
                        stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
                  <path v-else
                        stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                </svg>
                <span class="text-sm"
                      :class="role.permissions.includes(permission.name) ? 'text-green-800 font-medium' : 'text-gray-600'">
                  {{ permission.displayName }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Create/Edit Role Modal -->
    <div v-if="showCreateRoleModal || editingRole"
         class="fixed inset-0 bg-gray-600 bg-opacity-50 z-50 flex items-center justify-center p-4">
      <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg shadow-xl w-full max-w-2xl mx-auto max-h-[90vh] overflow-y-auto">
        <div class="p-6">
          <div class="flex items-center justify-between mb-6">
            <h3 class="text-lg font-medium text-[var(--color-heading)]">
              {{ editingRole ? 'Edit Role' : 'Create Custom Role' }}
            </h3>
            <button @click="closeModal" class="text-[var(--color-text)] opacity-50 hover:opacity-75 transition-opacity duration-200">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
              </svg>
            </button>
          </div>

          <form @submit.prevent="saveRole" class="space-y-6">
            <!-- Role Info -->
            <div class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Role Name</label>
                <input v-model="roleForm.name"
                       type="text"
                       class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] focus:border-[var(--color-accent)] transition-colors duration-200"
                       placeholder="e.g., Translator, Editor, Quality Checker"
                       required />
              </div>

              <div>
                <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Description</label>
                <textarea v-model="roleForm.description"
                          class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] focus:border-[var(--color-accent)] transition-colors duration-200 resize-vertical"
                          rows="3"
                          placeholder="Describe what this role is responsible for"></textarea>
              </div>
            </div>

            <!-- Permissions -->
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-4">Permissions</label>
              <div class="space-y-4">
                <div v-for="category in permissionCategories" :key="category.name" class="space-y-2">
                  <h5 class="text-sm font-medium text-[var(--color-heading)]">{{ category.displayName }}</h5>
                  <div class="grid grid-cols-1 md:grid-cols-2 gap-2 pl-4">
                    <label v-for="permission in category.permissions" :key="permission.name"
                           class="flex items-center p-2 rounded-md hover:bg-[var(--color-background-mute)] transition-colors duration-200 cursor-pointer">
                      <input type="checkbox"
                             :value="permission.name"
                             v-model="roleForm.permissions"
                             class="w-4 h-4 text-[var(--color-accent)] border-[var(--color-border)] rounded focus:ring-[var(--color-accent)] focus:ring-2" />
                      <span class="ml-2 text-sm text-[var(--color-text)]">{{ permission.displayName }}</span>
                      <span class="ml-auto text-xs text-[var(--color-text)] opacity-60">{{ permission.description }}</span>
                    </label>
                  </div>
                </div>
              </div>
            </div>

            <!-- Actions -->
            <div class="flex justify-end space-x-3 pt-6 border-t border-[var(--color-border)]">
              <button type="button"
                      @click="closeModal"
                      class="px-4 py-2 border border-[var(--color-border)] rounded-md text-sm font-medium text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[var(--color-border-hover)] transition-all duration-200">
                Cancel
              </button>
              <button type="submit"
                      :disabled="saveLoading"
                      class="px-4 py-2 border border-transparent rounded-md text-sm font-medium text-white bg-[var(--color-accent)] hover:bg-[var(--color-accent-hover)] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[var(--color-accent)] disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
                <svg v-if="saveLoading" class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                {{ saveLoading ? 'Saving...' : (editingRole ? 'Update Role' : 'Create Role') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- Success/Error Messages -->
    <div v-if="message.text"
         class="fixed top-4 right-4 max-w-sm w-full bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg shadow-lg z-50 transition-all duration-300"
         :class="{
           'border-green-500': message.type === 'success',
           'border-red-500': message.type === 'error'
         }">
      <div class="p-4">
        <div class="flex">
          <div class="flex-shrink-0">
            <svg v-if="message.type === 'success'" class="h-5 w-5 text-green-400" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
            </svg>
            <svg v-else class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="ml-3 w-0 flex-1">
            <p class="text-sm font-medium text-[var(--color-text)]">
              {{ message.text }}
            </p>
          </div>
          <div class="ml-4 flex-shrink-0 flex">
            <button @click="message.text = ''"
                    class="rounded-md inline-flex text-[var(--color-text)] opacity-50 hover:opacity-75 focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] transition-all duration-200">
              <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { teamService } from '../../services/teamService'

// Props
const props = defineProps({
  teamId: {
    type: Number,
    required: true
  }
})

// Reactive data
const loading = ref(false)
const saveLoading = ref(false)
const showCreateRoleModal = ref(false)
const editingRole = ref(null)
const customRoles = ref([])

const message = reactive({
  text: '',
  type: ''
})

const roleForm = reactive({
  name: '',
  description: '',
  permissions: []
})

// Default roles configuration
const defaultRoles = ref([
  {
    name: 'Admin',
    value: 0,
    description: 'Full team management permissions',
    permissions: [] // Will be populated with all permissions
  },
  {
    name: 'Member',
    value: 1,
    description: 'Can contribute content and moderate',
    permissions: ['CanAddTitle', 'CanEditTitle', 'CanAddChapter', 'CanEditChapter']
  },
  {
    name: 'Viewer',
    value: 2,
    description: 'Read-only access to team content',
    permissions: []
  }
])

// Available permissions
const availablePermissions = ref([
  { name: 'CanAddTitle', displayName: 'Add Titles', description: 'Create new manga titles' },
  { name: 'CanEditTitle', displayName: 'Edit Titles', description: 'Modify existing titles' },
  { name: 'CanDeleteTitle', displayName: 'Delete Titles', description: 'Remove titles from team' },
  { name: 'CanAddChapter', displayName: 'Add Chapters', description: 'Upload new chapters' },
  { name: 'CanEditChapter', displayName: 'Edit Chapters', description: 'Modify existing chapters' },
  { name: 'CanDeleteChapter', displayName: 'Delete Chapters', description: 'Remove chapters' },
  { name: 'CanAddMember', displayName: 'Add Members', description: 'Invite new team members' },
  { name: 'CanRemoveMember', displayName: 'Remove Members', description: 'Remove team members' },
  { name: 'CanViewAnalytics', displayName: 'View Analytics', description: 'Access team statistics' }
])

// Categorized permissions for better UX
const permissionCategories = computed(() => [
  {
    name: 'content',
    displayName: 'Content Management',
    permissions: availablePermissions.value.filter(p =>
      p.name.includes('Title') || p.name.includes('Chapter')
    )
  },
  {
    name: 'members',
    displayName: 'Member Management',
    permissions: availablePermissions.value.filter(p =>
      p.name.includes('Member')
    )
  },
  {
    name: 'analytics',
    displayName: 'Analytics & Reports',
    permissions: availablePermissions.value.filter(p =>
      p.name.includes('Analytics')
    )
  }
])

// Methods
const showMessage = (text, type) => {
  message.text = text
  message.type = type
  setTimeout(() => {
    message.text = ''
  }, type === 'success' ? 3000 : 5000)
}

const getRoleIconClass = (roleValue) => {
  switch (roleValue) {
    case 0: return 'bg-red-500'
    case 1: return 'bg-blue-500'
    case 2: return 'bg-gray-500'
    default: return 'bg-purple-500'
  }
}

const hasPermission = (role, permissionName) => {
  if (role.value === 0) return true // Admin has all permissions
  return role.permissions.includes(permissionName)
}

const editRole = (role) => {
  editingRole.value = role
  roleForm.name = role.name
  roleForm.description = role.description
  roleForm.permissions = [...role.permissions]
}

const editCustomRole = (role) => {
  editingRole.value = role
  roleForm.name = role.name
  roleForm.description = role.description
  roleForm.permissions = [...role.permissions]
}

const closeModal = () => {
  showCreateRoleModal.value = false
  editingRole.value = null
  roleForm.name = ''
  roleForm.description = ''
  roleForm.permissions = []
}

const saveRole = async () => {
  saveLoading.value = true

  try {
    if (editingRole.value) {
      // Update existing role
      if (editingRole.value.id) {
        // Custom role
        const roleIndex = customRoles.value.findIndex(r => r.id === editingRole.value.id)
        if (roleIndex !== -1) {
          customRoles.value[roleIndex] = {
            ...editingRole.value,
            name: roleForm.name,
            description: roleForm.description,
            permissions: [...roleForm.permissions]
          }
        }
      } else {
        // Default role
        const roleIndex = defaultRoles.value.findIndex(r => r.value === editingRole.value.value)
        if (roleIndex !== -1) {
          defaultRoles.value[roleIndex].permissions = [...roleForm.permissions]
        }
      }
      showMessage('Role updated successfully!', 'success')
    } else {
      // Create new custom role
      const newRole = {
        id: Date.now(), // In real app, this would come from backend
        name: roleForm.name,
        description: roleForm.description,
        permissions: [...roleForm.permissions],
        teamId: props.teamId,
        isCustom: true
      }
      customRoles.value.push(newRole)
      showMessage('Custom role created successfully!', 'success')
    }

    closeModal()
  } catch (error) {
    console.error('Error saving role:', error)
    showMessage('Failed to save role', 'error')
  } finally {
    saveLoading.value = false
  }
}

const deleteCustomRole = async (roleId) => {
  if (!confirm('Are you sure you want to delete this custom role? Members with this role will be converted to Viewers.')) {
    return
  }

  try {
    const roleIndex = customRoles.value.findIndex(r => r.id === roleId)
    if (roleIndex !== -1) {
      customRoles.value.splice(roleIndex, 1)
      showMessage('Custom role deleted successfully!', 'success')
    }
  } catch (error) {
    console.error('Error deleting custom role:', error)
    showMessage('Failed to delete role', 'error')
  }
}

const loadTeamRoles = async () => {
  loading.value = true

  try {
    // Load custom roles for this team
    // This would be an API call in real implementation
    await new Promise(resolve => setTimeout(resolve, 1000)) // Simulate API call

    // For now, we'll use mock data
    customRoles.value = [
      {
        id: 1,
        name: 'Translator',
        description: 'Responsible for translating chapters',
        permissions: ['CanAddChapter', 'CanEditChapter'],
        teamId: props.teamId,
        isCustom: true
      },
      {
        id: 2,
        name: 'Editor',
        description: 'Reviews and edits translated content',
        permissions: ['CanEditChapter', 'CanViewAnalytics'],
        teamId: props.teamId,
        isCustom: true
      }
    ]

    // Set admin to have all permissions
    defaultRoles.value[0].permissions = availablePermissions.value.map(p => p.name)

  } catch (error) {
    console.error('Error loading team roles:', error)
    showMessage('Failed to load team roles', 'error')
  } finally {
    loading.value = false
  }
}

// Lifecycle
onMounted(() => {
  loadTeamRoles()
})
</script>

<style scoped>
  /* Custom focus ring offset color */
  .focus\:ring-offset-2:focus {
    --tw-ring-offset-width: 2px;
    --tw-ring-offset-color: var(--color-background);
  }
</style>
