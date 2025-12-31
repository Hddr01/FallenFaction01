<template>
  <Card class="group hover:shadow-lg transition-all duration-300 overflow-hidden">
    <!-- Background Image Section -->
    <div class="relative h-32 overflow-hidden bg-gradient-to-br from-primary/10 to-primary/5">
      <div v-if="team.backgroundImagePath" class="absolute inset-0">
        <img :src="getImageUrl(team.backgroundImagePath)"
             :alt="`${team.name} background`"
             class="w-full h-full object-cover"
             @error="handleBackgroundError" />
        <div class="absolute inset-0 bg-gradient-to-t from-background/80 to-transparent" />
      </div>
      <div v-else class="flex items-center justify-center h-full">
        <Empty description="No background"
               class="text-muted-foreground/50">
          <template #icon>
            <ImageIcon class="h-12 w-12" />
          </template>
        </Empty>
      </div>
    </div>

    <Separator />

    <CardContent class="p-6 relative -mt-12">
      <!-- Avatar Section -->
      <div class="flex items-start gap-4 mb-4">
        <div class="relative">
          <Avatar class="h-20 w-20 border-4 border-background shadow-lg">
            <AvatarImage v-if="team.avatarImagePath"
                         :src="getImageUrl(team.avatarImagePath)"
                         :alt="team.name" />
            <AvatarFallback v-else class="bg-primary/10">
              <Empty description="" class="scale-75">
                <template #icon>
                  <Users class="h-8 w-8 text-muted-foreground" />
                </template>
              </Empty>
            </AvatarFallback>
          </Avatar>

          <!-- Role Badge -->
          <Badge v-if="userRole !== undefined"
                 :variant="getRoleBadgeVariant(userRole)"
                 class="absolute -bottom-1 -right-1 text-xs">
            {{ getRoleName(userRole) }}
          </Badge>
        </div>

        <div class="flex-1 mt-8">
          <div class="flex items-start justify-between">
            <div>
              <h3 class="text-xl font-bold group-hover:text-primary transition-colors">
                {{ team.name }}
              </h3>
              <p class="text-sm text-muted-foreground mt-1 line-clamp-2">
                {{ team.description }}
              </p>
            </div>
          </div>
        </div>
      </div>

      <Separator class="my-4" />

      <!-- Team Stats -->
      <div class="flex items-center justify-between text-sm">
        <div class="flex items-center gap-4">
          <div class="flex items-center gap-1 text-muted-foreground">
            <Users class="h-4 w-4" />
            <span>{{ team.memberCount || 0 }} members</span>
          </div>
          <div v-if="team.titleCount !== undefined" class="flex items-center gap-1 text-muted-foreground">
            <Book class="h-4 w-4" />
            <span>{{ team.titleCount }} titles</span>
          </div>
        </div>

        <div class="flex items-center gap-2">
          <Button variant="ghost"
                  size="sm"
                  @click.stop="$emit('view-team', team.id)">
            View Details
            <ChevronRight class="h-4 w-4 ml-1" />
          </Button>
        </div>
      </div>

      <!-- Created Date -->
      <div class="text-xs text-muted-foreground mt-3">
        Created {{ formatDate(team.createdDate) }}
      </div>
    </CardContent>
  </Card>
</template>

<script setup>
  import { computed } from 'vue';
  import { Card, CardContent } from '@/components/ui/card';
  import { Avatar, AvatarImage, AvatarFallback } from '@/components/ui/avatar';
  import { Badge } from '@/components/ui/badge';
  import { Button } from '@/components/ui/button';
  import { Separator } from '@/components/ui/separator';
  import { Empty } from '@/components/ui/empty';
  import { Users, Book, ChevronRight, ImageIcon } from 'lucide-vue-next';

  const props = defineProps({
    team: {
      type: Object,
      required: true
    },
    userRole: {
      type: Number,
      default: undefined
    }
  });

  defineEmits(['view-team']);

  const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5064';

  const getImageUrl = (path) => {
    if (!path) return '';
    if (path.startsWith('http')) return path;
    return `${API_BASE_URL}${path}`;
  };

  const handleBackgroundError = (event) => {
    event.target.style.display = 'none';
  };

  const getRoleName = (role) => {
    const roles = {
      0: 'Admin',
      1: 'Member',
      2: 'Viewer'
    };
    return roles[role] || 'Unknown';
  };

  const getRoleBadgeVariant = (role) => {
    const variants = {
      0: 'destructive',
      1: 'default',
      2: 'secondary'
    };
    return variants[role] || 'secondary';
  };

  const formatDate = (date) => {
    if (!date) return 'Unknown';
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };
</script>
