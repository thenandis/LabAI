import os
from anthropic import AnthropicVertex

os.environ["GOOGLE_APPLICATION_CREDENTIALS"] = '/Users/TheNandis/Desktop/CMU icl/vertex-ai-504517-428ed26d2609.json'

client = AnthropicVertex(region="global", project_id="vertex-ai-504517")
#model = "claude-sonnet-4@20250514"
model = "claude-haiku-4-5@20251001"

message = client.messages.create(
    model=model,
    max_tokens=100,
    messages=[
        {
            "role": "user",
            "content": "What is quantum computing? Answer in one sentence."
        }
    ]
)

if __name__ == "__main__":
    print("Response from Anthropic Vertex:")
    print(message.content[0].text)