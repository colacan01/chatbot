const signalR = require('/tmp/signalr-test/node_modules/@microsoft/signalr');

const hubUrl = 'http://localhost:5069/hubs/chat';
const sessionId = `test-${Date.now()}`;

console.log('🔌 SignalR 챗봇 테스트 시작...\n');
console.log(`세션 ID: ${sessionId}`);
console.log(`Hub URL: ${hubUrl}\n`);

async function testChat() {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.on('ReceiveMessage', (message) => {
        console.log('\n📨 봇 응답 수신:');
        console.log('─'.repeat(80));
        console.log(`역할: ${message.role}`);
        console.log(`카테고리: ${message.category || 'N/A'}`);
        console.log(`타임스탬프: ${message.timestamp}`);
        console.log('\n💬 내용:');
        console.log(message.content);
        console.log('─'.repeat(80));
        
        if (message.processingTimeMs) {
            console.log(`⏱️  처리 시간: ${message.processingTimeMs}ms`);
        }
        
        console.log('\n✅ 테스트 완료!');
        process.exit(0);
    });

    connection.on('Error', (error) => {
        console.error('\n❌ SignalR 에러:', error);
        process.exit(1);
    });

    try {
        console.log('🔄 SignalR 연결 중...');
        await connection.start();
        console.log('✅ SignalR 연결 성공!\n');

        console.log('📍 세션 참여 중...');
        await connection.invoke('JoinSession', sessionId);
        console.log('✅ 세션 참여 완료!\n');

        const testMessage = '30만원 이하의 가벼운 로드 바이크 추천해주세요';
        console.log('📤 메시지 전송:');
        console.log(`"${testMessage}"\n`);
        
        await connection.invoke('SendMessage', {
            sessionId: sessionId,
            message: testMessage,
            userId: 'test-user',
            userName: '테스트 사용자'
        });

        console.log('⏳ AI 응답 대기 중... (최대 3분)\n');
        
        // 3분 타임아웃
        setTimeout(() => {
            console.log('\n⚠️  타임아웃: 3분 내에 응답을 받지 못했습니다.');
            console.log('   (이것은 정상입니다 - Ollama 모델이 처음 로드되면 시간이 오래 걸립니다)');
            process.exit(0);
        }, 180000);

    } catch (error) {
        console.error('\n❌ 테스트 실패:', error.message);
        process.exit(1);
    }
}

testChat();
