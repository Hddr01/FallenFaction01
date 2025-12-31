<template>
  <div class="container mx-auto px-4 py-8 max-w-7xl">
    <!-- Loading State -->
    <div v-if="loading" class="flex items-center justify-center min-h-[400px]">
      <div class="text-center">
        <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
        <p class="text-muted-foreground">Loading team details...</p>
      </div>
    </div>

    <!-- Error State -->
    <Alert v-else-if="error" variant="destructive" class="mb-6">
      <AlertCircle class="h-4 w-4" />
      <AlertTitle>Error</AlertTitle>
      <AlertDescription>{{ error }}</AlertDescription>
    </Alert>

    <!-- Team Details -->
    <div v-else-if="team">
      <!-- Background Image Section -->
      <Card class="overflow-hidden mb-6">
        <div class="relative h-64 bg-gradient-to-br from-primary/10 to-primary/5">
          <div v-if="team.backgroundImagePath" class="absolute inset-0">
            <img :src="getImageUrl(team.backgroundImagePath)"
                 :alt="`${team.name} background`"
                 class="w-full h-full object-cover" />
            <div class="absolute inset-0 bg-gradient-to-t from-background via-background/50 to-transparent" />
          </div>
          <div v-else class="flex items-center justify-center h-full">
            <Empty description="No background image"
                   class="text-muted-foreground/50">
              <template #icon>
                <ImageIcon class="h-16 w-16" />
              </template>
            </Empty>
          </div>

          <!-- Background Edit Button -->
          <div v-if="canEdit" class="absolute top-4 right-4 flex gap-2">
            <Button size="sm"
                    variant="secondary"
                    @click="triggerBackgroundUpload"
                    :disabled="uploadingBackground">
              <Upload v-if="!uploadingBackground" class="h-4 w-4 mr-2" />
              <Loader2 v-else class="h-4 w-4 mr-2 animate-spin" />
              {{ team.backgroundImagePath ? 'Change' : 'Upload' }} Background
            </Button>
            <Button v-if="team.backgroundImagePath"
                    size="sm"
                    variant="destructive"
                    @click="deleteBackground"
                    :disabled="uploadingBackground">
              <Trash2 class="h-4 w-4" />
            </Button>
          </div>

          <input ref="backgroundInput"
                 type="file"
                 accept="image/*"
                 class="hidden"
                 @change="handleBackgroundUpload" />
        </div>

        <Separator />

        <CardContent class="p-8 relative">
          <!-- Avatar and Team Info -->
          <div class="flex items-start gap-6 -mt-20">
            <!-- Avatar Section -->
            <div class="relative">
              <Avatar class="h-32 w-32 border-4 border-background shadow-xl">
                <AvatarImage v-if="team.avatarImagePath"
                             :src="getImageUrl(team.avatarImagePath)"
                             :alt="team.name" />
                <AvatarFallback class="bg-primary/10 text-4xl">
                  <Empty v-if="!team.avatarImagePath" description="">
                    <template #icon>
                      <Users class="h-16 w-16 text-muted-foreground" />
                    </template>
                  </Empty>
                </AvatarFallback>
              </Avatar>

              <!-- Avatar Edit Buttons -->
              <div v-if="canEdit" class="absolute -bottom-2 -right-2 flex gap-1">
                <Button size="icon"
                        variant="secondary"
                        class="h-8 w-8 rounded-full shadow-lg"
                        @click="triggerAvatarUpload"
                        :disabled="uploadingAvatar">
                  <Upload v-if="!uploadingAvatar" class="h-4 w-4" />
                  <Loader2 v-else class="h-4 w-4 animate-spin" />
                </Button>
                <Button v-if="team.avatarImagePath"
                        size="icon"
                        variant="destructive"
                        class="h-8 w-8 rounded-full shadow-lg"
                        @click="deleteAvatar"
                        :disabled="uploadingAvatar">
                  <Trash2 class="h-3 w-3" />
                </Button>
              </div>

              <input ref="avatarInput"
                     type="file"
                     accept="image/*"
                     class="hidden"
                     @change="handleAvatarUpload" />
            </div>

            <!-- Team Header -->
            <div class="flex-1 mt-12">
              <div class="flex items-start justify-between">
                <div>
                  <div class="flex items-center gap-3 mb-2">
                    <h1 class="text-3xl font-bold">{{ team.name }}</h1>
                    <Badge v-if="team.isCreator" variant="default">
                      <Crown class="h-3 w-3 mr-1" />
                      Creator
                    </Badge>
                    <Badge v-else-if="team.userRole !== undefined" :variant="getRoleBadgeVariant(team.userRole)">
                      {{ getRoleName(team.userRole) }}
                    </Badge>
                  </div>
                  <p class="text-muted-foreground">{{ team.description }}</p>
                </div>

                <!-- Action Buttons -->
                <div v-if="canEdit" class="flex gap-2">
                  <Button @click="isEditDialogOpen = true">
                    <Settings class="h-4 w-4 mr-2" />
                    Edit Team
                  </Button>
                </div>
              </div>

              <!-- Team Stats -->
              <div class="flex items-center gap-6 mt-4">
                <div class="flex items-center gap-2 text-sm">
                  <Users class="h-4 w-4 text-muted-foreground" />
                  <span class="font-medium">{{ team.members?.length || 0 }}</span>
                  <span class="text-muted-foreground">Members</span>
                </div>
                <div class="flex items-center gap-2 text-sm">
                  <Book class="h-4 w-4 text-muted-foreground" />
                  <span class="font-medium">{{ team.titles?.length || 0 }}</span>
                  <span class="text-muted-foreground">Titles</span>
                </div>
                <div class="flex items-center gap-2 text-sm">
                  <Calendar class="h-4 w-4 text-muted-foreground" />
                  <span class="text-muted-foreground">Created {{ formatDate(team.createdDate) }}</span>
                </div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      <Separator class="my-8" />

      <!-- Tabs Section -->
      <Tabs default-value="members" class="w-full">
        <TabsList class="grid w-full grid-cols-3">
          <TabsTrigger value="members">
            <Users class="h-4 w-4 mr-2" />
            Members
          </TabsTrigger>
          <TabsTrigger value="titles">
            <Book class="h-4 w-4 mr-2" />
            Titles
          </TabsTrigger>
          <TabsTrigger value="permissions" v-if="canManageRoles">
            <Shield class="h-4 w-4 mr-2" />
            Permissions
          </TabsTrigger>
        </TabsList>

        <!-- Members Tab -->
        <TabsContent value="members">
          <Card>
            <CardHeader>
              <CardTitle>Team Members</CardTitle>
              <CardDescription>
                Manage team members and their roles
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div class="space-y-4">
                <div v-for="member in team.members"
                     :key="member.userId"
                     class="flex items-center justify-between p-4 border rounded-lg">
                  <div class="flex items-center gap-3">
                    <Avatar>
                      <AvatarFallback>
                        {{ member.username?.substring(0, 2).toUpperCase() }}
                      </AvatarFallback>
                    </Avatar>
                    <div>
                      <p class="font-medium">{{ member.username }}</p>
                      <p class="text-sm text-muted-foreground">
                        Joined {{ formatDate(member.joinedDate) }}
                      </p>
                    </div>
                  </div>

                  <div class="flex items-center gap-2">
                    <Badge :variant="getRoleBadgeVariant(member.role)">
                      {{ getRoleName(member.role) }}
                    </Badge>

                    <Button v-if="canManageRoles && !isCreator(member.userId)"
                            size="sm"
                            variant="ghost"
                            @click="openRoleDialog(member)">
                      <Settings class="h-4 w-4" />
                    </Button>
                  </div>
                </div>

                <Empty v-if="!team.members || team.members.length === 0">
                  <template #icon>
                    <Users class="h-12 w-12" />
                  </template>
                  <template #description>
                    No members yet
                  </template>
                </Empty>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <!-- Titles Tab -->
        <TabsContent value="titles">
          <Card>
            <CardHeader>
              <CardTitle>Team Titles</CardTitle>
              <CardDescription>
                Manga titles managed by this team
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
                <div v-for="title in team.titles"
                     :key="title.id"
                     class="group cursor-pointer"
                     @click="navigateToTitle(title.id)">
                  <div class="aspect-[2/3] rounded-lg overflow-hidden border bg-muted mb-2">
                    <img v-if="title.coverImagePath"
                         :src="getImageUrl(title.coverImagePath)"
                         :alt="title.englishTitle"
                         class="w-full h-full object-cover group-hover:scale-105 transition-transform" />
                    <div v-else class="flex items-center justify-center h-full">
                      <Book class="h-12 w-12 text-muted-foreground" />
                    </div>
                  </div>
                  <p class="text-sm font-medium line-clamp-2">{{ title.englishTitle }}</p>
                </div>

                <Empty v-if="!team.titles || team.titles.length === 0">
                  <template #icon>
                    <Book class="h-12 w-12" />
                  </template>
                  <template #description>
                    No titles yet
                  </template>
                </Empty>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <!-- Permissions Tab -->
        <TabsContent value="permissions" v-if="canManageRoles">
          <TeamRoleManagement :teamId="teamId" />
        </TabsContent>
      </Tabs>

      <!-- Edit Team Dialog -->
      <Dialog v-model:open="isEditDialogOpen">
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit Team</DialogTitle>
            <DialogDescription>
              Update your team's information
            </DialogDescription>
          </DialogHeader>

          <div class="space-y-4 py-4">
            <div class="space-y-2">
              <Label for="name">Team Name</Label>
              <Input id="name"
                     v-model="editForm.name"
                     placeholder="Enter team name" />
            </div>

            <div class="space-y-2">
              <Label for="description">Description</Label>
              <Textarea id="description"
                        v-model="editForm.description"
                        placeholder="Enter team description"
                        rows="4" />
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" @click="isEditDialogOpen = false">
              Cancel
            </Button>
            <Button @click="updateTeam" :disabled="updating">
              <Loader2 v-if="updating" class="h-4 w-4 mr-2 animate-spin" />
              Save Changes
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, onMounted } from 'vue';
  import { useRouter } from 'vue-router';
  import { toast } from 'sonner';
  import { teamService } from '@/services/teamService';
  import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
  import { Avatar, AvatarImage, AvatarFallback } from '@/components/ui/avatar';
  import { Badge } from '@/components/ui/badge';
  import { Button } from '@/components/ui/button';
  import { Separator } from '@/components/ui/separator';
  import { Empty } from '@/components/ui/empty';
  import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
  import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/components/ui/dialog';
  import { Label } from '@/components/ui/label';
  import { Input } from '@/components/ui/input';
  import { Textarea } from '@/components/ui/textarea';
  import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
  import TeamRoleManagement from './TeamRoleManagement.vue';
  import {
    Users, Book, Calendar, Crown, Settings, Shield,
    AlertCircle, Upload, Trash2, Loader2, ImageIcon
  } from 'lucide-vue-next';

  const props = defineProps({
    teamId: {
      type: Number,
      required: true
    }
  });

  const router = useRouter();

  const loading = ref(true);
  const error = ref(null);
  const team = ref(null);
  const isEditDialogOpen = ref(false);
  const updating = ref(false);
  const uploadingAvatar = ref(false);
  const uploadingBackground = ref(false);

  const avatarInput = ref(null);
  const backgroundInput = ref(null);

  const editForm = ref({
    name: '',
    description: ''
  });

  const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5064';

  const canEdit = computed(() => {
    return team.value?.userRole === 0 || team.value?.isCreator;
  });

  const canManageRoles = computed(() => {
    return team.value?.userRole === 0 || team.value?.isCreator;
  });

  const getImageUrl = (path) => {
    if (!path) return '';
    if (path.startsWith('http')) return path;
    // Remove /api from base URL for static files
    const baseUrl = API_BASE_URL.replace('/api', '');
    return `${baseUrl}${path}`;
  };

  const loadTeamDetails = async () => {
    loading.value = true;
    error.value = null;

    try {
      const result = await teamService.getTeamById(props.teamId);

      if (result.success) {
        team.value = result.data;
        editForm.value = {
          name: result.data.name,
          description: result.data.description
        };
      } else {
        error.value = result.error;
      }
    } catch (err) {
      error.value = 'Failed to load team details';
      console.error(err);
    } finally {
      loading.value = false;
    }
  };

  const triggerAvatarUpload = () => {
    avatarInput.value?.click();
  };

  const triggerBackgroundUpload = () => {
    backgroundInput.value?.click();
  };

  const handleAvatarUpload = async (event) => {
    const file = event.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      toast.error('Invalid file type', {
        description: 'Please upload an image file'
      });
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      toast.error('File too large', {
        description: 'Avatar image must be less than 5MB'
      });
      return;
    }

    uploadingAvatar.value = true;

    try {
      const formData = new FormData();
      formData.append('file', file);

      const token = localStorage.getItem('authToken');
      const response = await fetch(`${API_BASE_URL}/team/${props.teamId}/upload-avatar`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`
        },
        body: formData
      });

      const data = await response.json();

      if (response.ok) {
        toast.success('Success', {
          description: 'Avatar uploaded successfully'
        });
        await loadTeamDetails();
      } else {
        throw new Error(data.message || 'Upload failed');
      }
    } catch (err) {
      toast.error('Upload failed', {
        description: err.message
      });
    } finally {
      uploadingAvatar.value = false;
      if (avatarInput.value) {
        avatarInput.value.value = '';
      }
    }
  };

  const handleBackgroundUpload = async (event) => {
    const file = event.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      toast.error('Invalid file type', {
        description: 'Please upload an image file'
      });
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      toast.error('File too large', {
        description: 'Background image must be less than 10MB'
      });
      return;
    }

    uploadingBackground.value = true;

    try {
      const formData = new FormData();
      formData.append('file', file);

      const token = localStorage.getItem('authToken');
      const response = await fetch(`${API_BASE_URL}/team/${props.teamId}/upload-background`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`
        },
        body: formData
      });

      const data = await response.json();

      if (response.ok) {
        toast.success('Success', {
          description: 'Background uploaded successfully'
        });
        await loadTeamDetails();
      } else {
        throw new Error(data.message || 'Upload failed');
      }
    } catch (err) {
      toast.error('Upload failed', {
        description: err.message
      });
    } finally {
      uploadingBackground.value = false;
      if (backgroundInput.value) {
        backgroundInput.value.value = '';
      }
    }
  };

  const deleteAvatar = async () => {
    if (!confirm('Are you sure you want to delete the team avatar?')) return;

    try {
      const token = localStorage.getItem('authToken');
      const response = await fetch(`${API_BASE_URL}/team/${props.teamId}/avatar`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (response.ok) {
        toast.success('Success', {
          description: 'Avatar deleted successfully'
        });
        await loadTeamDetails();
      } else {
        throw new Error('Delete failed');
      }
    } catch (err) {
      toast.error('Delete failed', {
        description: err.message
      });
    }
  };

  const deleteBackground = async () => {
    if (!confirm('Are you sure you want to delete the team background?')) return;

    try {
      const token = localStorage.getItem('authToken');
      const response = await fetch(`${API_BASE_URL}/team/${props.teamId}/background`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (response.ok) {
        toast.success('Success', {
          description: 'Background deleted successfully'
        });
        await loadTeamDetails();
      } else {
        throw new Error('Delete failed');
      }
    } catch (err) {
      toast.error('Delete failed', {
        description: err.message
      });
    }
  };

  const updateTeam = async () => {
    updating.value = true;

    try {
      const result = await teamService.updateTeam(props.teamId, editForm.value);

      if (result.success) {
        toast.success('Success', {
          description: 'Team updated successfully'
        });
        isEditDialogOpen.value = false;
        await loadTeamDetails();
      } else {
        throw new Error(result.error);
      }
    } catch (err) {
      toast.error('Update failed', {
        description: err.message
      });
    } finally {
      updating.value = false;
    }
  };

  const getRoleName = (role) => {
    const roles = { 0: 'Admin', 1: 'Member', 2: 'Viewer' };
    return roles[role] || 'Unknown';
  };

  const getRoleBadgeVariant = (role) => {
    const variants = { 0: 'destructive', 1: 'default', 2: 'secondary' };
    return variants[role] || 'secondary';
  };

  const isCreator = (userId) => {
    return team.value?.creatorId === userId;
  };

  const formatDate = (date) => {
    if (!date) return 'Unknown';
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  const navigateToTitle = (titleId) => {
    router.push(`/title/${titleId}`);
  };

  const openRoleDialog = (member) => {
    console.log('Open role dialog for', member);
  };

  onMounted(() => {
    loadTeamDetails();
  });
</script>
