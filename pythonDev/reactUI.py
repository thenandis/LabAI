from http.server import BaseHTTPRequestHandler, HTTPServer


UI_HOST = "127.0.0.1"
UI_PORT = 7860

HTML_PAGE = """<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>React Calculator</title>
  <script crossorigin src="https://unpkg.com/react@18/umd/react.development.js"></script>
  <script crossorigin src="https://unpkg.com/react-dom@18/umd/react-dom.development.js"></script>
  <script src="https://unpkg.com/@babel/standalone/babel.min.js"></script>
  <style>
    body { font-family: sans-serif; max-width: 30rem; margin: 2rem auto; padding: 0 1rem; }
    form { display: grid; gap: 1rem; }
    label { display: grid; gap: .35rem; }
    input, select, button { font: inherit; padding: .5rem; }
    button { cursor: pointer; }
    .result { margin-top: 1rem; font-weight: bold; }
    .error { color: #b00020; }
  </style>
</head>
<body>
  <div id="root"></div>
  <script type="text/babel">
    const API_URL = "http://127.0.0.1:8000/calculate";

    function Calculator() {
      const [firstNumber, setFirstNumber] = React.useState("10");
      const [secondNumber, setSecondNumber] = React.useState("5");
      const [operation, setOperation] = React.useState("add");
      const [result, setResult] = React.useState("");
      const [error, setError] = React.useState("");

      async function calculate(event) {
        event.preventDefault();
        setResult("");
        setError("");

        const params = new URLSearchParams({
          a: firstNumber,
          b: secondNumber,
          operation,
        });

        try {
          const response = await fetch(`${API_URL}?${params}`);
          if (!response.ok) throw new Error(`API returned HTTP ${response.status}`);
          const data = await response.json();
          if (data.error) setError(data.error);
          else setResult(data.result);
        } catch (requestError) {
          setError(`Could not connect to the API: ${requestError.message}`);
        }
      }

      return (
        <main>
          <h1>React Calculator</h1>
          <form onSubmit={calculate}>
            <label>First number
              <input type="number" step="any" value={firstNumber}
                onChange={(event) => setFirstNumber(event.target.value)} required />
            </label>
            <label>Second number
              <input type="number" step="any" value={secondNumber}
                onChange={(event) => setSecondNumber(event.target.value)} required />
            </label>
            <label>Operation
              <select value={operation}
                onChange={(event) => setOperation(event.target.value)}>
                <option value="add">Add</option>
                <option value="subtract">Subtract</option>
                <option value="multiply">Multiply</option>
                <option value="divide">Divide</option>
              </select>
            </label>
            <button type="submit">Calculate</button>
          </form>
          {result !== "" && <p className="result">Result: {result}</p>}
          {error !== "" && <p className="result error">Error: {error}</p>}
        </main>
      );
    }

    ReactDOM.createRoot(document.getElementById("root")).render(<Calculator />);
  </script>
</body>
</html>
"""


class ReactUIRequestHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path != "/":
            self.send_error(404)
            return
        content = HTML_PAGE.encode("utf-8")
        self.send_response(200)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Content-Length", str(len(content)))
        self.end_headers()
        self.wfile.write(content)

    def log_message(self, format, *args):
        return


if __name__ == "__main__":
    server = HTTPServer((UI_HOST, UI_PORT), ReactUIRequestHandler)
    print(f"React UI running at http://{UI_HOST}:{UI_PORT}", flush=True)
    print("Press Ctrl+C to stop the server.", flush=True)
    server.serve_forever()
