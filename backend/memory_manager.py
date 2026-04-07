import chromadb
from chromadb.utils import embedding_functions
import uuid
import time

class MemoryManager:
    def __init__(self):
        self.client = chromadb.PersistentClient(path="./chroma_db")
        self.embedding_function = embedding_functions.OllamaEmbeddingFunction(
            url="http://localhost:11434/api/embeddings",
            model_name="nomic-embed-text"
        )
        self.collection = self.client.get_or_create_collection(
            name="agent_memories",
            embedding_function=self.embedding_function
        )

    def add_memory(self, agent_name, text, emotion_pad):
        memory_id = str(uuid.uuid4())
        timestamp = int(time.time())
        p = float(emotion_pad[0]) if len(emotion_pad) > 0 else 0.0
        a = float(emotion_pad[1]) if len(emotion_pad) > 1 else 0.0
        d = float(emotion_pad[2]) if len(emotion_pad) > 2 else 0.0

        self.collection.add(
            documents=[text],
            metadatas=[{
                "agent_name": agent_name,
                "timestamp": timestamp,
                "p_value": p, "a_value": a, "d_value": d,
                "distortion_level": 0
            }],
            ids=[memory_id]
        )

    def retrieve_relevant_memories(self, agent_name, query, limit=3):
        results = self.collection.query(query_texts=[query], n_results=limit, where={"agent_name": agent_name})
        return results['documents'][0] if results['documents'] and len(results['documents'][0]) > 0 else []

    def get_latest_memory(self, agent_name):
        results = self.collection.get(where={"agent_name": agent_name}, limit=1)
        if results['documents'] and len(results['documents']) > 0:
            return {"id": results['ids'][0], "text": results['documents'][0], "metadata": results['metadatas'][0]}
        return None

    def update_memory(self, memory_id, new_text, metadata):
        metadata["distortion_level"] = metadata.get("distortion_level", 0) + 1
        self.collection.update(ids=[memory_id], documents=[new_text], metadatas=[metadata])

    def get_all_memories(self, agent_name, limit=10):
        results = self.collection.get(where={"agent_name": agent_name}, limit=limit)
        memories = []
        if results['documents']:
            for i in range(len(results['documents'])):
                memories.append({
                    "text": results['documents'][i],
                    "distortion": results['metadatas'][i].get("distortion_level", 0)
                })
        return memories[::-1] # Najnowsze na górze