import { ChatMessage } from './chat-message.model';

export interface ChatSession {
  sessionId: string;
  userId?: number;
  userName?: string;
  title?: string;
  createdAt: Date;
  lastActivityAt: Date;
  isActive: boolean;
  totalMessages: number;
  messages?: ChatMessage[];
}
