import chromadb
from chromadb.utils import embedding_functions
import json
import re
import numpy as np
from typing import List, Dict, Any, Optional

# Initialize ChromaDB client
client = chromadb.Client()
