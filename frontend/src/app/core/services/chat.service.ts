import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { SignalRService } from './signalr.service';
import { AuthService } from './auth.service';
import { ChatMessage, SendMessageRequest, MessageRole, ChatStreamChunk } from '../models/chat-message.model';
import { ChatSession } from '../models/chat-session.model';
import { environment } from '../../../environments/environment';

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

  // localStorage 키
  private readonly LAST_SESSION_KEY = 'last_session_id';

  constructor(
    private signalRService: SignalRService,
    private http: HttpClient,
    private authService: AuthService
  ) {
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

    // SignalR 세션 히스토리 로드 이벤트 구독
    this.signalRService.sessionHistoryLoaded$.subscribe(sessionDto => {
      if (sessionDto && sessionDto.recentMessages) {
        console.log('📜 세션 히스토리 복원:', sessionDto.recentMessages.length, '개 메시지');

        // 메시지 복원
        const messages: ChatMessage[] = sessionDto.recentMessages.map((msg: any) => ({
          messageId: msg.id?.toString(),
          sessionId: sessionDto.sessionId,
          role: msg.role,
          content: msg.content,
          timestamp: new Date(msg.timestamp),
          category: msg.category
        }));
        this.messagesSubject.next(messages);

        // 세션 정보 업데이트
        const session: ChatSession = {
          sessionId: sessionDto.sessionId,
          userId: sessionDto.userId,
          userName: sessionDto.userName,
          title: sessionDto.title,
          createdAt: new Date(sessionDto.createdAt),
          lastActivityAt: new Date(sessionDto.lastActivityAt),
          isActive: sessionDto.isActive,
          totalMessages: sessionDto.totalMessages,
          messages: messages
        };
        this.sessionSubject.next(session);
      }
    });

    // SignalR 에러 이벤트 구독
    this.signalRService.error$.subscribe(error => {
      console.error('❌ SignalR 에러:', error);
      // 에러 메시지를 UI에 표시할 수 있도록 처리
    });
  }

  /**
   * 채팅 서비스를 초기화하고 SignalR 연결을 시작합니다
   * @param hubUrl SignalR 허브 URL
   * @param userId 사용자 ID (선택사항)
   * @param userName 사용자 이름 (선택사항)
   */
  public async initialize(hubUrl: string, userId?: number, userName?: string): Promise<void> {
    this.hubUrl = hubUrl;

    try {
      // SignalR 연결 시작
      await this.signalRService.startConnection(hubUrl);

      // 마지막 세션 복원 시도
      const lastSessionId = this.getLastSessionId();

      if (lastSessionId && userId) {
        console.log('🔄 마지막 세션 복원 시도:', lastSessionId);

        try {
          // 기존 세션 복원
          this.sessionId = lastSessionId;

          // 세션 참여
          await this.signalRService.joinSession(this.sessionId);

          // SignalR로 세션 히스토리 로드 요청
          await this.signalRService.loadSessionHistory(lastSessionId);

          console.log('✅ 마지막 세션 복원 완료:', lastSessionId);
        } catch (error) {
          console.warn('⚠️ 세션 복원 실패, 새 세션 시작:', error);
          // 복원 실패 시 새 세션 시작
          await this.startNewSessionInternal(userId, userName);
        }
      } else {
        // 마지막 세션이 없으면 새 세션 시작
        console.log('🆕 새 세션 시작 (마지막 세션 없음)');
        await this.startNewSessionInternal(userId, userName);
      }

      this.isConnectedSubject.next(true);

      console.log('✅ 채팅 서비스 초기화 완료. 세션 ID:', this.sessionId);
    } catch (error) {
      console.error('❌ 채팅 서비스 초기화 실패:', error);
      this.isConnectedSubject.next(false);
      throw error;
    }
  }

  /**
   * 새 세션을 시작합니다 (내부 메서드)
   */
  private async startNewSessionInternal(userId?: number, userName?: string): Promise<void> {
    const newSessionId = `session_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    this.sessionId = newSessionId;

    // localStorage에 마지막 세션 저장
    localStorage.setItem(this.LAST_SESSION_KEY, newSessionId);

    const session: ChatSession = {
      sessionId: newSessionId,
      userId: userId,
      userName: userName,
      createdAt: new Date(),
      lastActivityAt: new Date(),
      isActive: true,
      totalMessages: 0,
      messages: []
    };

    this.sessionSubject.next(session);
    await this.signalRService.joinSession(newSessionId);
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

  // ===== 세션 관리 메서드 =====

  /**
   * 사용자의 세션 목록을 조회합니다 (최근 30개)
   */
  public getUserSessions(): Observable<ChatSession[]> {
    return this.http.get<ChatSession[]>(`${environment.apiUrl}/api/chat/sessions`);
  }

  /**
   * 특정 세션의 히스토리를 로드합니다 (HTTP)
   */
  public loadSessionHistory(sessionId: string): Observable<ChatSession> {
    return this.http.get<ChatSession>(`${environment.apiUrl}/api/chat/sessions/${sessionId}`);
  }

  /**
   * 세션을 삭제합니다
   */
  public deleteSession(sessionId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/api/chat/sessions/${sessionId}`);
  }

  /**
   * 세션을 전환합니다
   */
  public switchSession(sessionId: string): void {
    console.log('🔄 세션 전환:', sessionId);

    // 메시지 초기화
    this.clearMessages();
    this.sessionId = sessionId;

    // localStorage에 마지막 세션 저장
    localStorage.setItem(this.LAST_SESSION_KEY, sessionId);

    // SignalR로 세션 히스토리 로드 요청
    this.signalRService.loadSessionHistory(sessionId);

    // 현재 세션 업데이트
    const user = this.authService.getCurrentUserValue();
    const session: ChatSession = {
      sessionId,
      userId: user?.id,
      userName: user?.userName || '',
      createdAt: new Date(),
      lastActivityAt: new Date(),
      isActive: true,
      totalMessages: 0,
      messages: []
    };

    this.sessionSubject.next(session);
  }

  /**
   * 새로운 세션을 시작합니다
   */
  public startNewSession(): void {
    const user = this.authService.getCurrentUserValue();
    const newSessionId = `session_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;

    console.log('🆕 새 세션 시작:', newSessionId);

    // 메시지 초기화
    this.clearMessages();
    this.sessionId = newSessionId;

    // localStorage에 마지막 세션 저장
    localStorage.setItem(this.LAST_SESSION_KEY, newSessionId);

    const session: ChatSession = {
      sessionId: newSessionId,
      userId: user?.id,
      userName: user?.userName || '',
      createdAt: new Date(),
      lastActivityAt: new Date(),
      isActive: true,
      totalMessages: 0,
      messages: []
    };

    this.sessionSubject.next(session);
    this.signalRService.joinSession(newSessionId);
  }

  /**
   * 마지막 세션 ID를 가져옵니다
   */
  public getLastSessionId(): string | null {
    return localStorage.getItem(this.LAST_SESSION_KEY);
  }

  /**
   * 현재 세션을 반환합니다
   */
  public get currentSession$(): Observable<ChatSession | null> {
    return this.sessionSubject.asObservable();
  }
}
