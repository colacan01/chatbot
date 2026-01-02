#!/bin/bash

echo "Testing Ollama response time with product context..."
echo "Start time: $(date '+%H:%M:%S')"

curl -s -X POST http://localhost:11434/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "model": "qwen2.5:14b",
    "messages": [
      {
        "role": "system",
        "content": "당신은 자전거 온라인 쇼핑몰의 전문 AI 상담원입니다. 현재 판매 중인 제품: 스피드스터 프로 카본 (로드, 350만원), 에어로 스프린트 엘리트 (로드, 450만원), 어드벤처 시커 (그래블, 320만원)"
      },
      {
        "role": "user",
        "content": "가벼운 로드 바이크 추천해주세요. 예산은 300만원입니다."
      }
    ],
    "stream": false,
    "options": {
      "temperature": 0.7
    }
  }' | jq -r '.message.content'

echo ""
echo "End time: $(date '+%H:%M:%S')"
