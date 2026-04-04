<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <!-- Page Header -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-[var(--color-heading)]">Title Management</h1>
        <p class="mt-2 text-[var(--color-text)] opacity-75">Edit, update and manage approved titles</p>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-5 gap-8">
        <!-- Left Column: Update Form -->
        <div class="lg:col-span-2">
          <div class="bg-[var(--color-background-soft)] rounded-lg shadow-md border border-[var(--color-border)]">
            <div class="px-6 py-4 border-b border-[var(--color-border)]">
              <h3 class="text-lg font-semibold text-[var(--color-heading)]">Update Title</h3>
            </div>

            <div class="p-6">
              <!-- Title ID Input -->
              <div class="mb-6">
                <div class="flex gap-2">
                  <input type="number"
                         v-model="titleIdInput"
                         placeholder="Enter Title ID"
                         class="flex-1 px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
                  <button @click="loadTitleById"
                          :disabled="!titleIdInput || isLoadingTitle"
                          class="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                    <span v-if="isLoadingTitle">Loading...</span>
                    <span v-else>Go</span>
                  </button>
                </div>
              </div>

              <!-- Update Form -->
              <form v-if="selectedTitle" @submit.prevent="updateTitle" class="space-y-6">
                <!-- Images -->
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Cover Image:</label>
                    <input ref="coverImageInput"
                           type="file"
                           accept="image/*"
                           @change="(e) => handleImageChange('coverImage', e)"
                           class="block w-full text-sm text-[var(--color-text)] file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-semibold file:bg-[var(--color-background-mute)] file:text-green-600 hover:file:bg-[var(--color-background-soft)] hover:file:text-green-700 transition-colors duration-200" />

                    <div v-if="imagePreview.coverImage || selectedTitle.coverImagePath" class="mt-3">
                      <div class="relative inline-block">
                        <img :src="imagePreview.coverImage || selectedTitle.coverImagePath"
                             alt="Cover Image"
                             class="w-24 h-32 object-cover rounded border border-[var(--color-border)]" />
                        <button v-if="imagePreview.coverImage"
                                type="button"
                                @click="removeImage('coverImage')"
                                class="absolute -top-2 -right-2 w-6 h-6 bg-red-500 text-white rounded-full flex items-center justify-center hover:bg-red-600 transition-colors duration-200">
                          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                          </svg>
                        </button>
                      </div>
                    </div>
                  </div>

                  <div>
                    <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Background Image:</label>
                    <input ref="backgroundImageInput"
                           type="file"
                           accept="image/*"
                           @change="(e) => handleImageChange('backgroundImage', e)"
                           class="block w-full text-sm text-[var(--color-text)] file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-semibold file:bg-[var(--color-background-mute)] file:text-green-600 hover:file:bg-[var(--color-background-soft)] hover:file:text-green-700 transition-colors duration-200" />

                    <div v-if="imagePreview.backgroundImage || selectedTitle.backgroundImagePath" class="mt-3">
                      <div class="relative inline-block">
                        <img :src="imagePreview.backgroundImage || selectedTitle.backgroundImagePath"
                             alt="Background Image"
                             class="w-32 h-24 object-cover rounded border border-[var(--color-border)]" />
                        <button v-if="imagePreview.backgroundImage"
                                type="button"
                                @click="removeImage('backgroundImage')"
                                class="absolute -top-2 -right-2 w-6 h-6 bg-red-500 text-white rounded-full flex items-center justify-center hover:bg-red-600 transition-colors duration-200">
                          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                          </svg>
                        </button>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Title Fields -->
                <div class="space-y-4">
                  <div>
                    <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Original Title:</label>
                    <input type="text"
                           v-model="selectedTitle.originalTitle"
                           class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
                  </div>

                  <div>
                    <label class="block text-sm font-medium text-[var(--color-text)] mb-1">English Title:</label>
                    <input type="text"
                           v-model="selectedTitle.englishTitle"
                           class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
                  </div>

                  <div>
                    <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Alternative Names:</label>
                    <input type="text"
                           v-model="selectedTitle.alternativeNames"
                           placeholder="Alternative Names, separated by '/'"
                           class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
                  </div>
                </div>

                <!-- Type and Release Date -->
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Type:</label>
                    <select v-model="selectedTitle.type"
                            class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200">
                      <option value="1">Novel</option>
                      <option value="2">Light Novel</option>
                      <option value="3">Web Novel</option>
                      <option value="4">Short Story</option>
                      <option value="5">Wuxia</option>
                      <option value="6">Xianxia</option>
                      <option value="7">Xuanhuan</option>
                      <option value="8">Classic Fiction</option>
                    </select>
                  </div>

                  <div>
                    <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Release Year:</label>
                    <input type="text"
                           v-model="selectedTitle.releaseDate"
                           class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
                  </div>
                </div>

                <!-- Multi-select fields -->
                <div class="space-y-4">
                  <MultiSelect :options="formData.authors"
                               v-model="selectedTitle.authors"
                               placeholder="Select authors"
                               label="Authors"
                               create-new-url="/people/Create"
                               create-new-text="Create New Author" />

                  <MultiSelect :options="formData.publishers"
                               v-model="selectedTitle.publishers"
                               placeholder="Select publishers"
                               label="Publishers"
                               create-new-url="/publisher/Create"
                               create-new-text="Create New Publisher" />

                  <MultiSelect :options="formData.teams"
                               v-model="selectedTitle.teams"
                               placeholder="Select teams"
                               label="Teams"
                               create-new-url="/team/Addteam"
                               create-new-text="Create New Team" />

                  <MultiSelect :options="formData.categories"
                               v-model="selectedTitle.categories"
                               placeholder="Select genres"
                               label="Genres" />

                  <MultiSelect :options="formData.tags"
                               v-model="selectedTitle.tags"
                               placeholder="Select tags"
                               label="Tags" />

                  <MultiSelect :options="formData.formats"
                               v-model="selectedTitle.formats"
                               placeholder="Select formats"
                               label="Release Format" />
                </div>

                <!-- Status and Settings -->
                <div class="bg-[var(--color-background-mute)] p-4 rounded-md border border-[var(--color-border)]">
                  <h4 class="text-sm font-medium text-[var(--color-text)] mb-3">Status & Settings</h4>

                  <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
                    <div>
                      <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Title Status:</label>
                      <select v-model="selectedTitle.statusTitle"
                              class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200">
                        <option value="done">Done</option>
                        <option value="inproces">In Process</option>
                      </select>
                    </div>

                    <div>
                      <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Translation Status:</label>
                      <select v-model="selectedTitle.statusTranslation"
                              class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200">
                        <option value="done">Done</option>
                        <option value="inproces">In Process</option>
                      </select>
                    </div>

                    <div>
                      <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Age Restriction:</label>
                      <select v-model="selectedTitle.ageRestriction"
                              class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200">
                        <option value="0">No Restriction</option>
                        <option value="12">12+</option>
                        <option value="16">16+</option>
                        <option value="18">18+</option>
                      </select>
                    </div>
                  </div>

                  <!-- Toggle Switches -->
                  <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div class="flex items-center">
                      <input type="checkbox"
                             v-model="selectedTitle.isAvailable"
                             id="isAvailable"
                             class="h-4 w-4 text-green-600 focus:ring-green-500 border-[var(--color-border)] rounded" />
                      <label for="isAvailable" class="ml-2 block text-sm text-[var(--color-text)]">
                        Title Is Available
                      </label>
                    </div>

                    <div class="flex items-center">
                      <input type="checkbox"
                             v-model="selectedTitle.areCommentsEnabled"
                             id="areCommentsEnabled"
                             class="h-4 w-4 text-green-600 focus:ring-green-500 border-[var(--color-border)] rounded" />
                      <label for="areCommentsEnabled" class="ml-2 block text-sm text-[var(--color-text)]">
                        Title Comments
                      </label>
                    </div>

                    <div class="flex items-center">
                      <input type="checkbox"
                             v-model="selectedTitle.areChapterCommentsEnabled"
                             id="areChapterCommentsEnabled"
                             class="h-4 w-4 text-green-600 focus:ring-green-500 border-[var(--color-border)] rounded" />
                      <label for="areChapterCommentsEnabled" class="ml-2 block text-sm text-[var(--color-text)]">
                        Chapter Comments
                      </label>
                    </div>
                  </div>
                </div>

                <!-- External Links -->
                <div>
                  <label class="block text-sm font-medium text-[var(--color-text)] mb-2">External Links:</label>
                  <div class="space-y-2">
                    <div v-for="(link, index) in selectedTitle.externalLinks"
                         :key="index"
                         class="flex gap-2">
                      <input type="url"
                             v-model="selectedTitle.externalLinks[index]"
                             placeholder="http://example.com"
                             class="flex-1 px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
                      <button type="button"
                              @click="removeExternalLink(index)"
                              class="px-3 py-2 border border-red-300 text-red-700 rounded-md hover:bg-red-50 focus:outline-none focus:ring-2 focus:ring-red-500 transition-colors duration-200">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                        </svg>
                      </button>
                    </div>
                  </div>
                  <button type="button"
                          @click="addExternalLink"
                          class="mt-2 text-green-600 hover:text-green-700 text-sm font-medium transition-colors duration-200">
                    Add Another Link
                  </button>
                </div>

                <!-- Description -->
                <div>
                  <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Description:</label>
                  <textarea v-model="selectedTitle.description"
                            rows="4"
                            class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200 resize-vertical"></textarea>
                </div>

                <!-- Action Buttons -->
                <div class="flex space-x-4">
                  <button type="submit"
                          :disabled="isUpdating"
                          class="px-6 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
                    <span v-if="isUpdating">Updating...</span>
                    <span v-else>Update Title</span>
                  </button>

                  <button type="button"
                          @click="deleteTitle"
                          :disabled="isUpdating"
                          class="px-6 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
                    Delete Title
                  </button>
                </div>
              </form>

              <!-- No Title Selected State -->
              <div v-else class="text-center py-8">
                <svg class="mx-auto h-12 w-12 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No title selected</h3>
                <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">Enter a Title ID or select from the list to edit</p>
              </div>
            </div>
          </div>
        </div>

        <!-- Right Column: Title List -->
        <div class="lg:col-span-3">
          <div class="bg-[var(--color-background-soft)] rounded-lg shadow-md border border-[var(--color-border)]">
            <div class="px-6 py-4 border-b border-[var(--color-border)]">
              <h3 class="text-lg font-semibold text-[var(--color-heading)] mb-4">Title List</h3>
              <input type="text"
                     v-model="searchQuery"
                     @input="searchTitles"
                     placeholder="Search by title name"
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
            </div>

            <!-- Loading State -->
            <div v-if="isLoadingTitles" class="p-6 text-center">
              <div class="inline-flex items-center">
                <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                <span class="text-[var(--color-text)]">Loading titles...</span>
              </div>
            </div>

            <!-- Titles Table -->
            <div v-else class="overflow-x-auto">
              <table class="min-w-full divide-y divide-[var(--color-border)]">
                <thead class="bg-[var(--color-background-mute)]">
                  <tr>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">ID</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Original Title</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">English Title</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Status</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody class="bg-[var(--color-background-soft)] divide-y divide-[var(--color-border)]">
                  <tr v-for="title in displayedTitles" :key="title.id" class="hover:bg-[var(--color-background-mute)] transition-colors duration-200">
                    <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-[var(--color-text)]">
                      {{ title.id }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)] max-w-xs truncate">
                      {{ title.originalTitle || 'N/A' }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)] max-w-xs truncate">
                      {{ title.englishTitle }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap">
                      <div class="flex flex-wrap gap-1">
                        <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium"
                              :class="title.isAvailable ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'">
                          {{ title.isAvailable ? 'Available' : 'Unavailable' }}
                        </span>
                        <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium"
                              :class="title.areCommentsEnabled ? 'bg-blue-100 text-blue-800' : 'bg-gray-100 text-gray-800'">
                          Comments: {{ title.areCommentsEnabled ? 'On' : 'Off' }}
                        </span>
                      </div>
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
                      <div class="flex space-x-2">
                        <button @click="selectTitle(title)"
                                class="inline-flex items-center px-3 py-1 border border-transparent text-xs font-medium rounded text-green-700 bg-green-100 hover:bg-green-200 focus:outline-none focus:ring-2 focus:ring-green-500 transition-colors duration-200">
                          Edit
                        </button>

                        <div class="relative inline-block text-left">
                          <button @click="toggleDropdown(title.id)"
                                  class="inline-flex items-center px-3 py-1 border border-transparent text-xs font-medium rounded text-gray-700 bg-gray-100 hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500 transition-colors duration-200">
                            Manage
                            <svg class="ml-1 h-3 w-3" viewBox="0 0 20 20" fill="currentColor">
                              <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd" />
                            </svg>
                          </button>

                          <div v-if="openDropdown === title.id"
                               class="absolute right-0 z-10 mt-2 w-56 rounded-md shadow-lg bg-[var(--color-background-soft)] ring-1 ring-[var(--color-border)] focus:outline-none">
                            <div class="py-1">
                              <button @click="toggleTitleAvailability(title)"
                                      class="block w-full text-left px-4 py-2 text-sm text-[var(--color-text)] hover:bg-[var(--color-background-mute)] transition-colors duration-200">
                                {{ title.isAvailable ? 'Mark as Unavailable' : 'Mark as Available' }}
                              </button>
                              <button @click="toggleTitleComments(title)"
                                      class="block w-full text-left px-4 py-2 text-sm text-[var(--color-text)] hover:bg-[var(--color-background-mute)] transition-colors duration-200">
                                {{ title.areCommentsEnabled ? 'Disable Comments' : 'Enable Comments' }}
                              </button>
                              <button @click="toggleChapterComments(title)"
                                      class="block w-full text-left px-4 py-2 text-sm text-[var(--color-text)] hover:bg-[var(--color-background-mute)] transition-colors duration-200">
                                {{ title.areChapterCommentsEnabled ? 'Disable Chapter Comments' : 'Enable Chapter Comments' }}
                              </button>
                              <hr class="my-1 border-[var(--color-border)]">
                              <button @click="confirmDeleteTitle(title)"
                                      class="block w-full text-left px-4 py-2 text-sm text-red-600 hover:bg-red-50 transition-colors duration-200">
                                Delete Permanently
                              </button>
                            </div>
                          </div>
                        </div>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>

              <!-- No Results -->
              <div v-if="displayedTitles.length === 0 && !isLoadingTitles" class="text-center py-8">
                <svg class="mx-auto h-8 w-8 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No titles found</h3>
                <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">Try adjusting your search query</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Success/Error Messages -->
      <div v-if="successMessage" class="fixed bottom-4 right-4 bg-green-50 border border-green-200 rounded-md p-4 shadow-lg z-50">
        <div class="flex">
          <svg class="h-5 w-5 text-green-400" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
          </svg>
          <div class="ml-3">
            <p class="text-sm font-medium text-green-800">{{ successMessage }}</p>
          </div>
          <button @click="successMessage = ''" class="ml-auto text-green-400 hover:text-green-600">
            <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>
      </div>

      <div v-if="errorMessage" class="fixed bottom-4 right-4 bg-red-50 border border-red-200 rounded-md p-4 shadow-lg z-50">
        <div class="flex">
          <svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
          </svg>
          <div class="ml-3">
            <p class="text-sm font-medium text-red-800">{{ errorMessage }}</p>
          </div>
          <button @click="errorMessage = ''" class="ml-auto text-red-400 hover:text-red-600">
            <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed, onUnmounted } from 'vue'
