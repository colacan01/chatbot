import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { SignalRService } from './signalr.service';
import { ChatMessage, SendMessageRequest, MessageRole, ChatStreamChunk } from '../models/chat-message.model';
import { ChatSession } from '../models/chat-session.model';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private messagesSubject = new BehaviorSubject<ChatMessage[]>([]);
  private sessionSubject = new BehaviorSubject<ChatSession | null>(null);
  private isTypingSubject = new BehaviorSubject<boolean>(false);
  private isConnectedSubject = new BehaviorSubject<boolean>(false);

  public messages$ = this.messagesSubject.asObservable();
  public session$ = this.sessionSubject.asObservable();
  public isTyping$ = this.isTypingSubject.asObservable();
  public isConnected$ = this.isConnectedSubject.asObservable();

  private sessionId: string = '';
  private hubUrl: string = '';

  // 스트리밍 상태 관리
  private streamingMessages = new Map<string, ChatMessage>();

  constructor(private signalRService: SignalRService) {
    // SignalR 메시지 구독
    this.signalRService.messages$.subscribe(message => {
      console.log('🔔 ChatService에서 메시지 수신:', message);
      this.addMessage(message);
      this.setTyping(false);
    });

    // 스트리밍 청크 구독
    this.signalRService.messageChunks$.subscribe(chunk => {
      console.log('🔔 ChatService에서 청크 수신:', chunk.messageId);
      this.handleStreamChunk(chunk);
    });

    // SignalR 연결 상태 구독
    this.signalRService.connectionState$.subscribe(state => {
      const isConnected = state === signalR.HubConnectionState.Connected;
      console.log('🔌 연결 상태 변경:', isConnected);
      this.isConnectedSubject.next(isConnected);
    });
  }

  /**
   * 채팅 서비스를 초기화하고 SignalR 연결을 시작합니다
   * @param hubUrl SignalR 허브 URL
   * @param userId 사용자 ID (선택사항)
   * @param userName 사용자 이름 (선택사항)
   */
  public async initialize(hubUrl: string, userId?: string, userName?: string): Promise<void> {
    this.hubUrl = hubUrl;
    this.sessionId = this.generateSessionId();

    // 세션 정보 설정
    const session: ChatSession = {
      sessionId: this.sessionId,
      userId: userId,
      userName: userName,
      createdAt: new Date(),
      lastActivityAt: new Date(),
      isActive: true,
      totalMessages: 0,
      messages: []
    };
    this.sessionSubject.next(session);

    try {
      // SignalR 연결 시작
      await this.signalRService.startConnection(hubUrl);

      // 세션 참여
      await this.signalRService.joinSession(this.sessionId);

      this.isConnectedSubject.next(true);

      console.log('✅ 채팅 서비스 초기화 완료. 세션 ID:', this.sessionId);
    } catch (error) {
      console.error('❌ 채팅 서비스 초기화 실패:', error);
      this.isConnectedSubject.next(false);
      throw error;
    }
  }

  /**
   * 메시지를 전송합니다
   * @param message 메시지 내용
   */
  public async sendMessage(message: string): Promise<void> {
    if (!message || message.trim().length === 0) {
      return;
    }

    const session = this.sessionSubject.value;
    if (!session) {
      throw new Error('세션이 초기화되지 않았습니다.');
    }

    // 사용자 메시지를 UI에 즉시 추가
    const userMessage: ChatMessage = {
      sessionId: this.sessionId,
      role: MessageRole.User,
      content: message.trim(),
      timestamp: new Date()
    };
    this.addMessage(userMessage);

    // 봇이 타이핑 중 표시
    this.setTyping(true);

    // SignalR로 메시지 전송
    const request: SendMessageRequest = {
      sessionId: this.sessionId,
      message: message.trim(),
      userId: session.userId,
      userName: session.userName
    };

    try {
      await this.signalRService.sendMessage(request);
    } catch (error) {
      console.error('❌ 메시지 전송 실패:', error);
      this.setTyping(false);
      throw error;
    }
  }

  /**
   * 스트리밍 방식으로 메시지를 전송합니다
   * @param message 메시지 내용
   */
  public async sendMessageStream(message: string): Promise<void> {
    if (!message || message.trim().length === 0) {
      return;
    }

    const session = this.sessionSubject.value;
    if (!session) {
      throw new Error('세션이 초기화되지 않았습니다.');
    }

    // 사용자 메시지를 UI에 즉시 추가
    const userMessage: ChatMessage = {
      sessionId: this.sessionId,
      role: MessageRole.User,
      content: message.trim(),
      timestamp: new Date()
    };
    this.addMessage(userMessage);

    // 봇이 타이핑 중 표시
    this.setTyping(true);

    // SignalR로 스트리밍 메시지 전송
    const request: SendMessageRequest = {
      sessionId: this.sessionId,
      message: message.trim(),
      userId: session.userId,
      userName: session.userName
    };

    try {
      await this.signalRService.sendMessageStream(request);
    } catch (error) {
      console.error('❌ 스트리밍 메시지 전송 실패:', error);
      this.setTyping(false);
      // 스트리밍 실패 시 기존 방식으로 폴백
      this.streamingMessages.clear();
      throw error;
    }
  }

  /**
   * 연결을 종료합니다
   */
  public async disconnect(): Promise<void> {
    if (this.sessionId) {
      await this.signalRService.leaveSession(this.sessionId);
    }
    await this.signalRService.stopConnection();
    this.isConnectedSubject.next(false);
  }

  /**
   * 채팅 기록을 지웁니다
   */
  public clearMessages(): void {
    this.messagesSubject.next([]);
  }

  /**
   * 현재 세션 ID를 반환합니다
   */
  public getSessionId(): string {
    return this.sessionId;
  }

  /**
   * 메시지를 추가합니다 (내부용)
   */
  private addMessage(message: ChatMessage): void {
    const currentMessages = this.messagesSubject.value;
    const newMessages = [...currentMessages, message];
    console.log('➕ 메시지 추가:', newMessages.length, '개 메시지');
    this.messagesSubject.next(newMessages);

    // 세션 업데이트
    const session = this.sessionSubject.value;
    if (session) {
      session.totalMessages = newMessages.length;
      session.lastActivityAt = new Date();
      this.sessionSubject.next(session);
    }
  }

  /**
   * 타이핑 상태를 설정합니다
   */
  private setTyping(isTyping: boolean): void {
    console.log('⌨️ 타이핑 상태 변경:', isTyping);
    this.isTypingSubject.next(isTyping);
  }

  /**
   * 고유한 세션 ID를 생성합니다
   */
  private generateSessionId(): string {
    const timestamp = Date.now();
    const random = Math.random().toString(36).substring(2, 15);
    return `session-${timestamp}-${random}`;
  }

  /**
   * 스트리밍 청크를 처리합니다
   */
  private handleStreamChunk(chunk: ChatStreamChunk): void {
    console.log('🔷 청크 처리 시작:', {
      messageId: chunk.messageId,
      contentLength: chunk.content?.length || 0,
      isComplete: chunk.isComplete,
      category: chunk.category
    });

    let streamingMessage = this.streamingMessages.get(chunk.messageId);

    if (!streamingMessage) {
      // 새로운 스트리밍 메시지 생성
      console.log('🆕 새 스트리밍 메시지 생성:', chunk.messageId);
      streamingMessage = {
        messageId: chunk.messageId,
        sessionId: chunk.sessionId,
        role: MessageRole.Assistant,
        content: '',
        timestamp: new Date(chunk.timestamp),
        category: chunk.category as any,
        isStreaming: true,
        streamComplete: false
      };

      this.streamingMessages.set(chunk.messageId, streamingMessage);

      // 메시지 배열에 추가
      const currentMessages = this.messagesSubject.value;
      this.messagesSubject.next([...currentMessages, streamingMessage]);
      console.log('📝 메시지 배열에 추가됨. 총 메시지:', currentMessages.length + 1);
    }

    if (chunk.isComplete) {
      // 스트리밍 완료
      console.log('✅ 스트리밍 완료:', {
        messageId: chunk.messageId,
        finalContentLength: streamingMessage.content.length,
        content: streamingMessage.content.substring(0, 100) + '...'
      });
      
      streamingMessage.isStreaming = false;
      streamingMessage.streamComplete = true;
      this.streamingMessages.delete(chunk.messageId);
      this.setTyping(false);
    } else {
      // 청크 내용 추가
      const beforeLength = streamingMessage.content.length;
      streamingMessage.content += chunk.content;
      console.log('📝 청크 내용 추가:', {
        messageId: chunk.messageId,
        beforeLength,
        chunkLength: chunk.content.length,
        afterLength: streamingMessage.content.length
      });
    }

    // 메시지 배열 업데이트 (불변성 유지)
    const currentMessages = this.messagesSubject.value;
    const updatedMessages = currentMessages.map(msg =>
      msg.messageId === chunk.messageId ? { ...streamingMessage! } : msg
    );

    console.log('🔄 메시지 배열 업데이트 완료. 총:', updatedMessages.length);
    this.messagesSubject.next(updatedMessages);

    // 세션 업데이트
    const session = this.sessionSubject.value;
    if (session) {
      session.lastActivityAt = new Date();
      this.sessionSubject.next(session);
    }
  }
}
