import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { ChatService } from '../../../../core/services/chat.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ChatSession } from '../../../../core/models/chat-session.model';

@Component({
  selector: 'app-chat-sidebar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-sidebar.component.html',
  styleUrl: './chat-sidebar.component.scss'
})
export class ChatSidebarComponent implements OnInit, OnDestroy {
  sessions: ChatSession[] = [];
  filteredSessions: ChatSession[] = [];
  currentSessionId: string | null = null;
  isCollapsed = false;
  isLoading = false;
  searchQuery = '';

  private destroy$ = new Subject<void>();

  constructor(
    private chatService: ChatService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadUserSessions();

    // 현재 세션 구독
    this.chatService.currentSession$
      .pipe(takeUntil(this.destroy$))
      .subscribe(session => {
        this.currentSessionId = session?.sessionId || null;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * 사용자의 세션 목록을 로드합니다
   */
  loadUserSessions(): void {
    this.isLoading = true;
    this.chatService.getUserSessions()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (sessions) => {
          this.sessions = sessions;
          this.filterSessions(); // 필터링 적용
          this.isLoading = false;
          console.log('✅ 세션 목록 로드 완료:', sessions.length, '개');
        },
        error: (err) => {
          console.error('❌ 세션 목록 로드 실패:', err);
          this.isLoading = false;
        }
      });
  }

  /**
   * 검색 쿼리에 따라 세션을 필터링합니다
   */
  filterSessions(): void {
    if (!this.searchQuery || this.searchQuery.trim() === '') {
      this.filteredSessions = [...this.sessions];
      return;
    }

    const query = this.searchQuery.toLowerCase().trim();
    this.filteredSessions = this.sessions.filter(session => {
      const title = this.getSessionTitle(session).toLowerCase();
      return title.includes(query);
    });

    console.log('🔍 검색 결과:', this.filteredSessions.length, '개 세션');
  }

  /**
   * 검색 입력이 변경될 때 호출됩니다
   */
  onSearchChange(): void {
    this.filterSessions();
  }

  /**
   * 검색어를 초기화합니다
   */
  clearSearch(): void {
    this.searchQuery = '';
    this.filterSessions();
  }

  /**
   * 세션을 선택하여 전환합니다
   */
  selectSession(sessionId: string): void {
    if (this.currentSessionId === sessionId) {
      return; // 이미 현재 세션
    }

    console.log('🔄 세션 선택:', sessionId);
    this.chatService.switchSession(sessionId);
  }

  /**
   * 새 대화를 시작합니다
   */
  startNewChat(): void {
    console.log('🆕 새 대화 시작');
    this.chatService.startNewSession();

    // 세션 목록 새로고침 (새 세션은 첫 메시지 후 서버에 저장됨)
    setTimeout(() => {
      this.loadUserSessions();
    }, 1000);
  }

  /**
   * 세션을 삭제합니다
   */
  deleteSession(sessionId: string, event: Event): void {
    event.stopPropagation(); // 세션 선택 이벤트 방지

    if (!confirm('이 대화를 삭제하시겠습니까?')) {
      return;
    }

    console.log('🗑️ 세션 삭제:', sessionId);

    this.chatService.deleteSession(sessionId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ 세션 삭제 완료');

          // 세션 목록에서 제거
          this.sessions = this.sessions.filter(s => s.sessionId !== sessionId);
          this.filterSessions(); // 필터링된 목록도 업데이트

          // 현재 세션이 삭제되면 새 세션 시작
          if (this.currentSessionId === sessionId) {
            this.startNewChat();
          }
        },
        error: (err) => {
          console.error('❌ 세션 삭제 실패:', err);
          alert('세션 삭제에 실패했습니다.');
        }
      });
  }

  /**
   * 사이드바 접기/펼치기
   */
  toggleSidebar(): void {
    this.isCollapsed = !this.isCollapsed;
    console.log('📂 사이드바 토글:', this.isCollapsed ? '접힘' : '펼침');
  }

  /**
   * 세션 제목을 포맷합니다 (없으면 기본값)
   */
  getSessionTitle(session: ChatSession): string {
    return session.title || '새 대화';
  }

  /**
   * 날짜를 포맷합니다
   */
  formatDate(date: Date): string {
    const now = new Date();
    const sessionDate = new Date(date);

    const diffMs = now.getTime() - sessionDate.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return '방금 전';
    if (diffMins < 60) return `${diffMins}분 전`;
    if (diffHours < 24) return `${diffHours}시간 전`;
    if (diffDays < 7) return `${diffDays}일 전`;

    // 1주일 이상: MM/DD HH:mm
    const month = String(sessionDate.getMonth() + 1).padStart(2, '0');
    const day = String(sessionDate.getDate()).padStart(2, '0');
    const hours = String(sessionDate.getHours()).padStart(2, '0');
    const mins = String(sessionDate.getMinutes()).padStart(2, '0');

    return `${month}/${day} ${hours}:${mins}`;
  }
}
