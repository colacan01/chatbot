import { ChatMessage } from './chat-message.model';

export interface ChatSession {
  sessionId: string;
  userId?: string;
  userName?: string;
  createdAt: Date;
  lastActivityAt: Date;
  isActive: boolean;
  totalMessages: number;
  messages?: ChatMessage[];
}
