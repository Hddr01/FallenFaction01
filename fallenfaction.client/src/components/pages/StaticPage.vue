<template>
  <div class="min-h-screen bg-[var(--color-background)]">
    <div class="max-w-3xl mx-auto px-6 py-12">
      <h1 class="text-3xl font-bold text-[var(--color-text)] mb-8">{{ pageTitle }}</h1>

      <!-- DMCA -->
      <template v-if="page === 'dmca'">
        <div class="prose-content">
          <h2>DMCA / Copyright Policy</h2>
          <p>FallenFaction respects the intellectual property rights of others and expects its users to do the same.</p>
          <h3>Reporting Copyright Infringement</h3>
          <p>If you believe that content hosted on our platform infringes your copyright, please send a written notice containing:</p>
          <ol>
            <li>Identification of the copyrighted work you claim has been infringed.</li>
            <li>Identification of the material that you claim is infringing, including the URL or other specific location on the site.</li>
            <li>Your contact information (name, address, email, phone number).</li>
            <li>A statement that you have a good faith belief that the use of the material is not authorized by the copyright owner.</li>
            <li>A statement that the information in the notification is accurate, and under penalty of perjury, that you are the copyright owner or authorized to act on behalf of the owner.</li>
            <li>Your physical or electronic signature.</li>
          </ol>
          <p>Please send DMCA notices to: <strong>dmca@fallenfaction.com</strong></p>
          <h3>Counter-Notification</h3>
          <p>If you believe your content was removed in error, you may submit a counter-notification with the required information as specified by the DMCA.</p>
          <h3>Repeat Infringers</h3>
          <p>We may terminate the accounts of users who are repeat infringers of copyright.</p>
        </div>
      </template>

      <!-- FAQ -->
      <template v-else-if="page === 'faq'">
        <div class="space-y-6">
          <div v-for="(item, i) in faqItems" :key="i"
               class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-5">
            <button @click="openFaq === i ? openFaq = null : openFaq = i"
                    class="w-full flex justify-between items-center text-left">
              <h3 class="font-medium text-[var(--color-text)]">{{ item.q }}</h3>
              <svg class="w-5 h-5 text-[var(--color-text)] opacity-40 transition-transform shrink-0 ml-4"
                   :class="{ 'rotate-180': openFaq === i }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
              </svg>
            </button>
            <p v-if="openFaq === i" class="mt-3 text-sm text-[var(--color-text)] opacity-70 leading-relaxed">{{ item.a }}</p>
          </div>
        </div>
      </template>

      <!-- Terms of Service -->
      <template v-else-if="page === 'terms'">
        <div class="prose-content">
          <p class="text-sm opacity-60 mb-6">Last updated: March 2026</p>
          <h2>1. Acceptance of Terms</h2>
          <p>By accessing and using FallenFaction, you accept and agree to be bound by these Terms of Service. If you do not agree, you may not use the platform.</p>
          <h2>2. User Accounts</h2>
          <p>You are responsible for maintaining the confidentiality of your account credentials and for all activities that occur under your account. You must be at least 13 years old to use this service.</p>
          <h2>3. User Content</h2>
          <p>You retain ownership of content you submit. By posting content, you grant FallenFaction a non-exclusive, worldwide, royalty-free license to display and distribute your content on the platform.</p>
          <h2>4. Prohibited Conduct</h2>
          <p>You agree not to: upload content that infringes copyright; harass, abuse, or threaten other users; attempt to gain unauthorized access; use automated systems to access the platform without permission; or post spam or misleading content.</p>
          <h2>5. Translation Teams</h2>
          <p>Translation teams are responsible for ensuring they have the right to translate and distribute the content they upload. FallenFaction is not responsible for unauthorized translations.</p>
          <h2>6. Termination</h2>
          <p>We reserve the right to suspend or terminate your account at any time for violations of these terms or any other reason at our discretion.</p>
          <h2>7. Limitation of Liability</h2>
          <p>FallenFaction is provided "as is" without any warranties. We are not liable for any damages arising from your use of the platform.</p>
          <h2>8. Changes to Terms</h2>
          <p>We may update these terms at any time. Continued use of the platform after changes constitutes acceptance.</p>
        </div>
      </template>

      <!-- About -->
      <template v-else-if="page === 'about'">
        <div class="prose-content">
          <p class="text-lg text-[var(--color-text)] opacity-80 mb-6">FallenFaction is a community-driven platform for reading and sharing translated novels, manga, manhwa, and manhua.</p>
          <h2>Our Mission</h2>
          <p>We aim to build the best community for translation teams and readers to connect, share, and enjoy stories from around the world.</p>
          <h2>Features</h2>
          <ul>
            <li>Team-based translation management with role permissions</li>
            <li>Chapter reading with progress tracking</li>
            <li>Bookmarks with custom folders</li>
            <li>Community comments and ratings</li>
            <li>Content moderation and quality control</li>
          </ul>
          <h2>Contact</h2>
          <p>For business inquiries, partnerships, or general questions, please reach out at <strong>contact@fallenfaction.com</strong></p>
        </div>
      </template>

      <!-- Contact -->
      <template v-else-if="page === 'contact'">
        <div class="prose-content mb-8">
          <p>Have a question, found a bug, or want to get in touch? Use the form below or email us directly.</p>
        </div>
        <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-6">
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Subject</label>
              <select v-model="contactForm.subject"
                      class="w-full bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-4 py-2">
                <option value="">Select a topic...</option>
                <option value="bug">Bug Report</option>
                <option value="feature">Feature Request</option>
                <option value="copyright">Copyright Issue</option>
                <option value="account">Account Issue</option>
                <option value="other">Other</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Email</label>
              <input v-model="contactForm.email" type="email" placeholder="your@email.com"
                     class="w-full bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-4 py-2" />
            </div>
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Message</label>
              <textarea v-model="contactForm.message" rows="5" placeholder="Describe your issue or question..."
                        class="w-full bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-4 py-2" />
            </div>
            <div class="flex justify-end">
              <button @click="submitContact" :disabled="!contactForm.subject || !contactForm.email || !contactForm.message"
                      class="px-6 py-2 bg-[var(--color-accent)] text-white rounded-lg hover:opacity-90 disabled:opacity-50">
                Send Message
              </button>
            </div>
          </div>
        </div>
        <div class="mt-8 text-center text-sm text-[var(--color-text)] opacity-60">
          Or email us directly at <strong>contact@fallenfaction.com</strong>
        </div>
      </template>

      <!-- Privacy Policy -->
      <template v-else-if="page === 'privacy'">
        <div class="prose-content">
          <p class="text-sm opacity-60 mb-6">Last updated: March 2026</p>
          <h2>Information We Collect</h2>
          <p>We collect information you provide directly (account info, profile data, content you post) and information collected automatically (usage data, device information, cookies).</p>
          <h2>How We Use Your Information</h2>
          <p>Your information is used to provide and improve the platform, personalize your experience, communicate with you, and ensure safety and security.</p>
          <h2>Sharing of Information</h2>
          <p>We do not sell your personal information. We may share information with service providers who assist in operating the platform, or when required by law.</p>
          <h2>Data Retention</h2>
          <p>We retain your data as long as your account is active. You may request deletion of your account and associated data at any time.</p>
          <h2>Your Rights</h2>
          <p>You have the right to access, correct, or delete your personal data. Contact us at privacy@fallenfaction.com for data-related requests.</p>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue';

