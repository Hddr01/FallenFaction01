<template>
  <div class="container mx-auto px-4 py-8 max-w-7xl">
    <!-- Header -->
    <div class="flex items-center justify-between mb-8">
      <div>
        <h1 class="text-4xl font-bold">Teams</h1>
        <p class="text-muted-foreground mt-2">
          Browse and join translation teams
        </p>
      </div>

      <Button @click="navigateToCreateTeam">
        <Plus class="h-4 w-4 mr-2" />
        Create Team
      </Button>
    </div>

    <!-- Search and Filters -->
    <Card class="mb-8">
      <CardContent class="p-6">
        <div class="flex gap-4">
          <div class="flex-1">
            <div class="relative">
              <Search class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input v-model="searchQuery"
                     placeholder="Search teams..."
                     class="pl-10"
                     @input="handleSearch" />
            </div>
          </div>

          <Select v-model="sortBy">
            <SelectTrigger class="w-[180px]">
              <SelectValue placeholder="Sort by" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="newest">Newest First</SelectItem>
              <SelectItem value="oldest">Oldest First</SelectItem>
              <SelectItem value="members">Most Members</SelectItem>
              <SelectItem value="titles">Most Titles</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </CardContent>
    </Card>

    <!-- Loading State -->
    <div v-if="loading" class="flex items-center justify-center min-h-[400px]">
      <div class="text-center">
        <Loader2 class="h-12 w-12 animate-spin text-primary mx-auto mb-4" />
        <p class="text-muted-foreground">Loading teams...</p>
      </div>
    </div>

    <!-- Error State -->
    <Alert v-else-if="error" variant="destructive" class="mb-6">
      <AlertCircle class="h-4 w-4" />
      <AlertTitle>Error</AlertTitle>
      <AlertDescription>{{ error }}</AlertDescription>
    </Alert>

    <!-- Teams Grid -->
    <div v-else-if="filteredTeams.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <TeamCard v-for="team in filteredTeams"
                :key="team.id"
                :team="team"
                :user-role="team.userRole"
                @view-team="viewTeam" />
    </div>

    <!-- Empty State -->
    <Card v-else class="p-12">
      <div class="text-center">
        <Empty description="No teams found">
          <template #icon>
            <Users class="h-16 w-16" />
          </template>
          <template #description>
            <div class="space-y-4">
              <p v-if="searchQuery" class="text-muted-foreground">
                No teams match your search criteria
              </p>
              <p v-else class="text-muted-foreground">
                No teams available yet
              </p>
              <Button @click="navigateToCreateTeam">
                <Plus class="h-4 w-4 mr-2" />
                Create the First Team
              </Button>
            </div>
          </template>
        </Empty>
      </div>
    </Card>

    <!-- Pagination (if needed) -->
    <div v-if="totalPages > 1" class="flex justify-center mt-8">
      <div class="flex gap-2">
        <Button variant="outline"
                size="sm"
                :disabled="currentPage === 1"
                @click="changePage(currentPage - 1)">
          <ChevronLeft class="h-4 w-4" />
          Previous
        </Button>

        <div class="flex items-center gap-2">
          <Button v-for="page in visiblePages"
                  :key="page"
                  :variant="page === currentPage ? 'default' : 'outline'"
                  size="sm"
                  @click="changePage(page)">
            {{ page }}
          </Button>
        </div>

        <Button variant="outline"
                size="sm"
                :disabled="currentPage === totalPages"
                @click="changePage(currentPage + 1)">
          Next
          <ChevronRight class="h-4 w-4" />
        </Button>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, onMounted, watch } from 'vue';
  import { useRouter } from 'vue-router';
  import { teamService } from '@/services/teamService';
  import { Card, CardContent } from '@/components/ui/card';
  import { Button } from '@/components/ui/button';
  import { Input } from '@/components/ui/input';
  import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
  import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
  import { Empty } from '@/components/ui/empty';
  import TeamCard from './TeamCard.vue';
  import { Search, Plus, Users, Loader2, AlertCircle, ChevronLeft, ChevronRight } from 'lucide-vue-next';

  const router = useRouter();

  const loading = ref(true);
  const error = ref(null);
  const teams = ref([]);
  const searchQuery = ref('');
  const sortBy = ref('newest');
  const currentPage = ref(1);
  const itemsPerPage = 12;

  const filteredTeams = computed(() => {
    let result = [...teams.value];

    // Apply search filter
    if (searchQuery.value) {
      const query = searchQuery.value.toLowerCase();
      result = result.filter(team =>
        team.name.toLowerCase().includes(query) ||
        team.description.toLowerCase().includes(query)
      );
    }

    // Apply sorting
    switch (sortBy.value) {
      case 'newest':
        result.sort((a, b) => new Date(b.createdDate) - new Date(a.createdDate));
        break;
      case 'oldest':
        result.sort((a, b) => new Date(a.createdDate) - new Date(b.createdDate));
        break;
      case 'members':
        result.sort((a, b) => (b.memberCount || 0) - (a.memberCount || 0));
        break;
      case 'titles':
        result.sort((a, b) => (b.titleCount || 0) - (a.titleCount || 0));
        break;
    }

    // Apply pagination
    const start = (currentPage.value - 1) * itemsPerPage;
    const end = start + itemsPerPage;
    return result.slice(start, end);
  });

  const totalPages = computed(() => {
    let result = teams.value;

    if (searchQuery.value) {
      const query = searchQuery.value.toLowerCase();
      result = result.filter(team =>
        team.name.toLowerCase().includes(query) ||
        team.description.toLowerCase().includes(query)
      );
    }

    return Math.ceil(result.length / itemsPerPage);
  });

  const visiblePages = computed(() => {
    const pages = [];
    const maxVisible = 5;
    let start = Math.max(1, currentPage.value - Math.floor(maxVisible / 2));
    let end = Math.min(totalPages.value, start + maxVisible - 1);

    if (end - start < maxVisible - 1) {
      start = Math.max(1, end - maxVisible + 1);
    }

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    return pages;
  });

  const loadTeams = async () => {
    loading.value = true;
    error.value = null;

    try {
      const result = await teamService.getAllTeams();

      if (result.success) {
        teams.value = result.data;
      } else {
        error.value = result.error;
      }
    } catch (err) {
      error.value = 'Failed to load teams';
      console.error(err);
    } finally {
      loading.value = false;
    }
  };

  const handleSearch = () => {
    currentPage.value = 1;
  };

  const changePage = (page) => {
    if (page >= 1 && page <= totalPages.value) {
      currentPage.value = page;
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  };

  const viewTeam = (teamId) => {
    router.push(`/team/${teamId}`);
  };

  const navigateToCreateTeam = () => {
    router.push('/team/addteam');
  };

  // Watch for sort changes
  watch(sortBy, () => {
    currentPage.value = 1;
  });

  onMounted(() => {
    loadTeams();
  });
</script>
