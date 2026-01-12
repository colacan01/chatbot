# 제품 벡터 임베딩 데이터 로더

PostgreSQL pgvector 테이블에 자전거 쇼핑몰 제품 데이터를 벡터 임베딩과 함께 적재하는 C# 콘솔 애플리케이션입니다.

## 기능

1. **제품 상세 설명 생성**: Ollama exaone3.5:7.8b 모델로 각 제품의 매력적인 상세 설명 자동 생성 (200-300자)
2. **벡터 임베딩 생성**: Ollama nomic-embed-text 모델로 768차원 벡터 생성
3. **PostgreSQL 저장**: pgvector 확장을 사용하여 벡터 검색이 가능한 테이블에 데이터 저장
4. **벡터 검색 테스트**: 저장 후 자동으로 벡터 유사도 검색 테스트 수행

## 시스템 요구사항

### 필수 소프트웨어
- .NET 9.0 SDK 이상
- PostgreSQL (pgvector 확장 지원)
- Ollama 서비스

### Ollama 모델
다음 모델들이 설치되어 있어야 합니다:
```bash
ollama pull exaone3.5:7.8b
ollama pull nomic-embed-text
```

## 데이터베이스 설정

### 연결 정보
- Host: `172.30.1.40`
- Port: `54322`
- Database: `chatbot_dev`
- Username: `chatbot`
- Password: `chatbot`

### 생성되는 테이블: `product_embeddings`

```sql
CREATE TABLE product_embeddings (
    id SERIAL PRIMARY KEY,
    product_code VARCHAR(50) UNIQUE NOT NULL,
    name VARCHAR(200) NOT NULL,
    name_korean VARCHAR(200) NOT NULL,
    category VARCHAR(50) NOT NULL,
    brand VARCHAR(100) NOT NULL,
    price DECIMAL(10, 2) NOT NULL,
    description TEXT,
    description_korean TEXT,
    detailed_description TEXT,              -- AI가 생성한 상세 설명
    specifications JSONB,
    stock_quantity INTEGER NOT NULL,
    is_available BOOLEAN NOT NULL DEFAULT true,
    image_url VARCHAR(500),
    embedding vector(768),                  -- 768차원 벡터 임베딩
    searchable_text TEXT,                   -- 검색용 결합 텍스트
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- 벡터 유사도 검색을 위한 HNSW 인덱스
CREATE INDEX idx_product_embeddings_vector
ON product_embeddings USING hnsw (embedding vector_cosine_ops);
```

### 생성되는 인덱스
- HNSW 벡터 인덱스 (코사인 유사도)
- category, brand, is_available 일반 인덱스
- searchable_text GIN 인덱스 (전문 검색용)

## 사용 방법

### 1. 실행 스크립트 사용 (권장)
```bash
cd /home/colacan/dev/dotnet/chatbot/scripts/VectorDataLoader
./run.sh
```

실행 스크립트는 다음을 자동으로 확인합니다:
- Ollama 서비스 연결 상태
- 필요한 모델 설치 여부
- PostgreSQL 연결 (선택사항)

### 2. 직접 실행
```bash
cd VectorDataLoader
dotnet run
```

### 3. 빌드 후 실행
```bash
cd VectorDataLoader
dotnet build
dotnet run --no-build
```

## 실행 과정

프로그램은 다음 단계를 수행합니다:

1. **서비스 초기화**: Ollama 및 PostgreSQL 서비스 연결
2. **데이터베이스 연결 테스트**: PostgreSQL 연결 확인
3. **데이터베이스 설정**: pgvector 확장 설치 및 테이블 생성
4. **제품 데이터 로드**: 10개 제품 데이터 로드
5. **제품 처리** (각 제품마다):
   - AI로 상세 설명 생성 (exaone3.5:7.8b)
   - 검색용 텍스트 결합
   - 벡터 임베딩 생성 (nomic-embed-text, 768차원)
   - PostgreSQL에 저장
6. **결과 확인**: 저장된 레코드 수 출력
7. **벡터 검색 테스트**: "가벼운 출퇴근용 자전거" 검색 시연

