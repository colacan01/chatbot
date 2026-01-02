export const environment = {
  production: true,
  apiUrl: 'https://your-production-api.com',
  signalRHubUrl: 'https://your-production-api.com/hubs/chat',
  appName: '자전거 쇼핑 챗봇',
  defaultLanguage: 'ko',
  chatConfig: {
    maxMessageLength: 2000,
    reconnectAttempts: 5,
    typingIndicatorDelay: 500
  }
};
