# Mnemesis: Generative Multi-Agent Simulation

An innovative multi-agent simulation built with **Unity 2D** and **Local LLM (Llama 3.2)**.

## Key Features
- **Distorted Memory Engine**: Agents mutate their memories based on emotional states (PAD model) during reflection cycles.
- **Autonomous Social Dynamics**: Agents gossip, form opinions, and compete for laboratory resources without central scripting.
- **Local AI**: Powered by Ollama and ChromaDB for private, cost-free execution.
- **Physical Interaction**: 2D top-down environment where physical proximity triggers social AI reasoning.

## Tech Stack
- **Frontend**: Unity 2D (C#)
- **Backend**: Python (FastAPI)
- **AI**: Ollama (Llama 3.2), ChromaDB (Vector Store)
- **Communication**: REST API (JSON)

## Setup
1. **Ollama**: Install Ollama and run `ollama pull llama3.2` and `ollama pull nomic-embed-text`.
2. **Backend**: Navigate to `/backend`, run `pip install -r requirements.txt` and `python main.py`.
3. **Frontend**: Open `/frontend` in Unity 6 and press Play.