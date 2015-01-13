namespace PostSharpWeaving.Target
{
    [PluggableClass]
    public class Target
    {
        public int Method(int p1)
        {
            return p1 * 2;
        }
    }
}