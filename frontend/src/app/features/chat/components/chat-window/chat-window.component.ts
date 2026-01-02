import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChatService } from '../../../../core/services/chat.service';
import { ChatMessage } from '../../../../core/models/chat-message.model';
import { MessageListComponent } from '../message-list/message-list.component';
import { ChatInputComponent } from '../chat-input/chat-input.component';
import { environment } from '../../../../../environments/environment.development';

@Component({
  selector: 'app-chat-window',
  standalone: true,
  imports: [CommonModule, MessageListComponent, ChatInputComponent],
  templateUrl: './chat-window.component.html',
  styleUrls: ['./chat-window.component.scss']
})
export class ChatWindowComponent implements OnInit, OnDestroy {
  messages: ChatMessage[] = [];
  isTyping = false;
  isConnected = false;
  connectionError: string | null = null;

  constructor(private chatService: ChatService) {}

  ngOnInit(): void {
    // 메시지 구독
    this.chatService.messages$.subscribe(messages => {
      this.messages = messages;
    });

    // 타이핑 상태 구독
    this.chatService.isTyping$.subscribe(isTyping => {
      this.isTyping = isTyping;
    });

    // 연결 상태 구독
    this.chatService.isConnected$.subscribe(isConnected => {
      this.isConnected = isConnected;
      if (!isConnected) {
        this.connectionError = 'SignalR 연결이 끊어졌습니다.';
      } else {
        this.connectionError = null;
      }
    });

    // 채팅 서비스 초기화
    this.initializeChat();
  }

  ngOnDestroy(): void {
    this.chatService.disconnect();
  }

  private async initializeChat(): Promise<void> {
    try {
      await this.chatService.initialize(
        environment.signalRHubUrl,
        'user-' + Date.now(), // 임시 사용자 ID
        '게스트 사용자'
      );
      this.connectionError = null;
    } catch (error) {
      console.error('채팅 초기화 실패:', error);
      this.connectionError = '서버에 연결할 수 없습니다. 잠시 후 다시 시도해주세요.';
    }
  }

  async onSendMessage(message: string): Promise<void> {
    try {
      await this.chatService.sendMessage(message);
    } catch (error) {
      console.error('메시지 전송 실패:', error);
      this.connectionError = '메시지 전송에 실패했습니다.';
    }
  }

  async retry(): Promise<void> {
    this.connectionError = null;
    await this.initializeChat();
  }
}