import MultiSelect from '../manga/MultiSelect.vue'
import adminApi from '../../services/adminApi.js'
import titleApi from '../../services/titleApi.js'

// State
const titleIdInput = ref('')
const selectedTitle = ref(null)
const allTitles = ref([])
const searchQuery = ref('')
const openDropdown = ref(null)

// Loading states
const isLoadingTitles = ref(true)
const isLoadingTitle = ref(false)
const isUpdating = ref(false)

// Messages
const successMessage = ref('')
const errorMessage = ref('')

// Form data (authors, artists, etc.)
const formData = reactive({
  authors: [],
  publishers: [],
  teams: [],
  categories: [],
  tags: [],
  formats: []
})

// Image handling
const coverImageInput = ref(null)
const backgroundImageInput = ref(null)
const imagePreview = reactive({
  coverImage: null,
  backgroundImage: null
})

// Computed
const displayedTitles = computed(() => {
  if (!searchQuery.value) return allTitles.value

  const query = searchQuery.value.toLowerCase()
  return allTitles.value.filter(title =>
    title.englishTitle.toLowerCase().includes(query) ||
    title.originalTitle?.toLowerCase().includes(query)
  )
})

// Methods
const loadFormData = async () => {
  try {
    const result = await titleApi.getFormData()
    if (result.success) {
      const data = result.data
      formData.authors = data.Authors || data.authors || []
      formData.publishers = data.Publishers || data.publishers || []
      formData.teams = data.Teams || data.teams || []
      formData.categories = data.Categories || data.categories || []
      formData.tags = data.Tags || data.tags || []
      formData.formats = data.Formats || data.formats || []
    }
  } catch (error) {
    console.error('Error loading form data:', error)
  }
}

