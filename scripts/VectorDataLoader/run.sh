#!/bin/bash

# 제품 벡터 임베딩 데이터 로더 실행 스크립트

echo "========================================"
echo "  제품 벡터 임베딩 데이터 로더"
echo "========================================"
echo ""

# 현재 스크립트 디렉토리로 이동
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/VectorDataLoader"

# Ollama 서비스 확인
echo "1. Ollama 서비스 연결 확인 중..."
if curl -s http://172.30.1.40:11434/api/tags > /dev/null 2>&1; then
    echo "   ✓ Ollama 서비스 연결 성공"
else
    echo "   ✗ Ollama 서비스에 연결할 수 없습니다."
    echo "   172.30.1.40:11434에서 Ollama가 실행 중인지 확인하세요."
    exit 1
fi

# 필요한 모델 확인
echo ""
echo "2. 필요한 Ollama 모델 확인 중..."
MODELS=$(curl -s http://172.30.1.40:11434/api/tags)

if echo "$MODELS" | grep -q "exaone3.5:7.8b"; then
    echo "   ✓ exaone3.5:7.8b 모델 확인됨"
else
    echo "   ✗ exaone3.5:7.8b 모델이 설치되지 않았습니다."
    echo "   다음 명령으로 설치하세요: ollama pull exaone3.5:7.8b"
    exit 1
fi

if echo "$MODELS" | grep -q "nomic-embed-text"; then
    echo "   ✓ nomic-embed-text 모델 확인됨"
else
    echo "   ✗ nomic-embed-text 모델이 설치되지 않았습니다."
    echo "   다음 명령으로 설치하세요: ollama pull nomic-embed-text"
    exit 1
fi

# PostgreSQL 연결 확인
echo ""
echo "3. PostgreSQL 연결 확인 중..."
if pg_isready -h 172.30.1.40 -p 54322 -U chatbot -d chatbot_dev > /dev/null 2>&1; then
    echo "   ✓ PostgreSQL 연결 성공"
else
    echo "   ⚠️  pg_isready를 사용할 수 없습니다."
    echo "   프로그램 실행 중에 연결을 확인합니다..."
fi

# 프로그램 실행
echo ""
echo "4. 데이터 로더 실행 중..."
echo ""
dotnet run

echo ""
echo "========================================"
echo "  실행 완료"
echo "========================================"
