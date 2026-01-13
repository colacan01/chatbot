# BGE Re-ranker v2-M3 ONNX Model

This directory should contain the ONNX model files for BGE Re-ranker v2-M3.

## Required Files

Download the following files from Hugging Face:

### Model Files (Excluded from Git - Large Files)
- Source: https://huggingface.co/corto-ai/bge-reranker-large-onnx
- Files:
  - `model.onnx` (~560MB) - **EXCLUDED FROM GIT**
  - `model.onnx_data` (~2.1GB) - **EXCLUDED FROM GIT**

### Tokenizer Files
- Source: https://huggingface.co/BAAI/bge-reranker-v2-m3
- Files:
  - `tokenizer.json` (Git LFS file, ~17MB)
  - `tokenizer_config.json`
  - `special_tokens_map.json`
  - `sentencepiece.bpe.model` (~132 bytes) - **EXCLUDED FROM GIT**

## Download Instructions

### Quick Setup (Recommended)

```bash
cd /storage/dev/dotnet/chatbot/backend/src/BicycleShopChatbot.Api/models/bge-reranker-v2-m3

# Install git-lfs if not already installed
git lfs install

# Download model files
git clone https://huggingface.co/corto-ai/bge-reranker-large-onnx temp_model
cp temp_model/model.onnx .
cp temp_model/model.onnx_data .
rm -rf temp_model

# Download tokenizer files
git clone https://huggingface.co/BAAI/bge-reranker-v2-m3 temp_tokenizer
cp temp_tokenizer/tokenizer_config.json .
cp temp_tokenizer/special_tokens_map.json .
cp temp_tokenizer/sentencepiece.bpe.model .
cd temp_tokenizer && git lfs pull && cd ..
cp temp_tokenizer/tokenizer.json .
rm -rf temp_tokenizer
```

### Option 1: Using Git LFS

```bash
# Install git-lfs if not already installed
git lfs install

# Clone the model repository
git clone https://huggingface.co/corto-ai/bge-reranker-large-onnx temp_model
cp temp_model/model.onnx .

# Clone the tokenizer repository
git clone https://huggingface.co/BAAI/bge-reranker-v2-m3 temp_tokenizer
cp temp_tokenizer/tokenizer_config.json .
cp temp_tokenizer/special_tokens_map.json .
cp temp_tokenizer/sentencepiece.bpe.model .

# For production use (optional but recommended):
# Download tokenizer.json via git-lfs (requires git-lfs pull)
cd temp_tokenizer && git lfs pull && cd ..
cp temp_tokenizer/tokenizer.json .

# Clean up
rm -rf temp_model temp_tokenizer
```

### Option 2: Manual Download

1. Go to https://huggingface.co/corto-ai/bge-reranker-large-onnx
2. Download `model.onnx`
3. Go to https://huggingface.co/BAAI/bge-reranker-v2-m3
4. Download all tokenizer files

### Alternative: Use Smaller Model

If memory is constrained, use `bge-reranker-base` (~270MB) instead:
- Source: https://huggingface.co/EmbeddedLLM/bge-reranker-base-onnx-o4-o2-gpu

## File Structure

After download, this directory should contain:

```
bge-reranker-v2-m3/
├── README.md (this file)
├── model.onnx (~560MB, REQUIRED - Git ignored)
├── model.onnx_data (~2.1GB, REQUIRED - Git ignored)
├── sentencepiece.bpe.model (132 bytes, REQUIRED - Git ignored)
├── tokenizer_config.json (REQUIRED)
├── special_tokens_map.json (REQUIRED)
└── tokenizer.json (~17MB, Optional - for future proper SentencePiece integration)
```

**Note**: The large model files (model.onnx, model.onnx_data, sentencepiece.bpe.model) are excluded from Git due to size limitations. You must download them manually following the instructions above.

## Verification

Run the following to verify files exist:

```bash
ls -lh /storage/dev/dotnet/chatbot/backend/src/BicycleShopChatbot.Api/models/bge-reranker-v2-m3/
```

Expected output should show all 5 files (plus this README).
