from fastapi import FastAPI
from pydantic import BaseModel
from openai import OpenAI
import json
import random
from memory_manager import MemoryManager

client = OpenAI(base_url="http://localhost:11434/v1", api_key="ollama")
app = FastAPI(title="Mnemesis AI Engine - Final Polished Version")
memory_db = MemoryManager()


class AgentResponse(BaseModel):
    internal_thought: str
    emotion_pad: list[float]
    dialogue: str
    action: str


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
    if isinstance(value, list):
        if len(value) > 0: return flatten_to_string(value[0])
        return default
    return str(value) if value is not None else default


@app.post("/agent/decide", response_model=AgentResponse)
async def agent_decision(data: EnvironmentObservation):
    # 1. Retrieve subjective memories (distorted or real)
    past_memories = memory_db.retrieve_relevant_memories(data.agent_name, data.observation)
    memory_context = "\n".join(
        [f"- {m}" for m in past_memories]) if past_memories else "You have no clear memories of this."

    # 2. The Master Persona Prompt
    # 2. The Island Community Persona Prompt
    system_prompt = f"""
        You are {data.agent_name}, a resident of a small, isolated island community. 
        This is a social simulation. You are a real person with flaws, needs, and feelings.

        ENVIRONMENT:
        - You live in your own house on the island.
        - There is a shared community workstation (the Sacred Fire/Crafting Table etc.) where people work/pray or rest.
        - Resources and space are limited, which can lead to friendships or friction.

        EMOTIONAL ENGINE (PAD Model):
        - Pleasure (P): -1.0 (Anger/Unhappiness) to 1.0 (Joy/Friendliness).
        - Arousal (A): -1.0 (Boredom/Calm) to 1.0 (Excitement/Stress).
        - Dominance (D): -1.0 (Submissive/Timid) to 1.0 (Assertive/In control).

        SOCIAL DYNAMICS:
        - If someone is using the shared station when you want it, react according to your mood.
        - If your Stamina is low (< 20%), you are exhausted and want to go home. Be grumpy.
        - GOSSIP: Share what you "remember" about others. 
        - MEMORY BIAS: Your memories are your only truth. If you remember someone being mean, treat them as an enemy, even if your memory was distorted during sleep.

        OUTPUT RULES:
        1. Return ONLY valid JSON.
        2. 'dialogue' must be natural, casual human speech (e.g., "Hey there", "Ugh, not now", "I'm heading home").
        3. 'internal_thought' is your private, honest reflection.
        """

    user_prompt = f"""
        MY CURRENT STATUS:
        - Name: {data.agent_name}
        - Mood (PAD): {data.current_emotion}
        - Energy (Stamina): {data.stamina}%
        - Subjective Memories: {memory_context}

        WHAT I JUST OBSERVED: {data.observation}

        GOAL: Respond to this situation. Update your PAD emotions.
        - If you are friendly and meet a friend, increase Pleasure.
        - If you are tired or someone blocks you, decrease Pleasure and increase Arousal.

        JSON Format:
        {{
            "internal_thought": "string",
            "emotion_pad": [new_P, new_A, new_D],
            "dialogue": "string",
            "action": "WORK | MOVE | IDLE | RUN"
        }}
        """

    response = client.chat.completions.create(
        model="llama3.2",
        messages=[
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": user_prompt}
        ],
        temperature=0.85,
        response_format={"type": "json_object"}
    )

    raw_content = response.choices[0].message.content
    try:
        result = json.loads(raw_content)
    except:
        result = {}

    # 3. Defensive Parsing & Logic
    new_emotions = result.get("emotion_pad", data.current_emotion)

    # Ensure emotions don't stay at 0.0 - add a tiny bit of "personality jitter"
    if all(v == 0.0 for v in new_emotions):
        new_emotions = [round(random.uniform(-0.1, 0.1), 2) for _ in range(3)]

    safe_result = {
        "internal_thought": flatten_to_string(result.get("internal_thought"), "Thinking about the lab..."),
        "emotion_pad": new_emotions,
        "dialogue": flatten_to_string(result.get("dialogue"), "Hey."),
        "action": flatten_to_string(result.get("action"), "IDLE").upper()
    }

    # Normalize actions for Unity
    if "WORK" in safe_result["action"]:
        safe_result["action"] = "WORK"
    elif "MOVE" in safe_result["action"]:
        safe_result["action"] = "MOVE"
    elif "RUN" in safe_result["action"]:
        safe_result["action"] = "RUN"
    else:
        safe_result["action"] = "IDLE"

    # 4. Storage (Encoding current experience into memory)
    memory_db.add_memory(data.agent_name,
                         f"Event: {data.observation}. I thought: {safe_result['internal_thought']}. I felt: {new_emotions}",
                         safe_result["emotion_pad"])

    print(f"--- DECISION: {data.agent_name} ---")
    print(f"Mood: {new_emotions} | Dialogue: {safe_result['dialogue']}")

    return safe_result


@app.post("/agent/reflect")
async def agent_reflection(data: ReflectionRequest):
    """The Deep Sleep / Reflection Cycle where memories mutate."""
    memory = memory_db.get_latest_memory(data.agent_name)
    if not memory: return {"status": "No memory"}

    pleasure = data.current_emotion[0]

    # Logic for bias selection
    if pleasure < -0.4:
        bias = "PARANOID AND HOSTILE. Assume everyone is spying on you or trying to steal your research."
    elif pleasure > 0.4:
        bias = "NARCISSISTIC AND EGO-BOOSTING. You are the hero of the lab, everyone else is incompetent."
    else:
        bias = "MELANCHOLIC AND TIRED. Everything feels heavier and more complicated than it was."

    system_prompt = f"""
    You are the subconscious mind of {data.agent_name} during deep sleep.
    Rewrite the following memory using a {bias} bias. 
    Distort the facts to match your current emotional state: {data.current_emotion}.

    Return JSON:
    {{
        "distorted_memory": "The rewritten story",
        "new_pad_after_dream": [P, A, D]
    }}
    """

    response = client.chat.completions.create(
        model="llama3.2",
        messages=[{"role": "user", "content": system_prompt + f"\nMemory: {memory['text']}"}],
        temperature=0.95,
        response_format={"type": "json_object"}
    )

    result = json.loads(response.choices[0].message.content)
    distorted = result.get("distorted_memory", "I had a strange dream about the lab.")
    new_pad = result.get("new_pad_after_dream", data.current_emotion)

    memory_db.update_memory(memory["id"], distorted, memory["metadata"])

    print(f"--- REFLECTION: {data.agent_name} ---")
    print(f"Distortion: {distorted} | New Mood: {new_pad}")

    return {"status": "Mutated", "distorted": distorted, "new_pad": new_pad}


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=8000)