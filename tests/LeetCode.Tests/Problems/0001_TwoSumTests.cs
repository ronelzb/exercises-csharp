using LeetCode.Problems;

namespace LeetCode.Tests.Problems;

public class TwoSumTests
{
  private readonly TwoSum sut = new();

  [Theory]
  [InlineData(new[] { 2, 7, 11, 15 }, 9, new[] { 0, 1 })]
  [InlineData(new[] { 3, 2, 4 }, 6, new[] { 1, 2 })]
  [InlineData(new[] { 3, 3 }, 6, new[] { 0, 1 })]
  public void Solve_ReturnsCorrectIndices(int[] nums, int target, int[] expected)
  {
    int[] result = sut.Solve(nums, target);

    Assert.Equal(expected, result);
  }
}
