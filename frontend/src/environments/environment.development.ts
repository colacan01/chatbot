export const environment = {
  production: false,
  apiUrl: 'http://localhost:5069',
  signalRHubUrl: 'http://localhost:5069/hubs/chat',
  appName: '자전거 쇼핑 챗봇',
  defaultLanguage: 'ko',
  chatConfig: {
    maxMessageLength: 2000,
    reconnectAttempts: 5,
    typingIndicatorDelay: 500
  }
};
