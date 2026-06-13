namespace LeetCode.Problems;

// https://leetcode.com/problems/two-sum/
public class TwoSum
{
  public int[] Solve(int[] nums, int target)
  {
    Dictionary<int, int> numToIndex = [];

    for (int i = 0; i < nums.Length; i++)
    {
      int currentValue = nums[i];
      int complement = target - currentValue;

      if (numToIndex.TryGetValue(complement, out int prevIndex))
      {
        return [prevIndex, i];
      }
      numToIndex[currentValue] = i;
    }

    return [];
  }
}