const loadTitles = async () => {
  try {
    isLoadingTitles.value = true
    const result = await adminApi.getApprovedTitles()
    if (result.success) {
      allTitles.value = result.data
    } else {
      showError(result.error)
    }
  } catch (error) {
    console.error('Error loading titles:', error)
    showError('Failed to load titles')
  } finally {
    isLoadingTitles.value = false
  }
}

const loadTitleById = async () => {
  if (!titleIdInput.value) return

  try {
    isLoadingTitle.value = true
    const result = await adminApi.getTitleDetails(titleIdInput.value)
    if (result.success) {
      selectTitleData(result.data)
    } else {
      showError(result.error)
    }
  } catch (error) {
    console.error('Error loading title:', error)
    showError('Failed to load title details')
  } finally {
    isLoadingTitle.value = false
  }
}

const selectTitle = async (title) => {
  try {
    const result = await adminApi.getTitleDetails(title.id)
    if (result.success) {
      selectTitleData(result.data)
      titleIdInput.value = title.id
    } else {
      showError(result.error)
    }
  } catch (error) {
    console.error('Error loading title details:', error)
    showError('Failed to load title details')
  }
}

const selectTitleData = (data) => {
  selectedTitle.value = {
    id: data.id,
    originalTitle: data.originalTitle || '',
    englishTitle: data.englishTitle || '',
    alternativeNames: data.alternativeNames || '',
    releaseDate: data.releaseDate || '',
    type: data.type || 1,
    statusTitle: data.statusTitle || 'inproces',
    statusTranslation: data.statusTranslation || 'inproces',
    ageRestriction: data.ageRestriction || 0,
    description: data.description || '',
    coverImagePath: data.coverImagePath || '',
    backgroundImagePath: data.backgroundImagePath || '',
    isAvailable: data.isAvailable ?? true,
    areCommentsEnabled: data.areCommentsEnabled ?? true,
    areChapterCommentsEnabled: data.areChapterCommentsEnabled ?? true,
    authors: data.authors || [],
    publishers: data.publishers || [],
    teams: data.teams || [],
    categories: data.categories || [],
    tags: data.tags || [],
    formats: data.formats || [],
    externalLinks: parseExternalLinks(data.externalLinks)
  }

  // Clear image previews
  clearImagePreviews()
}

