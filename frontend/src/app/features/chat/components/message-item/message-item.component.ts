import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChatMessage, MessageRole } from '../../../../core/models/chat-message.model';
import { formatDistanceToNow } from 'date-fns';
import { ko } from 'date-fns/locale';

@Component({
  selector: 'app-message-item',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './message-item.component.html',
  styleUrls: ['./message-item.component.scss']
})
export class MessageItemComponent {
  @Input() message!: ChatMessage;

  MessageRole = MessageRole;

  get isUser(): boolean {
    return this.message.role === MessageRole.User;
  }

  get isAssistant(): boolean {
    return this.message.role === MessageRole.Assistant;
  }

  get isStreaming(): boolean {
    return this.message.isStreaming === true;
  }

  get timeAgo(): string {
    return formatDistanceToNow(this.message.timestamp, {
      addSuffix: true,
      locale: ko
    });
  }

  get formattedContent(): string {
    // 간단한 마크다운 파싱 (볼드, 줄바꿈)
    let content = this.message.content;

    // 줄바꿈 처리
    content = content.replace(/\n/g, '<br>');

    // 볼드 처리 (**텍스트**)
    content = content.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');

    return content;
  }
}
