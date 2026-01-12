using VectorDataLoader.Data;
using VectorDataLoader.Services;

namespace VectorDataLoader;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  제품 벡터 임베딩 데이터 로더");
        Console.WriteLine("========================================\n");

        // 설정
        const string ollamaUrl = "http://172.30.1.40:11434";
        const string dbHost = "172.30.1.40";
        const int dbPort = 54322;
        const string dbName = "chatbot_dev";
        const string dbUser = "chatbot";
        const string dbPassword = "chatbot";

        try
        {
            // 1. 서비스 초기화
            Console.WriteLine("[1/6] 서비스 초기화 중...");
            var ollamaService = new OllamaService(ollamaUrl);
            var databaseService = new DatabaseService(dbHost, dbPort, dbName, dbUser, dbPassword);
            Console.WriteLine("  ✓ Ollama 서비스 초기화 완료");
            Console.WriteLine("  ✓ 데이터베이스 서비스 초기화 완료\n");

            // 2. 데이터베이스 연결 테스트
            Console.WriteLine("[2/6] 데이터베이스 연결 테스트 중...");
            if (!await databaseService.TestConnectionAsync())
            {
                Console.WriteLine("\n데이터베이스 연결 실패. 프로그램을 종료합니다.");
                return;
            }
            Console.WriteLine();

            // 3. pgvector 확장 설치 및 테이블 생성
            Console.WriteLine("[3/6] 데이터베이스 설정 중...");
            await databaseService.EnsurePgVectorExtensionAsync();
            await databaseService.CreateTableAsync();
            Console.WriteLine();

            // 4. 제품 데이터 로드
            Console.WriteLine("[4/6] 제품 데이터 로드 중...");
            var products = ProductSeedData.GetSeedProducts();
            Console.WriteLine($"  ✓ {products.Count}개 제품 데이터 로드 완료\n");

            // 5. 각 제품에 대해 설명 생성 및 벡터화
            Console.WriteLine("[5/6] 제품 처리 중 (설명 생성 → 벡터화 → DB 저장)...");
            Console.WriteLine("  ⚠️  이 작업은 시간이 걸릴 수 있습니다 (제품당 약 20-40초)\n");

            var successCount = 0;
            var failCount = 0;

            for (var i = 0; i < products.Count; i++)
            {
                var product = products[i];
                Console.WriteLine($"[{i + 1}/{products.Count}] {product.NameKorean} 처리 중...");

                try
                {
                    // 5-1. AI로 상세 설명 생성
                    product.DetailedDescription = await ollamaService.GenerateProductDescriptionAsync(product);

                    // 5-2. 검색용 텍스트 생성
                    var searchableText = ollamaService.BuildSearchableText(product);

                    // 5-3. 벡터 임베딩 생성
                    Console.WriteLine($"  [Ollama Embedding] 벡터 임베딩 생성 중...");
                    var embedding = await ollamaService.GenerateEmbeddingAsync(searchableText);

                    if (embedding == null || embedding.Length == 0)
                    {
                        Console.WriteLine($"  [ERROR] 벡터 임베딩 생성 실패\n");
                        failCount++;
                        continue;
                    }

                    Console.WriteLine($"  [Embedding] {embedding.Length}차원 벡터 생성 완료");

                    // 5-4. 데이터베이스에 저장
                    await databaseService.InsertProductEmbeddingAsync(product, embedding, searchableText);

                    Console.WriteLine($"  ✓ 완료!\n");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  [ERROR] 처리 실패: {ex.Message}\n");
                    failCount++;
                }
            }

            // 6. 결과 확인
            Console.WriteLine("[6/6] 처리 결과 확인 중...");
            var totalRecords = await databaseService.GetRecordCountAsync();
            Console.WriteLine($"  ✓ 데이터베이스에 저장된 레코드 수: {totalRecords}");
            Console.WriteLine($"  ✓ 성공: {successCount}개");
            if (failCount > 0)
            {
                Console.WriteLine($"  ✗ 실패: {failCount}개");
            }
            Console.WriteLine();

            // 7. 벡터 검색 테스트 (선택사항)
            if (successCount > 0)
            {
                Console.WriteLine("[선택] 벡터 검색 테스트 수행 중...");
                var testQuery = "가벼운 출퇴근용 자전거";
                Console.WriteLine($"  검색어: '{testQuery}'");

                var testEmbedding = await ollamaService.GenerateEmbeddingAsync(testQuery);
                if (testEmbedding != null)
                {
                    await databaseService.TestVectorSearchAsync(testEmbedding, 3);
                }
                Console.WriteLine();
            }

            Console.WriteLine("========================================");
            Console.WriteLine("  모든 작업이 완료되었습니다!");
            Console.WriteLine("========================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[치명적 오류] {ex.Message}");
            Console.WriteLine($"상세 정보: {ex.StackTrace}");
        }
    }
}
