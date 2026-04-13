importScripts('https://www.gstatic.com/firebasejs/9.0.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/9.0.0/firebase-messaging-compat.js');

firebase.initializeApp({
    apiKey: "AIzaSyArSld4c6rHoWUvK9H4CvkLnMAIfSWuEPY",
    authDomain: "mindspot-push.firebaseapp.com",
    projectId: "mindspot-push",
    storageBucket: "mindspot-push.appspot.com",
    messagingSenderId: "975948613803",
    appId: "975948613803:web:037aaf1c01058ab2a5cfeef"
});

const messaging = firebase.messaging();

messaging.onBackgroundMessage((payload) => {
    console.log('Background message:', payload);
    const notificationTitle = payload.notification.title;
    const notificationOptions = {
        body: payload.notification.body,
        icon: '/logo192.png'
    };
    self.registration.showNotification(notificationTitle, notificationOptions);
});