import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { ChatService } from '../../../../core/services/chat.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ChatMessage } from '../../../../core/models/chat-message.model';
import { MessageListComponent } from '../message-list/message-list.component';
import { ChatInputComponent } from '../chat-input/chat-input.component';
import { ChatSidebarComponent } from '../chat-sidebar/chat-sidebar.component';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-chat-window',
  standalone: true,
  imports: [CommonModule, MessageListComponent, ChatInputComponent, ChatSidebarComponent],
  templateUrl: './chat-window.component.html',
  styleUrls: ['./chat-window.component.scss']
})
export class ChatWindowComponent implements OnInit, OnDestroy {
  messages: ChatMessage[] = [];
  isTyping = false;
  isConnected = false;
  connectionError: string | null = null;
  currentUser: any = null;

  private destroy$ = new Subject<void>();

  constructor(
    private chatService: ChatService,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // 현재 사용자 정보 가져오기
    this.authService.currentUser$
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        this.currentUser = user;
        console.log('👤 현재 사용자:', user);
      });

    // 메시지 구독
    this.chatService.messages$
      .pipe(takeUntil(this.destroy$))
      .subscribe(messages => {
        console.log('💬 메시지 배열 업데이트:', messages.length, '개');
        this.messages = messages;
        this.cdr.detectChanges();
      });

    // 타이핑 상태 구독
    this.chatService.isTyping$
      .pipe(takeUntil(this.destroy$))
      .subscribe(isTyping => {
        console.log('⌨️ 타이핑 상태:', isTyping);
        this.isTyping = isTyping;
        this.cdr.detectChanges();
      });

    // 연결 상태 구독
    this.chatService.isConnected$
      .pipe(takeUntil(this.destroy$))
      .subscribe(isConnected => {
        console.log('🔌 연결 상태:', isConnected);
        this.isConnected = isConnected;
        if (!isConnected) {
          this.connectionError = 'SignalR 연결이 끊어졌습니다.';
        } else {
          this.connectionError = null;
        }
        this.cdr.detectChanges();
      });

    // 채팅 서비스 초기화
    this.initializeChat();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.chatService.disconnect();
  }

  private async initializeChat(): Promise<void> {
    try {
      const userId = this.currentUser?.id;
      const userName = this.currentUser?.userName || '게스트 사용자';

      await this.chatService.initialize(
        environment.signalRHubUrl,
        userId,
        userName
      );
      this.connectionError = null;
      console.log('✅ 채팅 초기화 성공');
    } catch (error) {
      console.error('❌ 채팅 초기화 실패:', error);
      this.connectionError = '서버에 연결할 수 없습니다. 잠시 후 다시 시도해주세요.';
    }
  }

  async onSendMessage(message: string): Promise<void> {
    try {
      // 스트리밍 방식으로 메시지 전송
      await this.chatService.sendMessageStream(message);
    } catch (error) {
      console.error('스트리밍 메시지 전송 실패, 기존 방식으로 재시도:', error);

      // 스트리밍 실패 시 기존 방식으로 폴백
      try {
        await this.chatService.sendMessage(message);
      } catch (fallbackError) {
        console.error('메시지 전송 완전 실패:', fallbackError);
        this.connectionError = '메시지 전송에 실패했습니다.';
      }
    }
  }

  async retry(): Promise<void> {
    this.connectionError = null;
    await this.initializeChat();
  }
}
