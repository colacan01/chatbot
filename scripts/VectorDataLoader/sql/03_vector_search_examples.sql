-- 벡터 검색 예제 쿼리
-- 데이터 로딩 후 테스트용으로 사용할 수 있습니다.

-- 1. 전체 제품 조회
SELECT
    id,
    product_code,
    name_korean,
    category,
    brand,
    price,
    stock_quantity
FROM product_embeddings
ORDER BY created_at DESC;

-- 2. 저장된 벡터 정보 확인
SELECT
    product_code,
    name_korean,
    category,
    array_length(embedding, 1) as vector_dimension
FROM product_embeddings
WHERE embedding IS NOT NULL
LIMIT 5;

-- 3. 벡터 유사도 검색 (예제 - 실제 사용 시 벡터값을 Ollama로부터 받아야 함)
-- 코사인 유사도: <=> 연산자 사용
-- 1 - (embedding <=> query_vector)로 유사도 점수 계산 (1에 가까울수록 유사)
/*
WITH query_vector AS (
    SELECT '[벡터 배열]'::vector as vec
)
SELECT
    product_code,
    name_korean,
    category,
    price,
    1 - (embedding <=> (SELECT vec FROM query_vector)) as similarity_score
FROM product_embeddings
WHERE embedding IS NOT NULL
ORDER BY embedding <=> (SELECT vec FROM query_vector)
LIMIT 5;
*/

-- 4. 카테고리별 제품 수
SELECT
    category,
    COUNT(*) as product_count,
    COUNT(embedding) as products_with_embeddings
FROM product_embeddings
GROUP BY category
ORDER BY product_count DESC;

-- 5. 가격대별 제품 분포
SELECT
    CASE
        WHEN price < 1000000 THEN '100만원 미만'
        WHEN price < 2000000 THEN '100-200만원'
        WHEN price < 3000000 THEN '200-300만원'
        WHEN price < 4000000 THEN '300-400만원'
        ELSE '400만원 이상'
    END as price_range,
    COUNT(*) as count
FROM product_embeddings
GROUP BY price_range
ORDER BY MIN(price);

-- 6. 브랜드별 제품 및 평균 가격
SELECT
    brand,
    COUNT(*) as product_count,
    ROUND(AVG(price)) as avg_price,
    MIN(price) as min_price,
    MAX(price) as max_price
FROM product_embeddings
GROUP BY brand
ORDER BY product_count DESC;

-- 7. 텍스트 기반 전문 검색 (to_tsvector 사용)
SELECT
    product_code,
    name_korean,
    category,
    price,
    ts_rank(to_tsvector('korean', searchable_text), query) as rank
FROM product_embeddings,
     to_tsquery('korean', '출퇴근 | 자전거') as query
WHERE to_tsvector('korean', searchable_text) @@ query
ORDER BY rank DESC;

-- 8. 재고가 있는 제품만 조회
SELECT
    product_code,
    name_korean,
    category,
    price,
    stock_quantity
FROM product_embeddings
WHERE is_available = true AND stock_quantity > 0
ORDER BY stock_quantity DESC;

-- 9. 가장 최근에 추가된 제품
SELECT
    product_code,
    name_korean,
    category,
    price,
    created_at
FROM product_embeddings
ORDER BY created_at DESC
LIMIT 5;

-- 10. 특정 제품의 상세 정보 조회
SELECT
    product_code,
    name,
    name_korean,
    category,
    brand,
    price,
    description_korean,
    detailed_description,
    specifications,
    stock_quantity,
    is_available,
    created_at
FROM product_embeddings
WHERE product_code = 'ROAD-001';
