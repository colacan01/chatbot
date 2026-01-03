import { Injectable, NgZone } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject, Observable } from 'rxjs';
import { ChatMessage, SendMessageRequest } from '../models/chat-message.model';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection?: signalR.HubConnection;
  private messageSubject = new Subject<ChatMessage>();
  private connectionStateSubject = new Subject<signalR.HubConnectionState>();

  public messages$ = this.messageSubject.asObservable();
  public connectionState$ = this.connectionStateSubject.asObservable();

  constructor(private ngZone: NgZone) {}

  /**
   * SignalR 허브에 연결합니다
   * @param hubUrl SignalR 허브 URL
   */
  public startConnection(hubUrl: string): Promise<void> {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // 재연결 간격 (ms)
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.registerHandlers();
    this.registerConnectionHandlers();

    return this.hubConnection
      .start()
      .then(() => {
        console.log('✅ SignalR 연결 성공');
        this.connectionStateSubject.next(this.hubConnection!.state);
      })
      .catch(err => {
        console.error('❌ SignalR 연결 실패:', err);
        throw err;
      });
  }

  /**
   * SignalR 연결을 종료합니다
   */
  public stopConnection(): Promise<void> {
    if (this.hubConnection) {
      return this.hubConnection.stop().then(() => {
        console.log('👋 SignalR 연결 종료');
      });
    }
    return Promise.resolve();
  }

  /**
   * 세션에 참여합니다
   * @param sessionId 세션 ID
   */
  public joinSession(sessionId: string): Promise<void> {
    if (!this.hubConnection) {
      return Promise.reject('SignalR 연결이 없습니다.');
    }

    return this.hubConnection.invoke('JoinSession', sessionId)
      .then(() => {
        console.log('✅ 세션 참여:', sessionId);
      })
      .catch(err => {
        console.error('❌ 세션 참여 실패:', err);
        throw err;
      });
  }

  /**
   * 세션에서 나갑니다
   * @param sessionId 세션 ID
   */
  public leaveSession(sessionId: string): Promise<void> {
    if (!this.hubConnection) {
      return Promise.reject('SignalR 연결이 없습니다.');
    }

    return this.hubConnection.invoke('LeaveSession', sessionId)
      .then(() => {
        console.log('👋 세션 나가기:', sessionId);
      })
      .catch(err => {
        console.error('❌ 세션 나가기 실패:', err);
        throw err;
      });
  }

  /**
   * 메시지를 전송합니다
   * @param request 메시지 전송 요청
   */
  public sendMessage(request: SendMessageRequest): Promise<void> {
    if (!this.hubConnection) {
      return Promise.reject('SignalR 연결이 없습니다.');
    }

    console.log('📤 메시지 전송:', request.message);

    return this.hubConnection.invoke('SendMessage', request)
      .catch(err => {
        console.error('❌ 메시지 전송 실패:', err);
        throw err;
      });
  }

  /**
   * 현재 연결 상태를 반환합니다
   */
  public getConnectionState(): signalR.HubConnectionState | undefined {
    return this.hubConnection?.state;
  }

  /**
   * 연결 여부를 확인합니다
   */
  public isConnected(): boolean {
    return this.hubConnection?.state === signalR.HubConnectionState.Connected;
  }

  /**
   * 서버에서 오는 이벤트 핸들러를 등록합니다
   */
  private registerHandlers(): void {
    if (!this.hubConnection) {
      return;
    }

    // 메시지 수신 핸들러 - NgZone 내에서 실행
    this.hubConnection.on('ReceiveMessage', (message: any) => {
      this.ngZone.run(() => {
        console.log('📩 메시지 수신:', message);

        const chatMessage: ChatMessage = {
          messageId: message.id?.toString(),
          sessionId: message.sessionId || '',
          role: message.role,
          content: message.content,
          timestamp: new Date(message.timestamp),
          category: message.category,
          productId: message.relatedProduct?.id,
          orderId: message.relatedOrder?.id
        };

        this.messageSubject.next(chatMessage);
      });
    });
  }

  /**
   * 연결 상태 변화 핸들러를 등록합니다
   */
  private registerConnectionHandlers(): void {
    if (!this.hubConnection) {
      return;
    }

    this.hubConnection.onreconnecting((error) => {
      this.ngZone.run(() => {
        console.warn('🔄 SignalR 재연결 시도 중...', error);
        this.connectionStateSubject.next(signalR.HubConnectionState.Reconnecting);
      });
    });

    this.hubConnection.onreconnected((connectionId) => {
      this.ngZone.run(() => {
        console.log('✅ SignalR 재연결 성공:', connectionId);
        this.connectionStateSubject.next(signalR.HubConnectionState.Connected);
      });
    });

    this.hubConnection.onclose((error) => {
      this.ngZone.run(() => {
        console.error('❌ SignalR 연결 종료:', error);
        this.connectionStateSubject.next(signalR.HubConnectionState.Disconnected);
      });
    });
  }
}
