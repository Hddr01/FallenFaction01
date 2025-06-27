<template>
  <ErrorPage 
    :status-code="404"
    :path="currentPath"
    :timestamp="currentTime"
    :show-retry="false"
    :show-details="true"
    message="The page you\'re looking for seems to have vanished into the digital void."
  />
</template>

<script>
import ErrorPage from './ErrorPage.vue';

export default {
  name: 'NotFoundPage',
  components: {
    ErrorPage
  },
  data() {
    return {
      currentPath: window.location.pathname,
      currentTime: new Date().toISOString()
    }
  }
}
</script>