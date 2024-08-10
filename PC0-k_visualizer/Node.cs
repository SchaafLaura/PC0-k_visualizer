internal class Node
{
    public int Path { get; private set; }
    public int PathIndex { get; private set; }
    public int Variable { get; private set; }
    public bool IsPathEnd { get; private set; }
    public int DomainIndex { get; set; } = -1;
    public Node(int Path, int PathIndex, int Variable, bool IsPathEnd)
    {
        this.Path       = Path;
        this.PathIndex  = PathIndex;
        this.Variable   = Variable;
        this.IsPathEnd  = IsPathEnd;
    }
}