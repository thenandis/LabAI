from collections import Counter
import csv
import time
from sqlalchemy import text

def display_message(text: str, counter: Counter):
    c1 = str.count(text, "a")
    print(f"Char count for {text}")
    print(f"Char count for 'a' = {c1}")
    for char, count in counter.items():
        print(f"Char count for {char} = {count}")
        
def countCharacters(text: str):
    counts: Counter[str] = Counter(text)
    return counts

def first_unique_char(text: str):
    counts = Counter(text)

    for char in text:
        if counts[char] == 1:
            return char

    return None

def first_unique_char2(text: str):
    counts = {}
    for ch in text:
        counts[ch] = counts.get(ch, 0) + 1

    for ch in text:
        if counts[ch] == 1:
            return ch
    return None

#list of input records as parameters to the function
#each record has a userid, prompt tokens, latency, and status
#output json
#function has to return used tokens and average latency for each user id

def process_records(records: list[dict]) -> dict[str, dict[str, float]] :
    user_data = {}
    
    unique_userids = set(record['user_id'] for record in records)
    
    for record in records:
        user_id = record['user_id']
        prompt_tokens = record['prompt_tokens']
        latency = record['latency']
        status = record['status']
        

        if user_id not in user_data:
            user_data[user_id] = {'total_tokens': 0, 'total_latency': 0, 'count': 0}

        user_data[user_id]['total_tokens'] += prompt_tokens
        user_data[user_id]['total_latency'] += latency
        user_data[user_id]['count'] += 1

    result = {}
    for user_id, data in user_data.items():
        average_latency = data['total_latency'] / data['count'] if data['count'] > 0 else 0
        result[user_id] = {
            'used_tokens': data['total_tokens'],
            'average_latency': average_latency
        }

    return result

#1 Word frequency, top N
def top_n_word_frequency(text: str, n: int) -> list[tuple[str, int]]:
    counts = Counter(text.lower().split())
    return counts.most_common(n)

#2 Deduplicate preserving order
def deduplicate_preserve_order(lst: list) -> list:
    seen = set()
    result = []
    for item in lst:
        if item not in seen:
            seen.add(item)
            result.append(item)
    return result
 
#3 Group records by key   
def group_records_by_key(records: list[dict], key: str) -> dict[str, list[dict]]:
    grouped = {}    #x = [] empty list, while x = {} empty dictionary
    for record in records:
        record_key = record[key]
        if record_key not in grouped:
            grouped[record_key] = []
        grouped[record_key].append(record)
    return grouped
    
#4 Flatten a nested dictionary
def flatten_dict(d: dict, parent_key: str = '', sep: str = '.') -> dict:
    items = []
    for k, v in d.items():
        new_key = f"{parent_key}{sep}{k}" if parent_key else k  # Construct the new key with parent key and separator if parent key exists
        if isinstance(v, dict):
            # Recursively flatten the nested dictionary and extend the items list. 
            # items.extend() is used to add all key-value pairs from the flattened sub-dictionary.
            items.extend(flatten_dict(v, new_key, sep=sep).items()) 
        else:
            items.append((new_key, v))
    return dict(items)

#5 Compare semantic versions
def compare_semver(version1: str, version2: str) -> int:
    v1_parts = [int(part) for part in version1.split('.')]
    v2_parts = [int(part) for part in version2.split('.')]
    for p1, p2 in zip(v1_parts, v2_parts):
        if p1 > p2:
            return 1
        elif p1 < p2:
            return -1
    if len(v1_parts) > len(v2_parts):
        return 1
    elif len(v1_parts) < len(v2_parts):
        return -1
    return 0

#6 Deep merge two config dictionaries
"""
State Management (Redux/Frontend Store): Updating a deeply nested state object in a web app. 
When a user changes one field in a multi-step checkout form, a deep merge updates that single field 
without destroying the data from previous steps.
"""
def deep_merge_dicts(dict1: dict, dict2: dict) -> dict:
    result = dict1.copy()
    for key, value in dict2.items():
        if key in result and isinstance(result[key], dict) and isinstance(value, dict):
            result[key] = deep_merge_dicts(result[key], value)
        else:
            result[key] = value
    return result

#7 Retry decorator with exponential backoff
def retry(exceptions, tries=4, delay=3, backoff=2):
    def decorator(func):
        def wrapper(*args, **kwargs):
            _tries, _delay = tries, delay
            while _tries > 1:
                try:
                    return func(*args, **kwargs)
                except exceptions as e:
                    print(f"{e}, Retrying in {_delay} seconds...")
                    time.sleep(_delay)
                    _tries -= 1
                    _delay *= backoff
            return func(*args, **kwargs)
        return wrapper
    return decorator

#8 Process a huge file without loading it entirely into memory
@retry((IOError, OSError), tries=3, delay=2, backoff=2)  #retry on IO and OS errors
def sum_column(path: str, column: str) -> float:
    total = 0.0
    with open(path, newline="") as f:
        for row in csv.DictReader(f):        # streams row by row
            try:
                total += float(row[column])
            except (ValueError, KeyError):
                continue                      # or count and report
    return total

#9 Concurrent fetch with a limit
import asyncio
import aiohttp

async def fetch(session, url):
    async with session.get(url) as response:
        return await response.text()

async def concurrent_fetch(urls, limit=5):
    semaphore = asyncio.Semaphore(limit)

    async with aiohttp.ClientSession() as session:
        async def sem_fetch(url):
            async with semaphore:
                return await fetch(session, url)

        tasks = [sem_fetch(url) for url in urls]
        return await asyncio.gather(*tasks)
    
