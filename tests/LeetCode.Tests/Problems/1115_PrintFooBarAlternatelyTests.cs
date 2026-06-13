using System.Text;
using LeetCode.Problems;

namespace LeetCode.Tests.Problems;

public class PrintFooBarAlternatelyTests
{
  [Theory]
  [InlineData(1, "foobar")]
  [InlineData(2, "foobarfoobar")]
  public async Task Solve_PrintsFooBarAlternately(int n, string expected)
  {
    using PrintFooBarAlternately sut = new(n);
    StringBuilder output = new();

    Task t1 = Task.Run(() => sut.Foo(() => output.Append("foo")));
    Task t2 = Task.Run(() => sut.Bar(() => output.Append("bar")));

    await Task.WhenAll(t1, t2);

    Assert.Equal(expected, output.ToString());
  }
}
