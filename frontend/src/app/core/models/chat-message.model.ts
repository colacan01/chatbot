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

  // 스트리밍 관련 필드
  isStreaming?: boolean;      // 현재 스트리밍 중
  streamComplete?: boolean;   // 스트리밍 완료
}

export interface ChatStreamChunk {
  sessionId: string;
  messageId: string;
  content: string;
  isComplete: boolean;
  timestamp: string;
  category?: string;
}

export interface SendMessageRequest {
  sessionId: string;
  message: string;
  userId?: string;
  userName?: string;
}
