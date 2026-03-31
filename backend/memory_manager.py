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
        self.collection.add(
            documents=[text],
            metadatas=[{
                "agent_name": agent_name,
                "timestamp": timestamp,
                "p_value": emotion_pad[0],
                "a_value": emotion_pad[1],
                "d_value": emotion_pad[2],
                "distortion_level": 0
            }],
            ids=[memory_id]
        )

    def retrieve_relevant_memories(self, agent_name, query, limit=3):
        results = self.collection.query(
            query_texts=[query],
            n_results=limit,
            where={"agent_name": agent_name}
        )
        if results['documents'] and len(results['documents'][0]) > 0:
            return results['documents'][0]
        return []

    def get_latest_memory(self, agent_name):
        results = self.collection.get(
            where={"agent_name": agent_name},
            limit=1
        )
        if results['documents'] and len(results['documents']) > 0:
            return {
                "id": results['ids'][0],
                "text": results['documents'][0],
                "metadata": results['metadatas'][0]
            }
        return None

    def update_memory(self, memory_id, new_text, metadata):
        current_distortion = metadata.get("distortion_level", 0)
        metadata["distortion_level"] = current_distortion + 1

        self.collection.update(
            ids=[memory_id],
            documents=[new_text],
            metadatas=[metadata]
        )