const parseExternalLinks = (linksString) => {
  if (!linksString) return ['']

  const links = linksString.split(';').filter(link => link.trim())
  return links.length > 0 ? links : ['']
}

const handleImageChange = (field, event) => {
  const file = event.target.files[0]
  if (file) {
    // Validate file
    if (!file.type.startsWith('image/')) {
      showError('Please select a valid image file')
      return
    }

    if (file.size > 5 * 1024 * 1024) {
      showError('File size must be less than 5MB')
      return
    }

    // Clear previous preview
    if (imagePreview[field]) {
      URL.revokeObjectURL(imagePreview[field])
    }

    // Create preview
    imagePreview[field] = URL.createObjectURL(file)
    selectedTitle.value[field + 'File'] = file
  }
}

const removeImage = (field) => {
  if (imagePreview[field]) {
    URL.revokeObjectURL(imagePreview[field])
    imagePreview[field] = null
  }

  selectedTitle.value[field + 'File'] = null

  // Clear file input
  const input = field === 'coverImage' ? coverImageInput.value : backgroundImageInput.value
  if (input) input.value = ''
}

const clearImagePreviews = () => {
  if (imagePreview.coverImage) {
    URL.revokeObjectURL(imagePreview.coverImage)
    imagePreview.coverImage = null
  }
  if (imagePreview.backgroundImage) {
    URL.revokeObjectURL(imagePreview.backgroundImage)
    imagePreview.backgroundImage = null
  }
}

