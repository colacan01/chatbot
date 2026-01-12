-- product_embeddings 테이블 생성
-- 이 스크립트는 C# 프로그램이 자동으로 실행하므로 수동 실행은 선택사항입니다.

DROP TABLE IF EXISTS product_embeddings;

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
    detailed_description TEXT,
    specifications JSONB,
    stock_quantity INTEGER NOT NULL,
    is_available BOOLEAN NOT NULL DEFAULT true,
    image_url VARCHAR(500),
    embedding vector(768),
    searchable_text TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- 벡터 유사도 검색을 위한 인덱스 생성 (HNSW 알고리즘)
CREATE INDEX IF NOT EXISTS idx_product_embeddings_vector
ON product_embeddings USING hnsw (embedding vector_cosine_ops);

-- 일반 검색을 위한 인덱스
CREATE INDEX IF NOT EXISTS idx_product_embeddings_category ON product_embeddings(category);
CREATE INDEX IF NOT EXISTS idx_product_embeddings_brand ON product_embeddings(brand);
CREATE INDEX IF NOT EXISTS idx_product_embeddings_is_available ON product_embeddings(is_available);

-- 전문 검색을 위한 인덱스 (선택사항)
-- CREATE INDEX IF NOT EXISTS idx_product_embeddings_searchable_text
-- ON product_embeddings USING gin(to_tsvector('korean', searchable_text));

-- 테이블 정보 확인
\d product_embeddings
