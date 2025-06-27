<template>
    <div>
        <NavBar/>
        <main class="main-content">
        </main>
    </div>
</template>

<script>
  import NavBar from './layout/NavBar.vue';
  import Footer from './layout/Footer.vue';
export default {
    name: 'HomePage',
    components: {
        NavBar, Footer,
    }
};
</script>

<style scoped>
.main-content {
    padding: 2rem;
}
</style>
