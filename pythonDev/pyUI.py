from http.server import BaseHTTPRequestHandler, HTTPServer


UI_HOST = "127.0.0.1"
UI_PORT = 7860

HTML_PAGE = """<!doctype html>
<html lang="en">
<head>
	<meta charset="utf-8">
	<meta name="viewport" content="width=device-width, initial-scale=1">
	<title>Calculator</title>
	<style>
		body { font-family: sans-serif; margin: 2rem auto; max-width: 30rem; padding: 0 1rem; }
		form { display: grid; gap: 1rem; }
		label { display: grid; gap: .35rem; }
		input, select, button { font: inherit; padding: .5rem; }
		button { cursor: pointer; }
		#result { margin-top: 1rem; font-weight: bold; }
		.error { color: #b00020; }
	</style>
</head>
<body>
	<h1>Calculator</h1>
	<form id="calculator-form">
		<label>First number <input id="a" type="number" step="any" value="10" required></label>
		<label>Second number <input id="b" type="number" step="any" value="5" required></label>
		<label>Operation
			<select id="operation">
				<option value="add">Add</option>
				<option value="subtract">Subtract</option>
				<option value="multiply">Multiply</option>
				<option value="divide">Divide</option>
			</select>
		</label>
		<button type="submit">Calculate</button>
	</form>
	<p id="result" aria-live="polite"></p>

	<script>
		const form = document.querySelector("#calculator-form");
		const result = document.querySelector("#result");

		form.addEventListener("submit", async (event) => {
			event.preventDefault();
			result.className = "";
			result.textContent = "Calculating...";

			const params = new URLSearchParams({
				a: document.querySelector("#a").value,
				b: document.querySelector("#b").value,
				operation: document.querySelector("#operation").value
			});

			try {
				const response = await fetch(`http://127.0.0.1:8000/calculate?${params}`);
				const data = await response.json();
				if (data.error) {
					result.className = "error";
					result.textContent = `Error: ${data.error}`;
				} else {
					result.textContent = `Result: ${data.result}`;
				}
			} catch (error) {
				result.className = "error";
				result.textContent = "Could not connect to the calculator API.";
			}
		});
	</script>
</body>
</html>
"""


class UIRequestHandler(BaseHTTPRequestHandler):
		def do_GET(self) -> None:
				if self.path != "/":
						self.send_error(404)
						return

				content = HTML_PAGE.encode("utf-8")
				self.send_response(200)
				self.send_header("Content-Type", "text/html; charset=utf-8")
				self.send_header("Content-Length", str(len(content)))
				self.end_headers()
				self.wfile.write(content)

		def log_message(self, format: str, *args: object) -> None:
				return


if __name__ == "__main__":
		server = HTTPServer((UI_HOST, UI_PORT), UIRequestHandler)
		print(f"UI running at http://{UI_HOST}:{UI_PORT}")
		server.serve_forever()