const props = defineProps({
  page: { type: String, required: true }
});

const openFaq = ref(null);
const contactForm = ref({ subject: '', email: '', message: '' });

const pageTitle = computed(() => {
  const titles = {
    dmca: 'DMCA / Copyright Policy',
    faq: 'Frequently Asked Questions',
    terms: 'Terms of Service',
    about: 'About FallenFaction',
    contact: 'Contact Us',
    privacy: 'Privacy Policy'
  };
  return titles[props.page] || 'Page';
});

const faqItems = [
  { q: 'How do I create a translation team?', a: 'Go to Teams from the navigation menu and click "Create Team". You can then invite members and assign roles with specific permissions.' },
  { q: 'How do I add a new title?', a: 'Navigate to "Add Title" from the menu. Fill in the title details, select your team, and submit it for review. An admin will approve it before it goes live.' },
  { q: 'Can multiple teams translate the same title?', a: 'Yes! Multiple teams can work on the same title. Readers can choose which team\'s translation to read via the chapter list.' },
  { q: 'How do bookmarks work?', a: 'Click the bookmark icon on any title to save it. You can organize bookmarks into custom folders and track your reading progress.' },
  { q: 'How do I report inappropriate content?', a: 'Click the report button (flag icon) on any comment, chapter, or title. Select a reason and provide details. An admin will review your report.' },
  { q: 'What roles are available in teams?', a: 'Teams have three roles: Admin (full control), Member (can add/edit titles and chapters), and Viewer (read-only access to team content).' },
  { q: 'How does the rating system work?', a: 'Logged-in users can rate titles on a scale. The displayed rating is the average of all user ratings.' },
  { q: 'Can I read without an account?', a: 'Yes, you can browse the catalog and read chapters without logging in. An account is needed for bookmarks, comments, ratings, and other interactive features.' },
];

function submitContact() {
  // TODO: Implement contact form submission endpoint
  alert('Thank you for your message! We will get back to you soon.');
  contactForm.value = { subject: '', email: '', message: '' };
}
</script>

<style scoped>
.prose-content h2 {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--color-text);
  margin-top: 1.5rem;
  margin-bottom: 0.75rem;
}
.prose-content h3 {
  font-size: 1.1rem;
  font-weight: 600;
  color: var(--color-text);
  margin-top: 1.25rem;
  margin-bottom: 0.5rem;
}
.prose-content p {
  color: var(--color-text);
  opacity: 0.8;
  line-height: 1.7;
  margin-bottom: 1rem;
}
.prose-content ul, .prose-content ol {
  color: var(--color-text);
  opacity: 0.8;
  padding-left: 1.5rem;
  margin-bottom: 1rem;
}
.prose-content li {
  margin-bottom: 0.5rem;
  line-height: 1.6;
}
.prose-content ol {
  list-style: decimal;
}
.prose-content ul {
  list-style: disc;
}
</style>
