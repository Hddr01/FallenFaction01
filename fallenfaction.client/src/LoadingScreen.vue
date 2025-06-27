<template>
  <div 
    v-if="isLoading" 
    class="loading-overlay" 
    :class="{ 
      'fade-out': isFadingOut,
      'loaded': imageLoaded 
    }"
  >
    <div class="loading-content" :class="{ 'fade-out': isFadingOut }">
      <!-- Background blur effect -->
      <div class="loading-background"></div>
      
      <!-- Main loading content -->
      <div class="loading-main">
        <div class="loading-gif-container">
          <img 
            :src="loadingGif" 
            alt="Loading..." 
            class="loading-gif"
            :class="{ 'loaded': imageLoaded }"
            @error="onImageError"
            @load="onImageLoad"
          />
          <!-- Fixed loading ring with perfect centering -->
          <div class="loading-ring"></div>
        </div>
        
        <p class="loading-text" :class="{ 'loaded': imageLoaded }">
          {{ loadingText }}
        </p>
        
        <!-- Progress dots -->
        <div class="loading-dots">
          <span class="dot"></span>
          <span class="dot"></span>
          <span class="dot"></span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';

const props = defineProps({
  loadingGif: {
    type: String,
    default: '/img/happy_girl.gif'
  },
  loadingText: {
    type: String,
    default: 'Loading...'
  }
});

const emit = defineEmits(['loading-complete']);

const isLoading = ref(true);
const isFadingOut = ref(false);
const imageLoaded = ref(false);

const startFadeOut = () => {
  isFadingOut.value = true;
  
  // Wait for fade animation to complete before emitting
  setTimeout(() => {
    isLoading.value = false;
    emit('loading-complete');
  }, 800); // Extended fade out duration for smoother transition
};

const onImageError = () => {
  imageLoaded.value = true;
};

const onImageLoad = () => {
  imageLoaded.value = true;
};

// Simulate minimum loading time for better UX
onMounted(() => {
  // Add a small delay to ensure smooth initial animation
  setTimeout(() => {
    if (!imageLoaded.value) {
      imageLoaded.value = true;
    }
  }, 300);
});

// Expose methods for parent component
defineExpose({
  startFadeOut
});
</script>

<style scoped>
.loading-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: linear-gradient(135deg, #000000 0%, #1a1a1a 50%, #000000 100%);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 9999;
  opacity: 1;
  transition: all 0.8s cubic-bezier(0.4, 0, 0.2, 1);
  overflow: hidden;
}

.loading-overlay.fade-out {
  opacity: 0;
  transform: scale(1.05);
}

.loading-background {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: radial-gradient(circle at center, rgba(255, 255, 255, 0.03) 0%, transparent 70%);
  animation: backgroundPulse 4s ease-in-out infinite;
}

.loading-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  transform: translateY(0);
  transition: all 0.8s cubic-bezier(0.4, 0, 0.2, 1);
}

.loading-content.fade-out {
  transform: translateY(-20px);
  opacity: 0.8;
}

.loading-main {
  position: relative;
  z-index: 2;
}

.loading-gif-container {
  position: relative;
  margin-bottom: 30px;
  transform: translateY(20px);
  opacity: 0;
  transition: all 0.6s cubic-bezier(0.4, 0, 0.2, 1);
  /* Fixed centering for all screen sizes */
  width: 200px;
  height: 200px;
  display: grid;
  place-items: center;
  /* Ensure container stays centered in parent */
  margin-left: auto;
  margin-right: auto;
}

.loading-gif-container.loaded,
.loaded .loading-gif-container {
  transform: translateY(0);
  opacity: 1;
}

.loading-gif {
  width: 200px;
  height: 200px;
  border-radius: 50%;
  filter: brightness(1.1) saturate(1.1);
  box-shadow: 
    0 0 30px rgba(255, 255, 255, 0.1),
    0 0 60px rgba(255, 255, 255, 0.05);
  transition: all 0.6s cubic-bezier(0.4, 0, 0.2, 1);
  transform: scale(0.9);
  /* Perfect centering */
  margin: 0;
  padding: 0;
  display: block;
  object-fit: cover;
  object-position: center;
}

.loading-gif.loaded {
  transform: scale(1);
}

/* FIXED: Perfect ring alignment - always centered relative to container */
.loading-ring {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  width: 220px;
  height: 220px;
  /* Perfect centering using margin auto */
  margin: auto;
  border: 2px solid transparent;
  border-top: 2px solid rgba(255, 255, 255, 0.3);
  border-radius: 50%;
  animation: spin 3s linear infinite;
  opacity: 0.7;
  /* Ensure it doesn't interfere with the image */
  pointer-events: none;
}

.loading-text {
  color: white;
  font-size: 20px;
  font-weight: 500;
  margin: 0 0 20px 0;
  letter-spacing: 1px;
  transform: translateY(20px);
  opacity: 0;
  transition: all 0.6s cubic-bezier(0.4, 0, 0.2, 1) 0.2s;
}

.loading-text.loaded,
.loaded .loading-text {
  transform: translateY(0);
  opacity: 0.9;
}

.loading-dots {
  display: flex;
  gap: 8px;
  justify-content: center;
  transform: translateY(20px);
  opacity: 0;
  transition: all 0.6s cubic-bezier(0.4, 0, 0.2, 1) 0.4s;
}

.loaded .loading-dots {
  transform: translateY(0);
  opacity: 1;
}

.dot {
  width: 8px;
  height: 8px;
  background: rgba(255, 255, 255, 0.6);
  border-radius: 50%;
  animation: dotPulse 1.5s ease-in-out infinite;
}

.dot:nth-child(2) {
  animation-delay: 0.2s;
}

.dot:nth-child(3) {
  animation-delay: 0.4s;
}

/* Animations */
@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

@keyframes dotPulse {
  0%, 100% { 
    opacity: 0.3;
    transform: scale(1); 
  }
  50% { 
    opacity: 1;
    transform: scale(1.2); 
  }
}

@keyframes backgroundPulse {
  0%, 100% { 
    opacity: 0.3; 
  }
  50% { 
    opacity: 0.6; 
  }
}

/* Responsive adjustments with improved centering */
@media (max-width: 768px) {
  .loading-gif-container {
    width: 150px;
    height: 150px;
    margin-bottom: 25px;
  }
  
  .loading-gif {
    width: 150px;
    height: 150px;
  }
  
  .loading-ring {
    width: 170px;
    height: 170px;
  }
  
  .loading-text {
    font-size: 18px;
  }
}

@media (max-width: 480px) {
  .loading-gif-container {
    width: 120px;
    height: 120px;
    margin-bottom: 20px;
  }
  
  .loading-gif {
    width: 120px;
    height: 120px;
  }
  
  .loading-ring {
    width: 140px;
    height: 140px;
  }
  
  .loading-text {
    font-size: 16px;
  }
}

/* Reduced motion preferences */
@media (prefers-reduced-motion: reduce) {
  .loading-overlay,
  .loading-content,
  .loading-gif-container,
  .loading-gif,
  .loading-text,
  .loading-dots {
    transition: opacity 0.3s ease;
    animation: none;
  }
  
  .loading-ring {
    animation: none;
    opacity: 0.3;
  }
  
  .dot {
    animation: none;
    opacity: 0.6;
  }
}
</style>