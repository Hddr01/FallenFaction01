<template>
    <div class="error-container">
        <!-- Blurred background image -->
        <div class="background-image"></div>

        <!-- Dark overlay for better text readability -->
        <div class="overlay"></div>

        <!-- Error content -->
        <div class="error-content">
            <div class="error-card">
                <div class="error-code">{{ statusCode }}</div>
                <h1 class="error-title">{{ errorTitle }}</h1>
                <p class="error-message">{{ errorMessage }}</p>

                <div class="error-actions">
                    <button @click="goHome" class="btn-home">
                        Go Home
                    </button>
                    <button @click="goBack" class="btn-back">
                        Go Back
                    </button>
                    <button v-if="showRetry" @click="retry" class="btn-retry">
                        Retry
                    </button>
                </div>

                <div v-if="statusCode === 403 && !isAuthenticated" class="error-actions">
                    <button @click="goToLogin" class="btn-login">
                        Login
                    </button>
                </div>
            </div>
        </div>
    </div>
</template>

<script>
export default {
    name: 'ErrorPage',
    props: {
        statusCode: {
            type: Number,
            default: 404
        },
        message: {
            type: String,
            default: ''
        },
        path: {
            type: String,
            default: ''
        },
        requestId: {
            type: String,
            default: ''
        },
        timestamp: {
            type: String,
            default: ''
        },
        showRetry: {
            type: Boolean,
            default: false
        },
        showDetails: {
            type: Boolean,
            default: false
        },
        isAuthenticated: {
            type: Boolean,
            default: false
        }
    },
    computed: {
        errorTitle() {
            switch (this.statusCode) {
                case 400: return 'Oops! Bad Request';
                case 401: return 'Oops! Unauthorized';
                case 403: return 'Oops! Access Denied';
                case 404: return 'Oops! Page Not Found';
                case 405: return 'Oops! Method Not Allowed';
                case 408: return 'Oops! Request Timeout';
                case 422: return 'Oops! Unprocessable Entity';
                case 429: return 'Oops! Too Many Requests';
                case 500: return 'Oops! Server Error';
                case 502: return 'Oops! Bad Gateway';
                case 503: return 'Oops! Service Unavailable';
                case 504: return 'Oops! Gateway Timeout';
                default: return 'Oops! Something Went Wrong';
            }
        },
        errorMessage() {
            if (this.message) return this.message;

            switch (this.statusCode) {
                case 400: return 'The request was invalid or malformed.';
                case 401: return 'Please log in to access this resource.';
                case 403: return 'You don\'t have permission to access this area.';
                case 404: return 'The page you\'re looking for seems to have vanished into the digital void.';
                case 405: return 'The requested method is not allowed for this resource.';
                case 408: return 'The request took too long to process.';
                case 422: return 'The request was well-formed but couldn\'t be processed.';
                case 429: return 'Too many requests. Please slow down and try again.';
                case 500: return 'Something went wrong on our end. Our team has been notified.';
                case 502: return 'Invalid response from the server.';
                case 503: return 'The service is temporarily unavailable.';
                case 504: return 'The server took too long to respond.';
                default: return 'An unexpected error occurred.';
            }
        }
    },
    methods: {
        goHome() {
            if (this.$router) {
                this.$router.push('/');
            } else {
                window.location.href = '/';
            }
        },
        goBack() {
            window.history.back();
        },
        retry() {
            window.location.reload();
        },
        goToLogin() {
            if (this.$router) {
                this.$router.push('/account/login');
            } else {
                window.location.href = '/account/login';
            }
        },
        formatTime(timestamp) {
            if (!timestamp) return '';
            return new Date(timestamp).toLocaleString();
        }
    }
};
</script>

