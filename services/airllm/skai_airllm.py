"""Skai's built-in AirLLM worker.

One JSON request is read from stdin and JSON events are written to stdout.
The worker is intentionally stateless per process; Skai serializes requests so
model memory is reclaimed when a response is complete or canceled.
"""
import json
import sys
import traceback


def emit(payload):
    sys.stdout.write(json.dumps(payload, ensure_ascii=False) + "\n")
    sys.stdout.flush()


def main(request):
    try:
        from airllm import AutoModel
        import torch

        kwargs = {"trust_remote_code": True}
        if request.get("compression") in ("4bit", "8bit"):
            kwargs["compression"] = request["compression"]
        if request.get("cache_path"):
            kwargs["layer_shards_saving_path"] = request["cache_path"]
        if request.get("hf_token"):
            kwargs["hf_token"] = request["hf_token"]
        if request.get("delete_original"):
            kwargs["delete_original"] = True
        model = AutoModel.from_pretrained(request["model"], **kwargs)
        if request.get("device") == "cpu":
            model.device = torch.device("cpu")
        messages = request.get("messages", [])
        tokenizer = model.tokenizer
        prompt = tokenizer.apply_chat_template(messages, tokenize=False, add_generation_prompt=True) if hasattr(tokenizer, "apply_chat_template") else "\n".join(f"{m['role']}: {m['content']}" for m in messages)
        tokens = tokenizer([prompt], return_tensors="pt", return_attention_mask=False, truncation=True, max_length=8192, padding=False)
        output = model.generate(tokens["input_ids"].to(model.device), max_new_tokens=request.get("max_new_tokens", 1024), temperature=request.get("temperature", 0.7), use_cache=True, return_dict_in_generate=True)
        text = tokenizer.decode(output.sequences[0], skip_special_tokens=True)
        if text.startswith(prompt):
            text = text[len(prompt):]
        emit({"type": "text", "text": text.strip()})
        emit({"type": "done"})
    except Exception as exc:
        emit({"type": "error", "message": f"{exc}\n{traceback.format_exc(limit=2)}"})
        return 1
    return 0


if __name__ == "__main__":
    line = sys.stdin.readline()
    sys.exit(main(json.loads(line)))
