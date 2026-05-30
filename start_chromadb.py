import os
import uvicorn

chroma_data = os.path.join(os.environ.get("APPDATA", "."), "淼喵妙脚本DIY", "chroma_data")
os.makedirs(chroma_data, exist_ok=True)
os.environ["CHROMA_DATA_PATH"] = chroma_data

uvicorn.run("chromadb.app:app", host="127.0.0.1", port=8000, log_level="error")
