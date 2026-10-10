import sys
from typing import List


def find_second_highest(numbers: List[int]) -> int:
	distinct = sorted(set(numbers), reverse=True)
	if len(distinct) < 2:
		raise ValueError("Need at least two distinct numbers")
	return distinct[1]


def find_shortest_subarray_with_same_degree(numbers: List[int]) -> int:
	frequencies = {}
	first_indices = {}
	degree = 0
	shortest_length = len(numbers)

	for index, number in enumerate(numbers):
		if number not in frequencies:
			frequencies[number] = 0
			first_indices[number] = index

		frequencies[number] += 1
		length = index - first_indices[number] + 1

		if frequencies[number] > degree:
			degree = frequencies[number]
			shortest_length = length
		elif frequencies[number] == degree:
			shortest_length = min(shortest_length, length)

	return shortest_length


# def main() -> None:
# 	data = sys.stdin.read().strip()

# 	if data:
# 		lines = [line for line in data.splitlines() if line.strip()]
# 		if not lines:
# 			return

# 		element_count = int(lines[0].strip())
# 		nums = []
# 		if len(lines) > 1:
# 			nums = [int(x) for x in lines[1].split()]
# 		nums = nums[:element_count]
# 		print(find_shortest_subarray_with_same_degree(nums))
# 		return

# 	numbers = [10, 5, 20, 15, 30, 25]
# 	second_highest = find_second_highest(numbers)
# 	print(f"Second highest number: {second_highest}")

# 	# Example input: 5 followed by 1 2 2 3 1. Prints 2.
# 	smallest_length = find_shortest_subarray_with_same_degree([1, 2, 2, 3, 1])
# 	print(smallest_length)

"""
find first missing positive integer
If the list is [3, 4, -1, 1], the output should be 2
[-3,-1,1,3,4]

"""

def findMissingPos(lst: List[int]) -> int:
  if (lst is None):
    return 0
  #dict d1 = new Dict(int,int);

  sortedList = sorted(lst)

  for i in range(len(sortedList) - 1):
    if sortedList[i] < 0 and sortedList[i+1] < 0 and sortedList[i+1] - sortedList[i] > 1:
		return sortedList[i] + 1
    # if sortedList[i+1] > 0 and sortedList[i] > 0 and sortedList[i+1] - sortedList[i] > 1:
    #   return sortedList[i] + 1
    # elif sortedList[i+1] > 0 and sortedList[i] < 0 and sortedList[i+1] - sortedList[i] > 1:
    #   return sortedList[i] + 1
    # elif sortedList[i+1] < 0 and sortedList[i] > 0 and sortedList[i+1] - sortedList[i] > -1:
    #   return sortedList[i] + 1
    # else:
    #   if sortedList[i+1] < 0 and sortedList[i] < 0 and sortedList[i+1] - sortedList[i] > -1:
    #     return sortedList[i] + 1
  
  return 0


if __name__ == "__main__":
	# main()
	# Example usage of findMissingPos
	example_list = [3, 4, -1, 1]
	print(f"First missing positive integer: {findMissingPos(example_list)}")