const addExternalLink = () => {
  selectedTitle.value.externalLinks.push('')
}

const removeExternalLink = (index) => {
  if (selectedTitle.value.externalLinks.length > 1) {
    selectedTitle.value.externalLinks.splice(index, 1)
  }
}

const updateTitle = async () => {
  if (!selectedTitle.value) return

  try {
    isUpdating.value = true

    // Prepare update data
    const updateData = {
      id: selectedTitle.value.id,
      originalTitle: selectedTitle.value.originalTitle,
      englishTitle: selectedTitle.value.englishTitle,
      alternativeNames: selectedTitle.value.alternativeNames,
      releaseDate: selectedTitle.value.releaseDate,
      type: selectedTitle.value.type,
      statusTitle: selectedTitle.value.statusTitle,
      statusTranslation: selectedTitle.value.statusTranslation,
      ageRestriction: selectedTitle.value.ageRestriction,
      description: selectedTitle.value.description,
      isAvailable: selectedTitle.value.isAvailable,
      areCommentsEnabled: selectedTitle.value.areCommentsEnabled,
      areChapterCommentsEnabled: selectedTitle.value.areChapterCommentsEnabled,
      authors: selectedTitle.value.authors,
      publishers: selectedTitle.value.publishers,
      teams: selectedTitle.value.teams,
      categories: selectedTitle.value.categories,
      tags: selectedTitle.value.tags,
      formats: selectedTitle.value.formats,
      externalLinks: selectedTitle.value.externalLinks.filter(link => link.trim())
    }

    // Add image files if they exist
    if (selectedTitle.value.coverImageFile) {
      updateData.coverImage = selectedTitle.value.coverImageFile
    }
    if (selectedTitle.value.backgroundImageFile) {
      updateData.backgroundImage = selectedTitle.value.backgroundImageFile
    }

    const result = await adminApi.updateTitle(updateData)
    if (result.success) {
      showSuccess('Title updated successfully!')
      await loadTitles() // Refresh the list
      clearImagePreviews() // Clear previews after successful update
    } else {
      showError(result.error)
    }
  } catch (error) {
    console.error('Error updating title:', error)
    showError('Failed to update title')
  } finally {
    isUpdating.value = false
  }
}

