import { Component } from '@angular/core';
import { ChatWindowComponent } from './features/chat/components/chat-window/chat-window.component';

@Component({
  selector: 'app-root',
  imports: [ChatWindowComponent],
  template: '<app-chat-window></app-chat-window>',
  styleUrl: './app.scss'
})
export class App {
  title = '자전거 쇼핑 챗봇';
}
