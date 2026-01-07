#!/usr/bin/env python3
"""Receipt parser using Claude API vision capabilities."""

import base64
import json
import sys
from pathlib import Path

import anthropic

DEFAULT_IMAGE_PATH = "input.png"

PROMPT = """Analyze this Australian receipt image and extract all items into structured JSON.

Return ONLY valid JSON with this exact structure:
{
  "store_name": "string or null if not visible",
  "date": "string or null if not visible",
  "items": [
    {
      "name": "item description",
      "quantity": 1,
      "price": 0.00,
      "weight": "string or null if not applicable"
    }
  ],
  "calculated_total": 0.00
}

Rules:
- Include EVERY item on the receipt
- Price should be the per-item price (not multiplied by quantity)
- Weight is for items sold by weight (e.g., "0.5kg") - set to null otherwise
- Calculate the total by summing (quantity * price) for all items
- Use null for any field that cannot be determined from the image
- Return ONLY the JSON, no other text"""


def load_image_as_base64(image_path: str) -> str:
    """Read image file and return base64 encoded string."""
    path = Path(image_path)
    if not path.exists():
        raise FileNotFoundError(f"Image not found: {image_path}")

    with open(path, "rb") as f:
        return base64.standard_b64encode(f.read()).decode("utf-8")


def get_media_type(image_path: str) -> str:
    """Determine media type from file extension."""
    suffix = Path(image_path).suffix.lower()
    media_types = {
        ".png": "image/png",
        ".jpg": "image/jpeg",
        ".jpeg": "image/jpeg",
        ".gif": "image/gif",
        ".webp": "image/webp",
    }
    return media_types.get(suffix, "image/png")


def parse_receipt(image_path: str) -> dict:
    """Send image to Claude API and parse receipt data."""
    client = anthropic.Anthropic()

    image_data = load_image_as_base64(image_path)
    media_type = get_media_type(image_path)

    message = client.messages.create(
        model="claude-sonnet-4-20250514",
        max_tokens=4096,
        messages=[
            {
                "role": "user",
                "content": [
                    {
                        "type": "image",
                        "source": {
                            "type": "base64",
                            "media_type": media_type,
                            "data": image_data,
                        },
                    },
                    {
                        "type": "text",
                        "text": PROMPT,
                    },
                ],
            }
        ],
    )

    response_text = message.content[0].text

    # Strip markdown code blocks if present
    if response_text.startswith("```"):
        lines = response_text.split("\n")
        # Remove first line (```json) and last line (```)
        lines = lines[1:-1] if lines[-1].strip() == "```" else lines[1:]
        response_text = "\n".join(lines)

    return json.loads(response_text)


def main():
    image_path = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_IMAGE_PATH
    result = parse_receipt(image_path)
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