const deleteTitle = async () => {
  if (!selectedTitle.value || !confirm('Are you sure you want to permanently delete this title? This action cannot be undone.')) {
    return
  }

  try {
    const result = await adminApi.deleteTitle(selectedTitle.value.id)
    if (result.success) {
      showSuccess('Title deleted successfully!')
      selectedTitle.value = null
      titleIdInput.value = ''
      await loadTitles()
    } else {
      showError(result.error)
    }
  } catch (error) {
    console.error('Error deleting title:', error)
    showError('Failed to delete title')
  }
}

const confirmDeleteTitle = async (title) => {
  if (confirm(`Are you sure you want to permanently delete "${title.englishTitle}"? This action cannot be undone.`)) {
    try {
      const result = await adminApi.deleteTitle(title.id)
      if (result.success) {
        showSuccess('Title deleted successfully!')
        if (selectedTitle.value?.id === title.id) {
          selectedTitle.value = null
          titleIdInput.value = ''
        }
        await loadTitles()
      } else {
        showError(result.error)
      }
    } catch (error) {
      console.error('Error deleting title:', error)
      showError('Failed to delete title')
    }
  }
  closeDropdown()
}

const toggleTitleAvailability = async (title) => {
  try {
    const result = await adminApi.toggleTitleAvailability(title.id)
    if (result.success) {
      showSuccess(result.message)
      await loadTitles()
      if (selectedTitle.value?.id === title.id) {
        selectedTitle.value.isAvailable = !selectedTitle.value.isAvailable
      }
    } else {
      showError(result.error)
    }
  } catch (error) {
    console.error('Error toggling availability:', error)
    showError('Failed to update availability')
  }
  closeDropdown()
}

