import asyncio
import os
from dotenv import load_dotenv
from agno.agent import Agent
from agno.models.ollama import Ollama
from playwright.async_api import async_playwright

load_dotenv()

# ── BROWSER STATE ─────────────────────────────────────────────────────────────
_browser = None
_context = None
_page = None
_playwright = None

async def get_page():
    global _browser, _context, _page, _playwright
    if _browser is None:
        _playwright = await async_playwright().start()
        _browser = await _playwright.chromium.launch(headless=True)
        _context = await _browser.new_context(
            viewport={"width": 1280, "height": 720},
            user_agent=(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
                "AppleWebKit/537.36 (KHTML, like Gecko) "
                "Chrome/120.0.0.0 Safari/537.36"
            )
        )
        # Remove webdriver fingerprint
        await _context.add_init_script(
            "Object.defineProperty(navigator, 'webdriver', {get: () => undefined})"
        )
        _page = await _context.new_page()
    return _page

async def teardown():
    global _browser, _playwright
    if _browser:
        await _browser.close()
        await _playwright.stop()
        _browser = None
        _playwright = None

# ── ASYNC TOOLS ───────────────────────────────────────────────────────────────
async def navigate(url: str) -> str:
    """Navigate the browser to a URL and return the page's text content.
    Use when you need to open a website or move to a new page.
    Input: full URL with https:// prefix.
    """
    page = await get_page()
    await page.goto(url, wait_until="domcontentloaded", timeout=20000)
    await page.wait_for_load_state("networkidle")
    content = await page.inner_text("body")
    return content[:4000]

async def extract_structured(css_selector: str) -> str:
    """Extract text from all elements matching a CSS selector on the current page.
    Use when you need to pull specific elements from the loaded page.
    Input: valid CSS selector string (e.g., 'h3 a', '.price_color').
    """
    page = await get_page()
    try:
        await page.wait_for_selector(css_selector, timeout=5000)
        elements = await page.query_selector_all(css_selector)
        texts = []
        for el in elements[:20]:  # Cap at 20 elements
            text = await el.inner_text()
            texts.append(text.strip())
        return "\n".join(texts) if texts else "No elements found."
    except Exception as e:
        return f"Extraction failed: {str(e)}"

async def get_current_url() -> str:
    """Return the URL the browser is currently on. No input required."""
    page = await get_page()
    return page.url

# ── AGENT CONFIGURATION ───────────────────────────────────────────────────────
tools = [navigate, extract_structured, get_current_url]

SYSTEM = (
    "You are a browser-based research agent with access to a real browser. "
    "Use navigate() to open pages, extract_structured() to pull specific elements, "
    "and get_current_url() to check where you are. "
    "Always navigate first, then extract. Be concise in your final answer."
)

# Use standard string for host if environment variable isn't active
ollama_host = os.getenv("OLLAMA_HOST", "http://127.0.0.1:11434")

# Change this section in your reference_agent.py file:
model = Ollama(
    id=os.getenv("PHI_MODEL_ID", "qwen2.5:14b"),  # <-- Swapped from phi4 to qwen2.5
    host=ollama_host,
    options={"temperature": 0},
)

agent = Agent(
    model=model,
    tools=tools,
    system_message=SYSTEM,
    markdown=True,
)

async def run_agent(query: str) -> str:
    try:
        # arun triggers the async agent loop inside Agno
        result = await agent.arun(query)
        return result.content
    finally:
        await teardown()

# ── DEMO RUNNER ───────────────────────────────────────────────────────────────
if __name__ == "__main__":
    query = (
        "Go to https://books.toscrape.com and extract the titles and prices "
        "of the first 5 books listed. Return them as a structured list."
    )
    
    print(f"Query: {query}\n")
    answer = asyncio.run(run_agent(query))
    print(f"Answer:\n{answer}")
