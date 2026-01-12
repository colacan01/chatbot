# 프로젝트 요약

## 생성된 파일 목록

### 핵심 코드 파일
1. **Models/Product.cs** - 제품 데이터 모델
2. **Data/ProductSeedData.cs** - 10개 제품 시드 데이터
3. **Services/OllamaService.cs** - Ollama API 통신 (LLM 설명 생성 + 임베딩)
4. **Services/DatabaseService.cs** - PostgreSQL 데이터베이스 작업
5. **Program.cs** - 메인 실행 로직

### 실행 및 문서 파일
6. **run.sh** - 실행 스크립트 (사전 체크 포함)
7. **README.md** - 상세 사용 가이드
8. **SUMMARY.md** - 이 파일

### SQL 스크립트 (참고용)
9. **sql/01_create_extension.sql** - pgvector 확장 설치
10. **sql/02_create_table.sql** - 테이블 생성 스크립트
11. **sql/03_vector_search_examples.sql** - 벡터 검색 예제 쿼리

## 기술 스택

### 프로그래밍
- **언어**: C# 10.0
- **프레임워크**: .NET 9.0
- **데이터베이스**: PostgreSQL + pgvector 확장

### AI/ML 모델 (Ollama)
- **LLM**: exaone3.5:7.8b (제품 설명 생성)
- **임베딩**: nomic-embed-text (768차원 벡터)

### NuGet 패키지
- Npgsql 10.0.1
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0
- System.Text.Json 10.0.1

## 주요 기능

### 1. 제품 설명 자동 생성
- Ollama exaone3.5:7.8b 모델 사용
- 제품 정보(이름, 카테고리, 가격, 사양 등)를 입력으로 사용
- 200-300자 분량의 매력적인 상세 설명 생성
- 한국어 최적화

### 2. 벡터 임베딩 생성
- Ollama nomic-embed-text 모델 사용
- 768차원 벡터 생성
- 제품명, 카테고리, 브랜드, 설명 등을 결합하여 임베딩

### 3. PostgreSQL 벡터 검색
- pgvector 확장 사용
- HNSW 인덱스로 빠른 유사도 검색
- 코사인 유사도 기반 검색

### 4. 완전 자동화
- pgvector 확장 자동 설치
- 테이블 자동 생성
- 데이터 자동 적재
- 검색 테스트 자동 수행

## 실행 방법

### 간단 실행
```bash
cd /home/colacan/dev/dotnet/chatbot/scripts/VectorDataLoader
./run.sh
```

### 수동 실행
```bash
cd VectorDataLoader
dotnet run
```

## 실행 결과

### 예상 출력
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

... (반복)

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

### 처리 시간
- **전체**: 약 4-7분
- **제품당**: 20-40초
  - 설명 생성: 15-30초
  - 임베딩: 3-5초
  - DB 저장: <1초

## 데이터베이스 스키마

### 테이블: product_embeddings

| 컬럼명 | 타입 | 설명 |
|--------|------|------|
| id | SERIAL | 기본 키 |
| product_code | VARCHAR(50) | 제품 코드 (UNIQUE) |
| name | VARCHAR(200) | 영문 이름 |
| name_korean | VARCHAR(200) | 한글 이름 |
| category | VARCHAR(50) | 카테고리 |
| brand | VARCHAR(100) | 브랜드 |
| price | DECIMAL(10,2) | 가격 |
| description | TEXT | 영문 설명 |
| description_korean | TEXT | 한글 설명 |
| detailed_description | TEXT | **AI 생성 상세 설명** |
| specifications | JSONB | 사양 정보 (JSON) |
| stock_quantity | INTEGER | 재고 수량 |
| is_available | BOOLEAN | 판매 가능 여부 |
| image_url | VARCHAR(500) | 이미지 URL |
| **embedding** | **vector(768)** | **벡터 임베딩** |
| searchable_text | TEXT | 검색용 텍스트 |
| created_at | TIMESTAMP | 생성 시각 |
| updated_at | TIMESTAMP | 수정 시각 |