const toggleTitleComments = async (title) => {
  try {
    const result = await adminApi.toggleTitleComments(title.id)
    if (result.success) {
      showSuccess(result.message)
      await loadTitles()
      if (selectedTitle.value?.id === title.id) {
        selectedTitle.value.areCommentsEnabled = !selectedTitle.value.areCommentsEnabled
      }
    } else {
      showError(result.error)
    }
  } catch (error) {
    console.error('Error toggling comments:', error)
    showError('Failed to update comments')
  }
  closeDropdown()
}

const toggleChapterComments = async (title) => {
  try {
    const result = await adminApi.toggleChapterComments(title.id)
    if (result.success) {
      showSuccess(result.message)
      await loadTitles()
      if (selectedTitle.value?.id === title.id) {
        selectedTitle.value.areChapterCommentsEnabled = !selectedTitle.value.areChapterCommentsEnabled
      }
    } else {
      showError(result.error)
    }
  } catch (error) {
    console.error('Error toggling chapter comments:', error)
    showError('Failed to update chapter comments')
  }
  closeDropdown()
}

const toggleDropdown = (titleId) => {
  openDropdown.value = openDropdown.value === titleId ? null : titleId
}

const closeDropdown = () => {
  openDropdown.value = null
}

const searchTitles = async () => {
  // Simple client-side search for now
  // Could be enhanced to use server-side search if needed
}

const showSuccess = (message) => {
  successMessage.value = message
  setTimeout(() => {
    successMessage.value = ''
  }, 5000)
}

const showError = (message) => {
  errorMessage.value = message
  setTimeout(() => {
    errorMessage.value = ''
  }, 5000)
}

// Click outside to close dropdown
const handleClickOutside = (event) => {
  if (!event.target.closest('.relative')) {
    closeDropdown()
  }
}

onMounted(async () => {
  console.log('TitleManagement component mounted')
  await Promise.all([
    loadFormData(),
    loadTitles()
  ])

  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  clearImagePreviews()
  document.removeEventListener('click', handleClickOutside)
})
</script>

<style scoped>
  /* Custom scrollbar for tables */
  .overflow-x-auto::-webkit-scrollbar {
    height: 8px;
  }

  .overflow-x-auto::-webkit-scrollbar-track {
    background: var(--color-background-mute);
  }

  .overflow-x-auto::-webkit-scrollbar-thumb {
    background: var(--color-border);
    border-radius: 4px;
  }

    .overflow-x-auto::-webkit-scrollbar-thumb:hover {
      background: var(--color-border-hover);
    }
</style>
