import { Component, EventEmitter, Output, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-chat-input',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-input.component.html',
  styleUrls: ['./chat-input.component.scss']
})
export class ChatInputComponent {
  @Input() disabled: boolean = false;
  @Input() maxLength: number = 2000;
  @Output() messageSent = new EventEmitter<string>();

  messageText: string = '';

  onSendMessage(): void {
    if (this.messageText && this.messageText.trim().length > 0 && !this.disabled) {
      this.messageSent.emit(this.messageText);
      this.messageText = '';
    }
  }

  onKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.onSendMessage();
    }
  }
}
