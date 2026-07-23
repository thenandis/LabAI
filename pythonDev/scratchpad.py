from collections import Counter
from sqlalchemy import text

def display_message(text: str, counter: Counter):
    print(f"Char count for {text}")
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

if __name__ == "__main__":
    sample = "abbccddeeffaax"
    #print("First unique Char ", first_unique_char(sample))
    display_message(sample, countCharacters(sample))
 
