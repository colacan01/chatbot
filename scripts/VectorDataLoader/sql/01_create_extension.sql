-- pgvector 확장 설치
-- PostgreSQL 슈퍼유저 권한이 필요할 수 있습니다.

CREATE EXTENSION IF NOT EXISTS vector;

-- 설치 확인
SELECT * FROM pg_extension WHERE extname = 'vector';
