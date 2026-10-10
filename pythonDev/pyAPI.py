# create an API endpoint for a simple calculator that can perform addition, subtraction, multiplication, and division. The API should accept two numbers and an operation as input parameters and return the result of the calculation.
import uvicorn

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field

app = FastAPI()

class CalculationRequest(BaseModel):
    a: float = Field(..., description="The first number")
    b: float = Field(..., description="The second number")
    operation: str = Field(..., description="The operation to perform: add, subtract, multiply, divide")
    
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://127.0.0.1:7860", "http://localhost:7860"],
    allow_methods=["GET"],
    allow_headers=["*"],
)

@app.get("/calculate")
def calculate(request: CalculationRequest):
    a = request.a
    b = request.b
    operation = request.operation
    if operation == "add":
        return {"result": a + b}
    elif operation == "subtract":
        return {"result": a - b}
    elif operation == "multiply":
        return {"result": a * b}
    elif operation == "divide":
        if b == 0:
            return {"error": "Division by zero is not allowed"}
        return {"result": a / b}
    else:
        return {"error": "Invalid operation"}
    
# To run the API, use the command: uvicorn pythonDev.pyAPI:app --reload
if __name__ == "__main__":
    uvicorn.run("pythonDev.pyAPI:app", host="127.0.0.1", port=8000, reload=True)