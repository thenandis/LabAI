// you can also use imports, for example:
// import java.util.*;

// you can write to stdout for debugging purposes, e.g.
// System.out.println("this is a debug message");

class Solution {
    public int solution(int[] A) {
        // Implement your solution here
        int smallest = 1;
        int secSmallest = 1;

        //Sort A
        Arrays.sort(A);
        int l = A.length;

        for (int i = 0; i < l; i++) {
            if (A[i] > 0 && A[i] == smallest) {
                smallest++; //increment to next smallest
            }
        }

        return smallest;
    }

    public void main(){
        int[] arr1 = {1, 3, 6, 4, 1, 2};
        int x = new Solution().solution(arr1);
        System.out.println("Smallest is - " + x);
    }
}
