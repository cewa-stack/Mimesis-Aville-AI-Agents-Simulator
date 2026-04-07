from fastapi import FastAPI
from pydantic import BaseModel
from openai import OpenAI
import json, time, random
from memory_manager import MemoryManager

client = OpenAI(base_url="http://localhost:11434/v1", api_key="ollama")
app = FastAPI(title="Mnemesis AI Engine")
memory_db = MemoryManager()
dialogue_history = {}


class AgentResponse(BaseModel):
    internal_thought: str
    emotion_pad: list[float]
    dialogue: str
    action: str
    target_location: list[float]


class EnvironmentObservation(BaseModel):
    agent_name: str
    current_emotion: list[float]
    observation: str
    stamina: float


class ReflectionRequest(BaseModel):
    agent_name: str
    current_emotion: list[float]


def flatten_to_string(value, default=""):
    if isinstance(value, str): return value
    if isinstance(value, dict): return " ".join([str(v) for v in value.values()])
    return str(value) if value is not None else default


@app.post("/agent/decide", response_model=AgentResponse)
async def agent_decision(data: EnvironmentObservation):
    past_memories = memory_db.retrieve_relevant_memories(data.agent_name, data.observation)
    memory_context = "\n".join([f"- {m}" for m in past_memories]) if past_memories else "None."

    history = dialogue_history.get(data.agent_name, [])[-3:]
    history_str = " | ".join(history) if history else "None."

    system_prompt = f"""You are {data.agent_name} on an island. Current time: {time.strftime('%H:%M:%S')}. 
    Be emotional, expressive, and never repeat these exactly: [{history_str}].
    If bored, use action 'EXPLORE' and set 'target_location' [x, y] within X:(-20,20) Y:(-15,15)."""

    user_prompt = f"Mood: {data.current_emotion}, Energy: {data.stamina}%, Memories: {memory_context}, Observation: {data.observation}. Respond in JSON."

    response = client.chat.completions.create(
        model="llama3.2",
        messages=[{"role": "system", "content": system_prompt}, {"role": "user", "content": user_prompt}],
        temperature=0.9,
        response_format={"type": "json_object"}
    )
    result = json.loads(response.choices[0].message.content)

    # Validation
    new_dialogue = flatten_to_string(result.get("dialogue"), "...")
    if data.agent_name not in dialogue_history: dialogue_history[data.agent_name] = []
    dialogue_history[data.agent_name].append(new_dialogue)

    safe_res = {
        "internal_thought": flatten_to_string(result.get("internal_thought"), "Thinking..."),
        "emotion_pad": result.get("emotion_pad", data.current_emotion)[:3],
        "dialogue": new_dialogue,
        "action": flatten_to_string(result.get("action"), "IDLE").upper(),
        "target_location": result.get("target_location", [0.0, 0.0])
    }

    memory_db.add_memory(data.agent_name, f"I saw {data.observation} and said '{safe_res['dialogue']}'",
                         safe_res["emotion_pad"])
    return safe_res


@app.get("/agent/{agent_name}/memories")
async def get_memories(agent_name: str):
    return {"memories": memory_db.get_all_memories(agent_name)}


@app.post("/agent/reflect")
async def reflect(data: ReflectionRequest):
    m = memory_db.get_latest_memory(data.agent_name)
    if not m: return {"status": "empty"}
    bias = "hostile" if data.current_emotion[0] < -0.3 else "melancholic"
    prompt = f"Rewrite this memory with a {bias} bias: {m['text']}. Return ONLY rewritten text."
    res = client.chat.completions.create(model="llama3.2", messages=[{"role": "user", "content": prompt}],
                                         temperature=0.9)
    distorted = res.choices[0].message.content.strip()
    memory_db.update_memory(m["id"], distorted, m["metadata"])
    return {"status": "done", "distorted": distorted}


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=8000)