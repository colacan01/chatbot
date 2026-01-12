using Npgsql;
using VectorDataLoader.Models;

namespace VectorDataLoader.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private const string TableName = "product_embeddings";
    private const int VectorDimension = 768;

    public DatabaseService(string host, int port, string database, string username, string password)
    {
        _connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    }

    /// <summary>
    /// 데이터베이스 연결을 테스트합니다.
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            Console.WriteLine("[PostgreSQL] 데이터베이스 연결 성공!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] 데이터베이스 연결 실패: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// pgvector 확장을 설치합니다.
    /// </summary>
    public async Task EnsurePgVectorExtensionAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var cmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS vector", connection);
            await cmd.ExecuteNonQueryAsync();

            Console.WriteLine("[PostgreSQL] pgvector 확장 확인/설치 완료");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] pgvector 확장 설치 실패: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// product_embeddings 테이블을 생성합니다.
    /// </summary>
    public async Task CreateTableAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var createTableSql = $@"
                DROP TABLE IF EXISTS {TableName};

                CREATE TABLE {TableName} (
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
                    embedding vector({VectorDimension}),
                    searchable_text TEXT,
                    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
                );

                -- 벡터 유사도 검색을 위한 인덱스 생성 (HNSW 알고리즘)
                CREATE INDEX IF NOT EXISTS idx_product_embeddings_vector
                ON {TableName} USING hnsw (embedding vector_cosine_ops);

                -- 일반 검색을 위한 인덱스
                CREATE INDEX IF NOT EXISTS idx_product_embeddings_category ON {TableName}(category);
                CREATE INDEX IF NOT EXISTS idx_product_embeddings_brand ON {TableName}(brand);
                CREATE INDEX IF NOT EXISTS idx_product_embeddings_is_available ON {TableName}(is_available);

                -- 전문 검색을 위한 인덱스 (선택사항)
                -- CREATE INDEX IF NOT EXISTS idx_product_embeddings_searchable_text
                -- ON {TableName} USING gin(to_tsvector('korean', searchable_text));
            ";

            await using var cmd = new NpgsqlCommand(createTableSql, connection);
            await cmd.ExecuteNonQueryAsync();

            Console.WriteLine($"[PostgreSQL] {TableName} 테이블 생성 완료 (벡터 차원: {VectorDimension})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] 테이블 생성 실패: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 제품과 임베딩을 데이터베이스에 삽입합니다.
    /// </summary>
    public async Task InsertProductEmbeddingAsync(Product product, float[] embedding, string searchableText)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var insertSql = $@"
                INSERT INTO {TableName}
                (product_code, name, name_korean, category, brand, price,
                 description, description_korean, detailed_description,
                 specifications, stock_quantity, is_available, image_url,
                 embedding, searchable_text)
                VALUES
                (@product_code, @name, @name_korean, @category, @brand, @price,
                 @description, @description_korean, @detailed_description,
                 @specifications::jsonb, @stock_quantity, @is_available, @image_url,
                 @embedding::vector, @searchable_text)
                ON CONFLICT (product_code)
                DO UPDATE SET
                    name = EXCLUDED.name,
                    name_korean = EXCLUDED.name_korean,
                    category = EXCLUDED.category,
                    brand = EXCLUDED.brand,
                    price = EXCLUDED.price,
                    description = EXCLUDED.description,
                    description_korean = EXCLUDED.description_korean,
                    detailed_description = EXCLUDED.detailed_description,
                    specifications = EXCLUDED.specifications,
                    stock_quantity = EXCLUDED.stock_quantity,
                    is_available = EXCLUDED.is_available,
                    image_url = EXCLUDED.image_url,
                    embedding = EXCLUDED.embedding,
                    searchable_text = EXCLUDED.searchable_text,
                    updated_at = NOW()
            ";

            await using var cmd = new NpgsqlCommand(insertSql, connection);
            cmd.Parameters.AddWithValue("product_code", product.ProductCode);
            cmd.Parameters.AddWithValue("name", product.Name);
            cmd.Parameters.AddWithValue("name_korean", product.NameKorean);
            cmd.Parameters.AddWithValue("category", product.Category);
            cmd.Parameters.AddWithValue("brand", product.Brand);
            cmd.Parameters.AddWithValue("price", product.Price);
            cmd.Parameters.AddWithValue("description", product.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("description_korean", product.DescriptionKorean ?? string.Empty);
            cmd.Parameters.AddWithValue("detailed_description", product.DetailedDescription ?? string.Empty);
            cmd.Parameters.AddWithValue("specifications", product.Specifications ?? "{}");
            cmd.Parameters.AddWithValue("stock_quantity", product.StockQuantity);
            cmd.Parameters.AddWithValue("is_available", product.IsAvailable);
            cmd.Parameters.AddWithValue("image_url", product.ImageUrl ?? string.Empty);
            cmd.Parameters.AddWithValue("embedding", $"[{string.Join(",", embedding)}]");
            cmd.Parameters.AddWithValue("searchable_text", searchableText);

            await cmd.ExecuteNonQueryAsync();

            Console.WriteLine($"  [DB] {product.NameKorean} 저장 완료");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [ERROR] 제품 저장 실패 ({product.ProductCode}): {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 저장된 레코드 수를 확인합니다.
    /// </summary>
    public async Task<int> GetRecordCountAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM {TableName}", connection);
            var count = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(count);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] 레코드 수 확인 실패: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 벡터 검색 테스트를 수행합니다.
    /// </summary>
    public async Task TestVectorSearchAsync(float[] queryVector, int topK = 3)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var searchSql = $@"
                SELECT
                    product_code,
                    name_korean,
                    category,
                    1 - (embedding <=> @query_vector::vector) as similarity
                FROM {TableName}
                WHERE embedding IS NOT NULL
                ORDER BY embedding <=> @query_vector::vector
                LIMIT @top_k
            ";

            await using var cmd = new NpgsqlCommand(searchSql, connection);
            cmd.Parameters.AddWithValue("query_vector", $"[{string.Join(",", queryVector)}]");
            cmd.Parameters.AddWithValue("top_k", topK);

            await using var reader = await cmd.ExecuteReaderAsync();

            Console.WriteLine("\n[벡터 검색 테스트 결과]");
            var rank = 1;
            while (await reader.ReadAsync())
            {
                var productCode = reader.GetString(0);
                var nameKorean = reader.GetString(1);
                var category = reader.GetString(2);
                var similarity = reader.GetDouble(3);

                Console.WriteLine($"  {rank}. {nameKorean} ({category}) - 유사도: {similarity:F4}");
                rank++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] 벡터 검색 테스트 실패: {ex.Message}");
        }
    }
}
