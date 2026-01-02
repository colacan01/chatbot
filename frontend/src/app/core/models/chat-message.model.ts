export enum MessageRole {
  System = 'System',
  User = 'User',
  Assistant = 'Assistant'
}

export enum ChatCategory {
  General = 'General',
  ProductSearch = 'ProductSearch',
  ProductDetails = 'ProductDetails',
  FAQ = 'FAQ',
  OrderStatus = 'OrderStatus',
  CustomerSupport = 'CustomerSupport'
}

export interface ChatMessage {
  messageId?: string;
  sessionId: string;
  role: MessageRole;
  content: string;
  timestamp: Date;
  category?: ChatCategory;
  intentDetected?: string;
  productId?: number;
  orderId?: number;
  tokensUsed?: number;
  processingTimeMs?: number;
}

export interface SendMessageRequest {
  sessionId: string;
  message: string;
  userId?: string;
  userName?: string;
}
