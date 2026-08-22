from collections import Counter
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

if __name__ == "__main__":
    # sample = "abbccddeeffaax"
    # #print("First unique Char ", first_unique_char(sample))
    # display_message(sample, countCharacters(sample))
    
    process_records([
        {'user_id': 'user1', 'prompt_tokens': 10, 'latency': 100, 'status': 'success'},
        {'user_id': 'user1', 'prompt_tokens': 20, 'latency': 200, 'status': 'success'},
        {'user_id': 'user2', 'prompt_tokens': 15, 'latency': 150, 'status': 'success'},
        {'user_id': 'user2', 'prompt_tokens': 25, 'latency': 250, 'status': 'success'}
    ])
 