<style scoped>
    .error-container {
        position: fixed;
        top: 0;
        left: 0;
        width: 100vw;
        height: 100vh;
        overflow: hidden;
        display: flex;
        align-items: center;
        justify-content: center;
    }

    .background-image {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background-image: url('/img/stage.png');
        background-size: cover;
        background-position: center;
        background-repeat: no-repeat;
        filter: blur(8px);
        transform: scale(1.1);
        z-index: 1;
    }

    .overlay {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(0, 0, 0, 0.5);
        z-index: 2;
    }

    .error-content {
        position: relative;
        z-index: 3;
        width: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 2rem;
    }

    .error-card {
        background: rgba(255, 255, 255, 0.15);
        backdrop-filter: blur(20px);
        border: 1px solid rgba(255, 255, 255, 0.2);
        border-radius: 20px;
        padding: 3rem;
        text-align: center;
        max-width: 600px;
        width: 100%;
        box-shadow: 0 25px 50px rgba(0, 0, 0, 0.3);
        color: white;
        position: relative;
    }

    /* Top Error Character GIF */
    .error-character {
        margin-bottom: 2rem;
        
    }

    .error-gif {
        width: 120px;
        height: 120px;
    }

    /* Bottom Cat GIF Positioning */
    .bottom-cat {
        position: absolute;
        bottom: 20px;
        left: 20px;
    }

    .cat-gif {
        width: 140px;
        height: 120px;
    }

    .error-code {
        font-size: clamp(6rem, 15vw, 12rem);
        font-weight: 900;
        background: linear-gradient(135deg, #ff6b6b, #ffd93d, #6bcf7f, #4d9de0, #9b59b6);
        background-size: 400% 400%;
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        background-clip: text;
        line-height: 1;
        margin-bottom: 1.5rem;
        animation: gradientShift 3s ease infinite;
        text-shadow: 0 0 30px rgba(255, 255, 255, 0.5);
    }

    @keyframes gradientShift {
        0% {
            background-position: 0% 50%;
        }

        50% {
            background-position: 100% 50%;
        }

        100% {
            background-position: 0% 50%;
        }
    }

    .error-title {
        font-size: clamp(2rem, 5vw, 3rem);
        color: #ffffff;
        margin-bottom: 1.5rem;
        font-weight: 700;
        text-shadow: 0 2px 10px rgba(0, 0, 0, 0.3);
    }

    .error-message {
        color: rgba(255, 255, 255, 0.9);
        margin-bottom: 3rem;
        font-size: clamp(1rem, 2.5vw, 1.2rem);
        line-height: 1.6;
        text-shadow: 0 1px 5px rgba(0, 0, 0, 0.3);
    }

    .error-actions {
        margin: 2rem 0;
        display: flex;
        flex-wrap: wrap;
        justify-content: center;
        gap: 1rem;
    }

    .btn-home, .btn-retry, .btn-login {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        border: none;
        padding: 14px 32px;
        border-radius: 50px;
        color: white;
        font-size: 1rem;
        font-weight: 600;
        cursor: pointer;
        transition: all 0.3s ease;
        box-shadow: 0 8px 25px rgba(102, 126, 234, 0.3);
        text-transform: uppercase;
        letter-spacing: 0.5px;
    }

        .btn-home:hover, .btn-retry:hover, .btn-login:hover {
            transform: translateY(-3px);
            box-shadow: 0 12px 35px rgba(102, 126, 234, 0.4);
            background: linear-gradient(135deg, #764ba2 0%, #667eea 100%);
        }

    .btn-back {
        background: transparent;
        border: 2px solid rgba(255, 255, 255, 0.3);
        padding: 12px 32px;
        border-radius: 50px;
        color: white;
        font-size: 1rem;
        font-weight: 600;
        cursor: pointer;
        transition: all 0.3s ease;
        backdrop-filter: blur(10px);
        text-transform: uppercase;
        letter-spacing: 0.5px;
    }

        .btn-back:hover {
            background: rgba(255, 255, 255, 0.1);
            border-color: rgba(255, 255, 255, 0.5);
            transform: translateY(-3px);
            box-shadow: 0 8px 25px rgba(255, 255, 255, 0.2);
        }

    /* Responsive Design */
    @media (max-width: 768px) {
        .error-card {
            padding: 2rem 1.5rem;
            margin: 1rem;
        }

        .error-gif {
            width: 100px;
            height: 100px;
        }

        .cat-gif {
            width: 60px;
            height: 60px;
        }

        .bottom-cat {
            bottom: 15px;
            left: 15px;
        }

        .error-actions {
            flex-direction: column;
            align-items: center;
        }

        .btn-home, .btn-back, .btn-retry, .btn-login {
            width: 100%;
            max-width: 250px;
            margin: 0.5rem 0;
        }
    }

    @media (max-width: 480px) {
        .error-card {
            padding: 1.5rem 1rem;
        }

        .error-message {
            font-size: 1rem;
        }

        .error-gif {
            width: 80px;
            height: 80px;
        }

        .cat-gif {
            width: 50px;
            height: 50px;
        }

        .bottom-cat {
            bottom: 10px;
            left: 10px;
        }
    }

    /* Add some subtle animations */
    .error-card {
        animation: fadeInUp 0.8s ease-out;
    }

    @keyframes fadeInUp {
        from {
            opacity: 0;
            transform: translateY(30px);
        }

        to {
            opacity: 1;
            transform: translateY(0);
        }
    }

    /* Pulsing effect for retry button when available */
    .btn-retry {
        animation: pulse 2s infinite;
    }

    @keyframes pulse {
        0% {
            box-shadow: 0 8px 25px rgba(102, 126, 234, 0.3);
        }

        50% {
            box-shadow: 0 8px 25px rgba(102, 126, 234, 0.6);
        }

        100% {
            box-shadow: 0 8px 25px rgba(102, 126, 234, 0.3);
        }
    }
</style>