### 인덱스
- `idx_product_embeddings_vector` - HNSW 벡터 인덱스 (코사인 유사도)
- `idx_product_embeddings_category` - 카테고리 인덱스
- `idx_product_embeddings_brand` - 브랜드 인덱스
- `idx_product_embeddings_is_available` - 판매가능 인덱스
- `idx_product_embeddings_searchable_text` - GIN 전문검색 인덱스

## 벡터 검색 사용법

### PostgreSQL에서 직접 검색

```sql
-- 1. Ollama로 검색어 임베딩 생성 (별도 API 호출 필요)
-- curl -X POST http://172.30.1.40:11434/api/embed \
--   -d '{"model":"nomic-embed-text","input":"출퇴근용 자전거"}'

-- 2. 벡터 유사도 검색
SELECT
    product_code,
    name_korean,
    category,
    price,
    1 - (embedding <=> '[벡터값]'::vector) as similarity
FROM product_embeddings
WHERE embedding IS NOT NULL
ORDER BY embedding <=> '[벡터값]'::vector
LIMIT 5;
```

### C#에서 사용 (예제)

```csharp
var ollamaService = new OllamaService("http://172.30.1.40:11434");
var dbService = new DatabaseService("172.30.1.40", 54322, "chatbot_dev", "chatbot", "chatbot");

// 검색어를 벡터로 변환
var queryEmbedding = await ollamaService.GenerateEmbeddingAsync("가벼운 출퇴근용 자전거");

// 벡터 검색
await dbService.TestVectorSearchAsync(queryEmbedding, 5);
```

## 처리된 제품 목록

| 코드 | 이름 | 카테고리 | 가격 |
|------|------|----------|------|
| ROAD-001 | 스피드스터 프로 카본 | Road | 3,500,000원 |
| MTB-001 | 마운틴 익스플로러 XT | Mountain | 2,800,000원 |
| HYBRID-001 | 시티 커뮤터 디럭스 | Hybrid | 890,000원 |
| EBIKE-001 | 이파워 크루저 | Electric | 4,200,000원 |
| ROAD-002 | 에어로 스프린트 엘리트 | Road | 4,500,000원 |
| MTB-002 | 트레일 블레이저 프로 | Mountain | 1,950,000원 |
| FOLD-001 | 컴팩트 폴더 | Folding | 650,000원 |
| KIDS-001 | 주니어 레이서 | Kids | 350,000원 |
| GRAVEL-001 | 어드벤처 시커 | Gravel | 3,200,000원 |
| EBIKE-002 | 시티 이-커뮤터 | Electric | 2,800,000원 |

## 환경 설정

### Ollama 서버
- **호스트**: 172.30.1.40
- **포트**: 11434
- **필수 모델**:
  - exaone3.5:7.8b
  - nomic-embed-text

### PostgreSQL 서버
- **호스트**: 172.30.1.40
- **포트**: 54322
- **데이터베이스**: chatbot_dev
- **사용자**: chatbot
- **비밀번호**: chatbot
- **필수 확장**: pgvector

## 문제 해결

### 자주 발생하는 오류

1. **Ollama 연결 실패**
   - 원인: Ollama 서비스 미실행
   - 해결: `systemctl status ollama` 확인

2. **모델 없음**
   - 원인: 필요한 모델 미설치
   - 해결:
     ```bash
     ollama pull exaone3.5:7.8b
     ollama pull nomic-embed-text
     ```

3. **PostgreSQL 연결 실패**
   - 원인: 방화벽, 잘못된 인증 정보
   - 해결: `pg_hba.conf` 및 연결 정보 확인

4. **pgvector 확장 오류**
   - 원인: 확장 미설치 또는 권한 부족
   - 해결: 슈퍼유저로 `CREATE EXTENSION vector;` 실행

## 향후 개선 사항

1. **성능 최적화**
   - 배치 처리로 임베딩 생성 속도 향상
   - 병렬 처리 구현

2. **오류 처리**
   - 재시도 로직 추가
   - 부분 실패 시 복구 기능

3. **모니터링**
   - 진행률 표시 개선
   - 로그 파일 저장

4. **유연성**
   - 설정 파일 외부화 (appsettings.json)
   - 명령줄 인자 지원

## 라이선스

자전거 쇼핑몰 챗봇 시스템의 일부입니다.
