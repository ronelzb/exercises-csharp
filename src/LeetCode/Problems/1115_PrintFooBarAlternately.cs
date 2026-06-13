namespace LeetCode.Problems;

// https://leetcode.com/problems/print-foobar-alternately/
public class PrintFooBarAlternately(int n) : IDisposable
{
  private readonly SemaphoreSlim fooReady = new(0, 1);
  private readonly SemaphoreSlim barReady = new(1, 1);

  public void Foo(Action printFoo)
  {
    for (int i = 0; i < n; i++)
    {
      try
      {
        barReady.Wait();
        printFoo();
      }
      finally
      {
        fooReady.Release();
      }
    }
  }
  public void Bar(Action printBar)
  {
    for (int i = 0; i < n; i++)
    {
      try
      {
        fooReady.Wait();
        printBar();
      }
      finally
      {
        barReady.Release();
      }
    }
  }

  public void Dispose()
  {
    fooReady.Dispose();
    barReady.Dispose();
    GC.SuppressFinalize(this);
  }
}