#palindrome functions
def longest_palindrome_simple(s: str) -> str:
    if not s:
        return ""

    longest = ""
    for i in range(len(s)):
        # Odd length palindromes
        temp = expand_around_center(s, i, i)
        if len(temp) > len(longest):
            longest = temp
        # Even length palindromes
        temp = expand_around_center(s, i, i + 1)
        if len(temp) > len(longest):
            longest = temp

    return longest

def expand_around_center(s: str, left: int, right: int) -> str:
    while left >= 0 and right < len(s) and s[left] == s[right]:
        left -= 1
        right += 1
    return s[left + 1:right]    

def findAllPalindromes(s: str):
    if not s:
        return 0, []

    # Step 1: Run standard Manacher's setup
    T = "#" + "#".join(s) + "#"
    n = len(T)
    P = [0] * n
    C, R = 0, 0

    for i in range(n):
        if i < R:
            P[i] = min(R - i, P[2 * C - i])
        while (i - 1 - P[i] >= 0 and 
               i + 1 + P[i] < n and 
               T[i - 1 - P[i]] == T[i + 1 + P[i]]):
            P[i] += 1
        if i + P[i] > R:
            C = i
            R = i + P[i]

    # Step 2: Extract ALL substrings and count them
    total_count = 0
    all_palindromes = []

    for i in range(n):
        # Calculate how many original palindromes are centered here
        count_at_center = (P[i] + 1) // 2
        total_count += count_at_center
        
        # To list them, step down from the max radius at this center
        # decreasing by 2 to jump over the '#' characters
        for radius in range(P[i], 0, -2):
            start = (i - radius) // 2
            substring = s[start : start + radius]
            all_palindromes.append(substring)

    return total_count, all_palindromes


if __name__ == "__main__":
    # sample = "abbccddeeffaax"
    # #print("First unique Char ", first_unique_char(sample))
    # display_message(sample, countCharacters(sample))
    
    """"
    sample = process_records([
        {'user_id': 'user1', 'prompt_tokens': 10, 'latency': 100, 'status': 'success'},
        {'user_id': 'user1', 'prompt_tokens': 20, 'latency': 200, 'status': 'success'},
        {'user_id': 'user2', 'prompt_tokens': 15, 'latency': 150, 'status': 'success'},
        {'user_id': 'user2', 'prompt_tokens': 25, 'latency': 250, 'status': 'success'}
    ])
    print(sample)
    
    text_sample = "this is a test this is only a test"
    text_sample2 = "The tax the Tax code, the CODE"
    top_words2 = top_n_word_frequency(text_sample2, 3)
    print(top_words2)
    top_words = top_n_word_frequency(text_sample, 3)
    print(top_words)
    
    # Example usage of deduplicate_preserve_order
    sample_list = [23, 17, 2001, 1, 2, 2, 3, 4, 4, 5, 2001]
    deduplicated_list = deduplicate_preserve_order(sample_list)
    print(deduplicated_list)
    
    print("Order does not matter", list(set(sample_list)))
    
    # Example usage of group_records_by_key, group blog posts by tags
    blog_posts = [
        {'title': 'Post 1', 'tags': 'python'},
        {'title': 'Post 2', 'tags': 'python'},
        {'title': 'Post 3', 'tags': 'ai'},
        {'title': 'Post 4', 'tags': 'ai'},
        {'title': 'Post 5', 'tags': 'ml'}
    ]
    grouped_posts = group_records_by_key(blog_posts, 'tags')
    print(grouped_posts)
    
    #Flatten a nested dictionary
    nested_dict = {
        'a': 1,
        'b': {
            'c': 2,
            'd': {
                'e': 3
            }
        }
    }
    flattened = flatten_dict(nested_dict)
    print(flattened)
    
    sample3 = flatten_dict({"db": {"host": "x", "port": 5432}, "debug": True})  
    print(sample3)
    
    for v1, v2 in [("1.2.10", "1.2.9"), ("2.0.0", "1.9.9")]:
        compare_result = compare_semver(v1, v2)
        print(f"Comparing {v1} and {v2}: {compare_result}")
    compare_result = compare_semver("1.2.0", "1.2")
    print(compare_result)
    
    #second dict overrides the first one where keys overlap
    merged_config = deep_merge_dicts(
        {"db": {"host": "prod", "port": 5432}, "debug": False},
        {"db": {"host": "local"}, "debug": True},
    )
    print(merged_config)

"""  # End of multi-line comment for sample usage of various functions
    """
    # Example usage of sum_column with retry decorator
    # Assuming 'SamplePythonInputCSV.csv' has a column named 'CRM'
    # Wrong file name to test retry mechanism. Wrong column to test error handling
    total_CRM = sum_column('/Users/TheNandis/Downloads/SamplePythonInputCSV1.csv', 'CRM1')
    print(f"Total CRM: {total_CRM}")
    
    # Example usage of concurrent_fetch with a limit
    urls = [
        "https://example.com",
        "https://example.org",
        "https://example.net"
    ]
    results = asyncio.run(concurrent_fetch(urls, limit=2))
    for url, content in zip(urls, results):
        print(f"Content from {url}: {content[:100]}...")  # Print first 100 characters
       
    """ 
    x = longest_palindrome_simple("racecarxyz")
    print(f"Longest palindromic substring (simple): {x}")
    
    y = expand_around_center("racecarxyx", 3, 3)    
    print(f"Result of expand_around_center: {y}")
    
    allP = findAllPalindromes("racecarxyx")
    print(f"Total palindromic substrings: {allP[0]}")
    print(f"All palindromic substrings: {allP[1]}") 
    
    