## 예상 소요 시간

- **전체 처리 시간**: 약 4-7분 (제품 10개 기준)
- **제품당 처리 시간**: 약 20-40초
  - 설명 생성: 15-30초 (exaone3.5:7.8b)
  - 임베딩 생성: 3-5초 (nomic-embed-text)
  - DB 저장: <1초

첫 실행 시 Ollama가 모델을 메모리에 로드하는 시간이 추가될 수 있습니다 (1-2분).

## 처리되는 제품 데이터

총 10개 제품:
1. 스피드스터 프로 카본 (ROAD-001) - 로드 바이크
2. 마운틴 익스플로러 XT (MTB-001) - 산악 자전거
3. 시티 커뮤터 디럭스 (HYBRID-001) - 하이브리드
4. 이파워 크루저 (EBIKE-001) - 전기 자전거
5. 에어로 스프린트 엘리트 (ROAD-002) - 로드 바이크
6. 트레일 블레이저 프로 (MTB-002) - 산악 자전거
7. 컴팩트 폴더 (FOLD-001) - 접이식 자전거
8. 주니어 레이서 (KIDS-001) - 어린이용
9. 어드벤처 시커 (GRAVEL-001) - 그래블 바이크
10. 시티 이-커뮤터 (EBIKE-002) - 전기 자전거

## 프로젝트 구조

```
VectorDataLoader/
├── Data/
│   └── ProductSeedData.cs          # 제품 시드 데이터
├── Models/
│   └── Product.cs                  # 제품 모델
├── Services/
│   ├── OllamaService.cs           # Ollama API 통신 (LLM + 임베딩)
│   └── DatabaseService.cs         # PostgreSQL 데이터베이스 작업
├── Program.cs                      # 메인 엔트리 포인트
└── VectorDataLoader.csproj         # 프로젝트 파일
```

## 의존성

```xml
<PackageReference Include="Npgsql" Version="10.0.1" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
<PackageReference Include="System.Text.Json" Version="10.0.1" />
```

## 설정 변경

`Program.cs` 파일의 상단에서 설정을 변경할 수 있습니다:

```csharp
// Ollama 서비스 URL
const string ollamaUrl = "http://172.30.1.40:11434";

// PostgreSQL 연결 정보
const string dbHost = "172.30.1.40";
const int dbPort = 54322;
const string dbName = "chatbot_dev";
const string dbUser = "chatbot";
const string dbPassword = "chatbot";
```

## 벡터 검색 사용 예제

데이터 적재 후 PostgreSQL에서 벡터 검색을 수행할 수 있습니다:

```sql
-- 코사인 유사도 검색 (가장 유사한 제품 3개)
SELECT
    product_code,
    name_korean,
    category,
    price,
    1 - (embedding <=> '[벡터값]'::vector) as similarity
FROM product_embeddings
WHERE embedding IS NOT NULL
ORDER BY embedding <=> '[벡터값]'::vector
LIMIT 3;
```

벡터값은 Ollama nomic-embed-text 모델로 검색 쿼리를 임베딩하여 얻을 수 있습니다.

## 문제 해결

### Ollama 연결 실패
- Ollama 서비스가 `172.30.1.40:11434`에서 실행 중인지 확인
- 방화벽 설정 확인

### 모델 없음 오류
```bash
ollama pull exaone3.5:7.8b
ollama pull nomic-embed-text
```

### PostgreSQL 연결 실패
- 호스트, 포트, 데이터베이스명, 사용자 정보 확인
- PostgreSQL이 외부 연결을 허용하는지 확인 (`pg_hba.conf`)

### pgvector 확장 오류
```sql
CREATE EXTENSION vector;
```
슈퍼유저 권한이 필요할 수 있습니다.

## 참고 자료

- [pgvector GitHub](https://github.com/pgvector/pgvector)
- [Ollama Documentation](https://github.com/ollama/ollama/blob/main/docs/api.md)
- [nomic-embed-text Model](https://ollama.com/library/nomic-embed-text)

## 라이선스

이 프로젝트는 자전거 쇼핑몰 챗봇 시스템의 일부입니다.
