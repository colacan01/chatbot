# 빠른 시작 가이드

## 사전 준비 체크리스트

### 1. Ollama 모델 설치 확인
원격 서버(172.30.1.40)에서 실행:
```bash
# Ollama 서비스 상태 확인
curl http://172.30.1.40:11434/api/tags

# 필요한 모델 설치
ollama pull exaone3.5:7.8b
ollama pull nomic-embed-text

# 설치된 모델 확인
ollama list
```

### 2. PostgreSQL 확인
```bash
# PostgreSQL 연결 테스트
psql -h 172.30.1.40 -p 54322 -U chatbot -d chatbot_dev

# pgvector 확장 설치 (슈퍼유저 권한 필요)
CREATE EXTENSION IF NOT EXISTS vector;

# 확장 확인
\dx vector
```

## 실행 방법

### Option 1: 자동 실행 스크립트 (권장)
```bash
cd /home/colacan/dev/dotnet/chatbot/scripts/VectorDataLoader
./run.sh
```

### Option 2: 수동 실행
```bash
cd /home/colacan/dev/dotnet/chatbot/scripts/VectorDataLoader/VectorDataLoader
dotnet run
```

### Option 3: 빌드 후 실행
```bash
cd /home/colacan/dev/dotnet/chatbot/scripts/VectorDataLoader/VectorDataLoader
dotnet build
dotnet run --no-build
```

## 예상 실행 시간

- **전체 소요 시간**: 약 4-7분
- **제품당 처리 시간**: 20-40초 × 10개 제품
  - AI 설명 생성: 15-30초
  - 벡터 임베딩: 3-5초
  - DB 저장: <1초

## 실행 과정

프로그램은 다음 6단계를 수행합니다:

1. ✓ 서비스 초기화 (Ollama, PostgreSQL)
2. ✓ 데이터베이스 연결 테스트
3. ✓ pgvector 확장 설치 및 테이블 생성
4. ✓ 제품 데이터 로드 (10개)
5. ✓ 각 제품 처리 (설명 생성 → 벡터화 → 저장)
6. ✓ 결과 확인 및 검색 테스트

## 성공 시 출력 예시

```
========================================
  제품 벡터 임베딩 데이터 로더
========================================

[1/6] 서비스 초기화 중...
  ✓ Ollama 서비스 초기화 완료
  ✓ 데이터베이스 서비스 초기화 완료

[2/6] 데이터베이스 연결 테스트 중...
[PostgreSQL] 데이터베이스 연결 성공!

[3/6] 데이터베이스 설정 중...
[PostgreSQL] pgvector 확장 확인/설치 완료
[PostgreSQL] product_embeddings 테이블 생성 완료 (벡터 차원: 768)

[4/6] 제품 데이터 로드 중...
  ✓ 10개 제품 데이터 로드 완료

[5/6] 제품 처리 중 (설명 생성 → 벡터화 → DB 저장)...
  ⚠️  이 작업은 시간이 걸릴 수 있습니다 (제품당 약 20-40초)

[1/10] 스피드스터 프로 카본 처리 중...
  [Ollama LLM] 스피드스터 프로 카본에 대한 설명 생성 중...
  [Ollama Embedding] 벡터 임베딩 생성 중...
  [Embedding] 768차원 벡터 생성 완료
  [DB] 스피드스터 프로 카본 저장 완료
  ✓ 완료!

[2/10] 마운틴 익스플로러 XT 처리 중...
...

[6/6] 처리 결과 확인 중...
  ✓ 데이터베이스에 저장된 레코드 수: 10
  ✓ 성공: 10개

[선택] 벡터 검색 테스트 수행 중...
  검색어: '가벼운 출퇴근용 자전거'

[벡터 검색 테스트 결과]
  1. 시티 커뮤터 디럭스 (Hybrid) - 유사도: 0.8234
  2. 컴팩트 폴더 (Folding) - 유사도: 0.7891
  3. 시티 이-커뮤터 (Electric) - 유사도: 0.7456

========================================
  모든 작업이 완료되었습니다!
========================================
```

## 결과 확인

### PostgreSQL에서 확인
```sql
-- 전체 제품 조회
SELECT product_code, name_korean, category, price
FROM product_embeddings
ORDER BY created_at DESC;

-- 벡터 차원 확인
SELECT product_code, name_korean,
       array_length(embedding, 1) as vector_dimension
FROM product_embeddings
WHERE embedding IS NOT NULL
LIMIT 5;

-- 저장된 레코드 수
SELECT COUNT(*) FROM product_embeddings;
```

### 벡터 검색 테스트
sql/03_vector_search_examples.sql 파일 참고

## 문제 해결

### Ollama 연결 실패
```bash
# Ollama 서비스 확인
curl http://172.30.1.40:11434/api/tags

# 방화벽 확인
telnet 172.30.1.40 11434
```

### 모델 없음
```bash
# 모델 설치
ollama pull exaone3.5:7.8b
ollama pull nomic-embed-text

# 설치 확인
ollama list
```

### PostgreSQL 연결 실패
```bash
# 연결 테스트
psql -h 172.30.1.40 -p 54322 -U chatbot -d chatbot_dev

# pg_hba.conf 확인 (서버에서)
# host    chatbot_dev    chatbot    0.0.0.0/0    md5
```

### pgvector 확장 오류
```sql
-- 슈퍼유저로 실행
CREATE EXTENSION vector;

-- 권한 확인
\dx
```

## 다음 단계

1. **데이터 확인**: PostgreSQL에서 저장된 데이터 조회
2. **검색 테스트**: sql/03_vector_search_examples.sql 파일의 쿼리 실행
3. **통합**: 기존 챗봇 시스템에 벡터 검색 기능 추가
4. **최적화**: 필요 시 인덱스 튜닝 및 성능 개선

## 참고 문서

- [README.md](README.md) - 상세 사용 가이드
- [SUMMARY.md](SUMMARY.md) - 프로젝트 전체 요약
- [sql/](sql/) - SQL 예제 스크립트

## 지원

문제가 발생하면 다음을 확인하세요:
1. Ollama 서비스 상태
2. PostgreSQL 연결 정보
3. 필요한 모델 설치 여부
4. 네트워크 연결 (172.30.1.40)
